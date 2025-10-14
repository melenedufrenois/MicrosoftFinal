var builder = DistributedApplication.CreateBuilder(args);
builder.AddProject<Projects.LoLProject_ApiService>("apiservice");
builder.Build().Run();