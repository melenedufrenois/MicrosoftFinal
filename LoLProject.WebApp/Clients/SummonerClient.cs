using System.Net.Http.Json;

namespace LoLProject.WebApp.Clients;

public class SummonerClient(HttpClient httpClient)
{
    public record SearchResult(string Puuid, string GameName, string TagLine, int IconId, long Level, bool IsFollowed);
    public record FollowRequest(string Puuid, string GameName, string TagLine, int IconId, long Level);
    
    // On utilise une classe simplifiée pour recevoir les données brutes
    public record SummonerDto(string GameName, string TagLine, int Followers);
    public record MatchDto(string ChampionName, int Kills, int Deaths, int Assists, bool Win, DateTime GameCreation);

    public async Task<SearchResult?> SearchAsync(string name, string tag)
    {
        var res = await httpClient.GetAsync($"/api/summoner/search/{name}/{tag}");
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<SearchResult>();
    }

    public async Task FollowAsync(FollowRequest req)
    {
        await httpClient.PostAsJsonAsync("/api/summoner/follow", req);
    }

    public async Task<List<SearchResult>> GetFavoritesAsync()
    {
        return await httpClient.GetFromJsonAsync<List<SearchResult>>("/api/summoner/favorites") ?? new();
    }
    
    public async Task<List<MatchDto>> GetMatchesAsync(string puuid)
    {
        return await httpClient.GetFromJsonAsync<List<MatchDto>>($"/api/summoner/matches/{puuid}") ?? new();
    }
    
    public async Task<List<SummonerDto>> GetAdminListAsync()
    {
        return await httpClient.GetFromJsonAsync<List<SummonerDto>>("/api/summoner/admin/all") ?? new();
    }
}