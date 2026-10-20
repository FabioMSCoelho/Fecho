namespace Fecho.Core.Entidades;

public sealed class Categoria : EntidadeBase
{
    public string Nome { get; }
    public string? Descricao { get; }

    public Categoria(int id, string nome, string? descricao = null) : base(id)
    {
        Nome = Validacao.Texto(nome, nameof(nome), 50);
        Descricao = Validacao.Opcional(descricao, nameof(descricao), 255);
    }

    public override string Resumir() => $"Categoria #{Id}: {Nome}";
}
