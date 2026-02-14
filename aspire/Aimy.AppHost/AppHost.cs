using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var minioUser = builder.AddParameter("minio-username", secret: true);
var minioPass = builder.AddParameter("minio-password", secret: true);

var storage = builder.AddMinioContainer("storage", minioUser, minioPass)
    .WithDataVolume("aimy-storage");

var postgres = builder.AddPostgres("postgres").WithPgWeb()
    .WithDataVolume("aimy-postgres");

var db = postgres.AddDatabase("aimydb");

var api = builder.AddProject<Projects.Aimy_API>("api")
    .WithReference(storage)
    .WithReference(db)
    .WaitFor(storage)
    .WaitFor(postgres);

builder.AddViteApp("frontend", "../../frontend")
    .WithReference(api);

builder.Build().Run();