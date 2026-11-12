using System.Globalization;
using System.Text;

namespace Fecho.ConsoleApp;

internal static class EntradaConsole
{
    internal static string Ler(string mensagem)
    {
        Console.Write(mensagem);
        return Console.ReadLine() ?? throw new EndOfStreamException();
    }

    internal static int LerId(string mensagem = "ID: ") => int.TryParse(Ler(mensagem), out var id) && id > 0
        ? id : throw new ArgumentException("Informe um ID inteiro positivo.");

    internal static string LerSenha(string mensagem)
    {
        if (Console.IsInputRedirected) return Ler(mensagem);
        Console.Write(mensagem);
        var senha = new StringBuilder();
        while (true)
        {
            var tecla = Console.ReadKey(intercept: true);
            if (tecla.Key == ConsoleKey.Enter) { Console.WriteLine(); return senha.ToString(); }
            if (tecla.Key == ConsoleKey.Backspace) { if (senha.Length > 0) senha.Length--; }
            else if (!char.IsControl(tecla.KeyChar)) senha.Append(tecla.KeyChar);
        }
    }

    internal static decimal? LerValor()
    {
        var texto = Ler("Valor estimado (opcional, ex.: 150,50; sem milhar): ").Trim();
        if (texto.Length == 0) return null;
        return decimal.TryParse(texto.Replace(',', '.'), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out var valor)
            ? valor : throw new ArgumentException("Valor estimado inválido.");
    }

    internal static T LerEnum<T>(string mensagem) where T : struct, Enum
    {
        var valores = Enum.GetValues<T>();
        Console.WriteLine(string.Join(" | ", valores.Select((valor, i) => $"{i + 1} {valor}")));
        return int.TryParse(Ler(mensagem), out var opcao) && opcao >= 1 && opcao <= valores.Length
            ? valores[opcao - 1] : throw new ArgumentException("Opção de estado de conservação inválida.");
    }

    internal static bool LerAtivo() => Ler("Usuário ativo? S/N: ").Trim().ToUpperInvariant() switch
    {
        "S" => true,
        "N" => false,
        _ => throw new ArgumentException("Informe S ou N.")
    };
}
