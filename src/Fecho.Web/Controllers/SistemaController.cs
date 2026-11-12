using Fecho.Infraestrutura.MySql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Fecho.Web.Controllers;

[AllowAnonymous]
public sealed class SistemaController : Controller
{
    // Não depende do banco: a página continua disponível mesmo com configuração ou servidor indisponível.
    [Route("erro")]
    public IActionResult Erro(int codigo = 500)
    {
        if (HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error is ErroPersistencia) codigo = 503;
        codigo = codigo is >= 400 and <= 599 ? codigo : 500;
        Response.StatusCode = codigo; ViewData["Codigo"] = codigo;
        return View("~/Views/Shared/Erro.cshtml");
    }
}
