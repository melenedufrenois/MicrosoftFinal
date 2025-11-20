namespace LoLProject.ApiService.DTOs;

// DTOs existants (ne pas supprimer)
public record SummonerResponseDto(string GameName, string TagLine, int ProfileIconId, long SummonerLevel);
public record AppUserResponseDto(Guid Id, string Username, string Email, SummonerResponseDto? Summoner);

// 👇 NOUVEAUX DTOs POUR LES CHAMPIONS ET TIPS 👇

public record AuthorResponseDto(string Username);

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
    List<TipResponseDto> Tips
);