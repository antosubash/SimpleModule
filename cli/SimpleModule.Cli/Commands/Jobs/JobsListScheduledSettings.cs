using System.ComponentModel;
using Spectre.Console.Cli;

namespace SimpleModule.Cli.Commands.Jobs;

public sealed class JobsListScheduledSettings : CommandSettings
{
    [Description(
        "Override the database connection string (e.g. \"Data Source=app.db\" or "
            + "\"Host=localhost;Database=app;Username=...;Password=...\"). "
            + "Defaults to Database:DefaultConnection from appsettings*.json."
    )]
    [CommandOption("-c|--connection <CONNECTION>")]
    public string? ConnectionString { get; set; }

    [Description("Database provider when --connection is supplied: Sqlite or Postgres.")]
    [CommandOption("-p|--provider <PROVIDER>")]
    public string? Provider { get; set; }
}
