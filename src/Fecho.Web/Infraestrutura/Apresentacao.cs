using Fecho.Core.Entidades;
using Fecho.Core.Enums;

namespace Fecho.Web.Infraestrutura;

public static class Apresentacao
{
    public static string Imagem(Objeto objeto)
    {
        if (objeto.Imagem?.StartsWith("/imagens/", StringComparison.Ordinal) == true
            || objeto.Imagem?.StartsWith("/arte/", StringComparison.Ordinal) == true) return objeto.Imagem;
        if (Uri.TryCreate(objeto.Imagem, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https") return uri.AbsoluteUri;
        return "/arte/objeto.svg";
    }
    public static string Estado(EstadoConservacao estado) => estado switch
    {
        EstadoConservacao.NOVO => "Novo", EstadoConservacao.MUITO_BOM => "Muito bom",
        EstadoConservacao.BOM => "Bom", EstadoConservacao.REGULAR => "Regular", _ => "Precisa de cuidado"
    };
    public static string Status(StatusMatch status) => status switch
    { StatusMatch.ATIVO => "É match!", StatusMatch.CONCLUIDO => "Troca concluída", _ => "Cancelado" };
}
