using System.Net;
using System.Net.Http.Headers;
using Moq; // Pour configurer le mock
using LoLProject.ApiService.Services; // Pour IRiotService

namespace LoLProject.Tests;

public class AdminEndpointsTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public AdminEndpointsTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SyncChampions_ShouldCallRiotService_AndReturnSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        // 1. On configure le Mock du RiotService
        // "Quand on appelle SyncChampionsAsync, retourne le nombre 150"
        _factory.RiotServiceMock
            .Setup(s => s.SyncChampionsAsync())
            .ReturnsAsync(150);

        // Act
        var response = await client.PostAsync("/api/lol/admin/sync-champions", null);

        // Assert
        response.EnsureSuccessStatusCode();
        
        // On vérifie que la méthode du service a bien été appelée UNE fois
        _factory.RiotServiceMock.Verify(s => s.SyncChampionsAsync(), Times.Once);
    }
}