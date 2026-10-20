using System.Text;
using Fecho.Core.Compatibilidade;
using Fecho.Core.Entidades;
using Fecho.Core.Enums;
using Fecho.Infraestrutura;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("FECHÔ — demonstração da entrega de POO\n");
var app = new AmbienteMemoria();
var fabio = app.Cadastro.CadastrarUsuario("Fábio", "fabio@example.com", "Fecho@2026", "11999990001", "São Paulo");
var giovana = app.Cadastro.CadastrarUsuario("Giovana", "giovana@example.com", "Fecho@2026", "11999990002", "São Paulo");
var ana = app.Cadastro.CadastrarUsuario("Ana", "ana@example.com", "Fecho@2026", "11999990003", "São Paulo");
var eletronicos = app.Cadastro.CadastrarCategoria("Eletrônicos");
var instrumentos = app.Cadastro.CadastrarCategoria("Instrumentos musicais");
var camera = app.Cadastro.CadastrarObjeto(fabio.Id, eletronicos.Id, "Câmera vintage", EstadoConservacao.BOM, valorEstimado: 450m);
var violao = app.Cadastro.CadastrarObjeto(giovana.Id, instrumentos.Id, "Violão acústico", EstadoConservacao.MUITO_BOM);
var teclado = app.Cadastro.CadastrarObjeto(giovana.Id, instrumentos.Id, "Teclado musical", EstadoConservacao.BOM);
var guitarra = app.Cadastro.CadastrarObjeto(ana.Id, instrumentos.Id, "Guitarra elétrica", EstadoConservacao.BOM);
var desejo = app.Cadastro.AdicionarDesejo(fabio.Id, "violao", instrumentos.Id);
app.Cadastro.AdicionarDesejo(fabio.Id, "acustico", instrumentos.Id);

Console.WriteLine("1. HERANÇA E POLIMORFISMO — uma List<EntidadeBase> com tipos diferentes");
List<EntidadeBase> entidades = [fabio, eletronicos, camera, desejo];
foreach (var entidade in entidades) Console.WriteLine($"   {entidade.Resumir()}");

Console.WriteLine("\n2. CRITÉRIOS POLIMÓRFICOS — mesmo método, resultados diferentes");
List<CriterioCompatibilidade> criterios = [new PorCategoria(), new PorPalavrasChave()];
foreach (var criterio in criterios)
{
    var cards = app.Descoberta.Descobrir(fabio.Id, criterio);
    Console.WriteLine($"   {criterio.Nome}: {string.Join(", ", cards.Select(o => o.Nome))}");
    if (cards.Count > 0) Console.WriteLine($"   Primeiro card [0]: {cards[0].Nome}");
}
Console.WriteLine("   Dois desejos da mesma pessoa não repetem um card: HashSet<Objeto>.");

Console.WriteLine("\n3. INTERESSES E MATCHES — só a reciprocidade libera contato");
var unilateral = app.Negociacao.RegistrarInteresse(fabio.Id, violao.Id, TipoInteresse.LIKE);
Console.WriteLine($"   Fábio curtiu o violão. Novos matches: {unilateral.NovosMatches.Count}.");
var reciproco = app.Negociacao.RegistrarInteresse(giovana.Id, camera.Id, TipoInteresse.LIKE);
var match = reciproco.NovosMatches.Single();
Console.WriteLine($"   Giovana curtiu a câmera. {match.Resumir()}");
app.Negociacao.RegistrarInteresse(fabio.Id, violao.Id, TipoInteresse.LIKE);
Console.WriteLine($"   Repetir o like mantém {app.Matches.Listar().Count} match e {app.Interesses.Listar().Count} interesses.");
var contato = app.Negociacao.ObterContato(fabio.Id, match.Id);
Console.WriteLine($"   Contato liberado para Fábio: {contato.Nome}, {contato.Telefone} (dados fictícios).");
try { app.Negociacao.ObterContato(ana.Id, match.Id); }
catch (UnauthorizedAccessException erro) { Console.WriteLine($"   Acesso de terceiro bloqueado: {erro.Message}"); }

Console.WriteLine("\n4. TROCA CONCLUÍDA — objetos indisponíveis e cancelamento de outros matches");
app.Negociacao.RegistrarInteresse(ana.Id, camera.Id, TipoInteresse.LIKE);
var concorrente = app.Negociacao.RegistrarInteresse(fabio.Id, guitarra.Id, TipoInteresse.LIKE).NovosMatches.Single();
var troca = app.Negociacao.ConcluirTroca(fabio.Id, match.Id, "Troca combinada em local público.");
Console.WriteLine($"   {troca.Resumir()}");
Console.WriteLine($"   Câmera: {camera.Status}; violão: {violao.Status}; outro match da câmera: {concorrente.Status}.");
Console.WriteLine($"   Teclado: {teclado.Status}; guitarra: {guitarra.Status}.");
Console.WriteLine("\nDemonstração finalizada. Os dados ficam em memória e são recriados a cada execução.");
