using System.ComponentModel.DataAnnotations;

namespace WebApiTemplate.Configuration.Options;

public sealed record ContactInfo
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = null!;

    [EmailAddress]
    [StringLength(254)]
    public string? Email { get; init; }

    [Required]
    [Url]
    [StringLength(500)]
    public string Url { get; init; } = null!;
}