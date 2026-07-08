using System.ComponentModel.DataAnnotations;
using WebApiTemplate.Configuration.Options;
using WebApiTemplate.Tests.Helpers;

namespace WebApiTemplate.Tests.Configuration.Options;

public class ContactInfoTests
{
    // ========================================================================
    // Функциональность
    // ========================================================================

    [Test]
    public async Task CanCreate_WithValidValues()
    {
        var contact = new ContactInfo
        {
            Name = "Test Dev",
            Email = "test@example.com",
            Url = "https://example.com"
        };

        await Assert.That(contact.Name).IsEqualTo("Test Dev");
        await Assert.That(contact.Email).IsEqualTo("test@example.com");
        await Assert.That(contact.Url).IsEqualTo("https://example.com");
    }

    [Test]
    public async Task CanCreate_WithMinimalValues()
    {
        var contact = new ContactInfo
        {
            Name = "Test Dev",
            Url = "https://example.com"
        };

        await Assert.That(contact.Name).IsEqualTo("Test Dev");
        await Assert.That(contact.Email).IsNull();
        await Assert.That(contact.Url).IsEqualTo("https://example.com");
    }

    [Test]
    public async Task CanCreate_WithLongValues()
    {
        var name = new string('A', 100);
        var url = "https://" + new string('a', 480) + ".com";

        var contact = new ContactInfo
        {
            Name = name,
            Email = "test@example.com",
            Url = url
        };

        await Assert.That(contact.Name).IsEqualTo(name);
        await Assert.That(contact.Url).IsEqualTo(url);
    }

    // ========================================================================
    // Валидация
    // ========================================================================

    private static IReadOnlyList<ValidationResult> Validate(ContactInfo contact)
        => RecursiveValidator.Validate(contact);

    private static bool IsValid(ContactInfo contact)
        => Validate(contact).Count == 0;

    [Test]
    public async Task Validate_WhenAllRequiredFieldsPresent_ReturnsValid()
    {
        var contact = new ContactInfo
        {
            Name = "Test Dev",
            Url = "https://example.com"
        };

        await Assert.That(IsValid(contact)).IsTrue();
    }

    [Test]
    public async Task Validate_WhenNameIsNull_ReturnsErrorForName()
    {
        var contact = new ContactInfo { Name = null!, Url = "https://example.com" };

        var errors = Validate(contact);
        await Assert.That(errors.Any(e => e.MemberNames.Contains("Name"))).IsTrue();
    }

    [Test]
    public async Task Validate_WhenNameIsTooShort_ReturnsErrorForName()
    {
        var contact = new ContactInfo { Name = "A", Url = "https://example.com" };

        var errors = Validate(contact);
        await Assert.That(errors.Any(e => e.MemberNames.Contains("Name"))).IsTrue();
    }

    [Test]
    public async Task Validate_WhenNameIsTooLong_ReturnsErrorForName()
    {
        var contact = new ContactInfo { Name = new string('A', 101), Url = "https://example.com" };

        var errors = Validate(contact);
        await Assert.That(errors.Any(e => e.MemberNames.Contains("Name"))).IsTrue();
    }

    [Test]
    public async Task Validate_WhenEmailIsInvalid_ReturnsErrorForEmail()
    {
        var contact = new ContactInfo
        {
            Name = "Test Dev",
            Email = "not-email",
            Url = "https://example.com"
        };

        var errors = Validate(contact);
        await Assert.That(errors.Any(e => e.MemberNames.Contains("Email"))).IsTrue();
    }

    [Test]
    public async Task Validate_WhenEmailIsNull_ReturnsValid()
    {
        var contact = new ContactInfo
        {
            Name = "Test Dev",
            Email = null,
            Url = "https://example.com"
        };

        await Assert.That(IsValid(contact)).IsTrue();
    }

    [Test]
    public async Task Validate_WhenUrlIsNull_ReturnsErrorForUrl()
    {
        var contact = new ContactInfo { Name = "Test Dev", Url = null! };

        var errors = Validate(contact);
        await Assert.That(errors.Any(e => e.MemberNames.Contains("Url"))).IsTrue();
    }

    [Test]
    public async Task Validate_WhenUrlIsInvalid_ReturnsErrorForUrl()
    {
        var contact = new ContactInfo { Name = "Test Dev", Url = "not-url" };

        var errors = Validate(contact);
        await Assert.That(errors.Any(e => e.MemberNames.Contains("Url"))).IsTrue();
    }

    [Test]
    public async Task Validate_WhenUrlIsTooLong_ReturnsErrorForUrl()
    {
        var contact = new ContactInfo
        {
            Name = "Test Dev",
            Url = "https://" + new string('a', 490) + ".com"
        };

        var errors = Validate(contact);
        await Assert.That(errors.Any(e => e.MemberNames.Contains("Url"))).IsTrue();
    }
}