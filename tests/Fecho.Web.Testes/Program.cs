using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Fecho.Core.Enums;
using Fecho.Core.Servicos;
using Fecho.Infraestrutura.MySql;
using MySqlConnector;

// Somente contra um servidor e banco de teste. As fixtures têm nomes únicos e são removidas no finally.
var endereco = Environment.GetEnvironmentVariable("FECHO_TEST_WEB_URL") ?? "http://127.0.0.1:5071";
var conexao = ConexaoMySql.DoAmbiente();
var repoUsuarios = new RepositorioUsuariosMySql(conexao);
var repoObjetos = new RepositorioObjetosMySql(conexao);
var categorias = new RepositorioCategoriasMySql(conexao);
var negociacao = new NegociacaoPersistenteService(new UnidadeNegociacaoMySql(conexao));
var prefixo = "Web-" + Guid.NewGuid().ToString("N")[..10];
var emails = Enumerable.Range(1, 3).Select(i => prefixo + i + "@example.com").ToArray();
var ids = new List<int>();
var categoria = categorias.Inserir(prefixo + " categoria", null);
var aprovados = 0;
void Exigir(bool condicao, string caso)
{
    if (!condicao) throw new Exception("FALHOU: " + caso);
    Console.WriteLine("OK " + caso); aprovados++;
}
Dictionary<string, string> Cadastro(int n) => new()
{
    ["Nome"] = prefixo + " Pessoa " + n, ["Email"] = emails[n - 1], ["Senha"] = "WebSenha@2026",
    ["Confirmacao"] = "WebSenha@2026", ["Telefone"] = "1198765000" + n, ["Cidade"] = "São Paulo"
};
Dictionary<string, string> Objeto(string nome) => new()
{
    ["Form.Nome"] = prefixo + nome, ["Form.CategoriaId"] = categoria.Id.ToString(CultureInfo.InvariantCulture),
    ["Form.Estado"] = "BOM", ["Form.Descricao"] = "Descrição de teste com acentuação", ["Form.ValorEstimado"] = "150,50"
};
int IdObjeto(Resposta resposta)
{
    if (resposta.Codigo != 302 || resposta.Localizacao is null) throw new Exception("Cadastro de objeto não redirecionou.");
    return int.Parse(Regex.Match(resposta.Localizacao, @"/objetos/(\d+)").Groups[1].Value, CultureInfo.InvariantCulture);
}
using var anonimo = new Navegador(endereco);
using var a = new Navegador(endereco);
using var b = new Navegador(endereco);
using var c = new Navegador(endereco);
try
{
    Exigir((await anonimo.Get("/saude")).Codigo == 200, "Servidor Web e conexão MySQL estão disponíveis");
    Exigir((await anonimo.Get("/meus-objetos")).Localizacao?.Contains("/conta/entrar", StringComparison.Ordinal) == true,
        "Página de gestão exige sessão autenticada");
    var semToken = await anonimo.Post("/conta/cadastrar", Cadastro(1), comToken: false);
    Exigir(semToken.Codigo == 400 && repoUsuarios.BuscarPorEmail(emails[0]) is null,
        "POST sem token CSRF é rejeitado sem criar usuário");
    var navegadores = new[] { a, b, c };
    for (var i = 1; i <= 3; i++)
    {
        var resposta = await navegadores[i - 1].Post("/conta/cadastrar", Cadastro(i), "/conta/cadastrar");
        Exigir(resposta.Codigo == 302, "Cadastro Web cria conta e autentica pessoa " + i);
        ids.Add(repoUsuarios.BuscarPorEmail(emails[i - 1])!.Id);
    }
    var duplicado = await anonimo.Post("/conta/cadastrar", Cadastro(1), "/conta/cadastrar");
    Exigir(duplicado.Codigo == 200 && WebUtility.HtmlDecode(duplicado.Html).Contains("Já existe um usuário", StringComparison.Ordinal),
        "Formulário Web apresenta erro de e-mail duplicado");
    var senhaErrada = await anonimo.Post("/conta/entrar", new() { ["Email"] = emails[0], ["Senha"] = "SenhaErrada@2026" }, "/conta/entrar");
    Exigir(WebUtility.HtmlDecode(senhaErrada.Html).Contains("E-mail ou senha inválidos", StringComparison.Ordinal), "Login rejeita senha incorreta");
    var autoLike = await a.Post("/objetos/novo", Objeto(" câmera"));
    var objetoA = IdObjeto(autoLike);
    var proprioLike = await a.Post($"/objetos/{objetoA}/interesse", new() { ["tipo"] = "LIKE" });
    Exigir(proprioLike.Codigo == 302 && !negociacao.ListarInteresses(ids[0]).ContainsKey(objetoA), "Regra impede interesse no próprio objeto");
    var semObjeto = await b.Post($"/objetos/{objetoA}/interesse", new() { ["tipo"] = "LIKE" });
    Exigir(semObjeto.Codigo == 302 && negociacao.ListarInteresses(ids[1]).Count == 0, "Usuário sem objeto disponível não participa de trocas");
    var camposB = Objeto(" violão"); camposB["usuarioId"] = ids[0].ToString(); camposB["Form.ProprietarioId"] = ids[0].ToString();
    var objetoB = IdObjeto(await b.Post("/objetos/novo", camposB));
    Exigir(repoObjetos.Buscar(objetoB)?.Proprietario.Id == ids[1], "Dono do objeto vem da sessão, ignorando IDs forjados no formulário");
    var objetoC = IdObjeto(await c.Post("/objetos/novo", Objeto(" livro")));
    Exigir(repoObjetos.Buscar(objetoA)?.ValorEstimado == 150.50m, "Formulário persiste valor decimal com vírgula corretamente");
    var naoAutorizado = await b.Get($"/objetos/{objetoA}/editar");
    Exigir(naoAutorizado.Codigo is 302 or 403, "Outro usuário não abre o formulário de edição");
    var edicaoAlheia = await b.Post($"/objetos/{objetoA}/editar", Objeto(" INVASÃO"));
    Exigir(edicaoAlheia.Codigo is 302 or 403 && repoObjetos.Buscar(objetoA)?.Nome.EndsWith(" câmera", StringComparison.Ordinal) == true,
        "POST forjado não altera objeto alheio");
    var excluirAlheio = await b.Post($"/objetos/{objetoA}/excluir", new());
    Exigir(excluirAlheio.Codigo is 302 or 403 && repoObjetos.Buscar(objetoA) is not null, "POST forjado não exclui objeto alheio");
    var invalido = Objeto(" inválido"); invalido["Form.CategoriaId"] = int.MaxValue.ToString(CultureInfo.InvariantCulture);
    var categoriaAusente = await a.Post("/objetos/novo", invalido);
    Exigir(categoriaAusente.Codigo == 200 && WebUtility.HtmlDecode(categoriaAusente.Html).Contains("Categoria não encontrada", StringComparison.Ordinal),
        "Tela valida vínculo com categoria existente");
    var imagemInvalida = await c.Upload("/objetos/novo", Objeto(" imagem inválida"), Encoding.UTF8.GetBytes("arquivo que não é imagem"));
    Exigir(imagemInvalida.Codigo == 200 && WebUtility.HtmlDecode(imagemInvalida.Html).Contains("imagem PNG, JPEG ou WebP", StringComparison.Ordinal),
        "Upload rejeita conteúdo que não é imagem");
    var imagemPng = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a1XkAAAAASUVORK5CYII=");
    var comImagem = IdObjeto(await c.Upload("/objetos/novo", Objeto(" imagem segura"), imagemPng));
    var caminhoImagem = repoObjetos.Buscar(comImagem)!.Imagem!;
    Exigir(Regex.IsMatch(caminhoImagem, @"^/imagens/[a-f0-9]{32}\.png$") && (await anonimo.Get(caminhoImagem)).Codigo == 200,
        "Imagem recebe nome seguro e pode ser exibida após cadastro");
    var excluirImagem = await c.Post($"/objetos/{comImagem}/excluir", new());
    Exigir(excluirImagem.Codigo == 302 && repoObjetos.Buscar(comImagem) is null, "Exclusão Web remove objeto cadastrado");
    var filtro = await anonimo.Get("/?Texto=" + Uri.EscapeDataString(prefixo + " VIOLAO"));
    Exigir(filtro.Codigo == 200 && WebUtility.HtmlDecode(filtro.Html).Contains(prefixo + " violão", StringComparison.Ordinal),
        "Busca Web encontra palavras sem diferenciar acentos ou caixa");
    var semResultado = await anonimo.Get("/?Texto=" + prefixo + "inexistente");
    Exigir(Regex.IsMatch(WebUtility.HtmlDecode(semResultado.Html), @"<strong>0</strong>\s*objetos disponíveis"), "Busca sem resultado exibe estado vazio");
    await a.Post("/desejos", new() { ["Form.Descricao"] = "violao", ["Form.CategoriaId"] = categoria.Id.ToString() });
    var recomendados = await a.Get("/?ParaMim=true");
    Exigir(WebUtility.HtmlDecode(recomendados.Html).Contains(prefixo + " violão", StringComparison.Ordinal), "Desejo persistido alimenta recomendações na tela");
    var publico = await anonimo.Get($"/objetos/{objetoB}");
    Exigir(!publico.Html.Contains("11987650002", StringComparison.Ordinal) && !publico.Html.Contains("PBKDF2", StringComparison.Ordinal),
        "Página pública não revela telefone nem hash de senha");
    await a.Post($"/objetos/{objetoB}/interesse", new() { ["tipo"] = "LIKE" });
    Exigir(negociacao.ListarMatches(ids[0]).Count == 0, "Like unilateral não cria match");
    await b.Post($"/objetos/{objetoA}/interesse", new() { ["tipo"] = "LIKE" });
    var match = negociacao.ListarMatches(ids[0]).Single();
    Exigir(match.Registro.Status == StatusMatch.ATIVO, "Likes recíprocos criam match persistente");
    var duplicacoes = await Task.WhenAll(
        b.Post($"/objetos/{objetoA}/interesse", new() { ["tipo"] = "LIKE" }),
        b.Post($"/objetos/{objetoA}/interesse", new() { ["tipo"] = "LIKE" }));
    Exigir(duplicacoes.All(r => r.Codigo == 302) && negociacao.ListarMatches(ids[0]).Count == 1,
        "Likes simultâneos e repetidos mantêm um único match");
    var detalheMatch = await a.Get($"/matches/{match.Registro.Id}");
    Exigir(detalheMatch.Codigo == 200 && detalheMatch.Html.Contains("11987650002", StringComparison.Ordinal), "Participante recebe contato somente após match");
    Exigir((await c.Get($"/matches/{match.Registro.Id}")).Codigo == 404, "Terceiro não acessa detalhes ou contato do match");
    var concluirAlheio = await c.Post($"/matches/{match.Registro.Id}/concluir", new());
    Exigir(concluirAlheio.Codigo is 302 or 403 && negociacao.ListarMatches(ids[0]).Single().Registro.Status == StatusMatch.ATIVO,
        "Terceiro não conclui uma troca por POST forjado");
    await b.Post($"/objetos/{objetoA}/interesse", new() { ["tipo"] = "DISLIKE" });
    Exigir(negociacao.ListarMatches(ids[0]).Single().Registro.Status == StatusMatch.CANCELADO
        && !(await a.Get($"/matches/{match.Registro.Id}")).Html.Contains("11987650002", StringComparison.Ordinal), "Retirar interesse cancela match e revoga contato");
    await b.Post($"/objetos/{objetoA}/interesse", new() { ["tipo"] = "LIKE" });
    match = negociacao.ListarMatches(ids[0]).Single(m => m.Registro.Status == StatusMatch.ATIVO);
    await c.Post($"/objetos/{objetoA}/interesse", new() { ["tipo"] = "LIKE" });
    await a.Post($"/objetos/{objetoC}/interesse", new() { ["tipo"] = "LIKE" });
    Exigir(negociacao.ListarMatches(ids[0]).Count(m => m.Registro.Status == StatusMatch.ATIVO) == 2, "Objeto pode receber propostas concorrentes antes da conclusão");
    var exclusaoComMatch = await a.Post($"/objetos/{objetoA}/excluir", new());
    Exigir(exclusaoComMatch.Codigo == 302 && repoObjetos.Buscar(objetoA) is not null, "Exclusão preserva objeto vinculado a matches");
    await a.Post($"/matches/{match.Registro.Id}/concluir", new() { ["observacao"] = new string('x', 501) });
    Exigir(repoObjetos.Buscar(objetoA)?.Status == StatusObjeto.DISPONIVEL
        && negociacao.ListarMatches(ids[0]).Single(m => m.Registro.Id == match.Registro.Id).Registro.Status == StatusMatch.ATIVO,
        "Observação inválida impede conclusão sem alterações parciais");
    await Task.WhenAll(
        a.Post($"/matches/{match.Registro.Id}/concluir", new() { ["observacao"] = "Troca de teste concluída" }),
        b.Post($"/matches/{match.Registro.Id}/concluir", new() { ["observacao"] = "Confirmação simultânea" }));
    Exigir(repoObjetos.Buscar(objetoA)?.Status == StatusObjeto.TROCADO && repoObjetos.Buscar(objetoB)?.Status == StatusObjeto.TROCADO
        && repoObjetos.Buscar(objetoC)?.Status == StatusObjeto.DISPONIVEL, "Conclusão marca somente os dois objetos envolvidos como trocados");
    var historico = negociacao.ListarMatches(ids[0]);
    Exigir(historico.Count(m => m.Registro.Status == StatusMatch.CONCLUIDO) == 1
        && historico.Where(m => m.Registro.Id != match.Registro.Id).All(m => m.Registro.Status == StatusMatch.CANCELADO),
        "Confirmações simultâneas mantêm uma conclusão e cancelam propostas concorrentes");
    var terceiroMatch = negociacao.ListarMatches(ids[2]).Single();
    Exigir(!(await c.Get($"/matches/{terceiroMatch.Registro.Id}")).Html.Contains("11987650001", StringComparison.Ordinal),
        "Contato de proposta concorrente é revogado após a troca");
    var perfilInvalido = await c.Post("/conta/perfil", new() { ["Nome"] = "", ["Email"] = emails[2], ["Telefone"] = "11987650003" });
    Exigir(perfilInvalido.Codigo == 200, "Perfil inválido reapresenta validação sem quebrar a página");
    var nomeSeguro = prefixo + " <script>alert(1)</script>";
    await c.Post("/conta/perfil", new() { ["Nome"] = nomeSeguro, ["Email"] = emails[2], ["Telefone"] = "11987650003", ["Cidade"] = "Campinas" });
    var perfil = await c.Get("/conta/perfil");
    Exigir(repoUsuarios.Buscar(ids[2])?.Nome == nomeSeguro && !perfil.Html.Contains("<script>alert(1)</script>", StringComparison.Ordinal),
        "Perfil persiste alterações e conteúdo do usuário é escapado no HTML");
    using var segundaSessao = new Navegador(endereco);
    await segundaSessao.Entrar(emails[2], "WebSenha@2026");
    var trocaSenha = await c.Post("/conta/senha", new() { ["Atual"] = "WebSenha@2026", ["Nova"] = "WebNova@2026", ["Confirmacao"] = "WebNova@2026" });
    Exigir(trocaSenha.Codigo == 302 && (await segundaSessao.Get("/meus-objetos")).Localizacao?.Contains("/conta/entrar", StringComparison.Ordinal) == true,
        "Troca de senha invalida também sessões antigas");
    await c.Entrar(emails[2], "WebNova@2026");
    var retornoExterno = await anonimo.Post("/conta/entrar", new() { ["Email"] = emails[0], ["Senha"] = "WebSenha@2026", ["Retorno"] = "https://example.com" }, "/conta/entrar");
    Exigir(retornoExterno.Localizacao == "/", "Login ignora destino externo para impedir redirecionamento aberto");
    var usuarioC = new UsuarioService(repoUsuarios).ConsultarPerfil(ids[2]);
    repoUsuarios.Atualizar(ids[2], usuarioC.Usuario.Nome, usuarioC.Usuario.Email, usuarioC.Telefone, null, null, null, false);
    Exigir((await c.Get("/meus-objetos")).Localizacao?.Contains("/conta/entrar", StringComparison.Ordinal) == true,
        "Desativar usuário invalida acesso Web na próxima requisição");
    Console.WriteLine($"{aprovados}/{aprovados} verificações Web aprovadas.");
}
catch (Exception ex) { Console.Error.WriteLine(ex); Environment.ExitCode = 1; }
finally
{
    using var db = conexao.Abrir();
    var existentes = emails.Select(repoUsuarios.BuscarPorEmail).Where(u => u is not null).Select(u => u!.Id).ToArray();
    if (existentes.Length > 0)
    {
        var marcadores = string.Join(',', existentes.Select((_, i) => "@u" + i));
        foreach (var sql in new[] {
            $"DELETE FROM trocas WHERE match_id IN (SELECT id FROM matches WHERE usuario1_id IN ({marcadores}) OR usuario2_id IN ({marcadores}))",
            $"DELETE FROM matches WHERE usuario1_id IN ({marcadores}) OR usuario2_id IN ({marcadores})",
            $"DELETE FROM interesses WHERE usuario_id IN ({marcadores}) OR objeto_id IN (SELECT id FROM objetos WHERE usuario_id IN ({marcadores}))",
            $"DELETE FROM desejos WHERE usuario_id IN ({marcadores})", $"DELETE FROM objetos WHERE usuario_id IN ({marcadores})",
            $"DELETE FROM usuarios WHERE id IN ({marcadores})" })
        {
            using var cmd = new MySqlCommand(sql, db);
            for (var i = 0; i < existentes.Length; i++) cmd.Parameters.AddWithValue("@u" + i, existentes[i]);
            cmd.ExecuteNonQuery();
        }
    }
    categorias.Excluir(categoria.Id);
}
