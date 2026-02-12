using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Aimy_API>("api");

builder.AddViteApp("frontend", "../../frontend")
    .WithReference(api);

builder.Build().Run();
