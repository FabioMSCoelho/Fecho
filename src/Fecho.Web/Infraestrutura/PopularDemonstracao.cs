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
        var nomes = new[] { "Eletrônicos", "Instrumentos musicais", "Livros", "Esportes", "Casa e jardim" };
        var mapa = categorias.Listar().ToDictionary(c => TextoNormalizado.De(c.Nome), c => c.Id);
        foreach (var nome in nomes) if (!mapa.ContainsKey(TextoNormalizado.De(nome))) mapa[TextoNormalizado.De(nome)] = categorias.Cadastrar(nome).Id;
        var ana = repoUsuarios.BuscarPorEmail("ana.demo@fecho.local")
            ?? usuarios.Cadastrar("Ana Martins", "ana.demo@fecho.local", senha, "11900000001", "São Paulo");
        var bruno = repoUsuarios.BuscarPorEmail("bruno.demo@fecho.local")
            ?? usuarios.Cadastrar("Bruno Costa", "bruno.demo@fecho.local", senha, "13900000002", "Santos");
        var clara = repoUsuarios.BuscarPorEmail("clara.demo@fecho.local")
            ?? usuarios.Cadastrar("Clara Almeida", "clara.demo@fecho.local", senha, "19900000003", "Campinas");
        var existentes = objetos.Listar();
        var exemplos = new[]
        {
            (ana.Id, "Eletrônicos", "Câmera analógica vintage", EstadoConservacao.BOM, 450m, "camera", "Uma câmera analógica com marcas de boas histórias. Funcionando, acompanha alça."),
            (bruno.Id, "Instrumentos musicais", "Violão acústico", EstadoConservacao.MUITO_BOM, 380m, "violao", "Violão acústico de madeira, bem cuidado e com cordas novas. Pronto para novas músicas."),
            (clara.Id, "Esportes", "Bicicleta urbana", EstadoConservacao.BOM, 650m, "bicicleta", "Bicicleta para passeios pela cidade. Freios revisados e pneus em bom estado."),
            (ana.Id, "Livros", "Coleção de literatura brasileira", EstadoConservacao.MUITO_BOM, 120m, "livros", "Três livros de literatura brasileira. Páginas conservadas, sem anotações."),
            (bruno.Id, "Eletrônicos", "Fone de ouvido sem fio", EstadoConservacao.BOM, 180m, "fones", "Fone confortável para ouvir música ou estudar. Bateria e conexão Bluetooth funcionando."),
            (clara.Id, "Casa e jardim", "Vaso de cerâmica com planta", EstadoConservacao.MUITO_BOM, 65m, "planta", "Uma planta para trazer mais verde à sua casa. Vaso de cerâmica incluso."),
            (ana.Id, "Eletrônicos", "Câmera compacta de viagem", EstadoConservacao.BOM, 220m, "camera", "Câmera compacta, leve e funcionando. Ideal para registrar os próximos passeios."),
            (bruno.Id, "Livros", "Livros para uma tarde tranquila", EstadoConservacao.BOM, 80m, "livros", "Seleção de romances bem conservados. Busco novas leituras na troca.")
        };
        foreach (var (usuarioId, categoria, nome, estado, valor, arte, descricao) in exemplos)
            if (!existentes.Any(o => o.Proprietario.Id == usuarioId && o.Nome == nome))
                objetos.Cadastrar(usuarioId, mapa[TextoNormalizado.De(categoria)], nome, estado, descricao, valor, $"/arte/{arte}.svg");
        Console.WriteLine("Demonstração preparada: ana.demo@fecho.local, bruno.demo@fecho.local e clara.demo@fecho.local.");
        Console.WriteLine("A senha é a fornecida em FECHO_DEMO_PASSWORD. Contas existentes não tiveram a senha alterada.");
    }
}
