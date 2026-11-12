using Fecho.Core.Contratos;
using Fecho.Core.Entidades;
using Fecho.Core.Seguranca;
using MySqlConnector;

namespace Fecho.Infraestrutura.MySql;

public sealed class RepositorioUsuariosMySql(ConexaoMySql conexoes)
    : RepositorioMySql(conexoes, "usuário"), ICrudUsuarios
{
    internal const string Consulta = """
        SELECT id, nome, email, senha, telefone, cidade, foto, descricao, data_cadastro, ativo
        FROM usuarios
        """;

    public Usuario Inserir(string nome, string email, SenhaProtegida senha, string telefone,
        string? cidade, string? foto, string? descricao)
    {
        var dados = new Usuario(1, nome, email, senha, telefone, cidade, foto, descricao);
        return Executar(conexao =>
        {
            using var cmd = new MySqlCommand("""
                INSERT INTO usuarios (nome, email, senha, telefone, cidade, foto, descricao, ativo, data_cadastro)
                VALUES (@nome, @email, @senha, @telefone, @cidade, @foto, @descricao, @ativo, UTC_TIMESTAMP())
                """, conexao);
            Preencher(cmd, dados.Nome, dados.Email, telefone.Trim(), dados.Cidade, dados.Foto, dados.Descricao, true);
            cmd.Parameters.AddWithValue("@senha", senha.ParaPersistencia());
            cmd.ExecuteNonQuery();
            return Buscar(conexao, checked((int)cmd.LastInsertedId))
                ?? throw new ErroPersistencia("O usuário cadastrado foi removido por outra operação. Consulte novamente.");
        });
    }

    public Usuario? Buscar(int id) => Executar(conexao => Buscar(conexao, id));

    public Usuario? BuscarPorEmail(string email) => Executar(conexao =>
    {
        using var cmd = new MySqlCommand(Consulta + " WHERE email = @email", conexao);
        cmd.Parameters.AddWithValue("@email", email);
        using var leitor = cmd.ExecuteReader();
        return leitor.Read() ? Ler(leitor) : null;
    });

    public IReadOnlyList<Usuario> Listar() => Executar<IReadOnlyList<Usuario>>(conexao =>
    {
        using var cmd = new MySqlCommand(Consulta + " ORDER BY id", conexao);
        using var leitor = cmd.ExecuteReader();
        var usuarios = new List<Usuario>();
        while (leitor.Read()) usuarios.Add(Ler(leitor));
        return usuarios.AsReadOnly();
    });

    public bool Atualizar(int id, string nome, string email, string telefone,
        string? cidade, string? foto, string? descricao, bool ativo) => Executar(conexao =>
    {
        // A edição do perfil nunca sobrescreve o hash nem a data original.
        using var cmd = new MySqlCommand("""
            UPDATE usuarios SET nome = @nome, email = @email, telefone = @telefone,
                cidade = @cidade, foto = @foto, descricao = @descricao, ativo = @ativo WHERE id = @id
            """, conexao);
        Preencher(cmd, nome, email, telefone, cidade, foto, descricao, ativo);
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    });

    public bool AlterarSenha(int id, SenhaProtegida senha) => Executar(conexao =>
    {
        using var cmd = new MySqlCommand("UPDATE usuarios SET senha = @senha WHERE id = @id", conexao);
        cmd.Parameters.AddWithValue("@senha", senha.ParaPersistencia());
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    });

    public bool Excluir(int id) => Executar(conexao =>
    {
        using var cmd = new MySqlCommand("DELETE FROM usuarios WHERE id = @id", conexao);
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    });

    private static Usuario? Buscar(MySqlConnection conexao, int id)
    {
        using var cmd = new MySqlCommand(Consulta + " WHERE id = @id", conexao);
        cmd.Parameters.AddWithValue("@id", id);
        using var leitor = cmd.ExecuteReader();
        return leitor.Read() ? Ler(leitor) : null;
    }

    private static void Preencher(MySqlCommand cmd, string nome, string email, string telefone,
        string? cidade, string? foto, string? descricao, bool ativo)
    {
        cmd.Parameters.AddWithValue("@nome", nome);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@telefone", telefone);
        cmd.Parameters.AddWithValue("@cidade", (object?)cidade ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@foto", (object?)foto ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@descricao", (object?)descricao ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ativo", ativo);
    }

    // Também usado pelo JOIN dos objetos, sem consultas adicionais para cada proprietário.
    internal static Usuario Ler(MySqlDataReader leitor, int inicio = 0)
    {
        SenhaProtegida senha;
        try { senha = SenhaProtegida.DaPersistencia(leitor.GetString(inicio + 3)); }
        catch (ArgumentException)
        {
            throw new ErroPersistencia("Usuário com hash de senha incompatível. Redefina a senha pelo ID na opção Alterar senha.");
        }
        if (leitor.IsDBNull(inicio + 8) || leitor.IsDBNull(inicio + 9))
            throw new ErroPersistencia("Usuário com data de cadastro ou situação nula no banco. Corrija esses campos no cadastro existente.");
        string? Opcional(int coluna) => leitor.IsDBNull(inicio + coluna) ? null : leitor.GetString(inicio + coluna);
        return new Usuario(leitor.GetInt32(inicio), leitor.GetString(inicio + 1), leitor.GetString(inicio + 2),
            senha, leitor.GetString(inicio + 4), Opcional(5), Opcional(6), Opcional(7),
            DateTime.SpecifyKind(leitor.GetDateTime(inicio + 8), DateTimeKind.Utc), leitor.GetBoolean(inicio + 9));
    }
}
