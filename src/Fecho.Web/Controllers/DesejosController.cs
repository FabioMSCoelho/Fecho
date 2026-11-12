using System.Security.Claims;
using Fecho.Core.Contratos;
using Fecho.Core.Servicos;
using Fecho.Infraestrutura.MySql;
using Fecho.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fecho.Web.Controllers;

[Authorize, Route("desejos")]
public sealed class DesejosController(DesejoService desejos, ICrudCategorias categorias) : Controller
{
    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("")]
    public IActionResult Index() => View(Pagina(new DesejoForm()));
    [HttpPost("")]
    public IActionResult Adicionar([Bind(Prefix = "Form")] DesejoForm form)
    {
        if (!ModelState.IsValid) return View("Index", Pagina(form));
        try { desejos.Adicionar(UsuarioId, form.Descricao, form.CategoriaId); TempData["Sucesso"] = "Desejo salvo. Veja os objetos que combinam com você."; return RedirectToAction(nameof(Index)); }
        catch (Exception ex) when (ex is ArgumentException or ErroPersistencia)
        { ModelState.AddModelError("", ex.Message); return View("Index", Pagina(form)); }
    }
    [HttpPost("{id:int:min(1)}/excluir")]
    public IActionResult Excluir(int id)
    {
        if (!desejos.Excluir(id, UsuarioId)) return NotFound();
        TempData["Sucesso"] = "Desejo removido."; return RedirectToAction(nameof(Index));
    }
    private DesejosPagina Pagina(DesejoForm form) => new(form, desejos.Listar(UsuarioId), categorias.Listar());
}
