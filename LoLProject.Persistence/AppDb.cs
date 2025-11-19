using Microsoft.EntityFrameworkCore;
using LoLProject.Persistence.Models; // Attention au changement ici

namespace LoLProject.Persistence; // Namespace changé

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    // ... (Ton code avec les DbSet reste identique) ...
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Summoner> Summoners => Set<Summoner>();
    public DbSet<Champion> Champions => Set<Champion>();
    public DbSet<ChampionTip> ChampionTips => Set<ChampionTip>();
    public DbSet<TipLike> TipLikes => Set<TipLike>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // ... (Ton code de configuration reste identique) ...
        // Configuration de la relation User <-> Summoner (1 to 1)
        modelBuilder.Entity<AppUser>()
            .HasOne(a => a.Summoner)
            .WithOne(s => s.AppUser)
            .HasForeignKey<Summoner>(s => s.AppUserId);

        // Configuration de la clé composite pour les Likes (Many to Many manuel)
        modelBuilder.Entity<TipLike>()
            .HasKey(tl => new { tl.ChampionTipId, tl.AppUserId });

        modelBuilder.Entity<TipLike>()
            .HasOne(tl => tl.ChampionTip)
            .WithMany(t => t.Likes)
            .HasForeignKey(tl => tl.ChampionTipId)
            .OnDelete(DeleteBehavior.NoAction); 

        modelBuilder.Entity<TipLike>()
            .HasOne(tl => tl.AppUser)
            .WithMany(u => u.Likes)
            .HasForeignKey(tl => tl.AppUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}