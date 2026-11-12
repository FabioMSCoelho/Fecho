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

    /// <summary>Reconstitui o hash existente sem conhecer nem recalcular a senha original.</summary>
    public static SenhaProtegida DaPersistencia(string valor)
    {
        if (string.IsNullOrEmpty(valor)) throw new ArgumentException("Hash de senha inválido.", nameof(valor));
        var partes = valor.Split('$');
        if (partes.Length != 4 || partes[0] != "PBKDF2-SHA256" || partes[1] != "600000")
            throw new ArgumentException("Formato de hash de senha não suportado.", nameof(valor));
        try
        {
            var salt = Convert.FromBase64String(partes[2]);
            var hash = Convert.FromBase64String(partes[3]);
            if (salt.Length != 16 || hash.Length != 32)
                throw new ArgumentException("Hash de senha inválido.", nameof(valor));
            return new SenhaProtegida(salt, hash);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Hash de senha inválido.", nameof(valor));
        }
    }

    // Cabe no campo usuarios.senha VARCHAR(255).
    public string ParaPersistencia() =>
        $"PBKDF2-SHA256${Iteracoes}${Convert.ToBase64String(_salt)}${Convert.ToBase64String(_hash)}";

    private static byte[] Derivar(string senha, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, HashAlgorithmName.SHA256, 32);

    public override string ToString() => "[senha protegida]";
}
