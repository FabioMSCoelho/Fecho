using System.Security.Claims;
using Fecho.Core.Enums;
using Fecho.Core.Servicos;
using Fecho.Infraestrutura.MySql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fecho.Web.Controllers;

[Authorize]
public sealed class NegociacaoController(NegociacaoPersistenteService negociacao) : Controller
{
    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("objetos/{id:int:min(1)}/interesse")]
    public IActionResult Interesse(int id, TipoInteresse? tipo, string? retorno)
    {
        if (!ModelState.IsValid || !tipo.HasValue || !Enum.IsDefined(tipo.Value)) return BadRequest();
        try
        {
            var novos = negociacao.Avaliar(UsuarioId, id, tipo.Value);
            TempData["Sucesso"] = novos > 0 ? "Deu match! Vocês já podem combinar a troca." : tipo == TipoInteresse.LIKE
                ? "Interesse registrado. Se a pessoa também curtir um objeto seu, é match!" : "Preferência atualizada.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException or ErroPersistencia)
        { TempData["Erro"] = ex.Message; }
        return !string.IsNullOrWhiteSpace(retorno) && Url.IsLocalUrl(retorno) ? LocalRedirect(retorno) : RedirectToAction("Index", "Catalogo");
    }

    [HttpGet("matches")]
    public IActionResult Matches() => View(negociacao.ListarMatches(UsuarioId));

    [HttpGet("matches/{id:int:min(1)}")]
    public IActionResult Detalhes(int id)
    {
        var match = negociacao.ListarMatches(UsuarioId).FirstOrDefault(m => m.Registro.Id == id);
        if (match is null) return NotFound();
        if (match.Registro.Status != StatusMatch.CANCELADO)
        {
            try { ViewData["Contato"] = negociacao.ObterContato(UsuarioId, id); }
            catch (InvalidOperationException) { ViewData["Contato"] = null; }
        }
        return View(match);
    }

    [HttpPost("matches/{id:int:min(1)}/cancelar")]
    public IActionResult Cancelar(int id) => Alterar(id, () => negociacao.Cancelar(UsuarioId, id), "Match cancelado. O contato foi revogado.");

    [HttpPost("matches/{id:int:min(1)}/concluir")]
    public IActionResult Concluir(int id, string? observacao) => Alterar(id,
        () => negociacao.Concluir(UsuarioId, id, observacao), "Troca concluída! Os dois objetos ganharam uma nova história.");

    private IActionResult Alterar(int id, Action acao, string mensagem)
    {
        try { acao(); TempData["Sucesso"] = mensagem; }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or ErroPersistencia) { TempData["Erro"] = ex.Message; }
        return RedirectToAction(nameof(Detalhes), new { id });
    }
}
