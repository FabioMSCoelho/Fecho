using Fecho.Core.Contratos;
using Fecho.Core.Entidades;
using Fecho.Core.Enums;
using MySqlConnector;

namespace Fecho.Infraestrutura.MySql;

public sealed class RepositorioObjetosMySql(ConexaoMySql conexoes)
    : RepositorioMySql(conexoes, "objeto"), ICrudObjetos
{
    internal const string Consulta = """
        SELECT o.id, o.nome, o.descricao, o.estado_conservacao, o.valor_estimado, o.imagem, o.data_cadastro, o.status,
               c.id, c.nome, c.descricao,
               u.id, u.nome, u.email, u.senha, u.telefone, u.cidade, u.foto, u.descricao, u.data_cadastro, u.ativo
        FROM objetos o JOIN categorias c ON c.id = o.categoria_id JOIN usuarios u ON u.id = o.usuario_id
        """;

    public Objeto Inserir(Objeto dados) => Executar(conexao =>
    {
        using var cmd = new MySqlCommand("""
            INSERT INTO objetos (usuario_id, categoria_id, nome, descricao, estado_conservacao,
                valor_estimado, imagem, status, data_cadastro)
            VALUES (@usuario, @categoria, @nome, @descricao, @estado, @valor, @imagem, 'DISPONIVEL', UTC_TIMESTAMP())
            """, conexao);
        Preencher(cmd, dados);
        cmd.Parameters.AddWithValue("@usuario", dados.Proprietario.Id);
        cmd.ExecuteNonQuery();
        return Buscar(conexao, checked((int)cmd.LastInsertedId))
            ?? throw new ErroPersistencia("O objeto cadastrado foi removido por outra operação. Consulte novamente.");
    });

    public Objeto? Buscar(int id) => Executar(conexao => Buscar(conexao, id));

    public IReadOnlyList<Objeto> Listar() => Executar<IReadOnlyList<Objeto>>(conexao =>
    {
        using var cmd = new MySqlCommand(Consulta + " ORDER BY o.id", conexao);
        using var leitor = cmd.ExecuteReader();
        var objetos = new List<Objeto>();
        while (leitor.Read()) objetos.Add(Ler(leitor));
        return objetos.AsReadOnly();
    });

    public bool Atualizar(Objeto dados) => Executar(conexao =>
    {
        using var cmd = new MySqlCommand("""
            UPDATE objetos SET categoria_id = @categoria, nome = @nome, descricao = @descricao,
                estado_conservacao = @estado, valor_estimado = @valor, imagem = @imagem WHERE id = @id
            """, conexao);
        Preencher(cmd, dados);
        cmd.Parameters.AddWithValue("@id", dados.Id);
        return cmd.ExecuteNonQuery() > 0;
    });

    public bool Excluir(int id) => Executar(conexao =>
    {
        using var cmd = new MySqlCommand("DELETE FROM objetos WHERE id = @id", conexao);
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    });

    private static Objeto? Buscar(MySqlConnection conexao, int id)
    {
        using var cmd = new MySqlCommand(Consulta + " WHERE o.id = @id", conexao);
        cmd.Parameters.AddWithValue("@id", id);
        using var leitor = cmd.ExecuteReader();
        return leitor.Read() ? Ler(leitor) : null;
    }

    private static void Preencher(MySqlCommand cmd, Objeto dados)
    {
        cmd.Parameters.AddWithValue("@categoria", dados.Categoria.Id);
        cmd.Parameters.AddWithValue("@nome", dados.Nome);
        cmd.Parameters.AddWithValue("@descricao", (object?)dados.Descricao ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@estado", dados.EstadoConservacao.ToString());
        cmd.Parameters.AddWithValue("@valor", (object?)dados.ValorEstimado ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@imagem", (object?)dados.Imagem ?? DBNull.Value);
    }

    internal static Objeto Ler(MySqlDataReader leitor)
    {
        if (leitor.IsDBNull(6) || leitor.IsDBNull(7)
            || !Enum.TryParse<EstadoConservacao>(leitor.GetString(3), out var estado) || !Enum.IsDefined(estado)
            || !Enum.TryParse<StatusObjeto>(leitor.GetString(7), out var status) || !Enum.IsDefined(status))
            throw new ErroPersistencia("Objeto com data, estado de conservação ou status inválido no banco.");
        string? Opcional(int coluna) => leitor.IsDBNull(coluna) ? null : leitor.GetString(coluna);
        return new Objeto(leitor.GetInt32(0), RepositorioUsuariosMySql.Ler(leitor, 11),
            new Categoria(leitor.GetInt32(8), leitor.GetString(9), Opcional(10)), leitor.GetString(1), estado,
            Opcional(2), leitor.IsDBNull(4) ? null : leitor.GetDecimal(4), Opcional(5),
            DateTime.SpecifyKind(leitor.GetDateTime(6), DateTimeKind.Utc), status);
    }
}
