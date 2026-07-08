using System.ComponentModel.DataAnnotations;
using WebApiTemplate.Configuration.Options;

namespace WebApiTemplate.Tests.Configuration.Options;

public class AppSettingsTests
{
    // ========================================================================
    // Функциональность
    // ========================================================================

    [Test]
    public async Task CanCreate_WithValidValues()
    {
        var options = new AppSettings { ServiceName = "TestService", Port = 8080 };

        await Assert.That(options.ServiceName).IsEqualTo("TestService");
        await Assert.That(options.Port).IsEqualTo(8080);
    }

    [Test]
    public async Task SectionName_IsCorrect()
    {
        await Assert.That(AppSettings.SectionName).IsEqualTo("AppSettings");
    }

    // ========================================================================
    // Валидация
    // ========================================================================

    private static bool Validate(AppSettings options, out List<ValidationResult> results)
    {
        results = new List<ValidationResult>();
        return Validator.TryValidateObject(options, new ValidationContext(options), results, true);
    }

    // ------------------------------------------------------------------------
    // ServiceName — [Required]
    // ------------------------------------------------------------------------

    [Test]
    public async Task Validate_WhenServiceNameIsNull_ReturnsErrorForServiceName()
    {
        var options = new AppSettings { ServiceName = null!, Port = 8080 };

        var isValid = Validate(options, out var results);

        await Assert.That(isValid).IsFalse();
        await Assert.That(results.Any(r => r.MemberNames.Contains("ServiceName"))).IsTrue();
    }

    // ------------------------------------------------------------------------
    // Port — [Range(1, 65535)]
    // Граничные значения: 0 (invalid), 1 (valid), 65535 (valid), 65536 (invalid)
    // ------------------------------------------------------------------------

    [Test]
    public async Task Validate_WhenPortIsZero_ReturnsErrorForPort()
    {
        var options = new AppSettings { ServiceName = "ValidService", Port = 0 };

        var isValid = Validate(options, out var results);

        await Assert.That(isValid).IsFalse();
        await Assert.That(results.Any(r => r.MemberNames.Contains("Port"))).IsTrue();
    }

    [Test]
    public async Task Validate_WhenPortIsMin_ReturnsValid()
    {
        var options = new AppSettings { ServiceName = "ValidService", Port = 1 };

        var isValid = Validate(options, out var results);

        await Assert.That(isValid).IsTrue();
    }

    [Test]
    public async Task Validate_WhenPortIsMax_ReturnsValid()
    {
        var options = new AppSettings { ServiceName = "ValidService", Port = 65535 };

        var isValid = Validate(options, out var results);

        await Assert.That(isValid).IsTrue();
    }

    [Test]
    public async Task Validate_WhenPortIsAboveMax_ReturnsErrorForPort()
    {
        var options = new AppSettings { ServiceName = "ValidService", Port = 65536 };

        var isValid = Validate(options, out var results);

        await Assert.That(isValid).IsFalse();
        await Assert.That(results.Any(r => r.MemberNames.Contains("Port"))).IsTrue();
    }

    // ------------------------------------------------------------------------
    // Комбинированные ошибки
    // ------------------------------------------------------------------------

    [Test]
    public async Task Validate_WhenBothFieldsInvalid_ReturnsMultipleErrors()
    {
        var options = new AppSettings { ServiceName = null!, Port = 0 };

        var isValid = Validate(options, out var results);

        await Assert.That(isValid).IsFalse();
        await Assert.That(results.Count).IsGreaterThanOrEqualTo(2);
        await Assert.That(results.Any(r => r.MemberNames.Contains("ServiceName"))).IsTrue();
        await Assert.That(results.Any(r => r.MemberNames.Contains("Port"))).IsTrue();
    }
}