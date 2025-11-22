using System.Net.Http.Json;
using LoLProject.ApiService.DTOs;
using LoLProject.WebApp.DTOs;
using CreateTipRequest = LoLProject.WebApp.DTOs.CreateTipRequest;
using LinkSummonerRequest = LoLProject.WebApp.DTOs.LinkSummonerRequest;

namespace LoLProject.WebApp.Clients;

public class LoLClient(HttpClient client)
{
    // Petit DTO interne pour lire le message { "message": "..." } de l'API
    private class AdminMessageDto { public string Message { get; set; } = ""; }
    
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
    
    // Récupérer les stats calculées
    public async Task<DashboardStatsResponseDto?> GetDashboardStatsAsync()
    {
        try {
            return await client.GetFromJsonAsync<DashboardStatsResponseDto>("/api/lol/dashboard/stats");
        } catch { return null; }
    }
    
    public async Task<List<AppUserDto>> AdminGetAllUsersAsync()
        => await client.GetFromJsonAsync<List<AppUserDto>>("/api/lol/admin/users") ?? [];

    public async Task<bool> AdminUnlinkUserAsync(Guid userId)
    {
        var response = await client.DeleteAsync($"/api/lol/admin/users/{userId}/unlink");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AdminLinkUserAsync(Guid userId, string gameName, string tagLine)
    {
        var request = new LinkSummonerRequest { GameName = gameName, TagLine = tagLine };
        var response = await client.PostAsJsonAsync($"/api/lol/admin/users/{userId}/link", request);
        return response.IsSuccessStatusCode;
    }
    
    // GESTION DES TIPS (ADMIN)
    public async Task<List<AdminTipDto>> AdminGetAllTipsAsync()
        => await client.GetFromJsonAsync<List<AdminTipDto>>("/api/lol/admin/tips") ?? [];

    public async Task<bool> AdminDeleteTipAsync(int id)
    {
        var response = await client.DeleteAsync($"/api/lol/admin/tips/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AdminUpdateTipAsync(int id, string newContent)
    {
        var response = await client.PutAsJsonAsync($"/api/lol/admin/tips/{id}", new UpdateTipRequest(newContent));
        return response.IsSuccessStatusCode;
    }
    
    // Permet à l'utilisateur courant de se délier
    public async Task<bool> UnlinkMySummonerAsync()
    {
        // On peut réutiliser l'endpoint admin en passant son propre ID, 
        // OU MIEUX : Créer un endpoint dédié "POST /dashboard/unlink" côté API.
        // Pour faire simple et rapide, créons un endpoint dédié.
        var response = await client.PostAsync("/api/lol/dashboard/unlink", null);
        return response.IsSuccessStatusCode;
    }
    
    public async Task<bool> DeleteMyTipAsync(int tipId)
    {
        var response = await client.DeleteAsync($"/api/lol/tips/{tipId}");
        return response.IsSuccessStatusCode;
    }
}