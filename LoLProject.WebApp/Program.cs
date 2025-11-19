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
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Aspire & Services de base
builder.AddServiceDefaults();
builder.Services.AddMudServices();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAntiforgery();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

// 2. Ton TokenHandler (Pour l'Auth API)
builder.Services.AddTransient<LoLProject.WebApp.Clients.TokenHandler>();

// 3. Configuration des Clients HTTP (CORRECTION ICI)
// On crée un handler qui ignore les erreurs SSL (Juste pour le Dev)
// Création d'un Handler qui s'en fiche des erreurs SSL (Self-Signed)
var unsafeHandler = new HttpClientHandler();
unsafeHandler.ServerCertificateCustomValidationCallback = 
    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

// Client TODO
builder.Services.AddHttpClient<ITodoClient, TodoClient>(client =>
    {
        // On remet "https+http", Aspire choisira le meilleur port dispo
        client.BaseAddress = new("https+http://apiservice");
    })
    .ConfigurePrimaryHttpMessageHandler(() => unsafeHandler) // Important !
    .AddHttpMessageHandler<LoLProject.WebApp.Clients.TokenHandler>();

// Client SUMMONER
builder.Services.AddHttpClient<SummonerClient>(client =>
    {
        client.BaseAddress = new("https+http://apiservice");
    })
    .ConfigurePrimaryHttpMessageHandler(() => unsafeHandler) // Important !
    .AddHttpMessageHandler<LoLProject.WebApp.Clients.TokenHandler>();

// 4. Authentification OIDC (Keycloak)
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    })
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        // Configuration
        options.Authority = builder.Configuration["Authentication:OIDC:Authority"];
        options.ClientId = builder.Configuration["Authentication:OIDC:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:OIDC:ClientSecret"];
        
        // Paramètres Dev
        options.RequireHttpsMetadata = false; 
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.CallbackPath = "/signin-oidc";
        options.SignedOutCallbackPath = "/signout-callback-oidc";
        
        // Mapping des claims
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "role";

        options.Events.OnRedirectToIdentityProvider = context =>
        {
            // Correctif pour Docker/Keycloak (évite les erreurs de loop http/https)
            context.ProtocolMessage.SetParameter("request_uri", null);
            return Task.CompletedTask;
        };

        // Extraction des Rôles Keycloak
        options.Events.OnTokenValidated = context =>
        {
            var principal = context.Principal;
            if (principal?.Identity is ClaimsIdentity identity)
            {
                // Mapping realm_access.roles -> claim "role"
                var realmAccess = principal.FindFirst("realm_access");
                if (realmAccess != null)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(realmAccess.Value);
                        if (doc.RootElement.TryGetProperty("roles", out var rolesElement))
                        {
                            foreach (var role in rolesElement.EnumerateArray())
                            {
                                identity.AddClaim(new Claim("role", role.GetString() ?? ""));
                            }
                        }
                    }
                    catch { }
                }
                
                // Mapping resource_access (si besoin)
                var resourceAccess = principal.FindFirst("resource_access");
                 if (resourceAccess != null)
                {
                     // Logique similaire si tes rôles sont au niveau client
                }
            }
            return Task.CompletedTask;
        };
    });

// Configuration cookie refresh (si tu as la classe)
// builder.Services.ConfigureCookieOidc(CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// app.UseHttpsRedirection(); // <-- COMMENTE CETTE LIGNE (Évite de forcer HTTPS en local si ça bug)
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/authentication/login", async (HttpContext context, string? returnUrl) =>
{
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
    {
        RedirectUri = returnUrl ?? "/"
    });
});

app.MapGet("/authentication/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
    {
        RedirectUri = "/"
    });
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();