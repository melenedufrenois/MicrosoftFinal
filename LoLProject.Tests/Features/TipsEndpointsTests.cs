using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoLProject.ApiService.DTOs; // Pour CreateTipRequest
using LoLProject.Persistence;
using LoLProject.Persistence.Models; // Pour Champion
using Microsoft.EntityFrameworkCore; // Pour Include/First
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
        // 1. Arrange : Préparer la BDD avec un Champion et un User
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            // Reset complet
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // Créer un Champion cible
            db.Champions.Add(new Champion 
            { 
                RiotId = "Teemo", Name = "Teemo", Title = "Scout", RiotKey = "17", 
                Description = "...", ImageUrl = "...", IconUrl = "..." 
            });

            // Créer l'utilisateur qui va poster (Doit matcher le TestAuthHandler)
            db.AppUsers.Add(new AppUser
            {
                Id = Guid.NewGuid(),
                KeycloakId = "test-keycloak-id-123", // ID du mock
                Username = "TestUser",
                Email = "test@test.com"
            });

            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        // Simuler l'auth
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        var newTip = new CreateTipRequest { Content = "Ne jamais chasser un Singed." };

        // 2. Act : Envoyer la requête POST sur le champion ID 1 (le premier ajouté)
        // On doit récupérer l'ID réel généré par la BDD
        int champId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            champId = db.Champions.First().Id;
        }

        var response = await client.PostAsJsonAsync($"/api/lol/champions/{champId}/tips", newTip);

        // 3. Assert : Vérifier le retour et la BDD
        response.EnsureSuccessStatusCode(); // 200 OK

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            var savedTip = await db.ChampionTips.FirstOrDefaultAsync();
            
            Assert.NotNull(savedTip);
            Assert.Equal("Ne jamais chasser un Singed.", savedTip.Content);
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
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var otherUser = new AppUser { Id = Guid.NewGuid(), KeycloakId = "other-id", Username = "Other", Email = "other@test.com" };
            var champ = new Champion { RiotId = "Zed", Name = "Zed", Title = "Shadow", RiotKey = "238", Description=".", ImageUrl=".", IconUrl="." };
            
            db.AppUsers.Add(otherUser);
            db.Champions.Add(champ);
            await db.SaveChangesAsync();

            var tip = new ChampionTip { Content = "Test", Author = otherUser, Champion = champ, CreatedAt = DateTime.UtcNow };
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
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); // Doit être 403
    }
}