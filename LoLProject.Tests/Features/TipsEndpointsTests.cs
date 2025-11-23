using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoLProject.ApiService.DTOs;
using LoLProject.Persistence;
using LoLProject.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LoLProject.Tests.Features;

public class TipsEndpointsTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public TipsEndpointsTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostTip_ShouldSaveTip_WhenUserIsAuthenticated()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        int champId;
        // Création d'un ID unique pour le contenu du tip afin de le retrouver sans ambiguïté
        var uniqueContent = $"Tip_{Guid.NewGuid()}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            
            // Pas de EnsureDeleted ! On ajoute juste nos données.
            
            var champ = new Champion 
            { 
                RiotId = $"Teemo_{Guid.NewGuid()}", 
                Name = "Teemo", Title = "Scout", RiotKey = "17", 
                Description = "...", ImageUrl = "...", IconUrl = "..." 
            };

            // On s'assure que l'user connecté existe (si pas déjà créé par un autre test)
            if (!db.AppUsers.Any(u => u.KeycloakId == "test-keycloak-id-123"))
            {
                db.AppUsers.Add(new AppUser { Id = Guid.NewGuid(), KeycloakId = "test-keycloak-id-123", Username = "TestUser", Email = "test@test.com" });
            }
            
            db.Champions.Add(champ);
            await db.SaveChangesAsync();
            champId = champ.Id;
        }

        var newTip = new CreateTipRequest { Content = uniqueContent };

        // Act
        var response = await client.PostAsJsonAsync($"/api/lol/champions/{champId}/tips", newTip);

        // Assert
        response.EnsureSuccessStatusCode(); 

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            // On cherche LE tip spécifique qu'on vient de créer
            var savedTip = await db.ChampionTips.FirstOrDefaultAsync(t => t.Content == uniqueContent);
            
            Assert.NotNull(savedTip);
            Assert.Equal(uniqueContent, savedTip.Content);
            Assert.Equal(champId, savedTip.ChampionId);
        }
    }

    [Fact]
    public async Task DeleteTip_ShouldReturnForbidden_WhenUserIsNotAuthor()
    {
        // 1. Arrange : Créer un tip appartenant à un AUTRE utilisateur
        int tipId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            
            // Pas de EnsureDeleted !
            
            var otherUser = new AppUser 
            { 
                Id = Guid.NewGuid(), 
                KeycloakId = $"other_{Guid.NewGuid()}", // ID unique
                Username = "Other", 
                Email = "other@test.com" 
            };
            
            var champ = new Champion 
            { 
                RiotId = $"Zed_{Guid.NewGuid()}", 
                Name = "Zed", Title = "Shadow", RiotKey = "238", 
                Description=".", ImageUrl=".", IconUrl="." 
            };
            
            db.AppUsers.Add(otherUser);
            db.Champions.Add(champ);
            await db.SaveChangesAsync();

            var tip = new ChampionTip { Content = "Secret", Author = otherUser, Champion = champ, CreatedAt = DateTime.UtcNow };
            db.ChampionTips.Add(tip);
            await db.SaveChangesAsync();
            tipId = tip.Id;
        }

        var client = _factory.CreateClient();
        // On est connecté en tant que "TestUser" (via TestAuthHandler), pas "Other"
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        // 2. Act
        var response = await client.DeleteAsync($"/api/lol/tips/{tipId}");

        // 3. Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}