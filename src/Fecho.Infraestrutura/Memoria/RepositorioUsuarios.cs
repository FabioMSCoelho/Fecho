using Fecho.Core.Entidades;

namespace Fecho.Infraestrutura.Memoria;

public sealed class RepositorioUsuarios : RepositorioMemoria<Usuario>
{
    protected override void ValidarNovo(Usuario entidade)
    {
        if (Listar().Any(u => u.Email.Equals(entidade.Email, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("E-mail já cadastrado.");
    }
}
