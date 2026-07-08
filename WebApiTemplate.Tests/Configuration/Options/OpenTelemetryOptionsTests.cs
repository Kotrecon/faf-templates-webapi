using System.ComponentModel.DataAnnotations;
using OpenTelemetry.Exporter;
using WebApiTemplate.Configuration.Options;

namespace WebApiTemplate.Tests.Configuration.Options;

public class OpenTelemetryOptionsTests
{
    // ========================================================================
    // Функциональность
    // ========================================================================

    [Test]
    public async Task CanCreate_WithValidValues()
    {
        var options = new OpenTelemetryOptions
        {
            Endpoint = "http://localhost:4317",
            Protocol = OtlpExportProtocol.HttpProtobuf,
            UseConsoleExporter = true,
            Headers = new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer token123"
            },
            LogLevel = new Dictionary<string, string>
            {
                ["Default"] = "Warning",
                ["Microsoft"] = "Error"
            }
        };

        await Assert.That(options.Endpoint).IsEqualTo("http://localhost:4317");
        await Assert.That(options.Protocol).IsEqualTo(OtlpExportProtocol.HttpProtobuf);
        await Assert.That(options.UseConsoleExporter).IsTrue();
        await Assert.That(options.Headers).ContainsKey("Authorization");
        await Assert.That(options.LogLevel).ContainsKey("Microsoft");
    }

    [Test]
    public async Task SectionName_IsCorrect()
    {
        await Assert.That(OpenTelemetryOptions.SectionName).IsEqualTo("OpenTelemetry");
    }

    // ========================================================================
    // Defaults
    // ========================================================================

    [Test]
    public async Task DefaultValues_AreCorrect()
    {
        var options = new OpenTelemetryOptions();

        await Assert.That(options.Protocol).IsEqualTo(OtlpExportProtocol.Grpc);
        await Assert.That(options.UseConsoleExporter).IsFalse();
        await Assert.That(options.Headers).IsEmpty();
        await Assert.That(options.LogLevel).ContainsKey("Default");
        await Assert.That(options.LogLevel["Default"]).IsEqualTo("Information");
    }

    // ========================================================================
    // Валидация
    // ========================================================================

    private static bool Validate(OpenTelemetryOptions options, out List<ValidationResult> results)
    {
        results = new List<ValidationResult>();
        return Validator.TryValidateObject(options, new ValidationContext(options), results, true);
    }

    [Test]
    public async Task Validate_WhenEndpointIsNull_ReturnsErrorForEndpoint()
    {
        var options = new OpenTelemetryOptions { Endpoint = null! };

        var isValid = Validate(options, out var results);

        await Assert.That(isValid).IsFalse();
        await Assert.That(results).Count().IsEqualTo(1);
        await Assert.That(results[0].MemberNames).Contains("Endpoint");
    }
}