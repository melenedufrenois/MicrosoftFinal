using System.Text.Json.Serialization;

namespace LoLProject.ApiService.DTOs;

// Réponse globale contenant la liste "data"
public class RiotChampionResponse
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("format")]
    public string Format { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("data")]
    public Dictionary<string, RiotChampionDto> Data { get; set; } = new();
}

// Le détail d'un champion venant du JSON Riot
public class RiotChampionDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";   // ex: "Aatrox" (Id Texte)

    [JsonPropertyName("key")]
    public string Key { get; set; } = "";  // ex: "266" (Id Numérique)

    [JsonPropertyName("name")]
    public string Name { get; set; } = ""; // ex: "Aatrox"

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("blurb")]
    public string Blurb { get; set; } = "";
    
    [JsonPropertyName("image")]
    public RiotImageDto Image { get; set; } = new();
    
    [JsonPropertyName("stats")]
    public RiotChampionStatsDto Stats { get; set; } = new();

}

public class RiotChampionStatsDto
{
    [JsonPropertyName("hp")]
    public double Hp { get; set; }
    [JsonPropertyName("hpperlevel")]
    public double HpPerLevel { get; set; }
    
    [JsonPropertyName("mp")]
    public double Mp { get; set; }
    [JsonPropertyName("mpperlevel")]
    public double MpPerLevel { get; set; }
    
    [JsonPropertyName("movespeed")]
    public double MoveSpeed { get; set; }
    
    [JsonPropertyName("armor")]
    public double Armor { get; set; }
    [JsonPropertyName("armorperlevel")]
    public double ArmorPerLevel { get; set; }
    
    [JsonPropertyName("spellblock")]
    public double SpellBlock { get; set; }
    [JsonPropertyName("spellblockperlevel")]
    public double SpellBlockPerLevel { get; set; }
    
    [JsonPropertyName("attackrange")]
    public double AttackRange { get; set; }
    
    [JsonPropertyName("hpregen")]
    public double HpRegen { get; set; }
    [JsonPropertyName("hpregenperlevel")]
    public double HpRegenPerLevel { get; set; }
    
    [JsonPropertyName("mpregen")]
    public double MpRegen { get; set; }
    [JsonPropertyName("mpregenperlevel")]
    public double MpRegenPerLevel { get; set; }
    
    [JsonPropertyName("crit")]
    public double Crit { get; set; }
    [JsonPropertyName("critperlevel")]
    public double CritPerLevel { get; set; }
    
    [JsonPropertyName("attackdamage")]
    public double AttackDamage { get; set; }
    [JsonPropertyName("attackdamageperlevel")]
    public double AttackDamagePerLevel { get; set; }
    
    [JsonPropertyName("attackspeedperlevel")]
    public double AttackSpeedPerLevel { get; set; }
    [JsonPropertyName("attackspeed")]
    public double AttackSpeed { get; set; }
}

public class RiotImageDto 
{
    [JsonPropertyName("full")]
    public string Full { get; set; } = ""; // ex: "Aatrox.png"
}

// Réponse de l'API Account-V1 (Recherche par Riot ID)
public class RiotAccountDto
{
    [JsonPropertyName("puuid")]
    public string Puuid { get; set; } = "";
    
    [JsonPropertyName("gameName")]
    public string GameName { get; set; } = "";
    
    [JsonPropertyName("tagLine")]
    public string TagLine { get; set; } = "";
}

// Réponse de l'API Summoner-V4 (Détails du compte)
public class RiotSummonerDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = ""; // L'ID crypté (pour les rangs plus tard)

    [JsonPropertyName("accountId")]
    public string AccountId { get; set; } = "";

    [JsonPropertyName("puuid")]
    public string Puuid { get; set; } = "";

    [JsonPropertyName("profileIconId")]
    public int ProfileIconId { get; set; }

    [JsonPropertyName("summonerLevel")]
    public long SummonerLevel { get; set; }
}

public record LinkSummonerRequest(string GameName, string TagLine);

// DTO pour l'envoi d'un tip (POST)
public class CreateTipRequest
{
    public string Content { get; set; } = "";
}

public class RiotMatchDto
{
    [JsonPropertyName("info")]
    public RiotMatchInfoDto Info { get; set; } = new();
}

public class RiotMatchInfoDto
{
    [JsonPropertyName("gameDuration")]
    public long GameDuration { get; set; } // Durée en secondes

    [JsonPropertyName("gameCreation")]
    public long GameCreation { get; set; } // Date

    [JsonPropertyName("gameMode")]
    public string GameMode { get; set; } = ""; 

    [JsonPropertyName("participants")]
    public List<RiotParticipantDto> Participants { get; set; } = new();
}

public class RiotParticipantDto
{
    [JsonPropertyName("puuid")]
    public string Puuid { get; set; } = "";

    [JsonPropertyName("championName")]
    public string ChampionName { get; set; } = "";
    
    [JsonPropertyName("championId")]
    public int ChampionId { get; set; }

    [JsonPropertyName("champLevel")]
    public int ChampLevel { get; set; }

    [JsonPropertyName("kills")]
    public int Kills { get; set; }

    [JsonPropertyName("deaths")]
    public int Deaths { get; set; }

    [JsonPropertyName("assists")]
    public int Assists { get; set; }

    [JsonPropertyName("win")]
    public bool Win { get; set; }
    
    [JsonPropertyName("teamPosition")]
    public string TeamPosition { get; set; } = "";

    // --- FARM ---
    [JsonPropertyName("totalMinionsKilled")]
    public int TotalMinionsKilled { get; set; }
    
    [JsonPropertyName("totalAllyJungleMinionsKilled")]
    public int TotalAllyJungleMinionsKilled { get; set; }
    
    [JsonPropertyName("totalEnemyJungleMinionsKilled")]
    public int TotalEnemyJungleMinionsKilled { get; set; }

    // Propriété calculée pour le CS Total
    public int TotalCs => TotalMinionsKilled + TotalAllyJungleMinionsKilled + TotalEnemyJungleMinionsKilled;

    // --- ECONOMIE & DEGATS ---
    [JsonPropertyName("goldEarned")]
    public int GoldEarned { get; set; }

    [JsonPropertyName("totalDamageDealtToChampions")]
    public int TotalDamageDealtToChampions { get; set; }

    [JsonPropertyName("totalDamageTaken")]
    public int TotalDamageTaken { get; set; }

    [JsonPropertyName("damageDealtToBuildings")]
    public int DamageDealtToBuildings { get; set; }
}

// --- DTOs POUR LE FRONTEND (Dashboard) ---

public class DashboardStatsResponseDto
{
    public OverviewStatsDto Overview { get; set; } = new();
    public List<MatchSummaryDto> MatchHistory { get; set; } = new();
}

public class OverviewStatsDto
{
    public int TotalGames { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double WinRate { get; set; }
    
    // Moyennes
    public double AvgKills { get; set; }
    public double AvgDeaths { get; set; }
    public double AvgAssists { get; set; }
    public double AvgKda { get; set; }
    public double AvgCs { get; set; }
    public double AvgCsPerMinute { get; set; }
    public double AvgGold { get; set; }
    public double AvgDamage { get; set; }
    public string SoloQueueTier { get; set; } = "Unranked"; 
    public string SoloQueueRank { get; set; } = "";
    public int SoloQueueLp { get; set; }
    public int TotalRankedWins { get; set; }
    public int TotalRankedLosses { get; set; }
}

public class MatchSummaryDto
{
    public string GameMode { get; set; } = "";
    public TimeSpan Duration { get; set; }
    public DateTime GameDate { get; set; }
    public RiotParticipantDto Stats { get; set; } = new();
}

public class RiotLeagueEntryDto
{
    [JsonPropertyName("queueType")]
    public string QueueType { get; set; } = ""; // ex: "RANKED_SOLO_5x5"

    [JsonPropertyName("tier")]
    public string Tier { get; set; } = ""; // ex: "GOLD"

    [JsonPropertyName("rank")]
    public string Rank { get; set; } = ""; // ex: "IV"

    [JsonPropertyName("leaguePoints")]
    public int LeaguePoints { get; set; }

    [JsonPropertyName("wins")]
    public int Wins { get; set; }

    [JsonPropertyName("losses")]
    public int Losses { get; set; }
}

public record AdminTipDto(int Id, string Content, DateTime CreatedAt, string AuthorName, string ChampionName, string ChampionIconUrl);
public record UpdateTipRequest(string Content);