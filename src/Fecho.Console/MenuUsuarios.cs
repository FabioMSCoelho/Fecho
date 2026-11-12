using Fecho.Core.Entidades;
using Fecho.Core.Servicos;

namespace Fecho.ConsoleApp;

internal static class MenuUsuarios
{
    internal static void Executar(UsuarioService servico) => MenuColecao.Executar("USUÁRIOS",
        () => servico.Cadastrar(EntradaConsole.Ler("Nome: "), EntradaConsole.Ler("E-mail: "),
            EntradaConsole.LerSenha("Senha (8 a 128 caracteres, sem eco no terminal): "),
            EntradaConsole.Ler("Telefone: "), EntradaConsole.Ler("Cidade (opcional): "),
            EntradaConsole.Ler("Foto, caminho ou URL (opcional): "), EntradaConsole.Ler("Descrição (opcional): ")),
        servico.Listar, servico.Buscar,
        atual => servico.Atualizar(atual.Id, EntradaConsole.Ler("Novo nome: "), EntradaConsole.Ler("Novo e-mail: "),
            EntradaConsole.Ler("Novo telefone: "), EntradaConsole.Ler("Nova cidade (opcional): "),
            EntradaConsole.Ler("Nova foto (opcional): "), EntradaConsole.Ler("Nova descrição (opcional): "), EntradaConsole.LerAtivo()),
        servico.Excluir, Mostrar,
        () => Console.WriteLine(servico.AlterarSenha(EntradaConsole.LerId(), EntradaConsole.LerSenha("Nova senha: "))
            ? "Senha alterada." : "Registro não encontrado."));

    // O terminal exibe o cadastro, sem revelar hash de senha ou o telefone reservado ao fluxo de contato.
    private static void Mostrar(Usuario usuario) => Console.WriteLine(
        $"{usuario.Id} | {usuario.Nome} | {usuario.Email} | Ativo: {(usuario.Ativo ? "SIM" : "NÃO")}\n"
        + $"Cidade: {usuario.Cidade ?? "(sem cidade)"} | Foto: {usuario.Foto ?? "(sem foto)"}\n"
        + $"Descrição: {usuario.Descricao ?? "(sem descrição)"} | Cadastro (UTC): {usuario.DataCadastro:yyyy-MM-dd HH:mm:ss}");
}
