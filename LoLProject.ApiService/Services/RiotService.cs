using System.Text.Json;
using LoLProject.ApiService.DTOs;
using LoLProject.Persistence;
using LoLProject.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LoLProject.ApiService.Services;

public class RiotService(HttpClient httpClient, IConfiguration configuration, IServiceProvider serviceProvider, ILogger<RiotService> logger, IMemoryCache cache)
{
    // RECUPERER LES CHAMPIONS DE RIOT ET LES SYNC DANS NOTRE BDD
    public async Task<int> SyncChampionsAsync()
    {
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
                var champion = new Champion
                {
                    RiotId = riotChamp.Id,
                    RiotKey = riotChamp.Key,
                    Name = riotChamp.Name,
                    Title = riotChamp.Title,
                    Description = riotChamp.Blurb,
                    ImageUrl = $"https://ddragon.leagueoflegends.com/cdn/img/champion/loading/{riotChamp.Id}_0.jpg",
                    IconUrl = $"https://ddragon.leagueoflegends.com/cdn/{latestVersion}/img/champion/{riotChamp.Id}.png",
                    
                    // 💡 AJOUT : Création de l'entité Stats liée
                    Stats = new ChampionStat
                    {
                        Hp = riotChamp.Stats.Hp,
                        HpPerLevel = riotChamp.Stats.HpPerLevel,
                        Mp = riotChamp.Stats.Mp,
                        MpPerLevel = riotChamp.Stats.MpPerLevel,
                        MoveSpeed = riotChamp.Stats.MoveSpeed,
                        Armor = riotChamp.Stats.Armor,
                        ArmorPerLevel = riotChamp.Stats.ArmorPerLevel,
                        SpellBlock = riotChamp.Stats.SpellBlock,
                        SpellBlockPerLevel = riotChamp.Stats.SpellBlockPerLevel,
                        AttackRange = riotChamp.Stats.AttackRange,
                        HpRegen = riotChamp.Stats.HpRegen,
                        HpRegenPerLevel = riotChamp.Stats.HpRegenPerLevel,
                        MpRegen = riotChamp.Stats.MpRegen,
                        MpRegenPerLevel = riotChamp.Stats.MpRegenPerLevel,
                        Crit = riotChamp.Stats.Crit,
                        CritPerLevel = riotChamp.Stats.CritPerLevel,
                        AttackDamage = riotChamp.Stats.AttackDamage,
                        AttackDamagePerLevel = riotChamp.Stats.AttackDamagePerLevel,
                        AttackSpeed = riotChamp.Stats.AttackSpeed,
                        AttackSpeedPerLevel = riotChamp.Stats.AttackSpeedPerLevel
                    }
                };

                newChampions.Add(champion);
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
    
    // RECUPERER UN INVOCATEUR VIA SON RIOT ID (GameName + TagLine)
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
            var accountUrl = $"https://europe.api.riotgames.com/riot/account/v1/accounts/by-riot-id/{safeGameName}/{safeTagLine}";
            
            var accountDto = await httpClient.GetFromJsonAsync<RiotAccountDto>(accountUrl);
            if (accountDto == null) 
            {
                logger.LogWarning("Compte Riot introuvable (Account-V1 renvoie null).");
                return null;
            }

            logger.LogInformation($"PUUID trouvé : {accountDto.Puuid}. Recherche invocateur...");

            // ÉTAPE B : Summoner-V4 (Plateforme 'euw1' pour l'Europe de l'Ouest)
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
    
    public async Task<DashboardStatsResponseDto?> GetLastMatchesStatsAsync(string puuid)
    {
        // 1. MISE EN CACHE : On vérifie si on a déjà les stats pour ce PUUID
        // On garde les données 10 minutes pour éviter de spammer F5 et perdre ses crédits API
        string cacheKey = $"stats_{puuid}";
        
        if (cache.TryGetValue(cacheKey, out DashboardStatsResponseDto? cachedStats))
        {
            logger.LogInformation("Stats récupérées depuis le cache !");
            return cachedStats;
        }

        var apiKey = configuration["RiotApi:ApiKey"];
        httpClient.DefaultRequestHeaders.Remove("X-Riot-Token");
        httpClient.DefaultRequestHeaders.Add("X-Riot-Token", apiKey);

        try
        {
            // --- 1. LANCEMENT DES REQUÊTES EN PARALLÈLE ---
            
            // A. Requête Historique (EUROPE)
            var matchIdsTask = httpClient.GetFromJsonAsync<List<string>>(
                $"https://europe.api.riotgames.com/lol/match/v5/matches/by-puuid/{puuid}/ids?start=0&count=20");

            // B. Requête Rang (EUW1 - Attention au domaine !) 
            var leagueTask = httpClient.GetFromJsonAsync<List<RiotLeagueEntryDto>>(
                $"https://euw1.api.riotgames.com/lol/league/v4/entries/by-puuid/{puuid}");

            // On attend que les deux répondent (ou plantent)
            await Task.WhenAll(matchIdsTask, leagueTask);

            var matchIds = matchIdsTask.Result;
            var leagueEntries = leagueTask.Result;

            // --- 2. TRAITEMENT DU RANG ---
            
            var soloQ = leagueEntries?.FirstOrDefault(l => l.QueueType == "RANKED_SOLO_5x5");
            
            // Valeurs par défaut si Unranked
            string tier = "Unranked";
            string rank = "";
            int lp = 0;
            int rWins = 0;
            int rLosses = 0;

            if (soloQ != null)
            {
                tier = soloQ.Tier;     // ex: EMERALD
                rank = soloQ.Rank;     // ex: II
                lp = soloQ.LeaguePoints;
                rWins = soloQ.Wins;
                rLosses = soloQ.Losses;
            }

            // --- 3. TRAITEMENT DES MATCHS (Code existant avec Chunking) ---
            
            if (matchIds == null || matchIds.Count == 0) return null;

            var history = new List<MatchSummaryDto>();
            var allPlayerStats = new List<RiotParticipantDto>();
            var chunks = matchIds.Chunk(10);

            foreach (var chunk in chunks)
            {
                // ... (Ton code existant de chunking et Task.WhenAll des détails de matchs) ...
                // ... (Copie-colle ton code précédent ici pour la boucle des matchs) ...
                 var tasks = chunk.Select(async id => 
                    {
                        try 
                        {
                            return await httpClient.GetFromJsonAsync<RiotMatchDto>($"https://europe.api.riotgames.com/lol/match/v5/matches/{id}");
                        }
                        catch { return null; }
                    });

                var matchesResults = await Task.WhenAll(tasks);

                foreach (var match in matchesResults)
                {
                    if (match?.Info == null) continue;
                    var playerDto = match.Info.Participants.FirstOrDefault(p => p.Puuid == puuid);
                    if (playerDto == null) continue;

                    allPlayerStats.Add(playerDto);
                    history.Add(new MatchSummaryDto
                    {
                        GameMode = match.Info.GameMode == "CLASSIC" ? "Faille" : "ARAM",
                        Duration = TimeSpan.FromSeconds(match.Info.GameDuration),
                        GameDate = DateTimeOffset.FromUnixTimeMilliseconds(match.Info.GameCreation).UtcDateTime,
                        Stats = playerDto
                    });
                }
                if (chunk.Length == 10) await Task.Delay(1200);
            }

            if (history.Count == 0) return null;

            // --- 4. CALCUL ET RETOUR ---

            // Calculs existants
            var totalGames = allPlayerStats.Count;
            var wins = allPlayerStats.Count(s => s.Win);
            var totalKills = allPlayerStats.Sum(s => (double)s.Kills);
            var totalDeaths = allPlayerStats.Sum(s => (double)s.Deaths);
            var totalAssists = allPlayerStats.Sum(s => (double)s.Assists);
            double globalKda = totalDeaths == 0 ? totalKills + totalAssists : (totalKills + totalAssists) / totalDeaths;
            double totalCs = allPlayerStats.Sum(s => s.TotalCs);
            double totalMinutes = history.Sum(h => h.Duration.TotalMinutes);

            var overview = new OverviewStatsDto
            {
                TotalGames = totalGames,
                Wins = wins,
                Losses = totalGames - wins,
                WinRate = totalGames > 0 ? Math.Round((double)wins / totalGames * 100, 0) : 0,
                AvgKills = Math.Round(allPlayerStats.Average(s => s.Kills), 1),
                AvgDeaths = Math.Round(allPlayerStats.Average(s => s.Deaths), 1),
                AvgAssists = Math.Round(allPlayerStats.Average(s => s.Assists), 1),
                AvgKda = Math.Round(globalKda, 2),
                AvgCsPerMinute = totalMinutes > 0 ? Math.Round(totalCs / totalMinutes, 1) : 0,
                AvgGold = Math.Round(allPlayerStats.Average(s => s.GoldEarned), 0),
                AvgDamage = Math.Round(allPlayerStats.Average(s => s.TotalDamageDealtToChampions), 0),

                // 👇 ON AJOUTE LES INFOS DE RANG ICI
                SoloQueueTier = tier,
                SoloQueueRank = rank,
                SoloQueueLp = lp,
                TotalRankedWins = rWins,
                TotalRankedLosses = rLosses
            };

            var result = new DashboardStatsResponseDto
            {
                Overview = overview,
                MatchHistory = history.OrderByDescending(h => h.GameDate).ToList()
            };

            // ... (Mise en cache et retour) ...
            var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
            cache.Set(cacheKey, result, cacheOptions);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError($"Erreur Stats : {ex.Message}");
            return null;
        }
    }
}