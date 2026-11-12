namespace Fecho.Infraestrutura.MySql;

/// <summary>Lê somente chaves FECHO_ do .env local. Variáveis já exportadas têm prioridade.</summary>
public static class ConfiguracaoLocal
{
    public static string LocalizarRaiz()
    {
        foreach (var inicio in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (var pasta = new DirectoryInfo(inicio); pasta is not null; pasta = pasta.Parent)
                if (File.Exists(Path.Combine(pasta.FullName, "Fecho.slnx"))) return pasta.FullName;
        return Directory.GetCurrentDirectory();
    }

    public static void Carregar(string arquivo)
    {
        if (!File.Exists(arquivo)) return;
        var exportadas = Environment.GetEnvironmentVariables().Keys.Cast<string>()
            .Where(c => c.StartsWith("FECHO_", StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal);
        foreach (var linha in File.ReadLines(arquivo))
        {
            var partes = linha.Trim().Split('=', 2);
            if (partes.Length != 2 || !partes[0].StartsWith("FECHO_", StringComparison.Ordinal)) continue;
            var chave = partes[0].Trim();
            if (exportadas.Contains(chave)) continue;
            var valor = partes[1].Trim();
            if (valor.Length >= 2 && (valor[0] == '\'' && valor[^1] == '\'' || valor[0] == '"' && valor[^1] == '"')) valor = valor[1..^1];
            Environment.SetEnvironmentVariable(chave, valor);
        }
    }
}
