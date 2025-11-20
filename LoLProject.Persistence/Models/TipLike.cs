namespace LoLProject.Persistence.Models; // Au lieu de ApiService.Models
public class TipLike
{
    public int ChampionTipId { get; set; }
    public ChampionTip ChampionTip { get; set; } = null!;

    public Guid AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;
}