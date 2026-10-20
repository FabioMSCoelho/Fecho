using Fecho.Core.Contratos;
using Fecho.Core.Entidades;
using Fecho.Core.Enums;
using Fecho.Core.Seguranca;

namespace Fecho.Core.Servicos;

public sealed class CadastroService
{
    private readonly IRepositorio<Usuario> _usuarios;
    private readonly IRepositorio<Categoria> _categorias;
    private readonly IRepositorio<Objeto> _objetos;
    private readonly IRepositorio<Desejo> _desejos;

    public CadastroService(IRepositorio<Usuario> usuarios, IRepositorio<Categoria> categorias,
        IRepositorio<Objeto> objetos, IRepositorio<Desejo> desejos)
    {
        _usuarios = usuarios;
        _categorias = categorias;
        _objetos = objetos;
        _desejos = desejos;
    }

    public Usuario CadastrarUsuario(string nome, string email, string senha, string telefone,
        string? cidade = null, string? foto = null, string? descricao = null)
    {
        var usuario = new Usuario(_usuarios.ProximoId(), nome, email,
            SenhaProtegida.Criar(senha), telefone, cidade, foto, descricao);
        _usuarios.Adicionar(usuario);
        return usuario;
    }

    public Usuario Autenticar(string email, string senha)
    {
        var usuario = _usuarios.Listar().FirstOrDefault(u =>
            u.Email.Equals(email?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (usuario is null || !usuario.Ativo || !usuario.Senha.Confere(senha))
            throw new UnauthorizedAccessException("E-mail ou senha inválidos.");
        return usuario;
    }

    public Categoria CadastrarCategoria(string nome, string? descricao = null)
    {
        var categoria = new Categoria(_categorias.ProximoId(), nome, descricao);
        _categorias.Adicionar(categoria);
        return categoria;
    }

    public Objeto CadastrarObjeto(int usuarioId, int categoriaId, string nome,
        EstadoConservacao estado, string? descricao = null, decimal? valorEstimado = null,
        string? imagem = null)
    {
        var objeto = new Objeto(_objetos.ProximoId(), ExigirAtivo(usuarioId),
            _categorias.Obter(categoriaId), nome, estado, descricao, valorEstimado, imagem);
        _objetos.Adicionar(objeto);
        return objeto;
    }

    public Desejo AdicionarDesejo(int usuarioId, string descricao, int? categoriaId = null)
    {
        var desejo = new Desejo(_desejos.ProximoId(), ExigirAtivo(usuarioId), descricao,
            categoriaId.HasValue ? _categorias.Obter(categoriaId.Value) : null);
        _desejos.Adicionar(desejo);
        return desejo;
    }

    private Usuario ExigirAtivo(int usuarioId)
    {
        var usuario = _usuarios.Obter(usuarioId);
        if (!usuario.Ativo) throw new InvalidOperationException("Usuário inativo.");
        return usuario;
    }
}
