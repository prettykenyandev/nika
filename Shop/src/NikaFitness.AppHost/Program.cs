var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL with a persistent volume and a single application database.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var nikadb = postgres.AddDatabase("nikadb");

// Redis for transient cart storage.
var redis = builder.AddRedis("redis");

// The ASP.NET Core API, wired to Postgres and Redis.
var api = builder.AddProject<Projects.NikaFitness_Api>("api")
    .WithReference(nikadb)
    .WithReference(redis)
    .WaitFor(nikadb)
    .WaitFor(redis);

// The Next.js storefront. Pinned to port 3000 so the API CORS origin stays stable in dev.
builder.AddNpmApp("storefront", "../../storefront", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("NEXT_PUBLIC_API_URL", api.GetEndpoint("http"))
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

// The Next.js admin / inventory app. Lives in a sibling folder next to "Shop".
// It talks to the API server-side (no CORS needed) and writes to the same database,
// so products it creates appear on the storefront. Pinned to port 3001.
builder.AddNpmApp("admin", "../../../Admin", "dev")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("API_URL", api.GetEndpoint("http"))
    .WithEnvironment("NEXT_PUBLIC_STOREFRONT_URL", "http://localhost:3000")
    .WithHttpEndpoint(port: 3001, env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();

