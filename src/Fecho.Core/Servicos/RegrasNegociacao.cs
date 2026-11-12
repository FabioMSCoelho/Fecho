using Fecho.Core.Entidades;
using Fecho.Core.Enums;

namespace Fecho.Core.Servicos;

public static class RegrasNegociacao
{
    public static void ValidarAvaliacao(Usuario usuario, Objeto objeto, bool possuiObjetoDisponivel)
    {
        if (!usuario.Ativo || !objeto.Proprietario.Ativo) throw new InvalidOperationException("Usuário inativo.");
        if (objeto.Proprietario.Equals(usuario)) throw new InvalidOperationException("Você não pode avaliar seu próprio objeto.");
        if (objeto.Status != StatusObjeto.DISPONIVEL) throw new InvalidOperationException("Objeto indisponível.");
        if (!possuiObjetoDisponivel) throw new InvalidOperationException("Cadastre pelo menos um objeto disponível para participar.");
    }
}
