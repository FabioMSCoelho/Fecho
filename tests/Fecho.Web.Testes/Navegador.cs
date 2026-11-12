using System.Net;
using System.Text.RegularExpressions;

internal sealed partial class Navegador(string endereco) : IDisposable
{
    private readonly HttpClient _http = new(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() })
    { BaseAddress = new Uri(endereco), Timeout = TimeSpan.FromSeconds(40) };

    internal async Task<Resposta> Get(string caminho)
    {
        using var resposta = await _http.GetAsync(caminho);
        return new Resposta((int)resposta.StatusCode, await resposta.Content.ReadAsStringAsync(), resposta.Headers.Location?.ToString());
    }
    internal async Task<Resposta> Pagina(string caminho)
    {
        for (var i = 0; i < 6; i++)
        {
            var resposta = await Get(caminho);
            if (resposta.Codigo is not (301 or 302 or 303 or 307 or 308)) return resposta;
            caminho = resposta.Localizacao ?? throw new Exception("Redirecionamento sem destino.");
            if (Uri.TryCreate(caminho, UriKind.Absolute, out var uri))
            {
                if (uri.Authority != _http.BaseAddress!.Authority) throw new Exception("Redirecionamento externo inesperado.");
                caminho = uri.PathAndQuery;
            }
        }
        throw new Exception("Ciclo de redirecionamentos.");
    }

    internal async Task<Resposta> Post(string caminho, Dictionary<string, string> dados, string paginaToken = "/meus-objetos", bool comToken = true)
    {
        if (comToken) dados["__RequestVerificationToken"] = await Token(paginaToken);
        using var resposta = await _http.PostAsync(caminho, new FormUrlEncodedContent(dados));
        return new Resposta((int)resposta.StatusCode, await resposta.Content.ReadAsStringAsync(), resposta.Headers.Location?.ToString());
    }
    internal async Task<Resposta> Upload(string caminho, Dictionary<string, string> dados, byte[] imagem)
    {
        dados["__RequestVerificationToken"] = await Token("/objetos/novo");
        using var conteudo = new MultipartFormDataContent();
        foreach (var (chave, valor) in dados) conteudo.Add(new StringContent(valor), chave);
        var arquivo = new ByteArrayContent(imagem);
        arquivo.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        conteudo.Add(arquivo, "Form.Imagem", "../../imagem.png");
        using var resposta = await _http.PostAsync(caminho, conteudo);
        return new Resposta((int)resposta.StatusCode, await resposta.Content.ReadAsStringAsync(), resposta.Headers.Location?.ToString());
    }
    internal async Task Entrar(string email, string senha)
    {
        var r = await Post("/conta/entrar", new() { ["Email"] = email, ["Senha"] = senha }, "/conta/entrar");
        if (r.Codigo != 302) throw new Exception("Não foi possível autenticar a conta de teste.");
    }
    private async Task<string> Token(string caminho)
    {
        var pagina = await Pagina(caminho);
        var token = TokenRegex().Match(pagina.Html);
        if (!token.Success) throw new Exception($"Token antifalsificação ausente em {caminho}, HTTP {pagina.Codigo}.");
        return WebUtility.HtmlDecode(token.Groups[1].Value);
    }
    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();
    public void Dispose() => _http.Dispose();
}

internal sealed record Resposta(int Codigo, string Html, string? Localizacao);
