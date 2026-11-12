using System.Security.Claims;
using Fecho.Core.Busca;
using Fecho.Core.Contratos;
using Fecho.Core.Enums;
using Fecho.Core.Servicos;
using Fecho.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fecho.Web.Controllers;

[AllowAnonymous]
public sealed class CatalogoController(ICrudObjetos objetos, ICrudCategorias categorias,
    BuscaObjetos busca, DesejoService desejos, NegociacaoPersistenteService negociacao) : Controller
{
    [HttpGet("/")]
    public IActionResult Index([FromQuery] FiltroCatalogo filtro)
    {
        int? usuarioId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
        if (!ModelState.IsValid) filtro = new FiltroCatalogo();
        if (!usuarioId.HasValue && (filtro.ParaMim || filtro.Curtidos)) return Challenge();
        var interesses = usuarioId.HasValue ? negociacao.ListarInteresses(usuarioId.Value) : new Dictionary<int, TipoInteresse>();
        var todos = objetos.Listar();
        var origem = todos.AsEnumerable();
        if (filtro.Curtidos) origem = origem.Where(o => interesses.GetValueOrDefault(o.Id) == TipoInteresse.LIKE && interesses.ContainsKey(o.Id));
        // A descoberta sempre retoma pelo primeiro objeto ainda não avaliado.
        // Remover os avaliados antes de paginar evita pular objetos ao avançar.
        var ignorados = filtro.Curtidos ? new HashSet<int>() : interesses.Keys.ToHashSet();
        if (!filtro.Curtidos) filtro.Pagina = 1;
        var resultado = busca.Executar(origem, filtro.ParaBusca(), usuarioId,
            filtro.ParaMim ? desejos.Listar(usuarioId!.Value) : null, ignorados);
        return View(new CatalogoPagina(filtro, resultado, categorias.Listar(), interesses,
            todos.Any(o => o.Proprietario.Id == usuarioId && o.Status == StatusObjeto.DISPONIVEL)));
    }

    [HttpGet("objetos/{id:int:min(1)}")]
    public IActionResult Detalhes(int id)
    {
        var objeto = objetos.Buscar(id);
        if (objeto is null || !objeto.Proprietario.Ativo) return NotFound();
        TipoInteresse? interesse = null;
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            var avaliados = negociacao.ListarInteresses(usuarioId);
            if (avaliados.TryGetValue(id, out var tipo)) interesse = tipo;
        }
        return View(new ObjetoDetalhe(objeto, interesse));
    }

}
