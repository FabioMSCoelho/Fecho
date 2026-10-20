using Fecho.Core.Entidades;

namespace Fecho.Core.Contratos;

/// <summary>Porta de armazenamento usada pelos serviços, sem dependência de MySQL.</summary>
public interface IRepositorio<T> where T : EntidadeBase
{
    int ProximoId();
    void Adicionar(T entidade);
    T? Buscar(int id);
    T Obter(int id);
    IReadOnlyList<T> Listar();
}
