using Microsoft.EntityFrameworkCore;
using LoLProject.ApiService.Models;

namespace LoLProject.ApiService.Data;

public sealed class AppDb(DbContextOptions<AppDb> opt) : DbContext(opt)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();
}