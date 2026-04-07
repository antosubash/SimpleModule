using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Menu;
using SimpleModule.Core.Settings;
using SimpleModule.Database;
using SimpleModule.FileStorage.Contracts;

namespace SimpleModule.FileStorage;

[Module(
    FileStorageConstants.ModuleName,
    RoutePrefix = FileStorageConstants.RoutePrefix,
    ViewPrefix = FileStorageConstants.ViewPrefix
)]
public class FileStorageModule : IModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<FileStorageDbContext>(
            configuration,
            FileStorageConstants.ModuleName
        );
        services.AddScoped<IFileStorageContracts, FileStorageService>();
    }

    public void ConfigureMenu(IMenuBuilder menus)
    {
        menus.Add(
            new MenuItem
            {
                Label = "Files",
                Url = "/files",
                Icon =
                    """<svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z"/></svg>""",
                Order = 50,
                Section = MenuSection.AppSidebar,
            }
        );
    }

    public void ConfigurePermissions(PermissionRegistryBuilder builder)
    {
        builder.AddPermissions<FileStoragePermissions>();
    }

    public void ConfigureSettings(ISettingsBuilder settings)
    {
        settings.Add(
            new SettingDefinition
            {
                Key = "FileStorage.MaxFileSizeMb",
                DisplayName = "Max File Size (MB)",
                Description = "Maximum allowed file size for uploads in megabytes.",
                Group = "FileStorage",
                Scope = SettingScope.Application,
                DefaultValue = "50",
                Type = SettingType.Number,
            }
        );
        settings.Add(
            new SettingDefinition
            {
                Key = "FileStorage.AllowedExtensions",
                DisplayName = "Allowed File Extensions",
                Description =
                    "Comma-separated list of allowed file extensions (e.g., .jpg,.pdf,.zip).",
                Group = "FileStorage",
                Scope = SettingScope.Application,
                DefaultValue = ".jpg,.jpeg,.png,.gif,.pdf,.doc,.docx,.xls,.xlsx,.zip",
                Type = SettingType.Text,
            }
        );
    }
}
