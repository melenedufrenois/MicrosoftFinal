namespace LoLProject.Persistence.Models;

public class AppUser
{
    public Guid Id { get; set; }
    public string KeycloakId { get; set; } = string.Empty; // Le 'sub' du token
    public string Email { get; set; } = string.Empty;
    
    public List<Subscription> Subscriptions { get; set; } = new();
}