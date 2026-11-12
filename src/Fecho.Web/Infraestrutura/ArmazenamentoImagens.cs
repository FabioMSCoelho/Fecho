namespace Fecho.Web.Infraestrutura;

public sealed class ArmazenamentoImagens(string pasta)
{
    public async Task<string?> SalvarAsync(IFormFile? arquivo)
    {
        if (arquivo is null || arquivo.Length == 0) return null;
        if (arquivo.Length > 2 * 1024 * 1024) throw new ArgumentException("A imagem deve ter até 2 MB.");
        await using var entrada = arquivo.OpenReadStream();
        var cabecalho = new byte[12];
        var lidos = await entrada.ReadAtLeastAsync(cabecalho, 12, throwOnEndOfStream: false);
        var extensao = lidos >= 8 && cabecalho.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? ".png"
            : lidos >= 3 && cabecalho[0] == 255 && cabecalho[1] == 216 && cabecalho[2] == 255 ? ".jpg"
            : lidos == 12 && System.Text.Encoding.ASCII.GetString(cabecalho, 0, 4) == "RIFF"
                && System.Text.Encoding.ASCII.GetString(cabecalho, 8, 4) == "WEBP" ? ".webp" : null;
        if (extensao is null) throw new ArgumentException("Envie uma imagem PNG, JPEG ou WebP válida.");
        var nome = Guid.NewGuid().ToString("N") + extensao;
        var destino = Path.Combine(pasta, nome);
        await using var saida = new FileStream(destino, FileMode.CreateNew);
        await saida.WriteAsync(cabecalho.AsMemory(0, lidos));
        await entrada.CopyToAsync(saida);
        return "/imagens/" + nome;
    }

    public void Remover(string? caminho)
    {
        if (caminho is null || !caminho.StartsWith("/imagens/", StringComparison.Ordinal)) return;
        var nome = caminho[9..];
        if (nome != Path.GetFileName(nome)) return;
        var destino = Path.Combine(pasta, nome);
        if (File.Exists(destino)) File.Delete(destino);
    }
}
