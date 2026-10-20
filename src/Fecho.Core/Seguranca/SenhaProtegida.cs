using System.Security.Cryptography;

namespace Fecho.Core.Seguranca;

/// <summary>Hash com salt individual; nenhuma senha em texto puro fica no usuário.</summary>
public sealed class SenhaProtegida
{
    private const int Iteracoes = 600_000;
    private readonly byte[] _salt;
    private readonly byte[] _hash;

    private SenhaProtegida(byte[] salt, byte[] hash) { _salt = salt; _hash = hash; }

    public static SenhaProtegida Criar(string senha)
    {
        if (senha is null || senha.Length is < 8 or > 128)
            throw new ArgumentException("Senha deve ter entre 8 e 128 caracteres.", nameof(senha));
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        return new SenhaProtegida(salt, Derivar(senha, salt));
    }

    public bool Confere(string? senha) => senha is { Length: >= 8 and <= 128 }
        && CryptographicOperations.FixedTimeEquals(_hash, Derivar(senha, _salt));

    // Cabe no campo usuarios.senha VARCHAR(255); destinado ao adaptador de persistência futuro.
    public string ParaPersistencia() =>
        $"PBKDF2-SHA256${Iteracoes}${Convert.ToBase64String(_salt)}${Convert.ToBase64String(_hash)}";

    private static byte[] Derivar(string senha, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, HashAlgorithmName.SHA256, 32);

    public override string ToString() => "[senha protegida]";
}
