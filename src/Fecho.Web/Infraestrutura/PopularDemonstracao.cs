using Fecho.Core.Contratos;
using Fecho.Core;
using Fecho.Core.Enums;
using Fecho.Core.Servicos;

namespace Fecho.Web.Infraestrutura;

internal static class PopularDemonstracao
{
    internal static void Executar(IServiceProvider servicos)
    {
        var senha = Environment.GetEnvironmentVariable("FECHO_DEMO_PASSWORD");
        if (senha is null || senha.Length is < 8 or > 128)
            throw new ArgumentException("Para preparar a demonstração, defina FECHO_DEMO_PASSWORD com 8 a 128 caracteres.");
        var repoUsuarios = servicos.GetRequiredService<ICrudUsuarios>();
        var usuarios = servicos.GetRequiredService<UsuarioService>();
        var categorias = servicos.GetRequiredService<CategoriaService>();
        var objetos = servicos.GetRequiredService<ObjetoService>();
        var desejos = servicos.GetRequiredService<DesejoService>();
        var nomes = new[] { "Eletrônicos", "Instrumentos musicais", "Livros", "Esportes", "Casa e jardim" };
        var mapa = categorias.Listar().ToDictionary(c => TextoNormalizado.De(c.Nome), c => c.Id);
        foreach (var nome in nomes) if (!mapa.ContainsKey(TextoNormalizado.De(nome))) mapa[TextoNormalizado.De(nome)] = categorias.Cadastrar(nome).Id;
        var ana = repoUsuarios.BuscarPorEmail("ana.demo@fecho.local")
            ?? usuarios.Cadastrar("Ana Martins", "ana.demo@fecho.local", senha, "11900000001", "São Paulo");
        var bruno = repoUsuarios.BuscarPorEmail("bruno.demo@fecho.local")
            ?? usuarios.Cadastrar("Bruno Costa", "bruno.demo@fecho.local", senha, "13900000002", "Santos");
        var clara = repoUsuarios.BuscarPorEmail("clara.demo@fecho.local")
            ?? usuarios.Cadastrar("Clara Almeida", "clara.demo@fecho.local", senha, "19900000003", "Campinas");
        var contas = new[] { ana, bruno, clara };
        var ativos = contas.Where(u => u.Ativo).Select(u => u.Id).ToHashSet();
        var existentes = objetos.Listar().Select(o => (o.Proprietario.Id, TextoNormalizado.De(o.Nome))).ToHashSet();
        var exemplos = new[]
        {
            (ana.Id, "Eletrônicos", "Câmera analógica vintage", EstadoConservacao.BOM, 450m, "camera", "Uma câmera analógica com marcas de boas histórias. Funcionando, acompanha alça."),
            (bruno.Id, "Instrumentos musicais", "Violão acústico", EstadoConservacao.MUITO_BOM, 380m, "violao", "Violão acústico de madeira, bem cuidado e com cordas novas. Pronto para novas músicas."),
            (clara.Id, "Esportes", "Bicicleta urbana", EstadoConservacao.BOM, 650m, "bicicleta", "Bicicleta para passeios pela cidade. Freios revisados e pneus em bom estado."),
            (ana.Id, "Livros", "Coleção de literatura brasileira", EstadoConservacao.MUITO_BOM, 120m, "livros", "Três livros de literatura brasileira. Páginas conservadas, sem anotações."),
            (bruno.Id, "Eletrônicos", "Fone de ouvido sem fio", EstadoConservacao.BOM, 180m, "fones", "Fone confortável para ouvir música ou estudar. Bateria e conexão Bluetooth funcionando."),
            (clara.Id, "Casa e jardim", "Vaso de cerâmica com planta", EstadoConservacao.MUITO_BOM, 65m, "planta", "Uma planta para trazer mais verde à sua casa. Vaso de cerâmica incluso."),
            (ana.Id, "Eletrônicos", "Câmera compacta de viagem", EstadoConservacao.BOM, 220m, "camera", "Câmera compacta, leve e funcionando. Ideal para registrar os próximos passeios."),
            (bruno.Id, "Livros", "Livros para uma tarde tranquila", EstadoConservacao.BOM, 80m, "livros", "Seleção de romances bem conservados. Busco novas leituras na troca."),
            (ana.Id, "Eletrônicos", "Leitor de livros digital", EstadoConservacao.MUITO_BOM, 320m, "leitor", "Leitor digital com tela de 6 polegadas e iluminação ajustável. Acompanha cabo e capa protetora."),
            (ana.Id, "Casa e jardim", "Luminária de mesa articulada", EstadoConservacao.BOM, 90m, "luminaria", "Luminária com braço articulado e luz para leitura. Ideal para uma mesa de estudos ou um cantinho de trabalho."),
            (ana.Id, "Esportes", "Raquete de tênis", EstadoConservacao.REGULAR, 160m, "raquete", "Raquete de tênis com encordoamento firme. Tem marcas de uso no cabo; indicada para quem está começando."),
            (ana.Id, "Instrumentos musicais", "Teclado musical portátil", EstadoConservacao.MUITO_BOM, 540m, "teclado", "Teclado musical de 61 teclas, com fonte e suporte para partituras. Todos os sons e teclas funcionando."),
            (bruno.Id, "Eletrônicos", "Caixa de som bluetooth", EstadoConservacao.BOM, 140m, "caixa-som", "Caixa de som portátil com conexão bluetooth e bateria recarregável. Acompanha cabo de carregamento."),
            (bruno.Id, "Esportes", "Mochila para trilhas", EstadoConservacao.MUITO_BOM, 110m, "mochila", "Mochila de 30 litros com alças acolchoadas e bolsos laterais. Limpa e pronta para a próxima caminhada."),
            (bruno.Id, "Livros", "Coleção de quadrinhos", EstadoConservacao.BOM, 95m, "quadrinhos", "Seis volumes de quadrinhos de aventura. Capas e páginas conservadas, com histórias completas para novas leituras."),
            (bruno.Id, "Esportes", "Skate street", EstadoConservacao.REGULAR, 210m, "skate", "Skate com rodas e rolamentos revisados. O shape tem marcas de uso e a lixa pode ser substituída."),
            (clara.Id, "Eletrônicos", "Controle de videogame sem fio", EstadoConservacao.MUITO_BOM, 190m, "controle", "Controle sem fio com botões e analógicos funcionando. Acompanha cabo USB; confira a compatibilidade com seu aparelho."),
            (clara.Id, "Casa e jardim", "Kit de jardinagem", EstadoConservacao.NOVO, 85m, "jardinagem", "Kit de jardinagem com pá, ancinho e borrifador. Nunca usado, ideal para cuidar de vasos e pequenas hortas."),
            (clara.Id, "Instrumentos musicais", "Ukulele soprano", EstadoConservacao.BOM, 240m, "ukulele", "Ukulele soprano com capa e afinador. Um instrumento leve para levar música aos encontros com amigos."),
            (clara.Id, "Livros", "Livro de receitas ilustrado", EstadoConservacao.NOVO, 55m, "receitas", "Livro de receitas com fotos e instruções para pratos do dia a dia. Sem anotações ou marcas de uso.")
        };
        var novosObjetos = 0;
        foreach (var (usuarioId, categoria, nome, estado, valor, arte, descricao) in exemplos)
            if (ativos.Contains(usuarioId) && existentes.Add((usuarioId, TextoNormalizado.De(nome))))
            {
                objetos.Cadastrar(usuarioId, mapa[TextoNormalizado.De(categoria)], nome, estado, descricao, valor, $"/arte/{arte}.svg");
                novosObjetos++;
            }
        var preferencias = new (int UsuarioId, string Categoria, string Descricao)[]
        {
            (ana.Id, "Instrumentos musicais", "violão"),
            (ana.Id, "Eletrônicos", "fone"),
            (ana.Id, "Casa e jardim", "planta"),
            (bruno.Id, "Eletrônicos", "câmera"),
            (bruno.Id, "Esportes", "bicicleta"),
            (bruno.Id, "Livros", "livro"),
            (clara.Id, "Instrumentos musicais", "teclado"),
            (clara.Id, "Livros", "quadrinhos"),
            (clara.Id, "Casa e jardim", "luminária")
        };
        var novosDesejos = 0;
        foreach (var conta in contas.Where(u => u.Ativo))
        {
            var atuais = desejos.Listar(conta.Id).Select(d => (d.Categoria?.Id, TextoNormalizado.De(d.Descricao))).ToHashSet();
            foreach (var (_, categoria, descricao) in preferencias.Where(p => p.UsuarioId == conta.Id))
            {
                var categoriaId = mapa[TextoNormalizado.De(categoria)];
                if (!atuais.Add(((int?)categoriaId, TextoNormalizado.De(descricao)))) continue;
                desejos.Adicionar(conta.Id, descricao, categoriaId);
                novosDesejos++;
            }
        }
        Console.WriteLine("Demonstração preparada: ana.demo@fecho.local, bruno.demo@fecho.local e clara.demo@fecho.local.");
        Console.WriteLine($"Adicionados {novosObjetos} objeto(s) e {novosDesejos} desejo(s). O conjunto completo tem 20 objetos e 9 desejos.");
        if (ativos.Count != contas.Length) Console.WriteLine("Contas de demonstração inativas foram preservadas e não receberam novos dados.");
        Console.WriteLine("Objetos existentes, fotos, avaliações, matches e trocas foram preservados.");
        Console.WriteLine("A senha é a fornecida em FECHO_DEMO_PASSWORD. Contas existentes não tiveram a senha alterada.");
    }
}
