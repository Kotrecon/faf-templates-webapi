using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebApiTemplate.Security;

namespace WebApiTemplate.Tests.Security;

public class AuthorizationExtensionsTests
{
    [Test]
    public async Task AddCustomAuthorization_AdminOnlyPolicyExists()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

        builder.AddCustomAuthorization();

        var options = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        await Assert.That(options.GetPolicy("AdminOnly")).IsNotNull();
    }

    [Test]
    public async Task AddCustomAuthorization_AdminOnlyRequiresAdminRole()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

        builder.AddCustomAuthorization();

        var options = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        var policy = options.GetPolicy("AdminOnly")!;

        var roleRequirement = policy.Requirements.OfType<Microsoft.AspNetCore.Authorization.Infrastructure.RolesAuthorizationRequirement>().FirstOrDefault();
        await Assert.That(roleRequirement).IsNotNull();

        var allowedRoles = roleRequirement!.AllowedRoles.ToList();
        await Assert.That(allowedRoles).Contains("Admin");
        await Assert.That(allowedRoles.Count).IsEqualTo(1);
    }

    [Test]
    public async Task AddCustomAuthorization_OperatorPolicyExists()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

        builder.AddCustomAuthorization();

        var options = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        await Assert.That(options.GetPolicy("Operator")).IsNotNull();
    }

    [Test]
    public async Task AddCustomAuthorization_OperatorRequiresAdminOrOperatorRole()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

        builder.AddCustomAuthorization();

        var options = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        var policy = options.GetPolicy("Operator")!;

        var roleRequirement = policy.Requirements.OfType<Microsoft.AspNetCore.Authorization.Infrastructure.RolesAuthorizationRequirement>().FirstOrDefault();
        await Assert.That(roleRequirement).IsNotNull();

        var allowedRoles = roleRequirement!.AllowedRoles.ToList();
        await Assert.That(allowedRoles).Contains("Admin");
        await Assert.That(allowedRoles).Contains("Operator");
        await Assert.That(allowedRoles.Count).IsEqualTo(2);
    }

    [Test]
    public async Task AddCustomAuthorization_AuditViewerPolicyExists()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

        builder.AddCustomAuthorization();

        var options = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        await Assert.That(options.GetPolicy("AuditViewer")).IsNotNull();
    }

    [Test]
    public async Task AddCustomAuthorization_AuditViewerRequiresAdminOperatorOrAuditorRole()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

        builder.AddCustomAuthorization();

        var options = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        var policy = options.GetPolicy("AuditViewer")!;

        var roleRequirement = policy.Requirements.OfType<Microsoft.AspNetCore.Authorization.Infrastructure.RolesAuthorizationRequirement>().FirstOrDefault();
        await Assert.That(roleRequirement).IsNotNull();

        var allowedRoles = roleRequirement!.AllowedRoles.ToList();
        await Assert.That(allowedRoles).Contains("Admin");
        await Assert.That(allowedRoles).Contains("Operator");
        await Assert.That(allowedRoles).Contains("Auditor");
        await Assert.That(allowedRoles.Count).IsEqualTo(3);
    }
}
