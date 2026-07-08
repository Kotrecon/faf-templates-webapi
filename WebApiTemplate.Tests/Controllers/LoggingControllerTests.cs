using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebApiTemplate.Controllers;
using WebApiTemplate.Contracts.Dto.Request.Logging;
using Faf.ResultPattern.Common;
using Serilog.Core;
using Serilog.Events;
using System.Security.Claims;

namespace WebApiTemplate.Tests.Controllers;

public class LoggingControllerTests
{
    private readonly Mock<ILogger<LoggingController>> _loggerMock;
    private readonly LoggingLevelSwitch _rootSwitch;
    private readonly Dictionary<string, LoggingLevelSwitch> _overrideSwitches;

    public LoggingControllerTests()
    {
        _loggerMock = new Mock<ILogger<LoggingController>>();
        _rootSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
        _overrideSwitches = new Dictionary<string, LoggingLevelSwitch>
        {
            ["Microsoft"] = new(LogEventLevel.Warning),
            ["System"] = new(LogEventLevel.Error)
        };
    }

    private LoggingController CreateController()
    {
        var controller = new LoggingController(_rootSwitch, _overrideSwitches, _loggerMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, "testuser")
                }, "test"))
            }
        };
        return controller;
    }

    [Test]
    public async Task GetLevel_ReturnsRootAndOverrides()
    {
        var controller = CreateController();

        var result = controller.GetLevel();

        var okResult = result as OkObjectResult;
        await Assert.That(okResult).IsNotNull();

        var value = okResult!.Value!;
        var defaultLevel = value.GetType().GetProperty("Default")!.GetValue(value)!.ToString();
        var overrides = value.GetType().GetProperty("Overrides")!.GetValue(value) as IDictionary<string, string>;

        await Assert.That(defaultLevel).IsEqualTo("Information");
        await Assert.That(overrides).ContainsKey("Microsoft");
        await Assert.That(overrides!["Microsoft"]).IsEqualTo("Warning");
    }

    [Test]
    public async Task SetLevel_WithValidLevel_ReturnsOk()
    {
        var controller = CreateController();
        var request = new SetLogLevelRequest(Category: null, Level: "Warning");

        var result = controller.SetLevel(request);

        var okResult = result as OkResult;
        await Assert.That(okResult).IsNotNull();
        await Assert.That(_rootSwitch.MinimumLevel).IsEqualTo(LogEventLevel.Warning);
    }

    [Test]
    public async Task SetLevel_WithNullCategory_ChangesRoot()
    {
        var controller = CreateController();
        var request = new SetLogLevelRequest(Category: null, Level: "Error");

        controller.SetLevel(request);

        await Assert.That(_rootSwitch.MinimumLevel).IsEqualTo(LogEventLevel.Error);
    }

    [Test]
    public async Task SetLevel_LogsWarning()
    {
        var controller = CreateController();
        var request = new SetLogLevelRequest(Category: null, Level: "Warning");

        controller.SetLevel(request);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SetLevel_WithoutUser_LogsUnknown()
    {
        var controller = new LoggingController(_rootSwitch, _overrideSwitches, _loggerMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
        };

        var request = new SetLogLevelRequest(Category: null, Level: "Warning");

        controller.SetLevel(request);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SetLevel_WithNullIdentityName_LogsUnknown()
    {
        var controller = new LoggingController(_rootSwitch, _overrideSwitches, _loggerMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        var request = new SetLogLevelRequest(Category: null, Level: "Warning");

        controller.SetLevel(request);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SetLevel_WithNullIdentity_LogsUnknown()
    {
        var controller = new LoggingController(_rootSwitch, _overrideSwitches, _loggerMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new ClaimsPrincipal()
            }
        };

        var request = new SetLogLevelRequest(Category: null, Level: "Warning");

        controller.SetLevel(request);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SetLevel_WithCategory_UpdatesOverride()
    {
        var controller = CreateController();
        var request = new SetLogLevelRequest(Category: "Microsoft", Level: "Error");

        var result = controller.SetLevel(request);

        var okResult = result as OkResult;
        await Assert.That(okResult).IsNotNull();
        await Assert.That(_overrideSwitches["Microsoft"].MinimumLevel).IsEqualTo(LogEventLevel.Error);
    }

    [Test]
    public async Task SetLevel_WithCategory_LogsWarning()
    {
        var controller = CreateController();
        var request = new SetLogLevelRequest(Category: "Microsoft", Level: "Error");

        controller.SetLevel(request);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SetLevel_WithCategoryAndNullUser_LogsUnknown()
    {
        var controller = new LoggingController(_rootSwitch, _overrideSwitches, _loggerMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
        };

        var request = new SetLogLevelRequest(Category: "Microsoft", Level: "Error");

        controller.SetLevel(request);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SetLevel_WithUnknownCategory_ReturnsNotFound()
    {
        var controller = CreateController();
        var request = new SetLogLevelRequest(Category: "Unknown", Level: "Warning");

        var result = controller.SetLevel(request);

        var objectResult = result as ObjectResult;
        await Assert.That(objectResult).IsNotNull();
        await Assert.That(objectResult!.StatusCode).IsEqualTo(404);

        var problem = objectResult.Value as ProblemDetails;
        await Assert.That(problem).IsNotNull();
        await Assert.That(problem!.Title).IsEqualTo("NotFound");
    }

    [Test]
    public async Task SetLevel_WithInvalidLevel_ReturnsValidationFailed()
    {
        var controller = CreateController();
        var request = new SetLogLevelRequest(Category: null, Level: "Critical");

        var result = controller.SetLevel(request);

        var objectResult = result as ObjectResult;
        await Assert.That(objectResult).IsNotNull();
        await Assert.That(objectResult!.StatusCode).IsEqualTo(422);

        var problem = objectResult.Value as ProblemDetails;
        await Assert.That(problem).IsNotNull();
        await Assert.That(problem!.Title).IsEqualTo("ValidationFailed");
    }

    [Test]
    public async Task SetLevel_WithNullLevel_ReturnsValidationFailed()
    {
        var controller = CreateController();
        var request = new SetLogLevelRequest(Category: null, Level: null!);

        var result = controller.SetLevel(request);

        var objectResult = result as ObjectResult;
        await Assert.That(objectResult).IsNotNull();
        await Assert.That(objectResult!.StatusCode).IsEqualTo(422);

        var problem = objectResult.Value as ProblemDetails;
        await Assert.That(problem).IsNotNull();
        await Assert.That(problem!.Title).IsEqualTo("ValidationFailed");
    }

    [Test]
    public async Task SetLevel_WithEmptyCategory_ReturnsValidationFailed()
    {
        var controller = CreateController();
        var request = new SetLogLevelRequest(Category: "", Level: "Debug");

        var result = controller.SetLevel(request);

        var objectResult = result as ObjectResult;
        await Assert.That(objectResult).IsNotNull();
        await Assert.That(objectResult!.StatusCode).IsEqualTo(422);

        var problem = objectResult.Value as ProblemDetails;
        await Assert.That(problem).IsNotNull();
        await Assert.That(problem!.Title).IsEqualTo("ValidationFailed");
    }

    [Test]
    public async Task SetLevel_ForbiddenLevel_ReturnsBusinessRuleViolation()
    {
        var controller = CreateController();
        var request = new SetLogLevelRequest(Category: null, Level: "Fatal");

        var result = controller.SetLevel(request);

        var objectResult = result as ObjectResult;
        await Assert.That(objectResult).IsNotNull();
        await Assert.That(objectResult!.StatusCode).IsEqualTo(400);

        var problem = objectResult.Value as ProblemDetails;
        await Assert.That(problem).IsNotNull();
        await Assert.That(problem!.Title).IsEqualTo("BusinessRuleViolation");
    }

    [Test]
    public async Task GetCategories_ReturnsConfiguredCategories()
    {
        var controller = CreateController();

        var result = controller.GetCategories();

        var okResult = result as OkObjectResult;
        var categories = okResult!.Value as IEnumerable<string>;

        await Assert.That(categories).Contains("Microsoft");
        await Assert.That(categories).Contains("System");
        await Assert.That(categories!.Count()).IsEqualTo(2);
    }
}
