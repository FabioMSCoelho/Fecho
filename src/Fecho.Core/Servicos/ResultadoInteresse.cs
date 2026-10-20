using Fecho.Core.Entidades;

namespace Fecho.Core.Servicos;

public sealed record ResultadoInteresse(Interesse Interesse, IReadOnlyList<Match> NovosMatches);
