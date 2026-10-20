using Fecho.Core.Enums;

namespace Fecho.Core.Entidades;

public sealed class Objeto : EntidadeBase
{
    public Usuario Proprietario { get; }
    public Categoria Categoria { get; }
    public string Nome { get; }
    public string? Descricao { get; }
    public EstadoConservacao EstadoConservacao { get; }
    public decimal? ValorEstimado { get; }
    public string? Imagem { get; }
    public DateTime DataCadastro { get; } = DateTime.UtcNow;
    public StatusObjeto Status { get; private set; } = StatusObjeto.DISPONIVEL;

    public Objeto(int id, Usuario proprietario, Categoria categoria, string nome,
        EstadoConservacao estadoConservacao, string? descricao = null,
        decimal? valorEstimado = null, string? imagem = null) : base(id)
    {
        Proprietario = proprietario ?? throw new ArgumentNullException(nameof(proprietario));
        Categoria = categoria ?? throw new ArgumentNullException(nameof(categoria));
        Nome = Validacao.Texto(nome, nameof(nome), 100);
        Descricao = Validacao.Opcional(descricao, nameof(descricao), 65_535);
        EstadoConservacao = Validacao.EnumValido(estadoConservacao);
        if (valorEstimado is < 0 or > 99_999_999.99m ||
            (valorEstimado.HasValue && decimal.Round(valorEstimado.Value, 2) != valorEstimado))
            throw new ArgumentException("Valor estimado deve caber em DECIMAL(10,2), sem ser negativo.");
        ValorEstimado = valorEstimado;
        Imagem = Validacao.Opcional(imagem, nameof(imagem), 255);
    }

    internal void MarcarTrocado()
    {
        if (Status != StatusObjeto.DISPONIVEL)
            throw new InvalidOperationException("Objeto indisponível para concluir a troca.");
        Status = StatusObjeto.TROCADO;
    }

    public override string Resumir() => $"Objeto #{Id}: {Nome} ({Status})";
}
