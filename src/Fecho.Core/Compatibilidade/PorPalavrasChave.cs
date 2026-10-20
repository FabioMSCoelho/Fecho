using Fecho.Core.Entidades;

namespace Fecho.Core.Compatibilidade;

public sealed class PorPalavrasChave : CriterioCompatibilidade
{
    public override string Nome => "Por palavras-chave e categoria opcional";

    public override bool Compativel(Objeto objeto, Desejo desejo)
    {
        if (!RespeitaCategoria(objeto, desejo)) return false;
        string texto = TextoNormalizado.De($"{objeto.Nome} {objeto.Descricao}");
        string[] palavras = TextoNormalizado.De(desejo.Descricao)
            .Split([' ', ',', '.', ';', ':', '-', '/', '!', '?'], StringSplitOptions.RemoveEmptyEntries);
        return palavras.Length > 0 && palavras.All(p => texto.Contains(p, StringComparison.Ordinal));
    }
}
