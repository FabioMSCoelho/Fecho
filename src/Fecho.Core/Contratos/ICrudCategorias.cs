using Fecho.Core.Entidades;

namespace Fecho.Core.Contratos;

/// <summary>CRUD persistente: identificadores de novos registros são gerados pelo banco.</summary>
public interface ICrudCategorias
{
    Categoria Inserir(string nome, string? descricao);
    Categoria? Buscar(int id);
    IReadOnlyList<Categoria> Listar();
    bool Atualizar(int id, string nome, string? descricao);
    bool Excluir(int id);
}
