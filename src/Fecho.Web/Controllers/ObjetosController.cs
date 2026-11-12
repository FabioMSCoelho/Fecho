using System.Globalization;
using System.Security.Claims;
using Fecho.Core.Contratos;
using Fecho.Core.Servicos;
using Fecho.Infraestrutura.MySql;
using Fecho.Web.Infraestrutura;
using Fecho.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fecho.Web.Controllers;

[Authorize]
public sealed class ObjetosController(ObjetoService objetos, ICrudCategorias categorias, ArmazenamentoImagens imagens) : Controller
{
    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("meus-objetos")]
    public IActionResult Meus() => View(objetos.Listar().Where(o => o.Proprietario.Id == UsuarioId)
        .OrderByDescending(o => o.DataCadastro).ThenByDescending(o => o.Id).ToList());

    [HttpGet("objetos/novo")]
    public IActionResult Novo() => View("Formulario", new ObjetoEdicao(new ObjetoForm(), categorias.Listar()));

    [HttpPost("objetos/novo"), RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Novo([Bind(Prefix = "Form")] ObjetoForm form)
    {
        if (!ModelState.IsValid) return View("Formulario", new ObjetoEdicao(form, categorias.Listar()));
        string? imagem = null;
        try
        {
            imagem = await imagens.SalvarAsync(form.Imagem);
            var objeto = objetos.Cadastrar(UsuarioId, form.CategoriaId, form.Nome, form.Estado, form.Descricao, form.LerValor(), imagem);
            TempData["Sucesso"] = "Objeto cadastrado. Ele já pode encontrar uma nova história.";
            return RedirectToAction("Detalhes", "Catalogo", new { id = objeto.Id });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or ErroPersistencia)
        { imagens.Remover(imagem); ModelState.AddModelError("", ex.Message); return View("Formulario", new ObjetoEdicao(form, categorias.Listar())); }
    }

    [HttpGet("objetos/{id:int:min(1)}/editar")]
    public IActionResult Editar(int id)
    {
        var atual = objetos.Buscar(id);
        if (atual is null) return NotFound();
        if (atual.Proprietario.Id != UsuarioId) return Forbid();
        return View("Formulario", new ObjetoEdicao(new ObjetoForm { Nome = atual.Nome, CategoriaId = atual.Categoria.Id,
            Estado = atual.EstadoConservacao, Descricao = atual.Descricao,
            ValorEstimado = atual.ValorEstimado?.ToString("0.00", CultureInfo.InvariantCulture) }, categorias.Listar(), atual));
    }

    [HttpPost("objetos/{id:int:min(1)}/editar"), RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Editar(int id, [Bind(Prefix = "Form")] ObjetoForm form)
    {
        var atual = objetos.Buscar(id);
        if (atual is null) return NotFound();
        if (atual.Proprietario.Id != UsuarioId) return Forbid();
        if (!ModelState.IsValid) return View("Formulario", new ObjetoEdicao(form, categorias.Listar(), atual));
        string? nova = null;
        try
        {
            nova = await imagens.SalvarAsync(form.Imagem);
            var imagem = nova ?? (form.RemoverImagem ? null : atual.Imagem);
            if (!objetos.Atualizar(id, form.CategoriaId, form.Nome, form.Estado, form.Descricao, form.LerValor(), imagem)) return NotFound();
            if (imagem != atual.Imagem) imagens.Remover(atual.Imagem);
            TempData["Sucesso"] = "Objeto atualizado.";
            return RedirectToAction(nameof(Meus));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or ErroPersistencia)
        { imagens.Remover(nova); ModelState.AddModelError("", ex.Message); return View("Formulario", new ObjetoEdicao(form, categorias.Listar(), atual)); }
    }

    [HttpPost("objetos/{id:int:min(1)}/excluir")]
    public IActionResult Excluir(int id)
    {
        var atual = objetos.Buscar(id);
        if (atual is null) return NotFound();
        if (atual.Proprietario.Id != UsuarioId) return Forbid();
        try
        {
            if (objetos.Excluir(id)) { imagens.Remover(atual.Imagem); TempData["Sucesso"] = "Objeto excluído."; }
        }
        catch (ErroPersistencia ex) { TempData["Erro"] = ex.Message; }
        return RedirectToAction(nameof(Meus));
    }
}
