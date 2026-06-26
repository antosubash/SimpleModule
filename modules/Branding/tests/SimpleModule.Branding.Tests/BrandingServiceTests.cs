using System.Text.Json;
using FluentAssertions;
using SimpleModule.Branding.Contracts;
using SimpleModule.Core.Settings;
using Xunit;

namespace SimpleModule.Branding.Tests;

public class BrandingServiceTests
{
    [Fact]
    public async Task GetBrandingAsync_ReturnsDefaults_WhenNothingStored()
    {
        var svc = new BrandingService(new FakeSettings());

        var dto = await svc.GetBrandingAsync();

        dto.AppName.Should().Be(BrandingDefaults.AppName);
        dto.ColorPrimary.Should().Be(BrandingDefaults.ColorPrimary);
        dto.ColorPrimaryDark.Should().Be(BrandingDefaults.ColorPrimaryDark);
        dto.LogoUrl.Should().BeNull();
        dto.FaviconUrl.Should().BeNull();
        dto.TopBar.Enabled.Should().BeFalse();
        dto.Footer.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task Update_Then_Get_RoundTrips()
    {
        var settings = new FakeSettings();
        var svc = new BrandingService(settings);

        await svc.UpdateAsync(
            new BrandingEditModel
            {
                AppName = "Acme",
                ColorPrimary = "#112233",
                CustomCss = ".x{color:red}",
                TopBar = new TopBarConfig { Enabled = true, Message = "Hi" },
                Footer = new FooterConfig { Enabled = true, Text = "© Acme" },
            }
        );

        var dto = await svc.GetBrandingAsync();
        dto.AppName.Should().Be("Acme");
        dto.ColorPrimary.Should().Be("#112233");
        dto.TopBar.Message.Should().Be("Hi");
        dto.Footer.Text.Should().Be("© Acme");
        (await svc.GetCustomCssAsync()).Should().Be(".x{color:red}");
    }

    [Fact]
    public async Task GetBrandingAsync_BuildsLogoUrl_WhenFileIdSet()
    {
        var settings = new FakeSettings();
        await settings.SetSettingAsync(
            BrandingSettingKeys.LogoFileId,
            JsonSerializer.SerializeToElement("42"),
            SettingScope.Application
        );
        var svc = new BrandingService(settings);

        var dto = await svc.GetBrandingAsync();

        dto.LogoUrl.Should().Be("/api/branding/assets/logo?v=42");
    }
}
