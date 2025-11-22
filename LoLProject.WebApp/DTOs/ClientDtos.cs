namespace LoLProject.WebApp.DTOs;

// --- Champions ---

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
    public ChampionStatsDto? Stats { get; set; }
}

//  DTO des Stats (Miroir de celui de l'API)
public class ChampionStatsDto
{
    public double Hp { get; set; }
    public double HpPerLevel { get; set; }
    public double Mp { get; set; }
    public double MpPerLevel { get; set; }
    public double MoveSpeed { get; set; }
    public double Armor { get; set; }
    public double ArmorPerLevel { get; set; }
    public double SpellBlock { get; set; }
    public double SpellBlockPerLevel { get; set; }
    public double AttackRange { get; set; }
    public double AttackDamage { get; set; }
    public double AttackDamagePerLevel { get; set; }
    public double AttackSpeed { get; set; }
}

// --- Tips ---

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

// DTO pour l'envoi d'un tip (POST)
public class CreateTipRequest
{
    public string Content { get; set; } = "";
}

// --- User & Summoner ---

public class AppUserDto
{
    public Guid Id { get; set; } 
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public SummonerDto? Summoner { get; set; }
}

// DTO Sans boucle de référence (User -> Summoner -> STOP)
public class SummonerDto
{
    public string GameName { get; set; } = "";
    public string TagLine { get; set; } = "";
    public int ProfileIconId { get; set; }      
    public long SummonerLevel { get; set; }     
}

// DTO pour la liaison de compte (POST)
public class LinkSummonerRequest
{
    public string GameName { get; set; } = "";
    public string TagLine { get; set; } = "";
}