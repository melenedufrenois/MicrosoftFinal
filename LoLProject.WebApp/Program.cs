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

// HttpClient typé vers l’API (via Aspire)
builder.Services.AddHttpClient<ITodoClient, TodoClient>((sp, client) =>
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        var httpUrl = cfg["services:apiservice:http:0"];
        client.BaseAddress = new Uri(httpUrl ?? "http://apiservice");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    })
    .AddHttpMessageHandler<LoLProject.WebApp.Clients.TokenHandler>();

// HttpClient nommé "api" si besoin
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
        // Pas de redirection vers /Account/AccessDenied (n'existe pas) → on renvoie juste 403
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

        // On indique que :
        // - le nom vient du claim "name"
        // - les rôles utiliseront ClaimTypes.Role (standard .NET)
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "name",
            RoleClaimType = ClaimTypes.Role,
        };

        // Mapper realm_access.roles -> claims "role" standards
        options.Events.OnTokenValidated = context =>
        {
            if (context.Principal?.Identity is ClaimsIdentity identity)
            {
                var realmAccess = identity.FindFirst("realm_access");
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
                                    // 👉 Ici on crée un claim de type ClaimTypes.Role
                                    identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                                }
                            }
                        }
                    }
                    catch
                    {
                        // si jamais le JSON est chelou, on ignore
                    }
                }
            }

            return Task.CompletedTask;
        };

        // Debug des erreurs OIDC si besoin
        options.Events.OnRemoteFailure = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 500;
            return context.Response.WriteAsync(context.Failure?.ToString() ?? "Unknown OIDC error");
        };

        options.ClaimActions.MapAll();
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

    await context.ChallengeAsync("oidc", new AuthenticationProperties
    {
        RedirectUri = redirectUri
    });
});

// Endpoint logout
app.MapGet("/authentication/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync("oidc", new AuthenticationProperties
    {
        RedirectUri = "/",
    });
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
