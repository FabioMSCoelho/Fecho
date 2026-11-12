using MySqlConnector;

namespace Fecho.Infraestrutura.MySql;

/// <summary>Compartilha abertura, descarte e tratamento de falhas dos repositórios SQL.</summary>
public abstract class RepositorioMySql(ConexaoMySql conexoes, string recurso)
{
    protected T Executar<T>(Func<MySqlConnection, T> operacao)
    {
        using var conexao = conexoes.Abrir();
        try { return operacao(conexao); }
        catch (MySqlException ex) { throw ConexaoMySql.Traduzir(ex, recurso); }
    }
}
