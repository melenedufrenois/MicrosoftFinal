namespace LoLProject.Persistence.Models;

public class Subscription
{
    public int Id { get; set; }
    public Guid TrackedSummonerId { get; set; }
    public TrackedSummoner TrackedSummoner { get; set; } = null!;
    
    public Guid AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;
    
    public DateTime FollowedAt { get; set; } = DateTime.UtcNow;
}