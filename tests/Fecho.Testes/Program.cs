using Fecho.Core.Compatibilidade;
using Fecho.Core.Entidades;
using Fecho.Core.Enums;
using Fecho.Core.Seguranca;
using Fecho.Infraestrutura;

// Verificador executável sem pacotes externos. Qualquer falha encerra com código 1.
var casos = new List<(string Nome, Action Executar)>
{
    ("HashSet usa identidade por tipo e id, sem confundir entidades", () =>
    {
        var c = new Cenario();
        HashSet<EntidadeBase> conjunto = [c.Ana, c.Eletronicos, c.Camera, c.Camera];
        Igual(3, conjunto.Count);
        conjunto.Add(new Categoria(c.Eletronicos.Id, "Outro nome, mesma identidade"));
        Igual(3, conjunto.Count);
    }),
    ("Repositório base despacha validação específica de e-mail", () =>
    {
        var c = new Cenario();
        Falha<InvalidOperationException>(() => c.App.Usuarios.Adicionar(
            new Usuario(4, "Outra pessoa", "ANA@example.com", Cenario.Senha, "11999990004")));
        Igual(3, c.App.Usuarios.Listar().Count);
    }),
    ("Categoria única ignora maiúsculas e acentos", () =>
    {
        var c = new Cenario();
        Falha<InvalidOperationException>(() => c.App.Cadastro.CadastrarCategoria("ELETRONICOS"));
    }),
    ("Valida nome, e-mail, telefone e identidade", () =>
    {
        Falha<ArgumentException>(() => new Categoria(1, " "));
        Falha<ArgumentOutOfRangeException>(() => new Categoria(0, "Livros"));
        Falha<ArgumentException>(() => new Usuario(1, "Ana", "invalido", Cenario.Senha, "11999990001"));
        Falha<ArgumentException>(() => new Usuario(1, "Ana", "ana@example.com", Cenario.Senha, "123"));
    }),
    ("Rejeita valores negativos, excessivos e mais de duas casas", () =>
    {
        var c = new Cenario();
        foreach (decimal valor in new[] { -1m, 100_000_000m, 1.001m })
            Falha<ArgumentException>(() => c.App.Cadastro.CadastrarObjeto(c.Ana.Id,
                c.Eletronicos.Id, "Teste", EstadoConservacao.BOM, valorEstimado: valor));
        Igual(3, c.App.Objetos.Listar().Count);
    }),
    ("Rejeita valores de enum desconhecidos antes de gravar", () =>
    {
        var c = new Cenario();
        Falha<ArgumentException>(() => c.App.Cadastro.CadastrarObjeto(c.Ana.Id,
            c.Eletronicos.Id, "Teste", (EstadoConservacao)99));
        Falha<ArgumentException>(() => c.App.Negociacao.RegistrarInteresse(c.Ana.Id, c.Violao.Id, (TipoInteresse)99));
        Igual(0, c.App.Interesses.Listar().Count);
    }),
    ("Referências inexistentes são recusadas", () =>
    {
        var c = new Cenario();
        Falha<KeyNotFoundException>(() => c.App.Cadastro.CadastrarObjeto(c.Ana.Id, 999, "Teste", EstadoConservacao.BOM));
        Falha<KeyNotFoundException>(() => c.App.Cadastro.AdicionarDesejo(999, "Livros"));
        Falha<KeyNotFoundException>(() => c.App.Negociacao.RegistrarInteresse(c.Ana.Id, 999, TipoInteresse.LIKE));
    }),
    ("Descrição respeita limite em bytes do TEXT, incluindo acentos", () =>
    {
        var c = new Cenario();
        Falha<ArgumentException>(() => c.App.Cadastro.CadastrarObjeto(c.Ana.Id,
            c.Eletronicos.Id, "Teste", EstadoConservacao.BOM, new string('á', 40_000)));
        Igual(3, c.App.Objetos.Listar().Count);
    }),
    ("Id sem espaço para sequência falha sem inserir parcialmente", () =>
    {
        var c = new Cenario();
        Falha<OverflowException>(() => c.App.Categorias.Adicionar(new Categoria(int.MaxValue, "Limite")));
        Igual(2, c.App.Categorias.Listar().Count);
    }),
    ("Coleção devolvida não permite remover dados do repositório", () =>
    {
        var c = new Cenario();
        var lista = c.App.Objetos.Listar();
        Falha<NotSupportedException>(() => ((IList<Objeto>)lista).Clear());
        Igual(3, c.App.Objetos.Listar().Count);
    }),
    ("Hash de senha tem salt individual e cabe no campo SQL", () =>
    {
        var outra = SenhaProtegida.Criar("Fecho@2026");
        Exigir(Cenario.Senha.ParaPersistencia() != outra.ParaPersistencia(), "Salt deve ser diferente.");
        Exigir(outra.ParaPersistencia().Length <= 255, "Hash excede VARCHAR(255).");
        Exigir(!outra.ParaPersistencia().Contains("Fecho@2026", StringComparison.Ordinal), "Senha exposta.");
        Falha<ArgumentException>(() => SenhaProtegida.Criar("curta"));
    }),
    ("Autenticação confere senha e bloqueia usuário inativo", () =>
    {
        var c = new Cenario();
        Igual(c.Ana, c.App.Cadastro.Autenticar(" ANA@example.com ", "Fecho@2026"));
        Falha<UnauthorizedAccessException>(() => c.App.Cadastro.Autenticar(c.Ana.Email, "Senha@errada"));
        Falha<UnauthorizedAccessException>(() => c.App.Cadastro.Autenticar("ninguem@example.com", "Fecho@2026"));
        c.Ana.Desativar();
        Falha<UnauthorizedAccessException>(() => c.App.Cadastro.Autenticar(c.Ana.Email, "Fecho@2026"));
    }),
    ("Cadastro público gera hash e permite autenticar o novo usuário", () =>
    {
        var app = new AmbienteMemoria();
        var usuario = app.Cadastro.CadastrarUsuario("Novo", "novo@example.com", "Fecho@2026", "11999990004");
        Igual(usuario, app.Cadastro.Autenticar(usuario.Email, "Fecho@2026"));
    }),
    ("Sem desejos a descoberta retorna lista vazia", () =>
    {
        var c = new Cenario();
        Igual(0, c.App.Descoberta.Descobrir(c.Ana.Id, new PorCategoria()).Count);
    }),
    ("HashSet evita duplicar objeto compatível com dois desejos", () =>
    {
        var c = new Cenario();
        c.App.Cadastro.AdicionarDesejo(c.Ana.Id, "Violão", c.Instrumentos.Id);
        c.App.Cadastro.AdicionarDesejo(c.Ana.Id, "Guitarra", c.Instrumentos.Id);
        var cards = c.App.Descoberta.Descobrir(c.Ana.Id, new PorCategoria());
        Igual(2, cards.Count);
        Igual(c.Violao, cards[0]);
        Igual(c.Guitarra, cards[1]);
    }),
    ("Polimorfismo muda a descoberta sem mudar o serviço", () =>
    {
        var c = new Cenario();
        c.App.Cadastro.AdicionarDesejo(c.Ana.Id, "VIOLAO acustico", c.Instrumentos.Id);
        Igual(2, c.App.Descoberta.Descobrir(c.Ana.Id, new PorCategoria()).Count);
        var precisos = c.App.Descoberta.Descobrir(c.Ana.Id, new PorPalavrasChave());
        Igual(1, precisos.Count);
        Igual(c.Violao, precisos[0]);
    }),
    ("Desejo sem categoria funciona na busca textual", () =>
    {
        var c = new Cenario();
        c.App.Cadastro.AdicionarDesejo(c.Ana.Id, "violao");
        Igual(0, c.App.Descoberta.Descobrir(c.Ana.Id, new PorCategoria()).Count);
        Igual(c.Violao, c.App.Descoberta.Descobrir(c.Ana.Id, new PorPalavrasChave()).Single());
    }),
    ("Busca textual também respeita categoria informada", () =>
    {
        var c = new Cenario();
        c.App.Cadastro.AdicionarDesejo(c.Ana.Id, "violao", c.Eletronicos.Id);
        Igual(0, c.App.Descoberta.Descobrir(c.Ana.Id, new PorPalavrasChave()).Count);
    }),
    ("Descoberta exclui objetos próprios, donos inativos e avaliações anteriores", () =>
    {
        var c = new Cenario();
        c.App.Cadastro.AdicionarDesejo(c.Ana.Id, "Todos", c.Instrumentos.Id);
        c.App.Cadastro.AdicionarDesejo(c.Ana.Id, "Câmera", c.Eletronicos.Id);
        c.App.Negociacao.RegistrarInteresse(c.Ana.Id, c.Violao.Id, TipoInteresse.DISLIKE);
        c.Carla.Desativar();
        Igual(0, c.App.Descoberta.Descobrir(c.Ana.Id, new PorCategoria()).Count);
    }),
    ("Like unilateral não gera match nem contato", () =>
    {
        var c = new Cenario();
        Igual(0, c.App.Negociacao.RegistrarInteresse(c.Ana.Id, c.Violao.Id, TipoInteresse.LIKE).NovosMatches.Count);
        Igual(0, c.App.Matches.Listar().Count);
        Falha<KeyNotFoundException>(() => c.App.Negociacao.ObterContato(c.Ana.Id, 1));
    }),
    ("Dislike não completa reciprocidade", () =>
    {
        var c = new Cenario();
        c.App.Negociacao.RegistrarInteresse(c.Ana.Id, c.Violao.Id, TipoInteresse.LIKE);
        Igual(0, c.App.Negociacao.RegistrarInteresse(c.Bruno.Id, c.Camera.Id, TipoInteresse.DISLIKE).NovosMatches.Count);
    }),
    ("Reciprocidade liga usuários aos dois objetos corretos", () =>
    {
        var c = new Cenario();
        var match = c.CriarMatch();
        Exigir(match.Envolve(c.Camera) && match.Envolve(c.Violao), "Par incorreto.");
        Exigir(match.Envolve(c.Ana) && match.Envolve(c.Bruno), "Participantes incorretos.");
        Igual(StatusMatch.ATIVO, match.Status);
    }),
    ("É proibido curtir objeto próprio", () =>
    {
        var c = new Cenario();
        Falha<InvalidOperationException>(() => c.App.Negociacao.RegistrarInteresse(c.Ana.Id, c.Camera.Id, TipoInteresse.LIKE));
        Igual(0, c.App.Interesses.Listar().Count);
    }),
    ("Participante sem objeto disponível não pode avaliar", () =>
    {
        var c = new Cenario();
        var novo = new Usuario(4, "Sem objeto", "novo@example.com", Cenario.Senha, "11999990004");
        c.App.Usuarios.Adicionar(novo);
        Falha<InvalidOperationException>(() => c.App.Negociacao.RegistrarInteresse(novo.Id, c.Camera.Id, TipoInteresse.LIKE));
    }),
    ("Usuário inativo não pode cadastrar, descobrir ou negociar", () =>
    {
        var c = new Cenario();
        c.Ana.Desativar();
        Falha<InvalidOperationException>(() => c.App.Cadastro.AdicionarDesejo(c.Ana.Id, "Livros"));
        Falha<InvalidOperationException>(() => c.App.Descoberta.Descobrir(c.Ana.Id, new PorCategoria()));
        Falha<InvalidOperationException>(() => c.App.Negociacao.RegistrarInteresse(c.Ana.Id, c.Violao.Id, TipoInteresse.LIKE));
    }),
    ("Repetir likes em qualquer direção não duplica interesse ou match", () =>
    {
        var c = new Cenario();
        c.CriarMatch();
        Igual(0, c.App.Negociacao.RegistrarInteresse(c.Ana.Id, c.Violao.Id, TipoInteresse.LIKE).NovosMatches.Count);
        Igual(0, c.App.Negociacao.RegistrarInteresse(c.Bruno.Id, c.Camera.Id, TipoInteresse.LIKE).NovosMatches.Count);
        Igual(2, c.App.Interesses.Listar().Count);
        Igual(1, c.App.Matches.Listar().Count);
    }),
    ("Repositório também protege UNIQUE de usuário e objeto", () =>
    {
        var c = new Cenario();
        c.App.Negociacao.RegistrarInteresse(c.Ana.Id, c.Violao.Id, TipoInteresse.LIKE);
        Falha<InvalidOperationException>(() => c.App.Interesses.Adicionar(
            new Interesse(99, c.Ana, c.Violao, TipoInteresse.DISLIKE)));
    }),
    ("Mudar like para dislike preserva registro e cancela match", () =>
    {
        var c = new Cenario();
        var match = c.CriarMatch();
        int id = c.App.Interesses.Listar().Single(i => i.Usuario.Equals(c.Ana)).Id;
        var resultado = c.App.Negociacao.RegistrarInteresse(c.Ana.Id, c.Violao.Id, TipoInteresse.DISLIKE);
        Igual(id, resultado.Interesse.Id);
        Igual(2, c.App.Interesses.Listar().Count);
        Igual(StatusMatch.CANCELADO, match.Status);
        Falha<InvalidOperationException>(() => c.App.Negociacao.ObterContato(c.Ana.Id, match.Id));
    }),
    ("Novo like após retirada de interesse permite novo match", () =>
    {
        var c = new Cenario();
        var anterior = c.CriarMatch();
        c.App.Negociacao.RegistrarInteresse(c.Ana.Id, c.Violao.Id, TipoInteresse.DISLIKE);
        var novo = c.App.Negociacao.RegistrarInteresse(c.Ana.Id, c.Violao.Id, TipoInteresse.LIKE).NovosMatches.Single();
        Exigir(anterior.Id != novo.Id, "Histórico de cancelamento deve ser preservado.");
        Igual(StatusMatch.CANCELADO, anterior.Status);
        Igual(StatusMatch.ATIVO, novo.Status);
    }),
    ("Usuário com vários objetos só combina os objetos efetivamente curtidos", () =>
    {
        var c = new Cenario();
        var tablet = c.App.Cadastro.CadastrarObjeto(c.Ana.Id, c.Eletronicos.Id, "Tablet", EstadoConservacao.BOM);
        c.App.Negociacao.RegistrarInteresse(c.Bruno.Id, tablet.Id, TipoInteresse.LIKE);
        var match = c.App.Negociacao.RegistrarInteresse(c.Ana.Id, c.Violao.Id, TipoInteresse.LIKE).NovosMatches.Single();
        Exigir(match.Envolve(tablet) && !match.Envolve(c.Camera), "Match usou objeto não curtido.");
    }),
    ("Duas reciprocidades específicas geram dois pares distintos", () =>
    {
        var c = new Cenario();
        var tablet = c.App.Cadastro.CadastrarObjeto(c.Ana.Id, c.Eletronicos.Id, "Tablet", EstadoConservacao.BOM);
        c.App.Negociacao.RegistrarInteresse(c.Bruno.Id, c.Camera.Id, TipoInteresse.LIKE);
        c.App.Negociacao.RegistrarInteresse(c.Bruno.Id, tablet.Id, TipoInteresse.LIKE);
        Igual(2, c.App.Negociacao.RegistrarInteresse(c.Ana.Id, c.Violao.Id, TipoInteresse.LIKE).NovosMatches.Count);
    }),
    ("Somente participantes recebem contato após match", () =>
    {
        var c = new Cenario();
        var match = c.CriarMatch();
        var contato = c.App.Negociacao.ObterContato(c.Ana.Id, match.Id);
        Igual(c.Bruno.Id, contato.UsuarioId);
        Igual("11999990002", contato.Telefone);
        Falha<UnauthorizedAccessException>(() => c.App.Negociacao.ObterContato(c.Carla.Id, match.Id));
        Exigir(!c.Bruno.Resumir().Contains(contato.Telefone, StringComparison.Ordinal), "Resumo expôs contato.");
    }),
    ("Terceiro não pode cancelar ou concluir o match", () =>
    {
        var c = new Cenario();
        var match = c.CriarMatch();
        Falha<UnauthorizedAccessException>(() => c.App.Negociacao.CancelarMatch(c.Carla.Id, match.Id));
        Falha<UnauthorizedAccessException>(() => c.App.Negociacao.ConcluirTroca(c.Carla.Id, match.Id));
        Igual(StatusMatch.ATIVO, match.Status);
        Igual(0, c.App.Trocas.Listar().Count);
    }),
    ("Histórico é exclusivo dos participantes e ordenado do mais recente", () =>
    {
        var c = new Cenario();
        var primeiro = c.CriarMatch();
        c.App.Negociacao.RegistrarInteresse(c.Carla.Id, c.Camera.Id, TipoInteresse.LIKE);
        var segundo = c.App.Negociacao.RegistrarInteresse(c.Ana.Id, c.Guitarra.Id, TipoInteresse.LIKE).NovosMatches.Single();
        Igual(segundo, c.App.Negociacao.ListarMatches(c.Ana.Id)[0]);
        Igual(primeiro, c.App.Negociacao.ListarMatches(c.Bruno.Id).Single());
        Igual(segundo, c.App.Negociacao.ListarMatches(c.Carla.Id).Single());
    }),
    ("Cancelar revoga contato e permite que objetos continuem disponíveis", () =>
    {
        var c = new Cenario();
        var match = c.CriarMatch();
        c.App.Negociacao.CancelarMatch(c.Bruno.Id, match.Id);
        Igual(StatusObjeto.DISPONIVEL, c.Camera.Status);
        Igual(StatusObjeto.DISPONIVEL, c.Violao.Status);
        Falha<InvalidOperationException>(() => c.App.Negociacao.ObterContato(c.Ana.Id, match.Id));
        Falha<InvalidOperationException>(() => c.App.Negociacao.ConcluirTroca(c.Ana.Id, match.Id));
    }),
    ("Concluir registra troca e torna os dois objetos indisponíveis", () =>
    {
        var c = new Cenario();
        var match = c.CriarMatch();
        var troca = c.App.Negociacao.ConcluirTroca(c.Bruno.Id, match.Id, "Combinado em local público.");
        Igual(match, troca.Match);
        Igual(StatusMatch.CONCLUIDO, match.Status);
        Igual(StatusObjeto.TROCADO, c.Camera.Status);
        Igual(StatusObjeto.TROCADO, c.Violao.Status);
        Igual(1, c.App.Trocas.Listar().Count);
    }),
    ("Troca não pode ser concluída duas vezes", () =>
    {
        var c = new Cenario();
        var match = c.CriarMatch();
        c.App.Negociacao.ConcluirTroca(c.Ana.Id, match.Id);
        Falha<InvalidOperationException>(() => c.App.Negociacao.ConcluirTroca(c.Bruno.Id, match.Id));
        Igual(1, c.App.Trocas.Listar().Count);
    }),
    ("Conclusão cancela matches concorrentes e impede segunda entrega", () =>
    {
        var c = new Cenario();
        var primeiro = c.CriarMatch();
        c.App.Negociacao.RegistrarInteresse(c.Carla.Id, c.Camera.Id, TipoInteresse.LIKE);
        var segundo = c.App.Negociacao.RegistrarInteresse(c.Ana.Id, c.Guitarra.Id, TipoInteresse.LIKE).NovosMatches.Single();
        c.App.Negociacao.ConcluirTroca(c.Ana.Id, primeiro.Id);
        Igual(StatusMatch.CANCELADO, segundo.Status);
        Igual(StatusObjeto.DISPONIVEL, c.Guitarra.Status);
        Falha<InvalidOperationException>(() => c.App.Negociacao.ConcluirTroca(c.Carla.Id, segundo.Id));
        Falha<InvalidOperationException>(() => c.App.Negociacao.ObterContato(c.Carla.Id, segundo.Id));
    }),
    ("Observação inválida não altera estados nem registra troca", () =>
    {
        var c = new Cenario();
        var match = c.CriarMatch();
        Falha<ArgumentException>(() => c.App.Negociacao.ConcluirTroca(c.Ana.Id, match.Id, new string('x', 501)));
        Igual(StatusMatch.ATIVO, match.Status);
        Igual(StatusObjeto.DISPONIVEL, c.Camera.Status);
        Igual(StatusObjeto.DISPONIVEL, c.Violao.Status);
        Igual(0, c.App.Trocas.Listar().Count);
    }),
    ("Objeto trocado não aparece na descoberta nem aceita interesse", () =>
    {
        var c = new Cenario();
        var match = c.CriarMatch();
        c.App.Negociacao.ConcluirTroca(c.Ana.Id, match.Id);
        c.App.Cadastro.AdicionarDesejo(c.Carla.Id, "Câmera", c.Eletronicos.Id);
        Igual(0, c.App.Descoberta.Descobrir(c.Carla.Id, new PorCategoria()).Count);
        Falha<InvalidOperationException>(() => c.App.Negociacao.RegistrarInteresse(c.Carla.Id, c.Camera.Id, TipoInteresse.LIKE));
    }),
    ("Desativação de um participante impede concluir e consultar seu contato", () =>
    {
        var c = new Cenario();
        var match = c.CriarMatch();
        c.Bruno.Desativar();
        Falha<InvalidOperationException>(() => c.App.Negociacao.ConcluirTroca(c.Ana.Id, match.Id));
        Falha<InvalidOperationException>(() => c.App.Negociacao.ObterContato(c.Ana.Id, match.Id));
        Igual(0, c.App.Trocas.Listar().Count);
    })
};

int falhas = 0;
foreach (var (nome, executar) in casos)
{
    try { executar(); Console.WriteLine($"OK    {nome}"); }
    catch (Exception erro)
    {
        falhas++;
        Console.Error.WriteLine($"FALHA {nome}\n{erro}");
    }
}
Console.WriteLine($"\n{casos.Count - falhas}/{casos.Count} cenários aprovados.");
return falhas == 0 ? 0 : 1;

static void Exigir(bool condicao, string mensagem)
{
    if (!condicao) throw new InvalidOperationException(mensagem);
}

static void Igual<T>(T esperado, T atual)
{
    Exigir(EqualityComparer<T>.Default.Equals(esperado, atual), $"Esperado: {esperado}; recebido: {atual}.");
}

static void Falha<T>(Action acao) where T : Exception
{
    try { acao(); }
    catch (T) { return; }
    throw new InvalidOperationException($"Esperava uma exceção {typeof(T).Name}.");
}

internal sealed class Cenario
{
    internal static readonly SenhaProtegida Senha = SenhaProtegida.Criar("Fecho@2026");
    internal AmbienteMemoria App { get; } = new();
    internal Usuario Ana { get; } = new(1, "Ana", "ana@example.com", Senha, "11999990001");
    internal Usuario Bruno { get; } = new(2, "Bruno", "bruno@example.com", Senha, "11999990002");
    internal Usuario Carla { get; } = new(3, "Carla", "carla@example.com", Senha, "11999990003");
    internal Categoria Eletronicos { get; }
    internal Categoria Instrumentos { get; }
    internal Objeto Camera { get; }
    internal Objeto Violao { get; }
    internal Objeto Guitarra { get; }

    internal Cenario()
    {
        App.Usuarios.Adicionar(Ana);
        App.Usuarios.Adicionar(Bruno);
        App.Usuarios.Adicionar(Carla);
        Eletronicos = App.Cadastro.CadastrarCategoria("Eletrônicos");
        Instrumentos = App.Cadastro.CadastrarCategoria("Instrumentos");
        Camera = App.Cadastro.CadastrarObjeto(Ana.Id, Eletronicos.Id, "Câmera vintage", EstadoConservacao.BOM);
        Violao = App.Cadastro.CadastrarObjeto(Bruno.Id, Instrumentos.Id, "Violão acústico", EstadoConservacao.BOM);
        Guitarra = App.Cadastro.CadastrarObjeto(Carla.Id, Instrumentos.Id, "Guitarra elétrica", EstadoConservacao.BOM);
    }

    internal Match CriarMatch()
    {
        App.Negociacao.RegistrarInteresse(Ana.Id, Violao.Id, TipoInteresse.LIKE);
        return App.Negociacao.RegistrarInteresse(Bruno.Id, Camera.Id, TipoInteresse.LIKE).NovosMatches.Single();
    }
}
