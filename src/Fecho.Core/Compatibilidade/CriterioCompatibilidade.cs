using Fecho.Core.Entidades;

namespace Fecho.Core.Compatibilidade;

/// <summary>Varia a descoberta de cards; jamais substitui os likes necessários ao match.</summary>
public abstract class CriterioCompatibilidade
{
    public abstract string Nome { get; }
    public abstract bool Compativel(Objeto objeto, Desejo desejo);

    protected static bool RespeitaCategoria(Objeto objeto, Desejo desejo) =>
        desejo.Categoria is null || objeto.Categoria.Equals(desejo.Categoria);
}
