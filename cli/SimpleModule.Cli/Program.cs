using SimpleModule.Cli.Commands.Dev;
using SimpleModule.Cli.Commands.Doctor;
using SimpleModule.Cli.Commands.Install;
using SimpleModule.Cli.Commands.List;
using SimpleModule.Cli.Commands.New;
using SimpleModule.Cli.Commands.Skill;
using SimpleModule.Cli.Commands.Version;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("sm");
    config.SetApplicationVersion(VersionCommand.ResolveVersion());

    config.AddExample("new", "project", "MyApp");
    config.AddExample("new", "module", "Customers");
    config.AddExample("new", "feature", "CreateCustomer", "--module", "Customers");
    config.AddExample("dev");
    config.AddExample("list");
    config.AddExample("doctor", "--fix");
    config.AddExample("skill", "add", "shadcn", "--source", "shadcn/ui/skills/shadcn");
    config.AddExample("skill", "update");

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
                .WithExample("new", "module", "Customers");
            newBranch
                .AddCommand<NewFeatureCommand>("feature")
                .WithDescription("Add a feature to an existing module")
                .WithExample("new", "feature", "CreateCustomer", "--module", "Customers");
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

    config.AddBranch(
        "skill",
        skillBranch =>
        {
            skillBranch.SetDescription("Manage Claude skills under .claude/skills");
            skillBranch
                .AddCommand<SkillAddCommand>("add")
                .WithDescription("Add a Claude skill (from GitHub, a local path, or a scaffold)")
                .WithExample("skill", "add", "shadcn", "--source", "shadcn/ui/skills/shadcn")
                .WithExample("skill", "add", "my-skill")
                .WithExample("skill", "add", "team-skill", "--source", "./skills/team-skill");
            skillBranch
                .AddCommand<SkillUpdateCommand>("update")
                .WithDescription("Re-fetch tracked skills and refresh skills-lock.json")
                .WithExample("skill", "update")
                .WithExample("skill", "update", "shadcn", "--ref", "main")
                .WithExample("skill", "update", "--check");
            skillBranch
                .AddCommand<SkillListCommand>("list")
                .WithDescription("List installed Claude skills and their tracked sources");
        }
    );

    config.AddCommand<VersionCommand>("version").WithDescription("Print the sm CLI version");
});

return app.Run(args);
