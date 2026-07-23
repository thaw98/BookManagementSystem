using Microsoft.AspNetCore.Authentication.Cookies;
using MudBlazor.Services;
using WebApp.Components;
using WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.Cookie.HttpOnly = true;
        options.Events.OnValidatePrincipal = context =>
        {
            var sessionId = context.Principal?.FindFirst("sid")?.Value;
            var store = context.HttpContext.RequestServices.GetRequiredService<TokenStorageService>();
            if (string.IsNullOrWhiteSpace(sessionId) || !store.Exists(sessionId))
                context.RejectPrincipal();
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<TokenStorageService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ApiClient>();
builder.Services.AddHttpClient("BmsApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7239");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapPost("/account/login", async (HttpContext context, AuthService authService, CancellationToken cancellationToken) =>
{
    var form = await context.Request.ReadFormAsync(cancellationToken);
    var result = await authService.SignInAsync(
        context,
        form["email"].ToString(),
        form["password"].ToString(),
        cancellationToken);

    if (!result.IsSuccess)
        return Results.Redirect("/login?error=1");

    var returnUrl = form["returnUrl"].ToString();
    return Results.Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
}).DisableAntiforgery();

app.MapPost("/account/logout", async (HttpContext context, AuthService authService, CancellationToken cancellationToken) =>
{
    await authService.SignOutAsync(context, cancellationToken);
    return Results.Redirect("/login");
}).DisableAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
