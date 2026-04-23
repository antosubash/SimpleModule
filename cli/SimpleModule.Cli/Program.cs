using SimpleModule.Cli.Commands.Dev;
using SimpleModule.Cli.Commands.Doctor;
using SimpleModule.Cli.Commands.Install;
using SimpleModule.Cli.Commands.List;
using SimpleModule.Cli.Commands.New;
using SimpleModule.Cli.Commands.Version;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("sm");
    config.SetApplicationVersion(VersionCommand.ResolveVersion());

    config.AddExample("new", "project", "MyApp");
    config.AddExample("new", "module", "Products");
    config.AddExample("new", "feature", "CreateProduct", "--module", "Products");
    config.AddExample("dev");
    config.AddExample("list");
    config.AddExample("doctor", "--fix");

    config.AddBranch(
        "new",
        newBranch =>
        {
            newBranch.SetDescription("Create new projects, modules, or features");
            newBranch
                .AddCommand<NewProjectCommand>("project")
                .WithDescription("Scaffold a new SimpleModule solution")
                .WithExample("new", "project", "MyApp");
            newBranch
                .AddCommand<NewModuleCommand>("module")
                .WithDescription("Scaffold a new module")
                .WithExample("new", "module", "Products");
            newBranch
                .AddCommand<NewFeatureCommand>("feature")
                .WithDescription("Add a feature to an existing module")
                .WithExample("new", "feature", "CreateProduct", "--module", "Products");
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
        .AddCommand<ListCommand>("list")
        .WithDescription("List modules in the current project with their route prefixes");

    config
        .AddCommand<InstallCommand>("install")
        .WithDescription("Install a SimpleModule package from NuGet");

    config
        .AddCommand<DoctorCommand>("doctor")
        .WithDescription("Validate project structure and conventions");

    config.AddCommand<VersionCommand>("version").WithDescription("Print the sm CLI version");
});

return app.Run(args);
