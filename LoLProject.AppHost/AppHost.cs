using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql").WithDataVolume();
var db  = sql.AddDatabase("lolproject");

var apiService = builder.AddProject<Projects.LoLProject_ApiService>("apiservice")
                        .WithReference(db);

builder.AddProject<Projects.LoLProject_WebApp>("webapp")
       .WithReference(apiService);

builder.Build().Run();