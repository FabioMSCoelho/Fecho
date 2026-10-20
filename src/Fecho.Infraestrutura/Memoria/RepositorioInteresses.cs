using Fecho.Core.Entidades;

namespace Fecho.Infraestrutura.Memoria;

public sealed class RepositorioInteresses : RepositorioMemoria<Interesse>
{
    protected override void ValidarNovo(Interesse entidade)
    {
        if (Listar().Any(i => i.Usuario.Equals(entidade.Usuario) && i.Objeto.Equals(entidade.Objeto)))
            throw new InvalidOperationException("Já existe uma avaliação para este usuário e objeto.");
    }
}
