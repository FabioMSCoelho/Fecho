using Fecho.Core.Entidades;

namespace Fecho.Infraestrutura.Memoria;

public sealed class RepositorioTrocas : RepositorioMemoria<Troca>
{
    protected override void ValidarNovo(Troca entidade)
    {
        if (Listar().Any(t => t.Match.Equals(entidade.Match)))
            throw new InvalidOperationException("Este match já tem uma troca registrada.");
    }
}
