using System.Diagnostics;
using System.Globalization;

/// <summary>Consulta a mesma base em outro processo, sem compartilhar objetos ou pool de conexões.</summary>
internal static class ProcessoPersistencia
{
    internal static bool ConsultaConfere(int id, string? nomeEsperado, string entidade = "categorias")
    {
        string executavel = Environment.ProcessPath
            ?? throw new InvalidOperationException("Não foi possível localizar o executável da suíte.");
        var inicio = new ProcessStartInfo(executavel)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // A suíte pode ser iniciada pelo host dotnet ou pelo executável gerado pelo SDK.
        if (Path.GetFileNameWithoutExtension(executavel).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            inicio.ArgumentList.Add(typeof(ProcessoPersistencia).Assembly.Location);
        inicio.ArgumentList.Add("--consultar");
        inicio.ArgumentList.Add(entidade);
        inicio.ArgumentList.Add(id.ToString(CultureInfo.InvariantCulture));

        // O processo herda FECHO_DB_* do ambiente, sem colocar credenciais nos argumentos.
        using var processo = Process.Start(inicio)
            ?? throw new InvalidOperationException("Não foi possível iniciar a consulta em outro processo.");
        if (!processo.WaitForExit(30_000))
        {
            processo.Kill(entireProcessTree: true);
            processo.WaitForExit();
            throw new TimeoutException("A consulta em outro processo excedeu 30 segundos.");
        }

        string saida = processo.StandardOutput.ReadToEnd().Trim();
        return nomeEsperado is null
            ? processo.ExitCode == 3 && saida.Length == 0
            : processo.ExitCode == 0 && saida == nomeEsperado;
    }
}
