using LoLProject.Persistence.Configurations;
using LoLProject.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace LoLProject.Persistence;

public sealed class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TodoItemConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}