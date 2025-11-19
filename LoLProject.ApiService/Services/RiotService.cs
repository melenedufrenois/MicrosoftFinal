using System.Net.Http.Json;
using Newtonsoft.Json.Linq;

namespace LoLProject.ApiService.Services;

public class RiotService(HttpClient httpClient, IConfiguration config, ILogger<RiotService> logger)
{
    private readonly string _apiKey = config["Riot:ApiKey"] ?? throw new Exception("Riot API Key manquante ! Vérifie appsettings.json");
    
    // "europe" pour Account-V1 et Match-V5
    private const string BaseUrlEurope = "https://europe.api.riotgames.com";
    // "euw1" pour Summoner-V4
    private const string BaseUrlEuw = "https://euw1.api.riotgames.com";

    public async Task<(string puuid, string gameName, string tagLine)?> GetAccountAsync(string gameName, string tagLine)
    {
        try
        {
            // IMPORTANT : On encode les noms (ex: "Hide on bush" -> "Hide%20on%20bush")
            var safeName = Uri.EscapeDataString(gameName);
            var safeTag = Uri.EscapeDataString(tagLine);

            var url = $"{BaseUrlEurope}/riot/account/v1/accounts/by-riot-id/{safeName}/{safeTag}?api_key={_apiKey}";
            
            // Log pour débugger si ça plante
            logger.LogInformation("Calling Riot Account: {Url}", url.Replace(_apiKey, "HIDDEN"));

            using var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Riot Account Error: {Code}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var obj = JObject.Parse(json);
            
            return (
                obj["puuid"]?.ToString() ?? "", 
                obj["gameName"]?.ToString() ?? "", 
                obj["tagLine"]?.ToString() ?? ""
            );
        }
        catch (Exception ex)
        { 
            logger.LogError(ex, "Exception during Riot Account fetch");
            return null; 
        }
    }

    public async Task<(int iconId, long level)?> GetSummonerInfoAsync(string puuid)
    {
        try
        {
            var url = $"{BaseUrlEuw}/lol/summoner/v4/summoners/by-puuid/{puuid}?api_key={_apiKey}";
            
            using var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var obj = JObject.Parse(json);
            
            return (
                obj["profileIconId"]?.Value<int>() ?? 0, 
                obj["summonerLevel"]?.Value<long>() ?? 0
            );
        }
        catch { return null; }
    }

    public async Task<List<dynamic>> GetLastMatchesAsync(string puuid, int count = 5)
    {
        var results = new List<dynamic>();
        try
        {
            // 1. Récupérer les IDs des matchs (EUROPE)
            var idsUrl = $"{BaseUrlEurope}/lol/match/v5/matches/by-puuid/{puuid}/ids?start=0&count={count}&api_key={_apiKey}";
            var matchIds = await httpClient.GetFromJsonAsync<List<string>>(idsUrl);

            // 2. Récupérer les détails (EUROPE)
            foreach (var matchId in matchIds ?? new List<string>())
            {
                var matchUrl = $"{BaseUrlEurope}/lol/match/v5/matches/{matchId}?api_key={_apiKey}";
                var response = await httpClient.GetAsync(matchUrl);
                if(!response.IsSuccessStatusCode) continue;

                var json = await response.Content.ReadAsStringAsync();
                var root = JObject.Parse(json);
                
                // Trouver le joueur dans la liste des participants
                var participant = root["info"]?["participants"]?.FirstOrDefault(p => p["puuid"]?.ToString() == puuid);

                if (participant != null)
                {
                    results.Add(new {
                        MatchId = matchId,
                        Champion = participant["championName"]?.ToString() ?? "Unknown",
                        Kills = participant["kills"]?.Value<int>() ?? 0,
                        Deaths = participant["deaths"]?.Value<int>() ?? 0,
                        Assists = participant["assists"]?.Value<int>() ?? 0,
                        Win = participant["win"]?.Value<bool>() ?? false,
                        Date = DateTimeOffset.FromUnixTimeMilliseconds(root["info"]?["gameCreation"]?.Value<long>() ?? 0).DateTime
                    });
                }
            }
        }
        catch(Exception ex) 
        {
            logger.LogError(ex, "Error fetching matches");
        }
        return results;
    }
}