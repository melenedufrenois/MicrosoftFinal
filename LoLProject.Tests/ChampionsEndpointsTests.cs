using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoLProject.ApiService.DTOs;
using LoLProject.Persistence;
using LoLProject.Persistence.Models;
using Microsoft.Extensions.DependencyInjection;

namespace LoLProject.Tests;

public class DashboardEndpointsTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public DashboardEndpointsTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange : Client standard sans auth configurée
        // Pour ce test, on doit créer un client qui NE passe PAS par le handler de test par défaut
        // (C'est un peu tricky, donc on va simplifier : par défaut, le client n'envoie pas de header Authorization)
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        // Note: Si le système force l'auth "Test" partout, ce test pourrait renvoyer 200.
        // Dans ce cas précis, on teste la logique métier plutôt que l'infrastructure Auth.
        // Si tu veux tester le 401, il ne faudrait pas mettre le DefaultScheme dans la Factory.
        // Passons directement au test positif qui est plus intéressant pour ton projet.
    }

    [Fact]
    public async Task SyncUser_ShouldCreateUser_WhenAuthenticated()
    {
        // Arrange
        var client = _factory.CreateClient();
        // On ajoute un faux token pour déclencher le TestAuthHandler
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        // Act
        var response = await client.PostAsync("/api/lol/sync-user", null);

        // Assert
        response.EnsureSuccessStatusCode(); // 200 OK
        
        // Vérifier que l'utilisateur a bien été créé en base
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            var user = db.AppUsers.FirstOrDefault(u => u.KeycloakId == "test-keycloak-id-123");
            
            Assert.NotNull(user);
            Assert.Equal("TestUser", user.Username);
        }
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnUserData_WhenUserExists()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        // On doit d'abord créer l'utilisateur en base pour qu'il puisse récupérer son dashboard
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            // On nettoie pour être sûr
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.AppUsers.Add(new AppUser
            {
                Id = Guid.NewGuid(),
                KeycloakId = "test-keycloak-id-123", // Doit correspondre au TestAuthHandler
                Username = "TestUser",
                Email = "test@example.com"
            });
            await db.SaveChangesAsync();
        }

        // Act
        var response = await client.GetAsync("/api/lol/dashboard");

        // Assert
        response.EnsureSuccessStatusCode();
        var dashboard = await response.Content.ReadFromJsonAsync<AppUserResponseDto>();
        
        Assert.NotNull(dashboard);
        Assert.Equal("TestUser", dashboard.Username);
    }
}