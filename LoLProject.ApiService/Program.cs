using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using LoLProject.Persistence;
using LoLProject.Persistence.Models;
using LoLProject.ApiService.Endpoints;
using LoLProject.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// --- 1. SERVICES (BUILDER) ---

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();

// 👇 CORRECTION IMPORTANTE : Enregistrement de la DB
// On n'enregistre SQL Server QUE si on n'est pas en mode "Testing".
// En mode "Testing", c'est ta Factory de test qui injectera la base en mémoire.
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.AddSqlServerDbContext<AppDb>(connectionName: "lolproject");
}

// Nettoyage des claims
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Authentification Keycloak
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.Authority = builder.Configuration["Authentication:OIDC:Authority"];
        options.Audience  = builder.Configuration["Authentication:OIDC:Audience"];
        options.RequireHttpsMetadata = false; 
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false, 
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = "name",
            RoleClaimType = "realm_access.roles",
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpClient<IRiotService, RiotService>();

// --- 2. CONSTRUCTION DE L'APP ---

var app = builder.Build();

// --- 3. MIDDLEWARE & PIPELINE ---

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Enregistrement des routes
app.MapLoLEndpoints();

// 👇 MIGRATION SÉCURISÉE (Une seule fois !)
// On ne lance la migration QUE si on n'est pas en test.
if (!app.Environment.IsEnvironment("Testing"))
{
    try 
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            // Double sécurité : on vérifie que c'est bien une base relationnelle (SQL)
            if (db.Database.IsRelational())
            {
                db.Database.Migrate();
            }
        }
    }
    catch (Exception ex)
    {
        // On log juste l'erreur pour ne pas crasher si la DB n'est pas encore prête
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Erreur lors de la migration de la base de données.");
    }
}

// Demo météo
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

public partial class Program { }