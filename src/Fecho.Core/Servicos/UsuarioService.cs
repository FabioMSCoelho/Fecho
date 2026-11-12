using Fecho.Core.Contratos;
using Fecho.Core.Entidades;
using Fecho.Core.Seguranca;

namespace Fecho.Core.Servicos;

public sealed class UsuarioService(ICrudUsuarios repositorio)
{
    public Usuario Cadastrar(string nome, string email, string senha, string telefone,
        string? cidade = null, string? foto = null, string? descricao = null)
    {
        var dados = new Usuario(1, nome, email, SenhaProtegida.Criar(senha), telefone, cidade, foto, descricao);
        return repositorio.Inserir(dados.Nome, dados.Email, dados.Senha, dados.Telefone,
            dados.Cidade, dados.Foto, dados.Descricao);
    }

    public IReadOnlyList<Usuario> Listar() => repositorio.Listar();
    public Usuario Autenticar(string email, string senha)
    {
        var usuario = repositorio.BuscarPorEmail(email.Trim().ToLowerInvariant());
        if (usuario is null || !usuario.Ativo || !usuario.ConferirSenha(senha))
            throw new UnauthorizedAccessException("E-mail ou senha inválidos.");
        return usuario;
    }

    public PerfilUsuario ConsultarPerfil(int id)
    {
        var usuario = Buscar(id) ?? throw new KeyNotFoundException("Usuário não encontrado.");
        return new PerfilUsuario(usuario, usuario.Telefone);
    }
    public Usuario? Buscar(int id) { ValidarId(id); return repositorio.Buscar(id); }
    public bool Excluir(int id) { ValidarId(id); return repositorio.Excluir(id); }

    public bool Atualizar(int id, string nome, string email, string telefone,
        string? cidade = null, string? foto = null, string? descricao = null, bool ativo = true)
    {
        var atual = Buscar(id);
        if (atual is null) return false;
        var dados = new Usuario(id, nome, email, atual.Senha, telefone, cidade, foto, descricao,
            atual.DataCadastro, ativo);
        return repositorio.Atualizar(id, dados.Nome, dados.Email, dados.Telefone,
            dados.Cidade, dados.Foto, dados.Descricao, dados.Ativo);
    }

    public bool AlterarSenha(int id, string novaSenha)
    {
        ValidarId(id);
        return repositorio.AlterarSenha(id, SenhaProtegida.Criar(novaSenha));
    }

    private static void ValidarId(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id), "Id deve ser positivo.");
    }
}

public sealed record PerfilUsuario(Usuario Usuario, string Telefone);
