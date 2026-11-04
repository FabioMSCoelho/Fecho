using Fecho.Core.Contratos;
using Fecho.Core.Entidades;

namespace Fecho.Core.Servicos;

public sealed class CategoriaService(ICrudCategorias repositorio)
{
    public Categoria Cadastrar(string nome, string? descricao = null)
    {
        var dados = new Categoria(1, nome, descricao);
        return repositorio.Inserir(dados.Nome, dados.Descricao);
    }

    public IReadOnlyList<Categoria> Listar() => repositorio.Listar();
    public Categoria? Buscar(int id) { ValidarId(id); return repositorio.Buscar(id); }
    public bool Excluir(int id) { ValidarId(id); return repositorio.Excluir(id); }
    public bool Atualizar(int id, string nome, string? descricao = null)
    {
        var dados = new Categoria(id, nome, descricao);
        return repositorio.Atualizar(id, dados.Nome, dados.Descricao);
    }

    private static void ValidarId(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id), "Id deve ser positivo.");
    }
}
