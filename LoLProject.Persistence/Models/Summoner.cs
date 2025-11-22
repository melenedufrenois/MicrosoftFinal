namespace LoLProject.Persistence.Models;
public class Summoner
{
    public int Id { get; set; }
    public required string Puuid { get; set; } // ID unique Riot Games
    public required string GameName { get; set; } // Ex: "Faker"
    public required string TagLine { get; set; } // Ex: "EUW"
    public long SummonerLevel { get; set; }
    public int ProfileIconId { get; set; }

    // Foreign Key vers AppUser
    public Guid AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;
}