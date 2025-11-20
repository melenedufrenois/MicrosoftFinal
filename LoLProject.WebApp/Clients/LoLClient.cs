using System.Net.Http.Json;
using LoLProject.WebApp.DTOs;

namespace LoLProject.WebApp.Clients;

public class LoLClient(HttpClient client)
{
    // Liste simple des champions
    public async Task<List<ChampionDto>> GetChampionsAsync() 
        => await client.GetFromJsonAsync<List<ChampionDto>>("/api/lol/champions") ?? [];

    // Détail complet d'un champion
    public async Task<ChampionDetailDto?> GetChampionDetailAsync(int id)
    {
        try {
            return await client.GetFromJsonAsync<ChampionDetailDto>($"/api/lol/champions/{id}");
        } catch { return null; }
    }
    
    // Synchronisation User au login
    public async Task SyncUserAsync()
    {
        await client.PostAsync("/api/lol/sync-user", null);
    }

    // Ajouter un tip
    public async Task<bool> AddTipAsync(int championId, string content)
    {
        // Utilisation du DTO explicite au lieu de l'objet anonyme
        var request = new CreateTipRequest { Content = content };
        
        var response = await client.PostAsJsonAsync($"/api/lol/champions/{championId}/tips", request);
        return response.IsSuccessStatusCode;
    }
    
    // Lier un compte Riot
    public async Task<bool> LinkSummonerAsync(string gameName, string tagLine)
    {
        // Utilisation du DTO explicite au lieu de l'objet anonyme
        var request = new LinkSummonerRequest { GameName = gameName, TagLine = tagLine };
        
        var response = await client.PostAsJsonAsync("/api/lol/dashboard/link", request);
        return response.IsSuccessStatusCode;
    }

    // Récupérer mon dashboard
    public async Task<AppUserDto?> GetMyDashboardAsync()
    {
        try {
            return await client.GetFromJsonAsync<AppUserDto>("/api/lol/dashboard");
        } catch { return null; }
    }

    // 👇 MÉTHODES ADMIN 👇

    public async Task<string> AdminSyncChampionsAsync()
    {
        var response = await client.PostAsync("/api/lol/admin/sync-champions", null);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AdminMessageDto>();
            return result?.Message ?? "Synchronisation terminée.";
        }
        return "Erreur lors de la synchronisation.";
    }

    public async Task<string> AdminResetChampionsAsync()
    {
        var response = await client.DeleteAsync("/api/lol/admin/reset-champions");
        return response.IsSuccessStatusCode ? "Base de données champions vidée." : "Erreur lors de la suppression.";
    }

    // Petit DTO interne pour lire le message { "message": "..." } de l'API
    private class AdminMessageDto { public string Message { get; set; } = ""; }
}