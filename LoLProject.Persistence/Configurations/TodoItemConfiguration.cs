using LoLProject.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoLProject.Persistence.Configurations;

public sealed class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> b)
    {
        b.ToTable("T_TodoItems");
        b.HasKey(t => t.Id);
        b.Property(t => t.Title).IsRequired().HasMaxLength(200);

        // Seed initial
        b.HasData(
            new TodoItem { Id = 1, Title = "Découvrir Aspire", Done = true },
            new TodoItem { Id = 2, Title = "Brancher EF Core", Done = false }
        );
    }
}