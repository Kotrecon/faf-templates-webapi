using System.ComponentModel.DataAnnotations;

namespace WebApiTemplate.Configuration.Options;

public sealed record JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    [MinLength(32, ErrorMessage = "Jwt:Key must be at least 32 characters")]
    public string Key { get; init; } = null!;

    [Required]
    public string Issuer { get; init; } = null!;

    [Required]
    public string Audience { get; init; } = null!;
}