namespace Fecho.Core.Entidades;

/// <summary>Identidade estável compartilhada pelas sete entidades do banco.</summary>
public abstract class EntidadeBase
{
    public int Id { get; }

    protected EntidadeBase(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id), "Id deve ser positivo.");
        Id = id;
    }

    public abstract string Resumir();

    // O tipo também participa: Usuário #1 não é igual a Objeto #1.
    // Id imutável evita corromper a localização de elementos em um HashSet.
    public sealed override bool Equals(object? obj) =>
        obj is EntidadeBase outra && GetType() == outra.GetType() && Id == outra.Id;

    public sealed override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public sealed override string ToString() => Resumir();
}
