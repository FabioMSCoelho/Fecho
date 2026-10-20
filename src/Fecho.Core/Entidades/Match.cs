using Fecho.Core.Enums;

namespace Fecho.Core.Entidades;

public sealed class Match : EntidadeBase
{
    public Usuario Usuario1 { get; }
    public Usuario Usuario2 { get; }
    public Objeto Objeto1 { get; }
    public Objeto Objeto2 { get; }
    public DateTime DataMatch { get; } = DateTime.UtcNow;
    public StatusMatch Status { get; private set; } = StatusMatch.ATIVO;

    // Só o serviço do domínio cria matches, após verificar os dois likes.
    internal Match(int id, Objeto objeto1, Objeto objeto2) : base(id)
    {
        ArgumentNullException.ThrowIfNull(objeto1);
        ArgumentNullException.ThrowIfNull(objeto2);
        if (objeto1.Proprietario.Equals(objeto2.Proprietario))
            throw new ArgumentException("Match exige dois proprietários diferentes.");
        Objeto1 = objeto1;
        Objeto2 = objeto2;
        Usuario1 = objeto1.Proprietario;
        Usuario2 = objeto2.Proprietario;
    }

    public bool Envolve(Usuario usuario) => Usuario1.Equals(usuario) || Usuario2.Equals(usuario);
    public bool Envolve(Objeto objeto) => Objeto1.Equals(objeto) || Objeto2.Equals(objeto);

    internal void Cancelar()
    {
        ExigirAtivo();
        Status = StatusMatch.CANCELADO;
    }

    internal void Concluir()
    {
        ExigirAtivo();
        Status = StatusMatch.CONCLUIDO;
    }

    private void ExigirAtivo()
    {
        if (Status != StatusMatch.ATIVO) throw new InvalidOperationException("Match não está ativo.");
    }

    public override string Resumir() => $"Match #{Id}: {Objeto1.Nome} ↔ {Objeto2.Nome} ({Status})";
}
