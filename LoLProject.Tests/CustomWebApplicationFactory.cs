using LoLProject.ApiService.Services; // Pour IRiotService
using LoLProject.Persistence; // Pour AppDb
using Microsoft.AspNetCore.Authentication; // Pour l'Auth
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost; // Pour ConfigureTestServices
using Microsoft.EntityFrameworkCore; // Pour InMemory
using Microsoft.Extensions.DependencyInjection;
using Moq; // Pour le Mock

namespace LoLProject.Tests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    public Mock<IRiotService> RiotServiceMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 1. On force l'environnement "Testing"
        // Cela désactive le AddSqlServerDbContext dans Program.cs
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // 2. On ajoute la base de données en mémoire
            // Comme Program.cs n'a pas ajouté SQL Server (grâce à l'environnement), 
            // on peut ajouter InMemory sans conflit ni nettoyage compliqué.
            services.AddDbContext<AppDb>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // 3. On Mock l'authentification
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });

            // 4. On remplace le vrai RiotService par le Mock
            var riotDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRiotService));
            if (riotDescriptor != null) services.Remove(riotDescriptor);
            services.AddSingleton(RiotServiceMock.Object);
        });
    }
}