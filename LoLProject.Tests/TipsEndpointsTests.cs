using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoLProject.ApiService.DTOs;
using LoLProject.Persistence;
using LoLProject.Persistence.Models;
using Microsoft.Extensions.DependencyInjection;

namespace LoLProject.Tests;

public class TipsEndpointsTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public TipsEndpointsTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DeleteTip_ShouldReturnForbidden_WhenUserIsNotAuthor()
    {
        // 1. Arrange : On prépare le terrain
        int tipId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            db.Database.EnsureCreated();

            // On crée un "Autre" utilisateur (différent de celui connecté via TestAuthHandler)
            var otherUser = new AppUser 
            { 
                Id = Guid.NewGuid(), 
                KeycloakId = "hacker-id", // ID différent !
                Username = "Hacker", 
                Email = "h@h.com" 
            };
            
            var champ = new Champion { RiotId = "Zed", Name = "Zed", Title = "X", RiotKey = "1", Description=".", ImageUrl=".", IconUrl="." };
            
            db.AppUsers.Add(otherUser);
            db.Champions.Add(champ);
            await db.SaveChangesAsync();

            // Cet autre utilisateur poste un tip
            var tip = new ChampionTip { Content = "Touche pas à ça", Author = otherUser, Champion = champ, CreatedAt = DateTime.UtcNow };
            db.ChampionTips.Add(tip);
            await db.SaveChangesAsync();
            tipId = tip.Id;
        }

        // 2. Act : Je suis connecté en tant que "TestUser" (via le Handler par défaut)
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        // J'essaie de supprimer le tip du "Hacker"
        var response = await client.DeleteAsync($"/api/lol/tips/{tipId}");

        // 3. Assert : Je dois me faire rejeter (403 Forbidden)
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostTip_ShouldWork_WhenAuthenticated()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        // Il faut un champion en base pour poster dessus
        int champId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            db.Database.EnsureCreated();
            var champ = new Champion { RiotId = "Lux", Name = "Lux", Title = "L", RiotKey = "2", Description=".", ImageUrl=".", IconUrl="." };
            db.Champions.Add(champ);
            
            // On s'assure que l'user connecté existe aussi en base (pour la FK)
            if (!db.AppUsers.Any(u => u.KeycloakId == "test-keycloak-id-123"))
            {
                db.AppUsers.Add(new AppUser { Id = Guid.NewGuid(), KeycloakId = "test-keycloak-id-123", Username = "TestUser" });
            }
            
            await db.SaveChangesAsync();
            champId = champ.Id;
        }

        // Act
        var response = await client.PostAsJsonAsync($"/api/lol/champions/{champId}/tips", new CreateTipRequest { Content = "Demacia !" });

        // Assert
        response.EnsureSuccessStatusCode();
    }
}