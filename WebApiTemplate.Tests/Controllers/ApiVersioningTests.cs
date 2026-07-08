using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebApiTemplate.Controllers;

namespace WebApiTemplate.Tests.Controllers;

public class ApiVersioningTests
{
    [Test]
    public async Task LoggingController_HasApiVersionAttribute()
    {
        var attribute = typeof(LoggingController).GetCustomAttributes(typeof(ApiVersionAttribute), false)
            .FirstOrDefault() as ApiVersionAttribute;

        await Assert.That(attribute).IsNotNull();
        await Assert.That(attribute!.Versions.Any(v => v == new ApiVersion(1, 0))).IsTrue();
    }

    [Test]
    public async Task LoggingController_HasCorrectRoute()
    {
        var routeAttribute = typeof(LoggingController).GetCustomAttributes(typeof(RouteAttribute), false)
            .FirstOrDefault() as RouteAttribute;

        await Assert.That(routeAttribute).IsNotNull();
        await Assert.That(routeAttribute!.Template).IsEqualTo("api/v{version:apiVersion}/[controller]");
    }

    [Test]
    public async Task ApiVersioning_DefaultVersion_Is1_0()
    {
        var services = new ServiceCollection();
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiVersioningOptions>>().Value;

        await Assert.That(options.DefaultApiVersion).IsEqualTo(new ApiVersion(1, 0));
    }

    [Test]
    public async Task ApiVersioning_AssumeDefaultVersionWhenUnspecified_IsTrue()
    {
        var services = new ServiceCollection();
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiVersioningOptions>>().Value;

        await Assert.That(options.AssumeDefaultVersionWhenUnspecified).IsTrue();
    }

    [Test]
    public async Task ApiVersioning_ReportApiVersions_IsTrue()
    {
        var services = new ServiceCollection();
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiVersioningOptions>>().Value;

        await Assert.That(options.ReportApiVersions).IsTrue();
    }
}
