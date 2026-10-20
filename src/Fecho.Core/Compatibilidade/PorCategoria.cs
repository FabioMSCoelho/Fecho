using Fecho.Core.Entidades;

namespace Fecho.Core.Compatibilidade;

public sealed class PorCategoria : CriterioCompatibilidade
{
    public override string Nome => "Por categoria";

    public override bool Compativel(Objeto objeto, Desejo desejo) =>
        desejo.Categoria is not null && RespeitaCategoria(objeto, desejo);
}
