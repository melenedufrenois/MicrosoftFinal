var builder = DistributedApplication.CreateBuilder(args);

builder.Build().Run();

builder.AddProject<Projects.LoLProject_ApiService>("apiservice");