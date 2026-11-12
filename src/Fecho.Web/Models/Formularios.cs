using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Fecho.Core.Busca;
using Fecho.Core.Entidades;
using Fecho.Core.Enums;

namespace Fecho.Web.Models;

public sealed class EntrarForm
{
    [Required(ErrorMessage = "Informe seu e-mail."), EmailAddress(ErrorMessage = "Informe um e-mail válido."), StringLength(150)]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Informe sua senha."), StringLength(128), DataType(DataType.Password)]
    public string Senha { get; set; } = "";
    public string? Retorno { get; set; }
}

public class PerfilForm
{
    [Required(ErrorMessage = "Informe seu nome."), StringLength(100)] public string Nome { get; set; } = "";
    [Required(ErrorMessage = "Informe seu e-mail."), EmailAddress(ErrorMessage = "Informe um e-mail válido."), StringLength(150)]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Informe seu telefone."), StringLength(20)] public string Telefone { get; set; } = "";
    [StringLength(100)] public string? Cidade { get; set; }
    [StringLength(500)] public string? Descricao { get; set; }
}

public sealed class CadastroForm : PerfilForm
{
    [Required(ErrorMessage = "Escolha uma senha."), StringLength(128, MinimumLength = 8, ErrorMessage = "A senha deve ter entre 8 e 128 caracteres."), DataType(DataType.Password)]
    public string Senha { get; set; } = "";
    [Required, Compare(nameof(Senha), ErrorMessage = "As senhas precisam ser iguais."), DataType(DataType.Password)]
    public string Confirmacao { get; set; } = "";
}

public sealed class SenhaForm
{
    [Required, StringLength(128), DataType(DataType.Password)] public string Atual { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 8, ErrorMessage = "A senha deve ter entre 8 e 128 caracteres."), DataType(DataType.Password)]
    public string Nova { get; set; } = "";
    [Required, Compare(nameof(Nova), ErrorMessage = "As senhas precisam ser iguais."), DataType(DataType.Password)]
    public string Confirmacao { get; set; } = "";
}

public sealed class ObjetoForm
{
    [Required(ErrorMessage = "Dê um nome ao objeto."), StringLength(100)] public string Nome { get; set; } = "";
    [Range(1, int.MaxValue, ErrorMessage = "Escolha uma categoria.")] public int CategoriaId { get; set; }
    [EnumDataType(typeof(EstadoConservacao), ErrorMessage = "Escolha um estado válido.")]
    public EstadoConservacao Estado { get; set; } = EstadoConservacao.BOM;
    [StringLength(10000)] public string? Descricao { get; set; }
    [RegularExpression(@"^\d{1,8}([.,]\d{1,2})?$", ErrorMessage = "Use um valor como 150,50, sem separador de milhar.")]
    public string? ValorEstimado { get; set; }
    public IFormFile? Imagem { get; set; }
    public bool RemoverImagem { get; set; }
    public decimal? LerValor() => string.IsNullOrWhiteSpace(ValorEstimado) ? null
        : decimal.Parse(ValorEstimado.Replace(',', '.'), CultureInfo.InvariantCulture);
}

public sealed class FiltroCatalogo
{
    [StringLength(150)] public string? Texto { get; set; }
    [Range(1, int.MaxValue)] public int? CategoriaId { get; set; }
    [EnumDataType(typeof(EstadoConservacao))] public EstadoConservacao? Estado { get; set; }
    [StringLength(100)] public string? Cidade { get; set; }
    [RegularExpression(@"^\d{1,8}([.,]\d{1,2})?$", ErrorMessage = "Valor máximo inválido.")] public string? ValorMaximo { get; set; }
    [EnumDataType(typeof(OrdemObjetos))] public OrdemObjetos Ordem { get; set; }
    public int Pagina { get; set; } = 1;
    public bool ParaMim { get; set; }
    public bool Curtidos { get; set; }
    public FiltroObjetos ParaBusca() => new(Texto, CategoriaId, Estado, Cidade,
        string.IsNullOrWhiteSpace(ValorMaximo) ? null : decimal.Parse(ValorMaximo.Replace(',', '.'), CultureInfo.InvariantCulture),
        Ordem, Pagina, ParaMim);
}

public sealed record CatalogoPagina(FiltroCatalogo Filtro, ResultadoBusca Resultado,
    IReadOnlyList<Categoria> Categorias, IReadOnlyDictionary<int, TipoInteresse> Interesses,
    bool PossuiObjetoDisponivel = false);
public sealed record ObjetoDetalhe(Objeto Objeto, TipoInteresse? Interesse);
public sealed record ObjetoEdicao(ObjetoForm Form, IReadOnlyList<Categoria> Categorias, Objeto? Atual = null);
public sealed class DesejoForm
{
    [Required(ErrorMessage = "Conte o que você procura."), StringLength(255)] public string Descricao { get; set; } = "";
    [Range(1, int.MaxValue)] public int? CategoriaId { get; set; }
}
public sealed record DesejosPagina(DesejoForm Form, IReadOnlyList<Desejo> Desejos, IReadOnlyList<Categoria> Categorias);
