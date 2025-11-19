namespace LoLProject.Persistence.Models;

public class TrackedSummoner
{
    public Guid Id { get; set; }
    public string Puuid { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public string TagLine { get; set; } = string.Empty;
    public int ProfileIconId { get; set; }
    public long SummonerLevel { get; set; }
    
    public List<Subscription> Subscriptions { get; set; } = new();
    public List<MatchCache> Matches { get; set; } = new();
}