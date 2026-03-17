var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").WithDataVolume().WithPgAdmin().WithLifetime(ContainerLifetime.Persistent);

var db = postgres.AddDatabase("simplemoduledb");

builder
    .AddProject<Projects.SimpleModule_Host>("simplemodule-host")
    .WithExternalHttpEndpoints()
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();
