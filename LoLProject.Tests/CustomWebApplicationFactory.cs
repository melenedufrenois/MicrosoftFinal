using LoLProject.Persistence;
using Microsoft.AspNetCore.Authentication; // Important
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LoLProject.Tests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // 1. Nettoyage DB (Comme avant)
            var dbContextDescriptors = services.Where(d => 
                    d.ServiceType == typeof(DbContextOptions<AppDb>) || 
                    d.ServiceType == typeof(AppDb) ||
                    (d.ServiceType.FullName?.Contains("IDbContextPool") == true) ||
                    (d.ImplementationType?.FullName?.Contains("DbContextPool") == true))
                .ToList();

            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            // 2. Ajout DB InMemory (Comme avant)
            services.AddDbContext<AppDb>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);

            // 👇 3. AJOUT : Mock de l'authentification
            services.AddAuthentication(options =>
                {
                    // On force l'utilisation de notre schéma "Test" par défaut
                    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });
        });
    }
}