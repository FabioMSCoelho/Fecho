using Fecho.Core.Entidades;
using Fecho.Core.Enums;
using Fecho.Core.Servicos;

namespace Fecho.ConsoleApp;

internal static class MenuObjetos
{
    internal static void Executar(ObjetoService servico) => MenuColecao.Executar("OBJETOS",
        () => servico.Cadastrar(EntradaConsole.LerId("ID do proprietário: "), EntradaConsole.LerId("ID da categoria: "),
            EntradaConsole.Ler("Nome: "), EntradaConsole.LerEnum<EstadoConservacao>("Estado de conservação: "),
            EntradaConsole.Ler("Descrição (opcional): "), EntradaConsole.LerValor(), EntradaConsole.Ler("Imagem (opcional): ")),
        servico.Listar, servico.Buscar,
        atual => servico.Atualizar(atual.Id, EntradaConsole.LerId("ID da categoria: "), EntradaConsole.Ler("Novo nome: "),
            EntradaConsole.LerEnum<EstadoConservacao>("Estado de conservação: "), EntradaConsole.Ler("Nova descrição (opcional): "),
            EntradaConsole.LerValor(), EntradaConsole.Ler("Nova imagem (opcional): ")),
        servico.Excluir, Mostrar);

    private static void Mostrar(Objeto objeto) => Console.WriteLine(
        $"{objeto.Id} | {objeto.Nome} | Proprietário: #{objeto.Proprietario.Id} {objeto.Proprietario.Nome}\n"
        + $"Categoria: #{objeto.Categoria.Id} {objeto.Categoria.Nome} | Estado: {objeto.EstadoConservacao} | Status: {objeto.Status}\n"
        + $"Valor estimado: {objeto.ValorEstimado?.ToString("F2") ?? "(sem valor)"} | Imagem: {objeto.Imagem ?? "(sem imagem)"}\n"
        + $"Descrição: {objeto.Descricao ?? "(sem descrição)"} | Cadastro (UTC): {objeto.DataCadastro:yyyy-MM-dd HH:mm:ss}");
}
