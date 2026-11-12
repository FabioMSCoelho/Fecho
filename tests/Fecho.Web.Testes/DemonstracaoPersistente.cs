using System.Diagnostics;
using Fecho.Core.Busca;
using Fecho.Core.Entidades;
using Fecho.Core.Enums;
using Fecho.Core.Servicos;
using Fecho.Infraestrutura.MySql;

internal static class DemonstracaoPersistente
{
    internal static async Task Executar()
    {
        if (Environment.GetEnvironmentVariable("FECHO_TEST_DEMO") != "1")
            throw new InvalidOperationException("Use FECHO_TEST_DEMO=1 e um banco de teste novo para esta verificação.");
        var raiz = ConfiguracaoLocal.LocalizarRaiz();
        var web = Path.Combine(raiz, "src", "Fecho.Web");
        var conexao = ConexaoMySql.DoAmbiente();
        var usuarios = new RepositorioUsuariosMySql(conexao);
        var objetos = new RepositorioObjetosMySql(conexao);
        var categorias = new RepositorioCategoriasMySql(conexao);
        var desejos = new RepositorioDesejosMySql(conexao, usuarios);
        var negociacao = new NegociacaoPersistenteService(new UnidadeNegociacaoMySql(conexao));
        var emails = new[] { "ana.demo@fecho.local", "bruno.demo@fecho.local", "clara.demo@fecho.local" };
        if (emails.Any(e => usuarios.BuscarPorEmail(e) is not null))
            throw new InvalidOperationException("Banco já contém contas de demonstração. Use um banco de teste novo; nenhum registro foi alterado.");

        var aprovados = 0;
        void Exigir(bool ok, string caso)
        {
            if (!ok) throw new InvalidOperationException("FALHOU: " + caso);
            Console.WriteLine("OK " + caso); aprovados++;
        }
        async Task<int> Popular(string? senha = null)
        {
            var host = Environment.ProcessPath ?? "dotnet";
            var inicio = new ProcessStartInfo(host)
            {
                WorkingDirectory = web, UseShellExecute = false,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            inicio.ArgumentList.Add(Path.Combine(web, "bin", "Release", "net10.0", "Fecho.Web.dll"));
            inicio.ArgumentList.Add("--seed-demo");
            if (senha is not null) inicio.Environment["FECHO_DEMO_PASSWORD"] = senha;
            using var processo = Process.Start(inicio) ?? throw new InvalidOperationException("Não foi possível iniciar a preparação.");
            var saida = processo.StandardOutput.ReadToEndAsync();
            var erro = processo.StandardError.ReadToEndAsync();
            await processo.WaitForExitAsync();
            var textoErro = await erro;
            _ = await saida;
            if (processo.ExitCode != 0 && senha is null)
                throw new InvalidOperationException("Preparação falhou: " + textoErro);
            return processo.ExitCode;
        }

        await Popular();
        var contas = emails.Select(e => usuarios.BuscarPorEmail(e)!).ToArray();
        var ids = contas.Select(u => u.Id).ToHashSet();
        List<Objeto> Exemplos() => objetos.Listar().Where(o => ids.Contains(o.Proprietario.Id)).ToList();
        var todos = Exemplos();
        Exigir(todos.Count == 20 && contas.Select(u => todos.Count(o => o.Proprietario.Id == u.Id)).SequenceEqual([7, 7, 6]),
            "Banco novo recebe vinte objetos distribuídos entre as três contas");
        Exigir(contas.All(u => desejos.Listar(u.Id).Count == 3), "Cada conta recebe três desejos de demonstração");
        Exigir(todos.All(o => o.Imagem is not null && File.Exists(Path.Combine(web, "wwwroot", o.Imagem.TrimStart('/')))),
            "Todos os exemplos possuem uma ilustração local existente");
        Exigir(contas.All(u => negociacao.ListarInteresses(u.Id).Count == 0 && negociacao.ListarMatches(u.Id).Count == 0),
            "Preparação inicial deixa avaliações e matches para a demonstração ao vivo");

        // Reproduz a base anterior, com oito objetos, e alterações feitas por quem a usou.
        foreach (var extra in todos.OrderBy(o => o.Id).Skip(8)) objetos.Excluir(extra.Id);
        foreach (var conta in contas)
            foreach (var desejo in desejos.Listar(conta.Id)) desejos.Excluir(desejo.Id, conta.Id);
        var camera = todos.Single(o => o.Nome == "Câmera analógica vintage");
        var violao = todos.Single(o => o.Nome == "Violão acústico");
        var editada = new Objeto(camera.Id, camera.Proprietario, camera.Categoria, camera.Nome,
            EstadoConservacao.REGULAR, "Descrição personalizada preservada", 499m, "/imagens/foto-preservada.png",
            camera.DataCadastro, camera.Status);
        objetos.Atualizar(editada);
        negociacao.Avaliar(contas[0].Id, violao.Id, TipoInteresse.LIKE);
        negociacao.Avaliar(contas[1].Id, camera.Id, TipoInteresse.LIKE);
        var match = negociacao.ListarMatches(contas[0].Id).Single();
        negociacao.Concluir(contas[0].Id, match.Registro.Id, "Histórico anterior à ampliação");
        var credenciais = contas.ToDictionary(u => u.Id, u => u.IdentificadorCredencial());
        var usuarioExtra = new UsuarioService(usuarios).Cadastrar("Outro usuário", $"sentinela-{Guid.NewGuid():N}@example.com",
            "SenhaFicticia@2026", "11900000009", "Curitiba");
        var objetoExtra = new ObjetoService(objetos, usuarios, categorias).Cadastrar(usuarioExtra.Id, camera.Categoria.Id,
            "Objeto de outro usuário", EstadoConservacao.BOM, imagem: "/imagens/outra-foto.png");

        await Popular();
        Exigir(Exemplos().Count == 20 && contas.All(u => desejos.Listar(u.Id).Count == 3),
            "Ampliação da base anterior acrescenta somente os doze objetos e nove desejos faltantes");
        Exigir(contas.All(u => usuarios.Buscar(u.Id)!.IdentificadorCredencial() == credenciais[u.Id]),
            "Contas existentes mantêm suas credenciais");
        var preservada = objetos.Buscar(camera.Id)!;
        Exigir(preservada.Descricao == editada.Descricao && preservada.ValorEstimado == 499m
            && preservada.Imagem == editada.Imagem && preservada.EstadoConservacao == EstadoConservacao.REGULAR
            && preservada.DataCadastro == camera.DataCadastro && preservada.Status == StatusObjeto.TROCADO,
            "Ampliação preserva edição, foto enviada, data e status de um objeto antigo");
        Exigir(negociacao.ListarInteresses(contas[0].Id).GetValueOrDefault(violao.Id) == TipoInteresse.LIKE
            && negociacao.ListarMatches(contas[0].Id).Single().Registro.Status == StatusMatch.CONCLUIDO,
            "Avaliações e histórico de troca permanecem após a ampliação");
        Exigir(objetos.Buscar(objetoExtra.Id)?.Imagem == objetoExtra.Imagem && usuarios.Buscar(usuarioExtra.Id)?.Email == usuarioExtra.Email,
            "Registros de usuários fora das contas de exemplo permanecem intactos");
        Exigir(contas.All(u => new BuscaObjetos().Executar(objetos.Listar(), new FiltroObjetos(ParaMim: true),
            u.Id, desejos.Listar(u.Id)).Total > 0), "Desejos de todas as contas encontram objetos compatíveis");

        var idsAntes = Exemplos().Select(o => o.Id).ToHashSet();
        var desejosAntes = contas.SelectMany(u => desejos.Listar(u.Id)).Select(d => d.Id).ToHashSet();
        await Popular();
        Exigir(idsAntes.SetEquals(Exemplos().Select(o => o.Id))
            && desejosAntes.SetEquals(contas.SelectMany(u => desejos.Listar(u.Id)).Select(d => d.Id)),
            "Repetir a preparação não duplica objetos nem desejos");
        Exigir(await Popular("curta") == 1 && idsAntes.SetEquals(Exemplos().Select(o => o.Id)),
            "Senha de demonstração inválida falha sem inserir novos registros");

        var clara = contas[2];
        var perfil = new UsuarioService(usuarios).ConsultarPerfil(clara.Id);
        usuarios.Atualizar(clara.Id, clara.Nome, clara.Email, perfil.Telefone, clara.Cidade, clara.Foto, clara.Descricao, false);
        var ukulele = Exemplos().Single(o => o.Nome == "Ukulele soprano");
        objetos.Excluir(ukulele.Id);
        await Popular();
        Exigir(!usuarios.Buscar(clara.Id)!.Ativo && objetos.Buscar(ukulele.Id) is null,
            "Conta inativa não é reativada nem recebe objetos novos");
        Console.WriteLine($"{aprovados}/{aprovados} verificações dos dados de demonstração aprovadas.");
    }
}
