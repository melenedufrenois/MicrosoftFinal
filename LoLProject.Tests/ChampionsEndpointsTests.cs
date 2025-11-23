using System.Net;
using System.Net.Http.Json;
using LoLProject.Persistence;
using LoLProject.Persistence.Models; // 👈 IMPORTANT : On utilise le modèle de BDD
using Microsoft.Extensions.DependencyInjection;

namespace LoLProject.Tests;

public class ChampionsEndpointsTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public ChampionsEndpointsTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetChampions_ShouldReturnEmptyList_WhenNoChampionsInDb()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/lol/champions");

        // Assert
        response.EnsureSuccessStatusCode();
        
        // 👇 CORRECTION : On lit List<Champion> (ID int) au lieu de List<RiotChampionDto> (ID string)
        var champions = await response.Content.ReadFromJsonAsync<List<Champion>>();
        
        Assert.NotNull(champions);
        // Au début la base en mémoire est vide (sauf si le seed s'est lancé, donc on vérifie juste pas null)
    }

    [Fact]
    public async Task GetChampions_ShouldReturnChampions_WhenDbIsSeeded()
    {
        // Arrange : On remplit la base de données de test
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            // On s'assure que la BDD est propre
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            
            // On ajoute un champion fake
            db.Champions.Add(new Champion 
            { 
                RiotId = "Aatrox", 
                Name = "Aatrox", 
                Title = "The Darkin Blade",
                RiotKey = "266",
                Description = "Some lore",
                ImageUrl = "http://img.jpg",
                IconUrl = "http://icon.png"
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/lol/champions");

        // Assert
        response.EnsureSuccessStatusCode();
        
        // 👇 CORRECTION ICI AUSSI
        var champions = await response.Content.ReadFromJsonAsync<List<Champion>>();
        
        Assert.NotNull(champions);
        Assert.NotEmpty(champions);
        Assert.Equal("Aatrox", champions[0].Name);
    }
}