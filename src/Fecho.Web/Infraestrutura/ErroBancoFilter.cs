using Fecho.Infraestrutura.MySql;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Fecho.Web.Infraestrutura;

public sealed class ErroBancoFilter(ILogger<ErroBancoFilter> logger, IModelMetadataProvider metadados) : IExceptionFilter
{
    public void OnException(ExceptionContext contexto)
    {
        if (contexto.Exception is not ErroPersistencia erro) return;
        logger.LogWarning("MySQL indisponível: {Mensagem}", erro.Message);
        contexto.Result = new ViewResult
        {
            ViewName = "~/Views/Shared/Erro.cshtml", StatusCode = 503,
            ViewData = new ViewDataDictionary(metadados, contexto.ModelState) { ["Title"] = "Voltamos já", ["Codigo"] = 503 }
        };
        contexto.ExceptionHandled = true;
    }
}
