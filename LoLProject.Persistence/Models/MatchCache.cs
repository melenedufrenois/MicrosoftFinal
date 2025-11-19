namespace LoLProject.Persistence.Models;

public class MatchCache
{
    public Guid Id { get; set; }
    public string RiotMatchId { get; set; } = string.Empty;
    public string ChampionName { get; set; } = string.Empty;
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public bool Win { get; set; }
    public DateTime GameCreation { get; set; }
    
    public Guid TrackedSummonerId { get; set; }
    public TrackedSummoner TrackedSummoner { get; set; } = null!;
}