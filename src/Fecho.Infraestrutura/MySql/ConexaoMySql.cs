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
        ConfiguracaoLocal.Carregar(Path.Combine(ConfiguracaoLocal.LocalizarRaiz(), ".env"));
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
            using var comando = new MySqlCommand("""
                SELECT c.id, c.nome, c.descricao, u.id, u.nome, u.email, u.senha, u.telefone,
                       u.cidade, u.foto, u.descricao, u.data_cadastro, u.ativo,
                       o.id, o.usuario_id, o.categoria_id, o.nome, o.descricao,
                       o.estado_conservacao, o.valor_estimado, o.imagem, o.status, o.data_cadastro,
                       d.id, d.usuario_id, d.categoria_id, d.descricao, i.id, i.usuario_id, i.objeto_id, i.tipo,
                       m.id, m.usuario1_id, m.usuario2_id, m.objeto1_id, m.objeto2_id, m.data_match, m.status,
                       t.id, t.match_id, t.data_conclusao, t.observacao
                FROM categorias c LEFT JOIN usuarios u ON 1 = 0 LEFT JOIN objetos o ON 1 = 0
                LEFT JOIN desejos d ON 1 = 0 LEFT JOIN interesses i ON 1 = 0
                LEFT JOIN matches m ON 1 = 0 LEFT JOIN trocas t ON 1 = 0 LIMIT 0
                """, conexao);
            using var leitor = comando.ExecuteReader();
        }
        catch (MySqlException ex) { throw Traduzir(ex); }
    }

    internal static ErroPersistencia Traduzir(MySqlException erro, string recurso = "categoria") => new(erro.Number switch
    {
        1045 => "Acesso negado ao MySQL. Confira usuário e senha.",
        1044 => "Usuário sem permissão para acessar o banco Fecho.",
        1049 => "Banco não encontrado. Importe database/fecho.sql antes de iniciar.",
        1146 => "Tabela não encontrada. Confira a importação de database/fecho.sql.",
        1054 => "Estrutura do banco incompatível. Confira o esquema de database/fecho.sql.",
        1062 => recurso switch
        {
            "usuário" => "Já existe um usuário com esse e-mail.",
            "categoria" => "Já existe uma categoria com esse nome.",
            _ => "Esse registro já existe. Consulte os dados antes de tentar novamente."
        },
        1451 => recurso switch
        {
            "usuário" => "Usuário vinculado a objetos ou matches: remova os vínculos antes de excluir.",
            "objeto" => "Objeto vinculado a matches: remova os vínculos antes de excluir.",
            _ => "Categoria vinculada a objetos ou desejos: remova os vínculos antes de excluir."
        },
        1452 => "Usuário ou categoria referenciado não existe. Confira os IDs informados.",
        _ => "Não foi possível concluir a operação no MySQL. Confira servidor, porta e permissões e tente novamente."
    });
}
