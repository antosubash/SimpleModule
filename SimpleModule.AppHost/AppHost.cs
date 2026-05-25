var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent);

var db = postgres.AddDatabase("simplemoduledb");

// Keycloak identity provider (opt-in via --launch-profile Keycloak)
var useKeycloak = builder.Configuration["Identity:Provider"] == "Keycloak";

IResourceBuilder<ContainerResource>? keycloak = null;
if (useKeycloak)
{
    var realmImportPath = Path.Combine(builder.AppHostDirectory, "keycloak");

    keycloak = builder
        .AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.2")
        .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
        .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
        .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", "admin")
        .WithEnvironment("KC_HTTP_ENABLED", "true")
        .WithEnvironment("KC_HOSTNAME_STRICT", "false")
        .WithEnvironment("KC_HEALTH_ENABLED", "true")
        .WithBindMount(realmImportPath, "/opt/keycloak/data/import", isReadOnly: true)
        .WithArgs("start-dev", "--import-realm")
        .WithLifetime(ContainerLifetime.Persistent);
}

var host = builder
    .AddProject<Projects.SimpleModule_Host>("simplemodule-host")
    .WithExternalHttpEndpoints()
    .WithReference(db)
    .WaitFor(db);

if (keycloak is not null)
{
    host.WithReference(keycloak.GetEndpoint("http"))
        .WaitFor(keycloak)
        .WithEnvironment("Identity__Provider", "Keycloak")
        .WithEnvironment("Keycloak__Authority", "http://localhost:8080/realms/simplemodule")
        .WithEnvironment("Keycloak__ClientId", "simplemodule-app")
        .WithEnvironment("Keycloak__ClientSecret", "simplemodule-dev-secret")
        .WithEnvironment("Keycloak__Realm", "simplemodule")
        .WithEnvironment(
            "Keycloak__AdminApiBaseUrl",
            "http://localhost:8080/admin/realms/simplemodule"
        )
        .WithEnvironment("Keycloak__AdminClientId", "simplemodule-admin")
        .WithEnvironment("Keycloak__AdminClientSecret", "simplemodule-admin-secret")
        .WithEnvironment("Keycloak__RequireHttpsMetadata", "false");
}

builder
    .AddProject<Projects.SimpleModule_Worker>("simplemodule-worker")
    .WithReference(db)
    .WaitFor(db)
    .WithReplicas(2);

builder.Build().Run();
