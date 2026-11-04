using MySqlConnector;

namespace Fecho.Infraestrutura.MySql;

public sealed class ConexaoMySql
{
    private readonly string _connectionString;

    public ConexaoMySql(string connectionString)
    {
        try
        {
            var opcoes = new MySqlConnectionStringBuilder(connectionString);
            // UPDATE de valores idênticos ainda encontra o registro.
            opcoes.UseAffectedRows = false;
            _connectionString = opcoes.ConnectionString;
        }
        catch (ArgumentException)
        {
            throw new ErroPersistencia("Configuração de conexão MySQL inválida.");
        }
    }

    public static ConexaoMySql DoAmbiente()
    {
        var senha = Environment.GetEnvironmentVariable("FECHO_DB_PASSWORD");
        if (string.IsNullOrEmpty(senha))
            throw new ErroPersistencia("Defina FECHO_DB_PASSWORD no ambiente antes de iniciar o CRUD.");
        if (!uint.TryParse(Environment.GetEnvironmentVariable("FECHO_DB_PORT") ?? "3306", out var porta)
            || porta is 0 or > 65535)
            throw new ErroPersistencia("FECHO_DB_PORT deve ser uma porta entre 1 e 65535.");
        var opcoes = new MySqlConnectionStringBuilder
        {
            Server = Environment.GetEnvironmentVariable("FECHO_DB_HOST") ?? "127.0.0.1",
            Port = porta,
            Database = Environment.GetEnvironmentVariable("FECHO_DB_NAME") ?? "fecho",
            UserID = Environment.GetEnvironmentVariable("FECHO_DB_USER") ?? "fecho",
            Password = senha,
            ConnectionTimeout = 5,
            DefaultCommandTimeout = 10,
            SslMode = MySqlSslMode.Preferred
        };
        return new ConexaoMySql(opcoes.ConnectionString);
    }

    public MySqlConnection Abrir()
    {
        var conexao = new MySqlConnection(_connectionString);
        try { conexao.Open(); return conexao; }
        catch (MySqlException ex) { conexao.Dispose(); throw Traduzir(ex); }
    }

    public void Verificar()
    {
        using var conexao = Abrir();
        try
        {
            using var comando = new MySqlCommand("SELECT id, nome, descricao FROM categorias LIMIT 0", conexao);
            using var leitor = comando.ExecuteReader();
        }
        catch (MySqlException ex) { throw Traduzir(ex); }
    }

    internal static ErroPersistencia Traduzir(MySqlException erro) => new(erro.Number switch
    {
        1045 => "Acesso negado ao MySQL. Confira usuário e senha.",
        1044 => "Usuário sem permissão para acessar o banco Fecho.",
        1049 => "Banco não encontrado. Importe database/fecho.sql antes de iniciar.",
        1146 => "Tabela não encontrada. Confira a importação de database/fecho.sql.",
        1062 => "Já existe uma categoria com esse nome.",
        1451 => "Categoria vinculada a objetos ou desejos: remova os vínculos antes de excluir.",
        _ => "Não foi possível concluir a operação no MySQL. Confira servidor, porta e permissões e tente novamente."
    });
}
