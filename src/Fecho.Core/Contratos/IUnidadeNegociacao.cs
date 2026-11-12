using Fecho.Core.Entidades;
using Fecho.Core.Enums;

namespace Fecho.Core.Contratos;

public sealed record InteresseRegistrado(int Id, int UsuarioId, int ObjetoId, TipoInteresse Tipo);
public sealed record MatchRegistrado(int Id, int Usuario1Id, int Usuario2Id, int Objeto1Id, int Objeto2Id,
    DateTime DataMatch, StatusMatch Status, DateTime? DataConclusao, string? Observacao);

/// <summary>Operações disponíveis dentro de uma única transação, sem conhecer o driver SQL.</summary>
public interface IContextoNegociacao
{
    IReadOnlyDictionary<int, Usuario> Usuarios { get; }
    IReadOnlyDictionary<int, Objeto> Objetos { get; }
    IReadOnlyList<InteresseRegistrado> Interesses { get; }
    IReadOnlyList<MatchRegistrado> Matches { get; }
    void GravarInteresse(int usuarioId, int objetoId, TipoInteresse tipo);
    int CriarMatch(Objeto objeto1, Objeto objeto2);
    void AlterarMatch(int id, StatusMatch status);
    void MarcarTrocados(int objeto1Id, int objeto2Id);
    int RegistrarTroca(int matchId, string? observacao);
    string ObterTelefone(int usuarioId);
}

public interface IUnidadeNegociacao
{
    T Ler<T>(Func<IContextoNegociacao, T> operacao);
    T Alterar<T>(Func<IContextoNegociacao, T> operacao);
}
