#:sdk Aspire.AppHost.Sdk@13.4.6
#:package Aspire.Hosting.JavaScript@13.4.6
#:package Aspire.Hosting.PostgreSQL@13.4.6
#:project services/api/GymAlternatief.Api.csproj

var builder = DistributedApplication.CreateBuilder(args);

var database = builder
    .AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("gymalternatief");

var api = builder
    .AddProject<Projects.GymAlternatief_Api>("api")
    .WithReference(database)
    .WaitFor(database);

builder
    .AddViteApp("web", "apps/web")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
