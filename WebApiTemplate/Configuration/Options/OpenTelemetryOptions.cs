using System.ComponentModel.DataAnnotations;
using OpenTelemetry.Exporter;

namespace WebApiTemplate.Configuration.Options;

public sealed record OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    [Required]
    public string Endpoint { get; init; } = null!;

    public OtlpExportProtocol Protocol { get; init; } = OtlpExportProtocol.Grpc;

    public Dictionary<string, string> Headers { get; init; } = new();

    public bool UseConsoleExporter { get; init; } = false;

    public Dictionary<string, string> LogLevel { get; init; } = new()
    {
        ["Default"] = "Information"
    };
}