namespace LoLProject.Persistence.Models; // Au lieu de ApiService.Models
public class ChampionTip
{
    public int Id { get; set; }
    public required string Content { get; set; } // Le texte de l'astuce
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relations
    public int ChampionId { get; set; }
    public Champion Champion { get; set; } = null!;

    public Guid AppUserId { get; set; }
    public AppUser Author { get; set; } = null!;

    public ICollection<TipLike> Likes { get; set; } = new List<TipLike>();
}