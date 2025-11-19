using System.Net.Http.Json;

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
        var response = await client.PostAsJsonAsync($"/api/lol/champions/{championId}/tips", content);
        return response.IsSuccessStatusCode;
    }
    
    // Lier un compte Riot
    public async Task<bool> LinkSummonerAsync(string gameName, string tagLine)
    {
        // On utilise un objet anonyme ou un DTO spécifique, les deux marchent ici
        var response = await client.PostAsJsonAsync("/api/lol/dashboard/link", new { GameName = gameName, TagLine = tagLine });
        return response.IsSuccessStatusCode;
    }

    // Récupérer mon dashboard
    public async Task<AppUserDto?> GetMyDashboardAsync()
    {
        try {
            return await client.GetFromJsonAsync<AppUserDto>("/api/lol/dashboard");
        } catch { return null; }
    }
}

// --- DTOs ---

public class ChampionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public string IconUrl { get; set; } = "";
}

public class ChampionDetailDto : ChampionDto
{
    public string Description { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public List<TipDto> Tips { get; set; } = new();
}

public class TipDto
{
    public int Id { get; set; }
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public AuthorDto Author { get; set; } = new();
}

public class AuthorDto
{
    public string Username { get; set; } = "";
}

public class AppUserDto
{
    public Guid Id { get; set; } // Guid côté serveur, int dans votre ancien code, vérifiez bien le type
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public SummonerDto? Summoner { get; set; }
}

// DTO Sans boucle de référence
public class SummonerDto
{
    public string GameName { get; set; } = "";
    public string TagLine { get; set; } = "";
    public int ProfileIconId { get; set; }      
    public long SummonerLevel { get; set; }     
}