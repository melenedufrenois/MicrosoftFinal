using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// 1. SQL Server : On laisse Aspire gérer le mot de passe.
// On ajoute WithDataVolume pour ne plus perdre les données au redémarrage.
var sql = builder.AddSqlServer("sql")
    .WithDataVolume() 
    .WithLifetime(ContainerLifetime.Persistent);

// 2. La Base de Données
var db = sql.AddDatabase("lolproject");

// 3. L'API : Elle recevra la bonne connexion automatiquement via WithReference
var apiService = builder.AddProject<Projects.LoLProject_ApiService>("apiservice")
    .WithReference(db)
    .WaitFor(db);

// 4. Keycloak : Persistant aussi
var keycloak = builder.AddKeycloak("keycloak", 8090)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// 5. Le Front
builder.AddProject<Projects.LoLProject_WebApp>("webapp")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();