using Fecho.Core.Entidades;
using Fecho.Infraestrutura.MySql;

namespace Fecho.ConsoleApp;

internal static class MenuColecao
{
    internal static void Executar<T>(string titulo, Func<T> cadastrar, Func<IReadOnlyList<T>> listar,
        Func<int, T?> buscar, Func<T, bool> atualizar, Func<int, bool> excluir, Action<T> mostrar,
        Action? alterarSenha = null) where T : EntidadeBase
    {
        Console.WriteLine($"\n{titulo} — MySQL");
        while (true)
        {
            Console.WriteLine("\n1 Cadastrar | 2 Listar | 3 Buscar por ID | 4 Atualizar | 5 Excluir"
                + (alterarSenha is null ? "" : " | 6 Alterar senha") + " | 0 Voltar");
            Console.Write("Opção: ");
            var opcao = Console.ReadLine();
            if (opcao is null or "0") return;
            try
            {
                switch (opcao)
                {
                    case "1": mostrar(cadastrar()); break;
                    case "2":
                        var registros = listar();
                        if (registros.Count == 0) Console.WriteLine("Nenhum registro cadastrado.");
                        foreach (var registro in registros) mostrar(registro);
                        break;
                    case "3" or "4" or "5":
                        var atual = buscar(EntradaConsole.LerId());
                        if (atual is null) { Console.WriteLine("Registro não encontrado."); break; }
                        mostrar(atual);
                        if (opcao == "4")
                        {
                            Console.WriteLine("Informe os novos valores; campos opcionais vazios serão removidos.");
                            Console.WriteLine(atualizar(atual) ? "Registro atualizado." : "Registro não encontrado.");
                        }
                        if (opcao == "5")
                        {
                            if (EntradaConsole.Ler("Excluir definitivamente? Digite SIM: ") != "SIM")
                            { Console.WriteLine("Exclusão cancelada."); break; }
                            Console.WriteLine(excluir(atual.Id) ? "Registro excluído." : "Registro não encontrado.");
                        }
                        break;
                    case "6" when alterarSenha is not null: alterarSenha(); break;
                    default: Console.WriteLine("Opção inválida."); break;
                }
            }
            catch (ErroPersistencia ex) { Console.Error.WriteLine(ex.Message); }
            catch (ArgumentException ex) { Console.Error.WriteLine($"Dados inválidos: {ex.Message}"); }
            catch (InvalidOperationException ex) { Console.Error.WriteLine(ex.Message); }
            catch (EndOfStreamException) { return; }
        }
    }
}
