namespace LoLProject.Persistence.Models; // Au lieu de ApiService.Models
public class AppUser
{
    public Guid Id { get; set; }
    public required string KeycloakId { get; set; } // L'ID unique venant de Keycloak (le 'sub')
    public required string Username { get; set; }
    public string? Email { get; set; }

    // Relations
    public Summoner? Summoner { get; set; } // Un user a 0 ou 1 compte LoL lié
    public ICollection<ChampionTip> Tips { get; set; } = new List<ChampionTip>();
    public ICollection<TipLike> Likes { get; set; } = new List<TipLike>();
}