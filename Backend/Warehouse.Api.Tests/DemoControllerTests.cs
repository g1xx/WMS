using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Warehouse.Api.Common;
using Warehouse.Api.Controllers;

namespace Warehouse.Api.Tests;

public class DemoControllerTests
{
    [Fact]
    public async Task GetHelp_DemoDisabled_Returns404WithoutTouchingAnyDependency()
    {
        // Both dependencies are deliberately null: this endpoint is [AllowAnonymous] and
        // serves credentials, a supervisor badge id, and live barcodes, so the config gate
        // has to be the very first thing it does. Passing nulls makes that structural — if
        // the gate ever stops short-circuiting and the method reaches the database or the
        // user manager, this test fails with a NullReferenceException instead of quietly
        // starting to leak data from a deployment that never opted in.
        var sut = new DemoController(null!, null!, new DemoSettings { Enabled = false });

        var result = await sut.GetHelp();

        // 404 rather than 403: with the demo off, the endpoint should look like it was never
        // deployed rather than advertising that there is a demo mode to switch on.
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void DemoSettings_DefaultsToDisabled()
    {
        // The default is the whole safety property: an appsettings.json with no DemoSettings
        // section (any real deployment) must leave the endpoint inert. Turning it on has to
        // be a deliberate act.
        new DemoSettings().Enabled.Should().BeFalse();
    }

    [Fact]
    public void DemoSettings_EnvironmentVariableOverridesAppsettingsFalse()
    {
        // This is the exact mechanism the deployed demo is switched on with: compose sets
        // DemoSettings__Enabled=true (see docker-compose.yml / DEMO_MODE in .env), which has
        // to beat the `false` committed in appsettings.json — under ASPNETCORE_ENVIRONMENT=
        // Production, without editing any appsettings file.
        //
        // Worth pinning rather than assuming, because both halves are silent when wrong:
        // renaming DemoSettings or Enabled, or losing the double underscore that maps to the
        // nested key, leaves the server quietly disabled with no error to notice.
        const string key = "DemoSettings__Enabled";
        var original = Environment.GetEnvironmentVariable(key);
        try
        {
            Environment.SetEnvironmentVariable(key, "true");

            var configuration = new ConfigurationBuilder()
                // Stands in for appsettings.json, which ships Enabled=false.
                .AddInMemoryCollection(new Dictionary<string, string?> { ["DemoSettings:Enabled"] = "false" })
                // Added after, exactly as the default host builder orders them — later
                // providers win.
                .AddEnvironmentVariables()
                .Build();

            var settings = configuration.GetSection("DemoSettings").Get<DemoSettings>();

            settings.Should().NotBeNull();
            settings!.Enabled.Should().BeTrue("the environment variable must outrank appsettings.json");
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, original);
        }
    }
}
