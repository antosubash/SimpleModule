using SimpleModule.PerfSeeder;
using Spectre.Console.Cli;

var app = new CommandApp<SeedCommand>();
app.Configure(config =>
{
    config.SetApplicationName("SimpleModule.PerfSeeder");
    config.PropagateExceptions();
});
return await app.RunAsync(args).ConfigureAwait(false);
