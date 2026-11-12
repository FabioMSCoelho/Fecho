using System.Security.Claims;
using Fecho.Core.Contratos;
using Fecho.Core.Entidades;
using Fecho.Core.Servicos;
using Fecho.Infraestrutura.MySql;
using Fecho.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fecho.Web.Controllers;

[Authorize, Route("conta")]
public sealed class ContaController(UsuarioService usuarios, ICrudUsuarios repositorio) : Controller
{
    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [AllowAnonymous, HttpGet("entrar")]
    public IActionResult Entrar(string? returnUrl = null) => User.Identity?.IsAuthenticated == true
        ? RedirectToAction("Index", "Catalogo") : View(new EntrarForm { Retorno = returnUrl });

    [AllowAnonymous, HttpPost("entrar"), EnableRateLimiting("autenticacao")]
    public async Task<IActionResult> Entrar(EntrarForm form)
    {
        if (!ModelState.IsValid) return View(form);
        try
        {
            await CriarSessao(usuarios.Autenticar(form.Email, form.Senha));
            return !string.IsNullOrWhiteSpace(form.Retorno) && Url.IsLocalUrl(form.Retorno)
                ? LocalRedirect(form.Retorno) : RedirectToAction("Index", "Catalogo");
        }
        catch (UnauthorizedAccessException ex) { ModelState.AddModelError("", ex.Message); return View(form); }
    }

    [AllowAnonymous, HttpGet("cadastrar")]
    public IActionResult Cadastrar() => View(new CadastroForm());

    [AllowAnonymous, HttpPost("cadastrar"), EnableRateLimiting("autenticacao")]
    public async Task<IActionResult> Cadastrar(CadastroForm form)
    {
        if (!ModelState.IsValid) return View(form);
        try
        {
            var usuario = usuarios.Cadastrar(form.Nome, form.Email, form.Senha, form.Telefone, form.Cidade, descricao: form.Descricao);
            await CriarSessao(usuario);
            TempData["Sucesso"] = "Sua conta está pronta. Agora, que tal cadastrar seu primeiro objeto?";
            return RedirectToAction("Meus", "Objetos");
        }
        catch (Exception ex) when (ex is ArgumentException or ErroPersistencia)
        { ModelState.AddModelError("", ex.Message); return View(form); }
    }

    [HttpPost("sair")]
    public async Task<IActionResult> Sair()
    { await HttpContext.SignOutAsync(); return RedirectToAction("Index", "Catalogo"); }

    [HttpGet("perfil")]
    public IActionResult Perfil()
    {
        var perfil = usuarios.ConsultarPerfil(UsuarioId);
        return View(new PerfilForm { Nome = perfil.Usuario.Nome, Email = perfil.Usuario.Email,
            Telefone = perfil.Telefone, Cidade = perfil.Usuario.Cidade, Descricao = perfil.Usuario.Descricao });
    }

    [HttpPost("perfil")]
    public async Task<IActionResult> Perfil(PerfilForm form)
    {
        if (!ModelState.IsValid) return View(form);
        try
        {
            var atual = repositorio.Buscar(UsuarioId)!;
            if (!usuarios.Atualizar(UsuarioId, form.Nome, form.Email, form.Telefone, form.Cidade, atual.Foto, form.Descricao, atual.Ativo))
                return NotFound();
            await CriarSessao(repositorio.Buscar(UsuarioId)!);
            TempData["Sucesso"] = "Perfil atualizado. Tudo salvo para a próxima troca.";
            return RedirectToAction(nameof(Perfil));
        }
        catch (Exception ex) when (ex is ArgumentException or ErroPersistencia)
        { ModelState.AddModelError("", ex.Message); return View(form); }
    }

    [HttpGet("senha")]
    public IActionResult Senha() => View(new SenhaForm());

    [HttpPost("senha"), EnableRateLimiting("autenticacao")]
    public async Task<IActionResult> Senha(SenhaForm form)
    {
        if (!ModelState.IsValid) return View(form);
        var usuario = repositorio.Buscar(UsuarioId)!;
        if (!usuario.ConferirSenha(form.Atual)) { ModelState.AddModelError("Atual", "A senha atual não confere."); return View(form); }
        usuarios.AlterarSenha(UsuarioId, form.Nova);
        await HttpContext.SignOutAsync();
        TempData["Sucesso"] = "Senha atualizada. Entre novamente com a nova senha.";
        return RedirectToAction(nameof(Entrar));
    }

    private Task CriarSessao(Usuario usuario) => HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome), new Claim("credencial", usuario.IdentificadorCredencial()) },
            CookieAuthenticationDefaults.AuthenticationScheme)), new AuthenticationProperties { IsPersistent = false });
}
