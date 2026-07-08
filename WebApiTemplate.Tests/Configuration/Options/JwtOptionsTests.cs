using System.ComponentModel.DataAnnotations;
using WebApiTemplate.Configuration.Options;

namespace WebApiTemplate.Tests.Configuration.Options;

public class JwtOptionsTests
{
    // ========================================================================
    // Функциональность
    // ========================================================================

    [Test]
    public async Task CanCreate_WithValidValues()
    {
        var options = new JwtOptions
        {
            Key = new string('K', 32),
            Issuer = "TestIssuer",
            Audience = "TestAudience"
        };

        await Assert.That(options.Key.Length).IsEqualTo(32);
        await Assert.That(options.Issuer).IsEqualTo("TestIssuer");
        await Assert.That(options.Audience).IsEqualTo("TestAudience");
    }

    [Test]
    public async Task SectionName_IsCorrect()
    {
        await Assert.That(JwtOptions.SectionName).IsEqualTo("Jwt");
    }

    // ========================================================================
    // Валидация
    // ========================================================================

    private static bool Validate(JwtOptions options, out List<ValidationResult> results)
    {
        results = new List<ValidationResult>();
        return Validator.TryValidateObject(options, new ValidationContext(options), results, true);
    }

    // ------------------------------------------------------------------------
    // Key — [Required] + [MinLength(32)]
    // ------------------------------------------------------------------------

    [Test]
    public async Task Validate_WhenKeyIsNull_ReturnsErrorForKey()
    {
        var options = new JwtOptions { Key = null!, Issuer = "I", Audience = "A" };

        var isValid = Validate(options, out var results);

        await Assert.That(isValid).IsFalse();
        await Assert.That(results.Any(r => r.MemberNames.Contains("Key"))).IsTrue();
    }

    [Test]
    public async Task Validate_WhenKeyIs31Chars_ReturnsErrorForKey()
    {
        var options = new JwtOptions
        {
            Key = new string('K', 31),  // меньше MinLength = 32
            Issuer = "I",
            Audience = "A"
        };

        var isValid = Validate(options, out var results);

        await Assert.That(isValid).IsFalse();
        await Assert.That(results.Any(r => r.MemberNames.Contains("Key"))).IsTrue();
    }

    [Test]
    public async Task Validate_WhenKeyIs32Chars_ReturnsValid()
    {
        var options = new JwtOptions
        {
            Key = new string('K', 32),  // граница MinLength
            Issuer = "I",
            Audience = "A"
        };

        var isValid = Validate(options, out var results);

        await Assert.That(isValid).IsTrue();
        await Assert.That(results).IsEmpty();
    }

    // ------------------------------------------------------------------------
    // Issuer — [Required]
    // ------------------------------------------------------------------------

    [Test]
    public async Task Validate_WhenIssuerIsNull_ReturnsErrorForIssuer()
    {
        var options = new JwtOptions { Key = new string('K', 32), Issuer = null!, Audience = "A" };

        var isValid = Validate(options, out var results);

        await Assert.That(isValid).IsFalse();
        await Assert.That(results.Any(r => r.MemberNames.Contains("Issuer"))).IsTrue();
    }

    // ------------------------------------------------------------------------
    // Audience — [Required]
    // ------------------------------------------------------------------------

    [Test]
    public async Task Validate_WhenAudienceIsNull_ReturnsErrorForAudience()
    {
        var options = new JwtOptions { Key = new string('K', 32), Issuer = "I", Audience = null! };

        var isValid = Validate(options, out var results);

        await Assert.That(isValid).IsFalse();
        await Assert.That(results.Any(r => r.MemberNames.Contains("Audience"))).IsTrue();
    }

    // ------------------------------------------------------------------------
    // Комбинированные ошибки
    // ------------------------------------------------------------------------

    [Test]
    public async Task Validate_WhenAllFieldsInvalid_ReturnsMultipleErrors()
    {
        var options = new JwtOptions { Key = null!, Issuer = null!, Audience = null! };

        var isValid = Validate(options, out var results);

        await Assert.That(isValid).IsFalse();
        await Assert.That(results.Count).IsGreaterThanOrEqualTo(3);
        await Assert.That(results.Any(r => r.MemberNames.Contains("Key"))).IsTrue();
        await Assert.That(results.Any(r => r.MemberNames.Contains("Issuer"))).IsTrue();
        await Assert.That(results.Any(r => r.MemberNames.Contains("Audience"))).IsTrue();
    }
}