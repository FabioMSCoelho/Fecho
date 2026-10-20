using Fecho.Core.Contratos;
using Fecho.Core.Entidades;
using Fecho.Core.Enums;

namespace Fecho.Core.Servicos;

/// <summary>Orquestra reciprocidade, autorização de contato e conclusão da troca.</summary>
public sealed class TrocaService
{
    private readonly IRepositorio<Usuario> _usuarios;
    private readonly IRepositorio<Objeto> _objetos;
    private readonly IRepositorio<Interesse> _interesses;
    private readonly IRepositorio<Match> _matches;
    private readonly IRepositorio<Troca> _trocas;

    public TrocaService(IRepositorio<Usuario> usuarios, IRepositorio<Objeto> objetos,
        IRepositorio<Interesse> interesses, IRepositorio<Match> matches, IRepositorio<Troca> trocas)
    {
        _usuarios = usuarios;
        _objetos = objetos;
        _interesses = interesses;
        _matches = matches;
        _trocas = trocas;
    }

    public ResultadoInteresse RegistrarInteresse(int usuarioId, int objetoId, TipoInteresse tipo)
    {
        Validacao.EnumValido(tipo);
        var usuario = ExigirAtivo(usuarioId);
        var objeto = _objetos.Obter(objetoId);
        if (objeto.Proprietario.Equals(usuario))
            throw new InvalidOperationException("Você não pode avaliar seu próprio objeto.");
        ExigirAtivo(objeto.Proprietario.Id);
        if (objeto.Status != StatusObjeto.DISPONIVEL)
            throw new InvalidOperationException("Objeto indisponível.");
        if (!_objetos.Listar().Any(o => o.Proprietario.Equals(usuario)
                && o.Status == StatusObjeto.DISPONIVEL))
            throw new InvalidOperationException("Cadastre pelo menos um objeto disponível para participar.");

        var interesse = _interesses.Listar().FirstOrDefault(i =>
            i.Usuario.Equals(usuario) && i.Objeto.Equals(objeto));
        if (interesse is null)
        {
            interesse = new Interesse(_interesses.ProximoId(), usuario, objeto, tipo);
            _interesses.Adicionar(interesse);
        }
        else
            interesse.Atualizar(tipo); // Um registro por usuário/objeto, conforme UNIQUE no SQL.

        if (tipo == TipoInteresse.DISLIKE)
        {
            foreach (var match in _matches.Listar().Where(m => m.Status == StatusMatch.ATIVO
                         && m.Envolve(usuario) && m.Envolve(objeto)))
                match.Cancelar();
            return new ResultadoInteresse(interesse, Array.Empty<Match>());
        }

        // Chave normalizada evita repetir o par quando os usuários aparecem na ordem inversa.
        HashSet<(int, int)> paresAtivos = _matches.Listar()
            .Where(m => m.Status == StatusMatch.ATIVO)
            .Select(m => ChavePar(m.Objeto1, m.Objeto2)).ToHashSet();
        List<Match> novos = new();
        var reciprocidades = _interesses.Listar().Where(i =>
            i.Tipo == TipoInteresse.LIKE && i.Usuario.Equals(objeto.Proprietario)
            && i.Objeto.Proprietario.Equals(usuario)
            && i.Objeto.Status == StatusObjeto.DISPONIVEL);
        foreach (var reciproco in reciprocidades)
        {
            if (!paresAtivos.Add(ChavePar(objeto, reciproco.Objeto))) continue;
            var match = new Match(_matches.ProximoId(), reciproco.Objeto, objeto);
            _matches.Adicionar(match);
            novos.Add(match);
        }
        return new ResultadoInteresse(interesse, novos.AsReadOnly());
    }

    public IReadOnlyList<Match> ListarMatches(int usuarioId)
    {
        var usuario = ExigirAtivo(usuarioId);
        return _matches.Listar().Where(m => m.Envolve(usuario))
            .OrderByDescending(m => m.DataMatch).ThenByDescending(m => m.Id).ToList().AsReadOnly();
    }

    public ContatoLiberado ObterContato(int solicitanteId, int matchId)
    {
        var match = ExigirParticipante(solicitanteId, matchId);
        if (match.Status == StatusMatch.CANCELADO)
            throw new InvalidOperationException("Match cancelado não libera contato.");
        var outro = match.Usuario1.Id == solicitanteId ? match.Usuario2 : match.Usuario1;
        ExigirAtivo(outro.Id);
        return new ContatoLiberado(outro.Id, outro.Nome, outro.Telefone);
    }

    public void CancelarMatch(int solicitanteId, int matchId) =>
        ExigirParticipante(solicitanteId, matchId).Cancelar();

    public Troca ConcluirTroca(int solicitanteId, int matchId, string? observacao = null)
    {
        var match = ExigirParticipante(solicitanteId, matchId);
        if (match.Status != StatusMatch.ATIVO)
            throw new InvalidOperationException("Somente um match ativo pode concluir uma troca.");
        ExigirAtivo(match.Usuario1.Id);
        ExigirAtivo(match.Usuario2.Id);
        if (match.Objeto1.Status != StatusObjeto.DISPONIVEL || match.Objeto2.Status != StatusObjeto.DISPONIVEL)
            throw new InvalidOperationException("Um dos objetos já está indisponível.");

        // Valida observação e unicidade antes de alterar os estados dos objetos.
        var troca = new Troca(_trocas.ProximoId(), match, observacao);
        _trocas.Adicionar(troca);
        match.Objeto1.MarcarTrocado();
        match.Objeto2.MarcarTrocado();
        match.Concluir();

        // Um objeto não pode ser entregue em duas trocas. Revoga propostas concorrentes.
        foreach (var outro in _matches.Listar().Where(m => m.Status == StatusMatch.ATIVO
                     && (m.Envolve(match.Objeto1) || m.Envolve(match.Objeto2))))
            outro.Cancelar();
        return troca;
    }

    private Match ExigirParticipante(int usuarioId, int matchId)
    {
        var usuario = ExigirAtivo(usuarioId);
        var match = _matches.Obter(matchId);
        if (!match.Envolve(usuario))
            throw new UnauthorizedAccessException("Somente os participantes podem acessar este match.");
        return match;
    }

    private Usuario ExigirAtivo(int usuarioId)
    {
        var usuario = _usuarios.Obter(usuarioId);
        if (!usuario.Ativo) throw new InvalidOperationException("Usuário inativo.");
        return usuario;
    }

    private static (int, int) ChavePar(Objeto a, Objeto b) =>
        a.Id < b.Id ? (a.Id, b.Id) : (b.Id, a.Id);
}
