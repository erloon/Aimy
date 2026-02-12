using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var minioUser = builder.AddParameter("minio-username", secret: true);
var minioPass = builder.AddParameter("minio-password", secret: true);

var storage = builder.AddMinioContainer("storage", minioUser, minioPass)
    .WithDataVolume("aimy-storage");

var api = builder.AddProject<Projects.Aimy_API>("api")
    .WithReference(storage)
    .WaitFor(storage);

builder.AddViteApp("frontend", "../../frontend")
    .WithReference(api);

builder.Build().Run();