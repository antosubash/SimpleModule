using SimpleModule.Cli.Commands.Dev;
using SimpleModule.Cli.Commands.Doctor;
using SimpleModule.Cli.Commands.Install;
using SimpleModule.Cli.Commands.New;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("sm");

    config.AddBranch(
        "new",
        newBranch =>
        {
            newBranch.SetDescription("Create new projects, modules, or features");
            newBranch
                .AddCommand<NewProjectCommand>("project")
                .WithDescription("Scaffold a new SimpleModule solution");
            newBranch
                .AddCommand<NewModuleCommand>("module")
                .WithDescription("Scaffold a new module");
            newBranch
                .AddCommand<NewFeatureCommand>("feature")
                .WithDescription("Add a feature to an existing module");
            newBranch
                .AddCommand<NewAgentCommand>("agent")
                .WithDescription("Add an AI agent to an existing module");
        }
    );

    config
        .AddCommand<DevCommand>("dev")
        .WithDescription(
            "Start the development environment (dotnet watch + Vite dev server with HMR)"
        );

    config
        .AddCommand<InstallCommand>("install")
        .WithDescription("Install a SimpleModule package from NuGet");

    config
        .AddCommand<DoctorCommand>("doctor")
        .WithDescription("Validate project structure and conventions");
});

return app.Run(args);
