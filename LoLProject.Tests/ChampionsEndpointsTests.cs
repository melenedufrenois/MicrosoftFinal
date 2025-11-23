using System.Net;
using System.Net.Http.Json;
using LoLProject.ApiService.DTOs;
using LoLProject.Persistence;
using LoLProject.Persistence.Models;
using LoLProject.WebApp.DTOs;
using Microsoft.EntityFrameworkCore;
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
    public async Task GetChampions_ShouldReturnData()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/lol/champions");

        // Assert
        response.EnsureSuccessStatusCode();
        var champions = await response.Content.ReadFromJsonAsync<List<ChampionDto>>(); // ou RiotChampionDto selon ton DTO
        Assert.NotNull(champions);
    }

    [Fact]
    public async Task GetChampionDetail_ShouldIncludeStatsAndTips()
    {
        int champId;
        
        // 1. Arrange : On ajoute un champion unique pour ce test
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            
            // ⚠️ Pas de EnsureDeleted() ici pour ne pas casser les autres tests qui tournent en parallèle !
            
            var champ = new Champion 
            { 
                RiotId = "Jinx_" + Guid.NewGuid(), // ID Unique pour éviter les collisions
                Name = "Jinx", 
                Title = "Loose Cannon", 
                RiotKey = "222", 
                Description = "Boom!", 
                ImageUrl = "img", 
                IconUrl = "icon",
                Stats = new ChampionStat { Hp = 600, AttackDamage = 60 } 
            };
            
            // On ajoute un utilisateur factice pour le tip
            var user = new AppUser { Id = Guid.NewGuid(), KeycloakId = "user_" + Guid.NewGuid(), Username = "Tester" };
            
            db.AppUsers.Add(user);
            db.Champions.Add(champ);
            await db.SaveChangesAsync(); // On sauvegarde pour générer l'ID

            // On ajoute le tip lié
            db.ChampionTips.Add(new ChampionTip 
            { 
                Content = "Get Excited!", 
                AppUserId = user.Id, // Utilisation de la FK directe si possible, sinon via l'objet
                ChampionId = champ.Id, 
                CreatedAt = DateTime.UtcNow 
            });
            await db.SaveChangesAsync();
            
            champId = champ.Id; // 👈 C'est ici qu'on capture le BON ID généré
        }

        var client = _factory.CreateClient();

        // 2. Act : On appelle l'API avec l'ID précis qu'on vient de créer
        var response = await client.GetAsync($"/api/lol/champions/{champId}");

        // 3. Assert
        response.EnsureSuccessStatusCode(); // Vérifie que ce n'est pas 404
        
        var detail = await response.Content.ReadFromJsonAsync<ChampionDetailResponseDto>();

        Assert.NotNull(detail);
        Assert.Equal("Jinx", detail.Name);
        
        // Vérification des inclusions
        Assert.NotNull(detail.Stats);
        Assert.Equal(600, detail.Stats.Hp);
        
        Assert.NotEmpty(detail.Tips);
        Assert.Equal("Get Excited!", detail.Tips[0].Content);
    }
}