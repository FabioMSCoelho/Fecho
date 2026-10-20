namespace Fecho.Core;

public static class Validacao
{
    public static string Texto(string? valor, string campo, int maximo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException($"{campo} é obrigatório.", campo);
        valor = valor.Trim();
        if (valor.Length > maximo)
            throw new ArgumentException($"{campo} aceita até {maximo} caracteres.", campo);
        return valor;
    }

    public static string? Opcional(string? valor, string campo, int maximo) =>
        string.IsNullOrWhiteSpace(valor) ? null : Texto(valor, campo, maximo);

    public static T EnumValido<T>(T valor) where T : struct, Enum =>
        Enum.IsDefined(valor) ? valor : throw new ArgumentException($"Valor inválido para {typeof(T).Name}.");
}
