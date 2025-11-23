using LoLProject.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LoLProject.Tests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 1. On force l'environnement "Testing"
        // Cela permet au Program.cs de sauter l'étape SQL Server d'Aspire
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // 2. On injecte notre base de données en mémoire
            // Comme SQL Server n'a pas été ajouté, il n'y a aucun conflit à nettoyer !
            services.AddDbContext<AppDb>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });
        });
    }
}