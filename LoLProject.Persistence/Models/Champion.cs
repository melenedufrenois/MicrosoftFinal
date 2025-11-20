namespace LoLProject.Persistence.Models;

public class Champion
{
    public int Id { get; set; }
    public required string RiotId { get; set; } // "Aatrox"
    public required string RiotKey { get; set; } // "266"
    public required string Name { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    
    // Image verticale (Loading Screen) - Pas de version dans l'URL
    public string? ImageUrl { get; set; } 
    
    // Petite icône carrée - Version dans l'URL
    public string? IconUrl { get; set; } 

    public ICollection<ChampionTip> Tips { get; set; } = new List<ChampionTip>();
}