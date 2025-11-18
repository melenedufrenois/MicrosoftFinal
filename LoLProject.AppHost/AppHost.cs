// LoLProject.AppHost/Program.cs
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql").WithDataVolume();
var db  = sql.AddDatabase("lolproject");

var apiService = builder.AddProject<Projects.LoLProject_ApiService>("apiservice")
    .WithReference(db)
    .WaitFor(db);

var keycloack = builder.AddKeycloak("keycloak", 8090)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.LoLProject_WebApp>("webapp")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();