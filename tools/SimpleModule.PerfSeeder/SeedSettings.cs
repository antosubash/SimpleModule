using System.ComponentModel;
using Spectre.Console.Cli;

namespace SimpleModule.PerfSeeder;

public sealed class SeedSettings : CommandSettings
{
    [CommandOption("-m|--module <MODULE>")]
    [Description("Target module: products, orders, auditlogs, or all. Default: all.")]
    public string Module { get; set; } = "all";

    [CommandOption("-c|--count <COUNT>")]
    [Description(
        "Row count override. Applied per module. "
            + "Defaults: products=1000000, orders=100000, auditlogs=500000."
    )]
    public int? Count { get; set; }

    [CommandOption("--connection <CONNSTR>")]
    [Description(
        "Override the database connection string. By default reads Database:DefaultConnection from appsettings.json."
    )]
    public string? Connection { get; set; }

    [CommandOption("--provider <PROVIDER>")]
    [Description(
        "Override the database provider (Sqlite|PostgreSql|SqlServer). By default auto-detected from the connection string."
    )]
    public string? Provider { get; set; }

    [CommandOption("--project <PATH>")]
    [Description(
        "Path to the host project directory used for appsettings.json discovery. Default: ./template/SimpleModule.Host relative to the solution root."
    )]
    public string? ProjectPath { get; set; }

    [CommandOption("--batch-size <SIZE>")]
    [Description("Rows per SaveChanges batch. Default: 5000.")]
    public int BatchSize { get; set; } = 5000;

    [CommandOption("--seed <SEED>")]
    [Description("Randomizer seed for deterministic data generation. Default: 42.")]
    public int RandomSeed { get; set; } = 42;

    [CommandOption("--truncate")]
    [Description(
        "Delete existing rows in the target tables before seeding. Keeps Products rows with Id <= 10 (the migration seed)."
    )]
    public bool Truncate { get; set; }

    [CommandOption("--create-schema")]
    [Description(
        "Call EnsureCreated on each target DbContext before seeding. Useful against a fresh database when migrations are unavailable. Skips silently if tables already exist."
    )]
    public bool CreateSchema { get; set; }
}
