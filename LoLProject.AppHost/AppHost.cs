using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql").WithDataVolume();
var db  = sql.AddDatabase("lolproject");

builder.AddProject<Projects.LoLProject_ApiService>("apiservice")
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();