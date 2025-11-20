using System.Text.Json;
using LoLProject.ApiService.DTOs;
using LoLProject.Persistence;
using LoLProject.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace LoLProject.ApiService.Services;

public class RiotService(HttpClient httpClient, IConfiguration configuration, IServiceProvider serviceProvider, ILogger<RiotService> logger)
{
    // Récupérer les champions (Déjà existant)
    public async Task<int> SyncChampionsAsync()
    {
        // ... (Garde ton code de synchro existant ici ou copie celui d'avant) ...
        // Pour simplifier le fichier, je me concentre sur la nouvelle méthode :
        
        var versions = await httpClient.GetFromJsonAsync<string[]>("https://ddragon.leagueoflegends.com/api/versions.json");
        if (versions == null || versions.Length == 0) throw new Exception("Version introuvable");
        var latestVersion = versions[0];

        var url = $"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/data/fr_FR/champion.json";
        var response = await httpClient.GetFromJsonAsync<RiotChampionResponse>(url);

        if (response?.Data == null) return 0;

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var existingRiotIds = await db.Champions.Select(c => c.RiotId).ToHashSetAsync(); 
        var newChampions = new List<Champion>();
        int count = 0;

        foreach (var riotChamp in response.Data.Values)
        {
            if (!existingRiotIds.Contains(riotChamp.Id))
            {
                newChampions.Add(new Champion
                {
                    RiotId = riotChamp.Id,
                    RiotKey = riotChamp.Key,
                    Name = riotChamp.Name,
                    Title = riotChamp.Title,
                    Description = riotChamp.Blurb,
                    ImageUrl = $"https://ddragon.leagueoflegends.com/cdn/img/champion/loading/{riotChamp.Id}_0.jpg",
                    IconUrl = $"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/img/champion/{riotChamp.Id}.png"
                });
                count++;
            }
        }

        if (newChampions.Any())
        {
            db.Champions.AddRange(newChampions);
            await db.SaveChangesAsync();
        }
        return count;
    }

    // --- NOUVELLE MÉTHODE ROBUSTE ---
    public async Task<Summoner?> GetSummonerByRiotIdAsync(string gameName, string tagLine)
    {
        var apiKey = configuration["RiotApi:ApiKey"];
        if (string.IsNullOrEmpty(apiKey)) 
        {
            logger.LogError("Clé API Riot manquante dans appsettings.json !");
            throw new Exception("Clé API Riot manquante !");
        }
        
        httpClient.DefaultRequestHeaders.Remove("X-Riot-Token");
        httpClient.DefaultRequestHeaders.Add("X-Riot-Token", apiKey);

        // 1. Nettoyage et Encodage des paramètres (Vital pour les espaces)
        var safeGameName = Uri.EscapeDataString(gameName.Trim());
        var safeTagLine = Uri.EscapeDataString(tagLine.Trim());

        try 
        {
            logger.LogInformation($"Recherche compte Riot : {safeGameName}#{safeTagLine} ...");

            // ÉTAPE A : Account-V1 (Region 'europe' couvre EUW, EUNE, TR, RU)
            // Si tu testes un compte NA, il faut changer 'europe' par 'americas'
            var accountUrl = $"https://europe.api.riotgames.com/riot/account/v1/accounts/by-riot-id/{safeGameName}/{safeTagLine}";
            
            var accountDto = await httpClient.GetFromJsonAsync<RiotAccountDto>(accountUrl);
            if (accountDto == null) 
            {
                logger.LogWarning("Compte Riot introuvable (Account-V1 renvoie null).");
                return null;
            }

            logger.LogInformation($"PUUID trouvé : {accountDto.Puuid}. Recherche invocateur...");

            // ÉTAPE B : Summoner-V4 (Plateforme 'euw1' pour l'Europe de l'Ouest)
            // Si ton compte est EUNE, il faut mettre 'eun1'. Si NA, 'na1'.
            // Pour l'instant on hardcode EUW1 pour le projet.
            var summonerUrl = $"https://euw1.api.riotgames.com/lol/summoner/v4/summoners/by-puuid/{accountDto.Puuid}";
            
            var summonerDto = await httpClient.GetFromJsonAsync<RiotSummonerDto>(summonerUrl);
            if (summonerDto == null)
            {
                logger.LogWarning("Invocateur introuvable sur EUW1 (Summoner-V4 renvoie null).");
                return null;
            }

            logger.LogInformation("Invocateur trouvé avec succès !");

            return new Summoner
            {
                Puuid = summonerDto.Puuid,
                GameName = accountDto.GameName,
                TagLine = accountDto.TagLine,
                SummonerLevel = summonerDto.SummonerLevel,
                ProfileIconId = summonerDto.ProfileIconId
            };
        }
        catch (HttpRequestException ex)
        {
            logger.LogError($"Erreur API Riot ({ex.StatusCode}) : {ex.Message}");
            
            // 403 = Clé API expirée ou invalide
            if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                throw new Exception("Clé API Riot expirée ou invalide !");
                
            // 404 = Pas trouvé
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound) 
                return null;
                
            throw;
        }
    }
}