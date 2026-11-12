using System.Data;
using Fecho.Core.Contratos;
using Fecho.Core.Entidades;
using Fecho.Core.Enums;
using MySqlConnector;

namespace Fecho.Infraestrutura.MySql;

public sealed class UnidadeNegociacaoMySql(ConexaoMySql conexoes) : IUnidadeNegociacao
{
    public T Ler<T>(Func<IContextoNegociacao, T> operacao) => Executar(operacao, false);
    public T Alterar<T>(Func<IContextoNegociacao, T> operacao) => Executar(operacao, true);

    private T Executar<T>(Func<IContextoNegociacao, T> operacao, bool escrita)
    {
        using var db = conexoes.Abrir();
        using var transacao = db.BeginTransaction(escrita ? IsolationLevel.ReadCommitted : IsolationLevel.RepeatableRead);
        try
        {
            if (escrita)
            {
                // MVP local: serializa negociações pela mesma ordem de usuários em todas as instâncias.
                // Impede dois likes simultâneos de criarem o mesmo par e duas conclusões de entregarem o mesmo objeto.
                using var trava = new MySqlCommand("SELECT id FROM usuarios ORDER BY id FOR UPDATE", db, transacao);
                using var leitor = trava.ExecuteReader();
                while (leitor.Read()) { }
            }
            var resultado = operacao(new Contexto(db, transacao));
            transacao.Commit();
            return resultado;
        }
        catch (MySqlException ex)
        {
            transacao.Rollback();
            if (ex.Number is 1205 or 1213) throw new ErroPersistencia("Outra negociação está em andamento. Tente novamente.");
            throw ConexaoMySql.Traduzir(ex, "negociação");
        }
        // Dispose desfaz também alterações quando a regra de negócio lança uma exceção.
    }

    private sealed class Contexto(MySqlConnection db, MySqlTransaction transacao) : IContextoNegociacao
    {
        private IReadOnlyDictionary<int, Usuario>? _usuarios;
        private IReadOnlyDictionary<int, Objeto>? _objetos;
        private IReadOnlyList<InteresseRegistrado>? _interesses;
        private IReadOnlyList<MatchRegistrado>? _matches;
        public IReadOnlyDictionary<int, Usuario> Usuarios => _usuarios ??= Consultar(RepositorioUsuariosMySql.Consulta,
            l => RepositorioUsuariosMySql.Ler(l)).ToDictionary(u => u.Id);
        public IReadOnlyDictionary<int, Objeto> Objetos => _objetos ??= Consultar(RepositorioObjetosMySql.Consulta,
            RepositorioObjetosMySql.Ler).ToDictionary(o => o.Id);
        public IReadOnlyList<InteresseRegistrado> Interesses => _interesses ??= Consultar(
            "SELECT id, usuario_id, objeto_id, tipo FROM interesses", l => new InteresseRegistrado(l.GetInt32(0),
                l.GetInt32(1), l.GetInt32(2), Enum.Parse<TipoInteresse>(l.GetString(3))));
        public IReadOnlyList<MatchRegistrado> Matches => _matches ??= Consultar("""
            SELECT m.id,m.usuario1_id,m.usuario2_id,m.objeto1_id,m.objeto2_id,m.data_match,m.status,t.data_conclusao,t.observacao
            FROM matches m LEFT JOIN trocas t ON t.match_id=m.id
            """, l => new MatchRegistrado(l.GetInt32(0), l.GetInt32(1), l.GetInt32(2), l.GetInt32(3), l.GetInt32(4),
                DateTime.SpecifyKind(l.GetDateTime(5), DateTimeKind.Utc), Enum.Parse<StatusMatch>(l.GetString(6)),
                l.IsDBNull(7) ? null : DateTime.SpecifyKind(l.GetDateTime(7), DateTimeKind.Utc), l.IsDBNull(8) ? null : l.GetString(8)));

        public void GravarInteresse(int usuarioId, int objetoId, TipoInteresse tipo)
        {
            Comando("""
                INSERT INTO interesses(usuario_id,objeto_id,tipo,data_interesse) VALUES(@usuario,@objeto,@tipo,UTC_TIMESTAMP())
                ON DUPLICATE KEY UPDATE tipo=@tipo,data_interesse=UTC_TIMESTAMP()
                """, ("@usuario", usuarioId), ("@objeto", objetoId), ("@tipo", tipo.ToString()));
            _interesses = null;
        }

        public int CriarMatch(Objeto objeto1, Objeto objeto2)
        {
            var id = Inserir("""
                INSERT INTO matches(usuario1_id,usuario2_id,objeto1_id,objeto2_id,data_match,status)
                VALUES(@u1,@u2,@o1,@o2,UTC_TIMESTAMP(),'ATIVO')
                """, ("@u1", objeto1.Proprietario.Id), ("@u2", objeto2.Proprietario.Id), ("@o1", objeto1.Id), ("@o2", objeto2.Id));
            _matches = null; return id;
        }

        public void AlterarMatch(int id, StatusMatch status)
        {
            if (Comando("UPDATE matches SET status=@status WHERE id=@id AND status='ATIVO'",
                ("@status", status.ToString()), ("@id", id)) != 1) throw new InvalidOperationException("Match não está ativo.");
            _matches = null;
        }

        public void MarcarTrocados(int objeto1Id, int objeto2Id)
        {
            if (Comando("UPDATE objetos SET status='TROCADO' WHERE id IN (@o1,@o2) AND status='DISPONIVEL'",
                ("@o1", objeto1Id), ("@o2", objeto2Id)) != 2) throw new InvalidOperationException("Um dos objetos já está indisponível.");
            _objetos = null;
        }

        public int RegistrarTroca(int matchId, string? observacao) => Inserir(
            "INSERT INTO trocas(match_id,data_conclusao,observacao) VALUES(@match,UTC_TIMESTAMP(),@observacao)",
            ("@match", matchId), ("@observacao", observacao));

        public string ObterTelefone(int usuarioId)
        {
            using var cmd = Preparar("SELECT telefone FROM usuarios WHERE id=@id", ("@id", usuarioId));
            return (string)(cmd.ExecuteScalar() ?? throw new KeyNotFoundException("Usuário não encontrado."));
        }

        private List<T> Consultar<T>(string sql, Func<MySqlDataReader, T> converter)
        {
            using var cmd = Preparar(sql); using var leitor = cmd.ExecuteReader();
            var itens = new List<T>(); while (leitor.Read()) itens.Add(converter(leitor)); return itens;
        }

        private int Comando(string sql, params (string Nome, object? Valor)[] parametros)
        { using var cmd = Preparar(sql, parametros); return cmd.ExecuteNonQuery(); }
        private int Inserir(string sql, params (string Nome, object? Valor)[] parametros)
        { using var cmd = Preparar(sql, parametros); cmd.ExecuteNonQuery(); return checked((int)cmd.LastInsertedId); }
        private MySqlCommand Preparar(string sql, params (string Nome, object? Valor)[] parametros)
        {
            var cmd = new MySqlCommand(sql, db, transacao);
            foreach (var (nome, valor) in parametros) cmd.Parameters.AddWithValue(nome, valor ?? DBNull.Value);
            return cmd;
        }
    }
}
