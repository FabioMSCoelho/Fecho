using System.Globalization;
using System.Text;

namespace Fecho.Core;

public static class TextoNormalizado
{
    public static string De(string texto)
    {
        var resultado = new StringBuilder();
        foreach (char c in texto.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                resultado.Append(char.ToLowerInvariant(c));
        return resultado.ToString().Normalize(NormalizationForm.FormC);
    }
}
