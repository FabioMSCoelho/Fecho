using Fecho.Core.Contratos;
using Fecho.Core.Entidades;
using Fecho.Core.Enums;

namespace Fecho.Core.Servicos;

public sealed class ObjetoService(ICrudObjetos repositorio, ICrudUsuarios usuarios, ICrudCategorias categorias)
{
    public Objeto Cadastrar(int usuarioId, int categoriaId, string nome, EstadoConservacao estado,
        string? descricao = null, decimal? valorEstimado = null, string? imagem = null)
    {
        var usuario = usuarios.Buscar(ValidarId(usuarioId))
            ?? throw new ArgumentException("Usuário não encontrado.");
        if (!usuario.Ativo) throw new InvalidOperationException("Usuário inativo não pode cadastrar objetos.");
        return repositorio.Inserir(new Objeto(1, usuario, ExigirCategoria(categoriaId), nome,
            estado, descricao, valorEstimado, imagem));
    }

    public Objeto? Buscar(int id) => repositorio.Buscar(ValidarId(id));
    public IReadOnlyList<Objeto> Listar() => repositorio.Listar();
    public bool Excluir(int id) => repositorio.Excluir(ValidarId(id));

    public bool Atualizar(int id, int categoriaId, string nome, EstadoConservacao estado,
        string? descricao = null, decimal? valorEstimado = null, string? imagem = null)
    {
        var atual = Buscar(id);
        if (atual is null) return false;
        var dados = new Objeto(id, atual.Proprietario, ExigirCategoria(categoriaId), nome,
            estado, descricao, valorEstimado, imagem, atual.DataCadastro, atual.Status);
        return repositorio.Atualizar(dados);
    }

    private Categoria ExigirCategoria(int id) => categorias.Buscar(ValidarId(id))
        ?? throw new ArgumentException("Categoria não encontrada.");

    private static int ValidarId(int id) => id > 0
        ? id : throw new ArgumentOutOfRangeException(nameof(id), "Id deve ser positivo.");
}
