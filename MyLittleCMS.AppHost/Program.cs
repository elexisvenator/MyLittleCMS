var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithArgs("postgres", "-c", "log_statement=all")
    .WithEnvironment("POSTGRES_DB", "my_little_cms")
    .WithPgAdmin(pgAdmin =>
    {
        pgAdmin.WithImage("dpage/pgadmin4", "latest");
    });
var mylittlecmsdb = postgres.AddDatabase("mylittlecmsdb", "my_little_cms");


builder.AddProject<Projects.MyLittleCMS_ApiService>("apiservice")
    .WithReference(mylittlecmsdb)
    .WaitFor(mylittlecmsdb);

builder.Build().Run();
