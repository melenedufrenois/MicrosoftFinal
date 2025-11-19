using Microsoft.EntityFrameworkCore;
using LoLProject.Persistence.Models;

namespace LoLProject.Persistence;

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>(); // Ton existant
    
    // Tes nouvelles tables
    public DbSet<TrackedSummoner> TrackedSummoners => Set<TrackedSummoner>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<MatchCache> Matches => Set<MatchCache>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Index unique pour éviter les doublons
        builder.Entity<TrackedSummoner>().HasIndex(s => s.Puuid).IsUnique();
        builder.Entity<AppUser>().HasIndex(u => u.KeycloakId).IsUnique();
        builder.Entity<MatchCache>().HasIndex(m => new { m.RiotMatchId, m.TrackedSummonerId }).IsUnique();
    }
}