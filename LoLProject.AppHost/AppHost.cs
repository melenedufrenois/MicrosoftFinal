// LoLProject.AppHost/Program.cs
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql").WithDataVolume();
var db  = sql.AddDatabase("lolproject");

var keycloack = builder.AddKeycloak("keycloak", 8090)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var apiService = builder.AddProject<Projects.LoLProject_ApiService>("apiservice")
    .WithReference(db)
    .WaitFor(db)
    .WithReference(keycloack); // <--- AJOUTE ÇA (L'API doit valider les tokens)

builder.AddProject<Projects.LoLProject_WebApp>("webapp")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithReference(keycloack); // <--- AJOUTE ÇA (Le Front doit rediriger vers Keycloak)

builder.Build().Run();