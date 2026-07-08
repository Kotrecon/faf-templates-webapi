using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebApiTemplate.Extensions.Cors;

namespace WebApiTemplate.Tests.Extensions.Cors;

public class CorsExtensionsTests
{
    [Test]
    public async Task AddCustomCors_RegistersCorsServices()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        builder.Services.AddCustomCors();

        var provider = builder.Services.BuildServiceProvider();

        await Assert.That(provider.GetService<ICorsPolicyProvider>()).IsNotNull();
        await Assert.That(provider.GetService<IOptions<CorsOptions>>()).IsNotNull();
    }

    [Test]
    public async Task AddCustomCors_DefaultPolicyResolvesViaProvider()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        builder.Services.AddCustomCors();

        var provider = builder.Services.BuildServiceProvider();
        var corsOptions = provider.GetRequiredService<IOptions<CorsOptions>>();
        var policyProvider = new DefaultCorsPolicyProvider(corsOptions);
        var httpContext = new DefaultHttpContext();

        var policy = await policyProvider.GetPolicyAsync(httpContext, null);

        await Assert.That(policy).IsNotNull();
    }

    [Test]
    public async Task AddCustomCors_DefaultPolicy_AllowsEverything()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        builder.Services.AddCustomCors();

        var provider = builder.Services.BuildServiceProvider();
        var corsOptions = provider.GetRequiredService<IOptions<CorsOptions>>();
        var policyProvider = new DefaultCorsPolicyProvider(corsOptions);
        var httpContext = new DefaultHttpContext();

        var policy = await policyProvider.GetPolicyAsync(httpContext, null);

        await Assert.That(policy).IsNotNull();
        await Assert.That(policy!.Origins).Contains("*");
        await Assert.That(policy.Methods).Contains("*");
        await Assert.That(policy.Headers).Contains("*");
    }
}
