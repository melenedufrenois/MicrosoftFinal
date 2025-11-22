using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using LoLProject.Persistence;
using LoLProject.Persistence.Models;
using LoLProject.ApiService.Endpoints;


var builder = WebApplication.CreateBuilder(args);

// Observabilité/health/logs partagés
builder.AddServiceDefaults();

// OpenAPI
builder.Services.AddOpenApi();

// Cache en mémoire
builder.Services.AddMemoryCache();

// Intégration Aspire + EF Core (utilise ConnectionStrings__lolproject)
builder.AddSqlServerDbContext<AppDb>(connectionName: "lolproject");

// On nettoie les mappings de claims par défaut de .NET
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// 🔐 Authentification JWT Bearer (tokens Keycloak)
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.Authority = builder.Configuration["Authentication:OIDC:Authority"];
        options.Audience  = builder.Configuration["Authentication:OIDC:Audience"];
        
        // 1. On autorise le HTTP (pas de SSL obligatoire)
        options.RequireHttpsMetadata = false; 

        // 2. On relâche la validation stricte
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // TRES IMPORTANT : On désactive la vérification de l'émetteur (Issuer)
            // Ça règle le conflit "localhost" vs "keycloak:8090"
            ValidateIssuer = false, 
            
            // IMPORTANT : On désactive la vérification de l'audience
            // Ça règle le problème si le mapper "api" est mal fait dans Keycloak
            ValidateAudience = false,

            // On garde quand même la vérification de la date (expiration)
            ValidateLifetime = true,

            // On garde la vérification de la signature (que ça vient bien de notre Keycloak)
            ValidateIssuerSigningKey = true,
            
            // Mapping des rôles
            NameClaimType = "name",
            RoleClaimType = "realm_access.roles", // Essaie ça, c'est souvent le défaut Keycloak
        };

        // Events pour débugger (optionnel, tu peux laisser ou enlever)
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"🛑 Auth Failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("✅ Token accepté (Sécurité réduite)");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpClient<LoLProject.ApiService.Services.RiotService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 🔑 Middleware d'authentification / autorisation
app.UseAuthentication();
app.UseAuthorization();

app.MapLoLEndpoints();
// Appliquer les migrations + seed au démarrage
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.Migrate();
}

// Demo météo (laisse ça public si tu veux)
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy",
    "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast(
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        )).ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
