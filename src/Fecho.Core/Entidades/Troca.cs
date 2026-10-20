using Fecho.Core.Enums;

namespace Fecho.Core.Entidades;

public sealed class Troca : EntidadeBase
{
    public Match Match { get; }
    public DateTime DataConclusao { get; } = DateTime.UtcNow;
    public string? Observacao { get; }

    internal Troca(int id, Match match, string? observacao = null) : base(id)
    {
        Match = match ?? throw new ArgumentNullException(nameof(match));
        if (match.Status != StatusMatch.ATIVO)
            throw new InvalidOperationException("A troca exige um match ativo.");
        Observacao = Validacao.Opcional(observacao, nameof(observacao), 500);
    }

    public override string Resumir() => $"Troca #{Id}: match #{Match.Id} concluído";
}
