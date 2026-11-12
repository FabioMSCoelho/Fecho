using Fecho.Core.Contratos;
using Fecho.Core.Entidades;

namespace Fecho.Core.Servicos;

public sealed class DesejoService(IRepositorioDesejos desejos, ICrudUsuarios usuarios, ICrudCategorias categorias)
{
    public IReadOnlyList<Desejo> Listar(int usuarioId) => desejos.Listar(usuarioId);
    public int Adicionar(int usuarioId, string descricao, int? categoriaId)
    {
        var usuario = usuarios.Buscar(usuarioId) ?? throw new KeyNotFoundException("Usuário não encontrado.");
        if (!usuario.Ativo) throw new InvalidOperationException("Usuário inativo.");
        var categoria = categoriaId.HasValue
            ? categorias.Buscar(categoriaId.Value) ?? throw new ArgumentException("Categoria não encontrada.") : null;
        return desejos.Adicionar(new Desejo(1, usuario, descricao, categoria));
    }
    public bool Excluir(int id, int usuarioId) => desejos.Excluir(id, usuarioId);
}
