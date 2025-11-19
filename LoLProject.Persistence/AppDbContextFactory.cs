using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using LoLProject.Persistence.Models;

namespace LoLProject.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDb>
{
    public AppDb CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDb>();
    
        var connectionString = "Server=localhost,14345;Database=LoLProject;User Id=sa;Password=yourStrong(!)Password;TrustServerCertificate=True;";

        optionsBuilder.UseSqlServer(connectionString);

        return new AppDb(optionsBuilder.Options);
    }
}