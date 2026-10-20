using Fecho.Core;
using Fecho.Core.Entidades;

namespace Fecho.Infraestrutura.Memoria;

public sealed class RepositorioCategorias : RepositorioMemoria<Categoria>
{
    protected override void ValidarNovo(Categoria entidade)
    {
        if (Listar().Any(c => TextoNormalizado.De(c.Nome) == TextoNormalizado.De(entidade.Nome)))
            throw new InvalidOperationException("Categoria já cadastrada.");
    }
}
