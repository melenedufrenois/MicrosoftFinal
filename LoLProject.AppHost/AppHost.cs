using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// 1) Ajouter le conteneur SQL Server
var sql = builder.AddSqlServer("sql").WithDataVolume(); // optionnel: persistance

// 2) Déclarer la base "lolproject" sur ce SQL Server
var database = sql.AddDatabase("lolproject");

// 3) Référencer la DB sur ton API (injecte la connection string + ordre de démarrage)
builder.AddProject<Projects.LoLProject_ApiService>("apiservice")
    .WithReference(database);

builder.Build().Run();