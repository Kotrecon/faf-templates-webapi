using System.ComponentModel.DataAnnotations;

namespace WebApiTemplate.Configuration.Options;

public sealed record ApiMetadataOptions
{
    public const string SectionName = "ApiMetadata";

    [Required]
    public string Title { get; init; } = null!;

    [Required]
    public string Version { get; init; } = null!;

    [Required]
    public string Description { get; init; } = null!;

    [Required]
    public ContactInfo Developer { get; init; } = null!;
}