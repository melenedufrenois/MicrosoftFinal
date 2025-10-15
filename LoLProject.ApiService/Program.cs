using Microsoft.EntityFrameworkCore;
using LoLProject.Persistence;
using LoLProject.Persistence.Models;

var builder = WebApplication.CreateBuilder(args);

// Observabilité/health/logs partagés
builder.AddServiceDefaults();

// OpenAPI
builder.Services.AddOpenApi();

// Intégration Aspire + EF Core (utilise ConnectionStrings__lolproject)
builder.AddSqlServerDbContext<AppDb>(connectionName: "lolproject");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Appliquer les migrations + seed au démarrage
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.Migrate();
}

// Demo météo
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
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

// Endpoints TODO (DbContext depuis la couche persistance)
app.MapGet("/api/todo", async (AppDb db) =>
    await db.Todos.AsNoTracking().ToListAsync());

app.MapPost("/api/todo", async (AppDb db, TodoItem t) =>
{
    db.Todos.Add(t);
    await db.SaveChangesAsync();
    return Results.Created($"/api/todo/{t.Id}", t);
});

app.MapPut("/api/todo/{id:int}", async (int id, AppDb db, TodoItem input) =>
{
    var t = await db.Todos.FindAsync(id);
    if (t is null) return Results.NotFound();
    t.Title = input.Title;
    t.Done = input.Done;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/todo/{id:int}", async (int id, AppDb db) =>
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
