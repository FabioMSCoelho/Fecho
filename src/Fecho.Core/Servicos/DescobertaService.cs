using Fecho.Core.Compatibilidade;
using Fecho.Core.Contratos;
using Fecho.Core.Entidades;
using Fecho.Core.Enums;

namespace Fecho.Core.Servicos;

public sealed class DescobertaService
{
    private readonly IRepositorio<Usuario> _usuarios;
    private readonly IRepositorio<Objeto> _objetos;
    private readonly IRepositorio<Desejo> _desejos;
    private readonly IRepositorio<Interesse> _interesses;

    public DescobertaService(IRepositorio<Usuario> usuarios, IRepositorio<Objeto> objetos,
        IRepositorio<Desejo> desejos, IRepositorio<Interesse> interesses)
    {
        _usuarios = usuarios;
        _objetos = objetos;
        _desejos = desejos;
        _interesses = interesses;
    }

    public IReadOnlyList<Objeto> Descobrir(int usuarioId, CriterioCompatibilidade criterio)
    {
        ArgumentNullException.ThrowIfNull(criterio);
        var usuario = _usuarios.Obter(usuarioId);
        if (!usuario.Ativo) throw new InvalidOperationException("Usuário inativo.");

        HashSet<int> avaliados = _interesses.Listar()
            .Where(i => i.Usuario.Equals(usuario)).Select(i => i.Objeto.Id).ToHashSet();
        var candidatos = _objetos.Listar().Where(o =>
            !o.Proprietario.Equals(usuario) && o.Proprietario.Ativo &&
            o.Status == StatusObjeto.DISPONIVEL && !avaliados.Contains(o.Id)).ToList();
        var desejos = _desejos.Listar().Where(d => d.Usuario.Equals(usuario)).ToList();

        // Um objeto pode satisfazer vários desejos. Set impede cards repetidos.
        HashSet<Objeto> unicos = new();
        List<Objeto> cards = new();
        foreach (var desejo in desejos)
            foreach (var objeto in candidatos)
                if (criterio.Compativel(objeto, desejo) && unicos.Add(objeto))
                    cards.Add(objeto);

        // List define a sequência e permite o acesso cards[0], usado pela interface de cards.
        return cards.OrderBy(o => o.DataCadastro).ThenBy(o => o.Id).ToList().AsReadOnly();
    }
}
