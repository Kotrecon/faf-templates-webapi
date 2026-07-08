using WebApiTemplate.Contracts.Dto.Request.Logging;

namespace WebApiTemplate.Tests.Contracts.Dto;

public class SetLogLevelRequestTests
{
    [Test]
    public async Task Validate_WhenRequestIsNull_ReturnsFailure()
    {
        var result = SetLogLevelValidator.Validate(null);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors[0].Message).IsEqualTo("Request is required.");
        await Assert.That(result.Errors[0].Code).IsEqualTo("ValidationFailed");
    }

    [Test]
    public async Task Validate_WhenLevelIsNull_ReturnsFailure()
    {
        var request = new SetLogLevelRequest(Category: "Microsoft", Level: null!);

        var result = SetLogLevelValidator.Validate(request);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors[0].Message).IsEqualTo("Level is required.");
        await Assert.That(result.Errors[0].Code).IsEqualTo("ValidationFailed");
    }

    [Test]
    public async Task Validate_WhenLevelIsEmpty_ReturnsFailure()
    {
        var request = new SetLogLevelRequest(Category: "Microsoft", Level: "");

        var result = SetLogLevelValidator.Validate(request);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors[0].Message).IsEqualTo("Level is required.");
        await Assert.That(result.Errors[0].Code).IsEqualTo("ValidationFailed");
    }

    [Test]
    public async Task Validate_WhenLevelIsInvalid_ReturnsFailure()
    {
        var request = new SetLogLevelRequest(Category: "Microsoft", Level: "Critical");

        var result = SetLogLevelValidator.Validate(request);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors[0].Message).Contains("Invalid level");
        await Assert.That(result.Errors[0].Code).IsEqualTo("ValidationFailed");
    }

    [Test]
    public async Task Validate_WhenLevelIsValid_ReturnsSuccess()
    {
        var request = new SetLogLevelRequest(Category: "Microsoft", Level: "Warning");

        var result = SetLogLevelValidator.Validate(request);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Errors).IsEmpty();
    }

    [Test]
    public async Task Validate_WhenCategoryIsNull_ReturnsSuccess()
    {
        var request = new SetLogLevelRequest(Category: null, Level: "Information");

        var result = SetLogLevelValidator.Validate(request);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Errors).IsEmpty();
    }

    [Test]
    public async Task Validate_WhenCategoryIsEmpty_ReturnsFailure()
    {
        var request = new SetLogLevelRequest(Category: "", Level: "Information");

        var result = SetLogLevelValidator.Validate(request);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors[0].Message).IsEqualTo("Category cannot be empty string.");
        await Assert.That(result.Errors[0].Code).IsEqualTo("ValidationFailed");
    }

    [Test]
    public async Task Validate_WhenCategoryIsValid_ReturnsSuccess()
    {
        var request = new SetLogLevelRequest(Category: "Microsoft.AspNetCore", Level: "Warning");

        var result = SetLogLevelValidator.Validate(request);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Errors).IsEmpty();
    }
}
