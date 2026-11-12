using Fecho.Core.Contratos;
using Fecho.Core.Entidades;
using Fecho.Core.Enums;

namespace Fecho.Core.Servicos;

public sealed record CartaoMatch(MatchRegistrado Registro, Objeto SeuObjeto, Objeto OutroObjeto);

/// <summary>Regras da negociação persistente; cada alteração é confirmada ou desfeita por inteiro.</summary>
public sealed class NegociacaoPersistenteService(IUnidadeNegociacao unidade)
{
    public int Avaliar(int usuarioId, int objetoId, TipoInteresse tipo) => unidade.Alterar(contexto =>
    {
        Validacao.EnumValido(tipo);
        var usuario = ExigirAtivo(contexto, usuarioId);
        var objeto = contexto.Objetos.GetValueOrDefault(objetoId) ?? throw new KeyNotFoundException("Objeto não encontrado.");
        RegrasNegociacao.ValidarAvaliacao(usuario, objeto, contexto.Objetos.Values.Any(o =>
            o.Proprietario.Id == usuarioId && o.Status == StatusObjeto.DISPONIVEL));
        contexto.GravarInteresse(usuarioId, objetoId, tipo);
        if (tipo == TipoInteresse.DISLIKE)
        {
            foreach (var m in contexto.Matches.Where(m => m.Status == StatusMatch.ATIVO
                && (m.Usuario1Id == usuarioId || m.Usuario2Id == usuarioId)
                && (m.Objeto1Id == objetoId || m.Objeto2Id == objetoId))) contexto.AlterarMatch(m.Id, StatusMatch.CANCELADO);
            return 0;
        }
        HashSet<(int, int)> pares = contexto.Matches.Where(m => m.Status == StatusMatch.ATIVO)
            .Select(m => Par(m.Objeto1Id, m.Objeto2Id)).ToHashSet();
        var novos = 0;
        foreach (var interesse in contexto.Interesses.Where(i => i.UsuarioId == objeto.Proprietario.Id && i.Tipo == TipoInteresse.LIKE))
        {
            var reciproco = contexto.Objetos.GetValueOrDefault(interesse.ObjetoId);
            if (reciproco is null || reciproco.Proprietario.Id != usuarioId || reciproco.Status != StatusObjeto.DISPONIVEL
                || !pares.Add(Par(reciproco.Id, objetoId))) continue;
            contexto.CriarMatch(reciproco, objeto); novos++;
        }
        return novos;
    });

    public IReadOnlyDictionary<int, TipoInteresse> ListarInteresses(int usuarioId) => unidade.Ler(c =>
    {
        ExigirAtivo(c, usuarioId);
        return (IReadOnlyDictionary<int, TipoInteresse>)c.Interesses.Where(i => i.UsuarioId == usuarioId)
            .ToDictionary(i => i.ObjetoId, i => i.Tipo);
    });

    public IReadOnlyList<CartaoMatch> ListarMatches(int usuarioId) => unidade.Ler(c =>
    {
        ExigirAtivo(c, usuarioId);
        return (IReadOnlyList<CartaoMatch>)c.Matches.Where(m => m.Usuario1Id == usuarioId || m.Usuario2Id == usuarioId)
            .OrderByDescending(m => m.DataMatch).ThenByDescending(m => m.Id)
            .Select(m => m.Usuario1Id == usuarioId
                ? new CartaoMatch(m, c.Objetos[m.Objeto1Id], c.Objetos[m.Objeto2Id])
                : new CartaoMatch(m, c.Objetos[m.Objeto2Id], c.Objetos[m.Objeto1Id])).ToList().AsReadOnly();
    });

    public ContatoLiberado ObterContato(int usuarioId, int matchId) => unidade.Ler(c =>
    {
        var match = ExigirParticipante(c, usuarioId, matchId);
        if (match.Status == StatusMatch.CANCELADO) throw new InvalidOperationException("Match cancelado não libera contato.");
        var outro = ExigirAtivo(c, match.Usuario1Id == usuarioId ? match.Usuario2Id : match.Usuario1Id);
        return new ContatoLiberado(outro.Id, outro.Nome, c.ObterTelefone(outro.Id));
    });

    public void Cancelar(int usuarioId, int matchId) => unidade.Alterar(c =>
    {
        var match = ExigirParticipante(c, usuarioId, matchId);
        if (match.Status != StatusMatch.ATIVO) throw new InvalidOperationException("Somente um match ativo pode ser cancelado.");
        c.AlterarMatch(match.Id, StatusMatch.CANCELADO); return true;
    });

    public int Concluir(int usuarioId, int matchId, string? observacao = null) => unidade.Alterar(c =>
    {
        var match = ExigirParticipante(c, usuarioId, matchId);
        if (match.Status != StatusMatch.ATIVO) throw new InvalidOperationException("Somente um match ativo pode concluir uma troca.");
        ExigirAtivo(c, match.Usuario1Id); ExigirAtivo(c, match.Usuario2Id);
        if (c.Objetos[match.Objeto1Id].Status != StatusObjeto.DISPONIVEL || c.Objetos[match.Objeto2Id].Status != StatusObjeto.DISPONIVEL)
            throw new InvalidOperationException("Um dos objetos já está indisponível.");
        var texto = Validacao.Opcional(observacao, nameof(observacao), 500);
        var id = c.RegistrarTroca(match.Id, texto);
        c.MarcarTrocados(match.Objeto1Id, match.Objeto2Id);
        c.AlterarMatch(match.Id, StatusMatch.CONCLUIDO);
        foreach (var outro in c.Matches.Where(m => m.Id != match.Id && m.Status == StatusMatch.ATIVO
            && (m.Objeto1Id == match.Objeto1Id || m.Objeto2Id == match.Objeto1Id
                || m.Objeto1Id == match.Objeto2Id || m.Objeto2Id == match.Objeto2Id))) c.AlterarMatch(outro.Id, StatusMatch.CANCELADO);
        return id;
    });

    private static Usuario ExigirAtivo(IContextoNegociacao contexto, int id)
    {
        var usuario = contexto.Usuarios.GetValueOrDefault(id) ?? throw new KeyNotFoundException("Usuário não encontrado.");
        if (!usuario.Ativo) throw new InvalidOperationException("Usuário inativo.");
        return usuario;
    }

    private static MatchRegistrado ExigirParticipante(IContextoNegociacao contexto, int usuarioId, int matchId)
    {
        ExigirAtivo(contexto, usuarioId);
        var match = contexto.Matches.FirstOrDefault(m => m.Id == matchId) ?? throw new KeyNotFoundException("Match não encontrado.");
        if (match.Usuario1Id != usuarioId && match.Usuario2Id != usuarioId)
            throw new UnauthorizedAccessException("Somente os participantes podem acessar este match.");
        return match;
    }

    private static (int, int) Par(int a, int b) => a < b ? (a, b) : (b, a);
}
