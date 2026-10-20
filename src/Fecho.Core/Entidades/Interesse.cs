using Fecho.Core.Enums;

namespace Fecho.Core.Entidades;

public sealed class Interesse : EntidadeBase
{
    public Usuario Usuario { get; }
    public Objeto Objeto { get; }
    public TipoInteresse Tipo { get; private set; }
    public DateTime DataInteresse { get; private set; } = DateTime.UtcNow;

    public Interesse(int id, Usuario usuario, Objeto objeto, TipoInteresse tipo) : base(id)
    {
        Usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));
        Objeto = objeto ?? throw new ArgumentNullException(nameof(objeto));
        if (usuario.Equals(objeto.Proprietario))
            throw new ArgumentException("Não é permitido avaliar seu próprio objeto.");
        Tipo = Validacao.EnumValido(tipo);
    }

    internal void Atualizar(TipoInteresse tipo)
    {
        Tipo = Validacao.EnumValido(tipo);
        DataInteresse = DateTime.UtcNow;
    }

    public override string Resumir() => $"Interesse #{Id}: {Usuario.Nome} → {Objeto.Nome}: {Tipo}";
}
