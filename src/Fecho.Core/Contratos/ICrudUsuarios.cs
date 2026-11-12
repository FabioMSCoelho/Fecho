using Fecho.Core.Entidades;
using Fecho.Core.Seguranca;

namespace Fecho.Core.Contratos;

public interface ICrudUsuarios
{
    Usuario Inserir(string nome, string email, SenhaProtegida senha, string telefone,
        string? cidade, string? foto, string? descricao);
    Usuario? Buscar(int id);
    Usuario? BuscarPorEmail(string email);
    IReadOnlyList<Usuario> Listar();
    bool Atualizar(int id, string nome, string email, string telefone,
        string? cidade, string? foto, string? descricao, bool ativo);
    bool AlterarSenha(int id, SenhaProtegida senha);
    bool Excluir(int id);
}
