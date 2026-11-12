using Fecho.Core.Entidades;

namespace Fecho.Core.Contratos;

public interface ICrudObjetos
{
    Objeto Inserir(Objeto dados);
    Objeto? Buscar(int id);
    IReadOnlyList<Objeto> Listar();
    // Edita os dados descritivos; proprietário, status e data de cadastro são preservados.
    bool Atualizar(Objeto dados);
    bool Excluir(int id);
}
