using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using LoLProject.Persistence;
using LoLProject.Persistence.Models;

var builder = WebApplication.CreateBuilder(args);

// Observabilité/health/logs partagés
builder.AddServiceDefaults();

// OpenAPI
builder.Services.AddOpenApi();

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
        options.RequireHttpsMetadata = false; // on est en HTTP en local

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = "name",
            // si dans Keycloak ton claim s'appelle "roles", garde "roles"
            RoleClaimType = "roles",
        };

        // ne pas remapper automatiquement les claims (on garde les noms bruts)
        options.MapInboundClaims = false;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 🔑 Middleware d'authentification / autorisation
app.UseAuthentication();
app.UseAuthorization();

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

// 🔒 Endpoints TODO protégés avec [Authorize]
app.MapGet("/api/todo", [Authorize] async (AppDb db) =>
    await db.Todos.AsNoTracking().ToListAsync());

app.MapPost("/api/todo", [Authorize] async (AppDb db, TodoItem t) =>
{
    db.Todos.Add(t);
    await db.SaveChangesAsync();
    return Results.Created($"/api/todo/{t.Id}", t);
});

app.MapPut("/api/todo/{id:int}", [Authorize] async (int id, AppDb db, TodoItem input) =>
{
    var t = await db.Todos.FindAsync(id);
    if (t is null) return Results.NotFound();
    t.Title = input.Title;
    t.Done = input.Done;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/todo/{id:int}", [Authorize] async (int id, AppDb db) =>
{
    var t = await db.Todos.FindAsync(id);
    if (t is null) return Results.NotFound();
    db.Todos.Remove(t);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
