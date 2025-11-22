namespace LoLProject.ApiService.DTOs;

// DTOs existants (ne pas supprimer)
public record SummonerResponseDto(
    string GameName, 
    string TagLine, 
    int ProfileIconId, 
    long SummonerLevel);

public record AppUserResponseDto(Guid Id, 
    string Username, 
    string Email, 
    SummonerResponseDto? Summoner);

// 👇 NOUVEAUX DTOs POUR LES CHAMPIONS ET TIPS 👇

public class AuthorResponseDto
{
    public Guid Id { get; set; } // 👈 L'ID manquant
    public string Username { get; set; } = "";
}
public record TipResponseDto(
    int Id, 
    string Content, 
    DateTime CreatedAt, 
    AuthorResponseDto Author
);

public record ChampionDetailResponseDto(
    int Id, 
    string Name, 
    string Title, 
    string IconUrl,
    string Description,
    string ImageUrl,
    List<TipResponseDto> Tips,
    ChampionStatsResponseDto? Stats
);

public record ChampionStatsResponseDto(
    double Hp, double HpPerLevel,
    double Mp, double MpPerLevel,
    double MoveSpeed,
    double Armor, double ArmorPerLevel,
    double SpellBlock, double SpellBlockPerLevel,
    double AttackRange,
    double AttackDamage, double AttackDamagePerLevel,
    double AttackSpeed
);