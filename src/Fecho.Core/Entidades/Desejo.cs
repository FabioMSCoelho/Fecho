namespace Fecho.Core.Entidades;

public sealed class Desejo : EntidadeBase
{
    public Usuario Usuario { get; }
    public Categoria? Categoria { get; }
    public string Descricao { get; }

    public Desejo(int id, Usuario usuario, string descricao, Categoria? categoria = null) : base(id)
    {
        Usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));
        Categoria = categoria; // categoria_id permite NULL no SQL original.
        Descricao = Validacao.Texto(descricao, nameof(descricao), 255);
    }

    public override string Resumir() => $"Desejo #{Id}: {Descricao}";
}
