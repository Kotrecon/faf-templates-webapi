using System.ComponentModel.DataAnnotations;

namespace WebApiTemplate.Configuration.Options;

public sealed record AppSettings
{
    public const string SectionName = "AppSettings";

    [Required]
    public string ServiceName { get; init; } = null!;

    [Range(1, 65535)]
    public int Port { get; init; }
}