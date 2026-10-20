using System.Net.Mail;
using Fecho.Core.Seguranca;

namespace Fecho.Core.Entidades;

public sealed class Usuario : EntidadeBase
{
    public string Nome { get; }
    public string Email { get; }
    internal string Telefone { get; }
    internal SenhaProtegida Senha { get; }
    public string? Foto { get; }
    public string? Cidade { get; }
    public string? Descricao { get; }
    public DateTime DataCadastro { get; } = DateTime.UtcNow;
    public bool Ativo { get; private set; } = true;

    public Usuario(int id, string nome, string email, SenhaProtegida senha, string telefone,
        string? cidade = null, string? foto = null, string? descricao = null) : base(id)
    {
        Nome = Validacao.Texto(nome, nameof(nome), 100);
        Email = Validacao.Texto(email, nameof(email), 150).ToLowerInvariant();
        if (!MailAddress.TryCreate(Email, out var endereco) || endereco.Address != Email)
            throw new ArgumentException("E-mail inválido.", nameof(email));
        Senha = senha ?? throw new ArgumentNullException(nameof(senha));
        Telefone = Validacao.Texto(telefone, nameof(telefone), 20);
        if (Telefone.Count(char.IsDigit) is < 10 or > 15)
            throw new ArgumentException("Telefone deve conter de 10 a 15 dígitos.", nameof(telefone));
        Cidade = Validacao.Opcional(cidade, nameof(cidade), 100);
        Foto = Validacao.Opcional(foto, nameof(foto), 255);
        Descricao = Validacao.Opcional(descricao, nameof(descricao), 500);
    }

    public void Desativar() => Ativo = false;
    public override string Resumir() => $"Usuário #{Id}: {Nome}";
}
