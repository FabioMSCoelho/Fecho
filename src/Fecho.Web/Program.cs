using System.Security.Claims;
using System.Threading.RateLimiting;
using Fecho.Core.Busca;
using Fecho.Core.Contratos;
using Fecho.Core.Servicos;
using Fecho.Infraestrutura.MySql;
using Fecho.Web.Infraestrutura;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args.Where(a => a != "--seed-demo").ToArray());
var raiz = ConfiguracaoLocal.LocalizarRaiz();
ConfiguracaoLocal.Carregar(Path.Combine(raiz, ".env"));
var dadosConfigurados = Environment.GetEnvironmentVariable("FECHO_DATA_DIR");
var dados = string.IsNullOrWhiteSpace(dadosConfigurados) ? Path.Combine(raiz, ".tools", "web") : Path.GetFullPath(dadosConfigurados);
Directory.CreateDirectory(Path.Combine(dados, "chaves"));
Directory.CreateDirectory(Path.Combine(dados, "imagens"));
builder.WebHost.UseUrls(builder.Configuration["urls"] ?? "http://127.0.0.1:5070");
builder.Services.AddDataProtection().SetApplicationName("Fecho.Web")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dados, "chaves")));
builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    o.Filters.Add(new ResponseCacheAttribute { NoStore = true, Location = ResponseCacheLocation.None });
    o.Filters.Add<ErroBancoFilter>();
});
builder.Services.AddScoped(_ => ConexaoMySql.DoAmbiente());
builder.Services.AddScoped<ICrudUsuarios, RepositorioUsuariosMySql>();
builder.Services.AddScoped<ICrudCategorias, RepositorioCategoriasMySql>();
builder.Services.AddScoped<ICrudObjetos, RepositorioObjetosMySql>();
builder.Services.AddScoped<IRepositorioDesejos, RepositorioDesejosMySql>();
builder.Services.AddScoped<IUnidadeNegociacao, UnidadeNegociacaoMySql>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<CategoriaService>();
builder.Services.AddScoped<ObjetoService>();
builder.Services.AddScoped<DesejoService>();
builder.Services.AddScoped<NegociacaoPersistenteService>();
builder.Services.AddSingleton<BuscaObjetos>();
builder.Services.AddSingleton(new ArmazenamentoImagens(Path.Combine(dados, "imagens")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.Cookie.Name = "Fecho.Sessao";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    o.LoginPath = "/conta/entrar";
    o.AccessDeniedPath = "/erro?codigo=403";
    o.ExpireTimeSpan = TimeSpan.FromHours(4);
    o.SlidingExpiration = false;
    o.Events.OnValidatePrincipal = async contexto =>
    {
        var repo = contexto.HttpContext.RequestServices.GetRequiredService<ICrudUsuarios>();
        var valido = int.TryParse(contexto.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
        var usuario = valido ? repo.Buscar(id) : null;
        if (usuario is null || !usuario.Ativo || usuario.IdentificadorCredencial() != contexto.Principal?.FindFirstValue("credencial"))
        {
            contexto.RejectPrincipal();
            await contexto.HttpContext.SignOutAsync();
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("autenticacao", contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
});

var app = builder.Build();
if (args.Contains("--seed-demo", StringComparer.Ordinal))
{
    using var escopo = app.Services.CreateScope();
    PopularDemonstracao.Executar(escopo.ServiceProvider);
    return;
}
// A ramificação de erro não repete autenticação ou consultas: funciona também
// quando o banco falha durante a revalidação de uma sessão já autenticada.
app.UseExceptionHandler(erro => erro.Run(async contexto =>
{
    contexto.Response.StatusCode = contexto.Features.Get<IExceptionHandlerFeature>()?.Error is ErroPersistencia ? 503 : 500;
    contexto.Response.ContentType = "text/html; charset=utf-8";
    contexto.Response.Headers.CacheControl = "no-store";
    contexto.Response.Headers.XContentTypeOptions = "nosniff";
    contexto.Response.Headers["Content-Security-Policy"] = "default-src 'self'; style-src 'self'; frame-ancestors 'none'";
    await contexto.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "erro.html"));
}));
app.UseStatusCodePagesWithReExecute("/erro", "?codigo={0}");
app.Use(async (contexto, proximo) =>
{
    contexto.Response.Headers.XContentTypeOptions = "nosniff";
    contexto.Response.Headers["Referrer-Policy"] = "same-origin";
    contexto.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data: blob: https: http:; font-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    await proximo();
});
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(dados, "imagens")), RequestPath = "/imagens"
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapGet("/saude", () =>
{
    try { ConexaoMySql.DoAmbiente().Verificar(); return Results.Ok(new { status = "ok", banco = "MySQL" }); }
    catch (ErroPersistencia) { return Results.Json(new { status = "indisponivel" }, statusCode: 503); }
}).AllowAnonymous();
app.MapControllerRoute("default", "{controller=Catalogo}/{action=Index}/{id?}");
app.Run();
