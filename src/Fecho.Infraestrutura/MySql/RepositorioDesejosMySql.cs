using Fecho.Core.Contratos;
using Fecho.Core.Entidades;
using MySqlConnector;

namespace Fecho.Infraestrutura.MySql;

public sealed class RepositorioDesejosMySql(ConexaoMySql conexoes, ICrudUsuarios usuarios)
    : RepositorioMySql(conexoes, "desejo"), IRepositorioDesejos
{
    public IReadOnlyList<Desejo> Listar(int usuarioId)
    {
        var usuario = usuarios.Buscar(usuarioId) ?? throw new KeyNotFoundException("Usuário não encontrado.");
        return Executar<IReadOnlyList<Desejo>>(db =>
        {
            using var cmd = new MySqlCommand("""
                SELECT d.id, d.descricao, c.id, c.nome, c.descricao FROM desejos d
                LEFT JOIN categorias c ON c.id = d.categoria_id WHERE d.usuario_id = @usuario ORDER BY d.id DESC
                """, db);
            cmd.Parameters.AddWithValue("@usuario", usuarioId);
            using var leitor = cmd.ExecuteReader();
            var lista = new List<Desejo>();
            while (leitor.Read()) lista.Add(new Desejo(leitor.GetInt32(0), usuario, leitor.GetString(1),
                leitor.IsDBNull(2) ? null : new Categoria(leitor.GetInt32(2), leitor.GetString(3),
                    leitor.IsDBNull(4) ? null : leitor.GetString(4))));
            return lista.AsReadOnly();
        });
    }

    public int Adicionar(Desejo desejo) => Executar(db =>
    {
        using var cmd = new MySqlCommand("INSERT INTO desejos(usuario_id, categoria_id, descricao) VALUES(@usuario,@categoria,@descricao)", db);
        cmd.Parameters.AddWithValue("@usuario", desejo.Usuario.Id);
        cmd.Parameters.AddWithValue("@categoria", (object?)desejo.Categoria?.Id ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@descricao", desejo.Descricao);
        cmd.ExecuteNonQuery(); return checked((int)cmd.LastInsertedId);
    });

    public bool Excluir(int id, int usuarioId) => Executar(db =>
    {
        using var cmd = new MySqlCommand("DELETE FROM desejos WHERE id = @id AND usuario_id = @usuario", db);
        cmd.Parameters.AddWithValue("@id", id); cmd.Parameters.AddWithValue("@usuario", usuarioId);
        return cmd.ExecuteNonQuery() > 0;
    });
}
