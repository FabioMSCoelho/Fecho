using Fecho.Core.Entidades;

namespace Fecho.Core.Contratos;

public interface IRepositorioDesejos
{
    IReadOnlyList<Desejo> Listar(int usuarioId);
    int Adicionar(Desejo desejo);
    bool Excluir(int id, int usuarioId);
}
