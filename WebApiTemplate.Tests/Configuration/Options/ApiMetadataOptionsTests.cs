using System.ComponentModel.DataAnnotations;
using WebApiTemplate.Configuration.Options;
using WebApiTemplate.Tests.Helpers;

namespace WebApiTemplate.Tests.Configuration.Options;

public class ApiMetadataOptionsTests
{
    // ========================================================================
    // Функциональность
    // ========================================================================

    [Test]
    public async Task CanCreate_WithValidValues()
    {
        var options = new ApiMetadataOptions
        {
            Title = "Test API",
            Version = "1.0.0",
            Description = "Test description",
            Developer = new ContactInfo
            {
                Name = "Test Dev",
                Email = "test@example.com",
                Url = "https://example.com"
            }
        };

        await Assert.That(options.Title).IsEqualTo("Test API");
        await Assert.That(options.Version).IsEqualTo("1.0.0");
        await Assert.That(options.Description).IsEqualTo("Test description");
        await Assert.That(options.Developer.Name).IsEqualTo("Test Dev");
        await Assert.That(options.Developer.Url).IsEqualTo("https://example.com");
    }

    [Test]
    public async Task CanCreate_WithMinimalDeveloper()
    {
        var options = new ApiMetadataOptions
        {
            Title = "Test API",
            Version = "1.0.0",
            Description = "Test description",
            Developer = new ContactInfo
            {
                Name = "Test Dev",
                Url = "https://example.com"
            }
        };

        await Assert.That(options.Developer.Email).IsNull();
    }

    [Test]
    public async Task SectionName_IsCorrect()
    {
        await Assert.That(ApiMetadataOptions.SectionName).IsEqualTo("ApiMetadata");
    }

    // ========================================================================
    // Валидация (рекурсивная)
    // ========================================================================

    private static IReadOnlyList<ValidationResult> Validate(ApiMetadataOptions options)
        => RecursiveValidator.Validate(options);

    private static bool IsValid(ApiMetadataOptions options)
        => Validate(options).Count == 0;

    [Test]
    public async Task Validate_WhenAllRequiredFieldsPresent_ReturnsValid()
    {
        var options = new ApiMetadataOptions
        {
            Title = "Test API",
            Version = "1.0.0",
            Description = "Test description",
            Developer = new ContactInfo { Name = "Test Dev", Url = "https://example.com" }
        };

        await Assert.That(IsValid(options)).IsTrue();
    }

    [Test]
    public async Task Validate_WhenTitleIsNull_ReturnsErrorForTitle()
    {
        var options = new ApiMetadataOptions
        {
            Title = null!,
            Version = "1.0.0",
            Description = "Test description",
            Developer = new ContactInfo { Name = "Test Dev", Url = "https://example.com" }
        };

        var errors = Validate(options);
        await Assert.That(errors.Count).IsGreaterThan(0);
        await Assert.That(errors.Any(e => e.MemberNames.Contains("Title"))).IsTrue();
    }

    [Test]
    public async Task Validate_WhenVersionIsNull_ReturnsErrorForVersion()
    {
        var options = new ApiMetadataOptions
        {
            Title = "Test API",
            Version = null!,
            Description = "Test description",
            Developer = new ContactInfo { Name = "Test Dev", Url = "https://example.com" }
        };

        var errors = Validate(options);
        await Assert.That(errors.Any(e => e.MemberNames.Contains("Version"))).IsTrue();
    }

    [Test]
    public async Task Validate_WhenDescriptionIsNull_ReturnsErrorForDescription()
    {
        var options = new ApiMetadataOptions
        {
            Title = "Test API",
            Version = "1.0.0",
            Description = null!,
            Developer = new ContactInfo { Name = "Test Dev", Url = "https://example.com" }
        };

        var errors = Validate(options);
        await Assert.That(errors.Any(e => e.MemberNames.Contains("Description"))).IsTrue();
    }

    [Test]
    public async Task Validate_WhenDeveloperIsNull_ReturnsErrorForDeveloper()
    {
        var options = new ApiMetadataOptions
        {
            Title = "Test API",
            Version = "1.0.0",
            Description = "Test description",
            Developer = null!
        };

        var errors = Validate(options);
        await Assert.That(errors.Any(e => e.MemberNames.Contains("Developer"))).IsTrue();
    }

    // ========================================================================
    // Nested validation (критично!)
    // ========================================================================

    [Test]
    public async Task Validate_WhenDeveloperNameIsTooShort_ReturnsNestedError()
    {
        var options = new ApiMetadataOptions
        {
            Title = "Test API",
            Version = "1.0.0",
            Description = "Test description",
            Developer = new ContactInfo
            {
                Name = "A",  // меньше MinimumLength = 2
                Url = "https://example.com"
            }
        };

        var errors = Validate(options);
        await Assert.That(errors.Any(e => e.MemberNames.Contains("Name"))).IsTrue();
    }

    [Test]
    public async Task Validate_WhenDeveloperUrlIsInvalid_ReturnsNestedError()
    {
        var options = new ApiMetadataOptions
        {
            Title = "Test API",
            Version = "1.0.0",
            Description = "Test description",
            Developer = new ContactInfo
            {
                Name = "Test Dev",
                Url = "not-url"
            }
        };

        var errors = Validate(options);
        await Assert.That(errors.Any(e => e.MemberNames.Contains("Url"))).IsTrue();
    }
}