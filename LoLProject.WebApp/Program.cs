using LoLProject.WebApp;
using LoLProject.WebApp.Clients;
using LoLProject.WebApp.Components;
using LoLProject.WebApp.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Aspire defaults
builder.AddServiceDefaults();

// Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Antiforgery
builder.Services.AddAntiforgery();

// HttpContextAccessor pour le TokenHandler
builder.Services.AddHttpContextAccessor();

// Handler qui ajoute l'access_token aux appels API
builder.Services.AddTransient<LoLProject.WebApp.Clients.TokenHandler>();

// 1. Configuration du LoLClient (CORRIGÉ)
builder.Services.AddHttpClient<LoLProject.WebApp.Clients.LoLClient>(client =>
{
    // Récupération propre de l'URL via la configuration Aspire
    var cfg = builder.Configuration;
    var httpUrl = cfg["services:apiservice:http:0"];
    client.BaseAddress = new Uri(httpUrl ?? "http://apiservice");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Nécessaire pour Docker/Dev
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
})
// 👇 C'EST LA LIGNE QU'IL VOUS MANQUAIT 👇
.AddHttpMessageHandler<LoLProject.WebApp.Clients.TokenHandler>();


// HttpClient nommé "api" si besoin (TodoClient etc)
builder.Services.AddHttpClient("api", (sp, c) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    c.BaseAddress = new Uri(cfg["services:apiservice:http:0"]!);
});

// 🔐 Authentification + OIDC
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = "oidc";
    })
    // Cookie : gestion de l'accès refusé
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    })
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = builder.Configuration["Authentication:OIDC:Authority"];
        options.ClientId = builder.Configuration["Authentication:OIDC:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:OIDC:ClientSecret"];
        options.RequireHttpsMetadata = false;
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.CallbackPath = "/signin-oidc";
        options.SignedOutCallbackPath = "/signout-callback-oidc";
        options.UseTokenLifetime = true;
        options.MapInboundClaims = false;

        // Désactiver PAR si Keycloak ne l’aime pas
        options.Events.OnRedirectToIdentityProvider = context =>
        {
            context.ProtocolMessage.SetParameter("request_uri", null);
            context.ProtocolMessage.SetParameter("request", null);
            return Task.CompletedTask;
        };

        // On veut un access_token pour l’API
        options.Scope.Add("api");
        options.Scope.Add("openid");
        options.Scope.Add("profile");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "name",
            RoleClaimType = ClaimTypes.Role,
        };

        // Mapper realm_access.roles -> claims "role" standards
        options.Events.OnTokenValidated = context =>
        {
            // 💡 CORRECTION CRITIQUE POUR BLAZOR
            // On ne modifie pas l'identité en place, on en recrée une propre pour le Cookie
            var principal = context.Principal;
            if (principal == null) return Task.CompletedTask;

            // Création explicite de l'identité cookie
            var newIdentity = new ClaimsIdentity(
                CookieAuthenticationDefaults.AuthenticationScheme, 
                ClaimTypes.Name, 
                ClaimTypes.Role);

            // On copie toutes les infos existantes (sub, name, etc.)
            newIdentity.AddClaims(principal.Claims);

            // On cherche les rôles Keycloak
            var realmAccess = principal.FindFirst("realm_access");
            if (realmAccess is not null && !string.IsNullOrEmpty(realmAccess.Value))
            {
                try
                {
                    using var doc = JsonDocument.Parse(realmAccess.Value);
                    if (doc.RootElement.TryGetProperty("roles", out var rolesElement)
                        && rolesElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var roleJson in rolesElement.EnumerateArray())
                        {
                            var roleName = roleJson.GetString();
                            if (!string.IsNullOrWhiteSpace(roleName))
                            {
                                // Ajout du rôle standard que Blazor comprend
                                newIdentity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                            }
                        }
                    }
                }
                catch { /* ignore */ }
            }

            // On remplace le Principal par notre version propre et complète
            context.Principal = new ClaimsPrincipal(newIdentity);

            return Task.CompletedTask;
        };

        options.Events.OnRemoteFailure = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 500;
            return context.Response.WriteAsync(context.Failure?.ToString() ?? "Unknown OIDC error");
        };
    });

builder.Services.AddAuthorization();

// Pour <AuthorizeView> et [Authorize]
builder.Services.AddCascadingAuthenticationState();

// Refresh token + cookies
builder.Services.ConfigureCookieOidc(
    CookieAuthenticationDefaults.AuthenticationScheme,
    "oidc");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Antiforgery
app.UseAntiforgery();

// Auth / Authorize
app.UseAuthentication();
app.UseAuthorization();

// Endpoint login
app.MapGet("/authentication/login", async (HttpContext context, string? returnUrl) =>
{
    var redirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
    await context.ChallengeAsync("oidc", new AuthenticationProperties { RedirectUri = redirectUri });
});

// Endpoint logout
app.MapGet("/authentication/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync("oidc", new AuthenticationProperties { RedirectUri = "/", });
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();