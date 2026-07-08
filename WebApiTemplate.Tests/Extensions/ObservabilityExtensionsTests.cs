using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApiTemplate.Extensions;
using Serilog.Events;

namespace WebApiTemplate.Tests.Extensions;

public class ObservabilityExtensionsTests
{
    // ========================================================================
    // AddCustomLogging — Serilog
    // ========================================================================

    [Test]
    public async Task AddCustomLogging_RegistersRootLevelSwitch()
    {
        var webBuilder = WebApplication.CreateBuilder();
        webBuilder.Configuration.Sources.Clear();
        webBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Serilog:MinimumLevel:Default"] = "Warning"
        });

        webBuilder.AddCustomLogging();
        var host = webBuilder.Build();

        var rootSwitch = host.Services.GetRequiredService<Serilog.Core.LoggingLevelSwitch>();

        await Assert.That(rootSwitch.MinimumLevel).IsEqualTo(LogEventLevel.Warning);
    }

    [Test]
    public async Task AddCustomLogging_WhenDefaultLevelMissing_UsesInformation()
    {
        var webBuilder = WebApplication.CreateBuilder();
        webBuilder.Configuration.Sources.Clear();
        // НЕ добавляем Serilog:MinimumLevel:Default

        webBuilder.AddCustomLogging();
        var host = webBuilder.Build();

        var rootSwitch = host.Services.GetRequiredService<Serilog.Core.LoggingLevelSwitch>();

        await Assert.That(rootSwitch.MinimumLevel).IsEqualTo(LogEventLevel.Information);
    }

    [Test]
    public async Task AddCustomLogging_RegistersOverrideSwitches()
    {
        var webBuilder = WebApplication.CreateBuilder();
        webBuilder.Configuration.Sources.Clear();
        webBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Serilog:MinimumLevel:Override:Microsoft"] = "Warning",
            ["Serilog:MinimumLevel:Override:System"] = "Error"
        });

        webBuilder.AddCustomLogging();
        var host = webBuilder.Build();

        var overrides = host.Services.GetRequiredService<IDictionary<string, Serilog.Core.LoggingLevelSwitch>>();

        await Assert.That(overrides).ContainsKey("Microsoft");
        await Assert.That(overrides).ContainsKey("System");
        await Assert.That(overrides["Microsoft"].MinimumLevel).IsEqualTo(LogEventLevel.Warning);
        await Assert.That(overrides["System"].MinimumLevel).IsEqualTo(LogEventLevel.Error);
    }

    // ========================================================================
    // AddCustomOpenTelemetry — регистрация
    // ========================================================================

    [Test]
    public async Task AddCustomOpenTelemetry_DoesNotThrow()
    {
        var webBuilder = WebApplication.CreateBuilder();
        webBuilder.Configuration.Sources.Clear();
        webBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AppSettings:ServiceName"] = "TestService",
            ["OpenTelemetry:Endpoint"] = "http://localhost:4317"
        });

        webBuilder.AddCustomOpenTelemetry();

        await Task.CompletedTask;
    }

    [Test]
    public async Task AddCustomOpenTelemetry_CanBuildHost()
    {
        var webBuilder = WebApplication.CreateBuilder();
        webBuilder.Configuration.Sources.Clear();
        webBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AppSettings:ServiceName"] = "TestService",
            ["OpenTelemetry:Endpoint"] = "http://localhost:4317"
        });

        webBuilder.AddCustomOpenTelemetry();
        var host = webBuilder.Build();

        await Assert.That(host).IsNotNull();
    }
}