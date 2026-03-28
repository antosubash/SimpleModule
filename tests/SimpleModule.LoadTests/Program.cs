using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using SimpleModule.LoadTests.Infrastructure;
using SimpleModule.LoadTests.Scenarios;

// Parse --scenario argument
var scenarioFilter = args
    .SkipWhile(a => a != "--scenario")
    .Skip(1)
    .FirstOrDefault();

using var serviceAccount = new ServiceAccount();
var client = serviceAccount.Client;

// Register all scenarios
var allScenarios = new Dictionary<string, ScenarioProps>(StringComparer.OrdinalIgnoreCase)
{
    ["products_crud"] = ProductsScenario.Create(client),
    ["orders_crud"] = OrdersScenario.Create(client),
    ["users_crud"] = UsersScenario.Create(client),
    ["settings_ops"] = SettingsScenario.Create(client),
    ["auditlogs_read"] = AuditLogsScenario.Create(client),
    ["files_ops"] = FileStorageScenario.Create(client),
    ["pagebuilder_crud"] = PageBuilderScenario.Create(client),
    ["admin_ops"] = AdminScenario.Create(client),
    ["mixed_realistic"] = MixedWorkloadScenario.Create(client),
};

// Select scenarios to run
ScenarioProps[] scenarios = scenarioFilter is not null && allScenarios.TryGetValue(scenarioFilter, out var selected)
    ? [selected]
    : allScenarios.Values.ToArray();

Console.WriteLine($"Running {scenarios.Length} scenario(s)...");
if (scenarioFilter is not null)
    Console.WriteLine($"Filter: {scenarioFilter}");

NBomberRunner
    .RegisterScenarios(scenarios)
    .WithReportFolder("reports")
    .WithReportFileName("loadtest_report")
    .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
    .Run();
