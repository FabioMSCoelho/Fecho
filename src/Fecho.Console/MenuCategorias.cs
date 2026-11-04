using Fecho.Core.Entidades;
using Fecho.Core.Servicos;
using Fecho.Infraestrutura.MySql;

namespace Fecho.ConsoleApp;

public static class MenuCategorias
{
    public static int Executar()
    {
        CategoriaService servico;
        try
        {
            var conexao = ConexaoMySql.DoAmbiente();
            conexao.Verificar();
            servico = new CategoriaService(new RepositorioCategoriasMySql(conexao));
        }
        catch (ErroPersistencia ex) { Console.Error.WriteLine(ex.Message); return 1; }

        Console.WriteLine("FECHÔ — CRUD de categorias conectado ao MySQL");
        while (true)
        {
            Console.WriteLine("\n1 Cadastrar | 2 Listar | 3 Buscar por ID | 4 Atualizar | 5 Excluir | 0 Sair");
            Console.Write("Opção: ");
            var opcao = Console.ReadLine();
            if (opcao is null or "0") return 0;
            try
            {
                switch (opcao)
                {
                    case "1": Mostrar(servico.Cadastrar(Ler("Nome: "), Ler("Descrição (opcional): "))); break;
                    case "2":
                        var lista = servico.Listar();
                        if (lista.Count == 0) Console.WriteLine("Nenhuma categoria cadastrada.");
                        foreach (var categoria in lista) Mostrar(categoria);
                        break;
                    case "3": Mostrar(servico.Buscar(LerId())); break;
                    case "4":
                        var id = LerId();
                        var atual = servico.Buscar(id);
                        if (atual is null) { Console.WriteLine("Categoria não encontrada."); break; }
                        Mostrar(atual);
                        Console.WriteLine("Informe os novos valores; descrição vazia remove a descrição.");
                        Console.WriteLine(servico.Atualizar(id, Ler("Novo nome: "), Ler("Nova descrição: "))
                            ? "Categoria atualizada." : "Categoria não encontrada.");
                        break;
                    case "5":
                        var excluirId = LerId();
                        var excluir = servico.Buscar(excluirId);
                        if (excluir is null) { Console.WriteLine("Categoria não encontrada."); break; }
                        Mostrar(excluir);
                        if (Ler("Excluir definitivamente? Digite SIM: ") != "SIM")
                        { Console.WriteLine("Exclusão cancelada."); break; }
                        Console.WriteLine(servico.Excluir(excluirId) ? "Categoria excluída." : "Categoria não encontrada.");
                        break;
                    default: Console.WriteLine("Opção inválida."); break;
                }
            }
            catch (ErroPersistencia ex) { Console.Error.WriteLine(ex.Message); }
            catch (ArgumentException ex) { Console.Error.WriteLine($"Dados inválidos: {ex.Message}"); }
            catch (EndOfStreamException) { return 0; }
        }
    }

    private static string Ler(string mensagem)
    {
        Console.Write(mensagem);
        return Console.ReadLine() ?? throw new EndOfStreamException();
    }

    private static int LerId() => int.TryParse(Ler("ID: "), out var id) && id > 0
        ? id : throw new ArgumentException("Informe um ID inteiro positivo.");

    private static void Mostrar(Categoria? categoria) => Console.WriteLine(categoria is null
        ? "Categoria não encontrada." : $"{categoria.Id} | {categoria.Nome} | {categoria.Descricao ?? "(sem descrição)"}");
}
