var builder = DistributedApplication.CreateBuilder(args);

var minioUser = builder.AddParameter("minio-username", secret: true);
var minioPass = builder.AddParameter("minio-password", secret: true);
var openrouterApiKey = builder.AddParameter("openrouter-api-key", secret: true);

var storage = builder.AddMinioContainer("storage", minioUser, minioPass)
    .WithDataVolume("aimy-storage")
    .WithLifetime(ContainerLifetime.Persistent);

var postgres = builder.AddPostgres("postgres")
    .WithImage("pgvector/pgvector")
    .WithImageTag("pg17")
    .WithPgWeb(pgWeb => pgWeb.WithHostPort(8081))
    .WithDataVolume("aimy-postgres")
    .WithLifetime(ContainerLifetime.Persistent);

var db = postgres.AddDatabase("aimydb");

var markitdown = builder.AddContainer("markitdown", "mcp/markitdown")
    .WithArgs("--http", "--host", "0.0.0.0", "--port", "3001")
    .WithHttpEndpoint(targetPort: 3001, name: "http")
    .WithLifetime(ContainerLifetime.Persistent);

var api = builder.AddProject<Projects.Aimy_API>("api")
    .WithReference(storage)
    .WithReference(db)
    .WaitFor(storage)
    .WaitFor(postgres)
    .WaitFor(markitdown)
    .WithEnvironment("MARKITDOWN_MCP_URL", markitdown.GetEndpoint("http"))
    .WithEnvironment("OPENROUTER_API_KEY", openrouterApiKey);

var frontend = builder.AddViteApp("frontend", "../../frontend")
    .WithReference(api)
    .WaitFor(api);

frontend.WithEndpoint("http", endpoint => endpoint.Port = 3000);

builder.Build().Run();
