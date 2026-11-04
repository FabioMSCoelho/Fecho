using Fecho.Core.Contratos;
using Fecho.Core.Entidades;
using MySqlConnector;

namespace Fecho.Infraestrutura.MySql;

public sealed class RepositorioCategoriasMySql(ConexaoMySql conexoes) : ICrudCategorias
{
    public Categoria Inserir(string nome, string? descricao)
    {
        var dados = new Categoria(1, nome, descricao);
        return Executar(conexao =>
        {
            using var cmd = new MySqlCommand("INSERT INTO categorias (nome, descricao) VALUES (@nome, @descricao)", conexao);
            Preencher(cmd, dados);
            cmd.ExecuteNonQuery();
            return new Categoria(checked((int)cmd.LastInsertedId), dados.Nome, dados.Descricao);
        });
    }

    public Categoria? Buscar(int id) => Executar<Categoria?>(conexao =>
    {
        using var cmd = new MySqlCommand("SELECT id, nome, descricao FROM categorias WHERE id = @id", conexao);
        cmd.Parameters.AddWithValue("@id", id);
        using var leitor = cmd.ExecuteReader();
        return leitor.Read() ? Ler(leitor) : null;
    });

    public IReadOnlyList<Categoria> Listar() => Executar<IReadOnlyList<Categoria>>(conexao =>
    {
        using var cmd = new MySqlCommand("SELECT id, nome, descricao FROM categorias ORDER BY id", conexao);
        using var leitor = cmd.ExecuteReader();
        var categorias = new List<Categoria>();
        while (leitor.Read()) categorias.Add(Ler(leitor));
        return categorias.AsReadOnly();
    });

    public bool Atualizar(int id, string nome, string? descricao)
    {
        var dados = new Categoria(id, nome, descricao);
        return Executar(conexao =>
        {
            using var cmd = new MySqlCommand("UPDATE categorias SET nome = @nome, descricao = @descricao WHERE id = @id", conexao);
            Preencher(cmd, dados);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        });
    }

    public bool Excluir(int id) => Executar(conexao =>
    {
        using var cmd = new MySqlCommand("DELETE FROM categorias WHERE id = @id", conexao);
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    });

    private T Executar<T>(Func<MySqlConnection, T> operacao)
    {
        using var conexao = conexoes.Abrir();
        try { return operacao(conexao); }
        catch (MySqlException ex) { throw ConexaoMySql.Traduzir(ex); }
    }

    private static void Preencher(MySqlCommand cmd, Categoria dados)
    {
        cmd.Parameters.AddWithValue("@nome", dados.Nome);
        cmd.Parameters.AddWithValue("@descricao", (object?)dados.Descricao ?? DBNull.Value);
    }

    private static Categoria Ler(MySqlDataReader leitor) => new(leitor.GetInt32(0), leitor.GetString(1),
        leitor.IsDBNull(2) ? null : leitor.GetString(2));
}
