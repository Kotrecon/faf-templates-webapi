using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using WebApiTemplate.Configuration.Options;
using WebApiTemplate.Extensions;

namespace WebApiTemplate.Tests.Extensions;

public class ConfigurationExtensionsTests
{
    // ========================================================================
    // AddAppSettings
    // ========================================================================

    [Test]
    public async Task AddAppSettings_WhenSectionExists_ReturnsTrue()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AppSettings:ServiceName"] = "TestService",
            ["AppSettings:Port"] = "8080"
        });

        var result = builder.AddAppSettings();

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task AddAppSettings_WhenSectionExists_RegistersOptions()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AppSettings:ServiceName"] = "TestService",
            ["AppSettings:Port"] = "8080"
        });

        builder.AddAppSettings();
        var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptions<AppSettings>>().Value;

        await Assert.That(options.ServiceName).IsEqualTo("TestService");
        await Assert.That(options.Port).IsEqualTo(8080);
    }

    [Test]
    public async Task AddAppSettings_WhenSectionMissing_ReturnsFalse()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.Sources.Clear();
        // НЕ добавляем AppSettings секцию

        var result = builder.AddAppSettings();

        await Assert.That(result).IsFalse();
    }

    // ========================================================================
    // AddApiMetadata
    // ========================================================================

    [Test]
    public async Task AddApiMetadata_WhenSectionExists_ReturnsTrue()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ApiMetadata:Title"] = "Test API",
            ["ApiMetadata:Version"] = "1.0.0",
            ["ApiMetadata:Description"] = "Test description",
            ["ApiMetadata:Developer:Name"] = "Test Dev",
            ["ApiMetadata:Developer:Url"] = "https://example.com"
        });

        var result = builder.AddApiMetadata();

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task AddApiMetadata_WhenSectionExists_RegistersOptions()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ApiMetadata:Title"] = "Test API",
            ["ApiMetadata:Version"] = "1.0.0",
            ["ApiMetadata:Description"] = "Test description",
            ["ApiMetadata:Developer:Name"] = "Test Dev",
            ["ApiMetadata:Developer:Url"] = "https://example.com"
        });

        builder.AddApiMetadata();
        var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptions<ApiMetadataOptions>>().Value;

        await Assert.That(options.Title).IsEqualTo("Test API");
        await Assert.That(options.Version).IsEqualTo("1.0.0");
        await Assert.That(options.Developer.Name).IsEqualTo("Test Dev");
    }

    [Test]
    public async Task AddApiMetadata_WhenSectionMissing_ReturnsFalse()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.Sources.Clear();

        var result = builder.AddApiMetadata();

        await Assert.That(result).IsFalse();
    }

    // ========================================================================
    // AddJwt
    // ========================================================================

    [Test]
    public async Task AddJwt_WhenSectionExists_ReturnsTrue()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = new string('K', 32),
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience"
        });

        var result = builder.AddJwt();

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task AddJwt_WhenSectionExists_RegistersOptions()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = new string('K', 32),
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience"
        });

        builder.AddJwt();
        var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptions<JwtOptions>>().Value;

        await Assert.That(options.Key.Length).IsEqualTo(32);
        await Assert.That(options.Issuer).IsEqualTo("TestIssuer");
        await Assert.That(options.Audience).IsEqualTo("TestAudience");
    }

    [Test]
    public async Task AddJwt_WhenSectionMissing_ReturnsFalse()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.Sources.Clear();

        var result = builder.AddJwt();

        await Assert.That(result).IsFalse();
    }

    // ========================================================================
    // AddOpenTelemetryOptions
    // ========================================================================

    [Test]
    public async Task AddOpenTelemetryOptions_WhenSectionExists_ReturnsTrue()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OpenTelemetry:Endpoint"] = "http://localhost:4317"
        });

        var result = builder.AddOpenTelemetryOptions();

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task AddOpenTelemetryOptions_WhenSectionExists_RegistersOptions()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OpenTelemetry:Endpoint"] = "http://localhost:4317",
            ["OpenTelemetry:Protocol"] = "HttpProtobuf",
            ["OpenTelemetry:UseConsoleExporter"] = "true"
        });

        builder.AddOpenTelemetryOptions();
        var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;

        await Assert.That(options.Endpoint).IsEqualTo("http://localhost:4317");
        await Assert.That(options.Protocol).IsEqualTo(OtlpExportProtocol.HttpProtobuf);
        await Assert.That(options.UseConsoleExporter).IsTrue();
    }

    [Test]
    public async Task AddOpenTelemetryOptions_WhenSectionMissing_ReturnsFalse()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.Sources.Clear();

        var result = builder.AddOpenTelemetryOptions();

        await Assert.That(result).IsFalse();
    }
}