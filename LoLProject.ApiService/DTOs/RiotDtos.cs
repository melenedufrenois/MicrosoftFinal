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