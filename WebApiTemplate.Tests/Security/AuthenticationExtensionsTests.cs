using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WebApiTemplate.Security;

namespace WebApiTemplate.Tests.Security;

public class AuthenticationExtensionsTests
{
    private static (IHost host, JwtBearerOptions jwtOptions) BuildHostWithJwt(Action<Dictionary<string, string?>>? configure = null)
    {
        var config = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "ThisIsAVeryLongKeyForTesting123456"
        };
        configure?.Invoke(config);

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(config);

        builder.AddCustomAuthentication();

        var host = builder.Build();
        var jwtOptions = host.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);

        return (host, jwtOptions);
    }

    [Test]
    public async Task AddCustomAuthentication_ValidateIssuer_IsTrue()
    {
        var (_, jwtOptions) = BuildHostWithJwt();

        await Assert.That(jwtOptions.TokenValidationParameters.ValidateIssuer).IsTrue();
    }

    [Test]
    public async Task AddCustomAuthentication_ValidIssuer_FromConfig()
    {
        var (_, jwtOptions) = BuildHostWithJwt(cfg => cfg["Jwt:Issuer"] = "TestIssuer");

        await Assert.That(jwtOptions.TokenValidationParameters.ValidIssuer).IsEqualTo("TestIssuer");
    }

    [Test]
    public async Task AddCustomAuthentication_ValidIssuer_DefaultWebApiTemplate()
    {
        var (_, jwtOptions) = BuildHostWithJwt();

        await Assert.That(jwtOptions.TokenValidationParameters.ValidIssuer).IsEqualTo("WebApiTemplate");
    }

    [Test]
    public async Task AddCustomAuthentication_ValidIssuer_DefaultIndependentFromConfig()
    {
        var (_, jwtOptions) = BuildHostWithJwt(cfg =>
        {
            cfg["Jwt:Issuer"] = "CustomIssuer";
        });

        await Assert.That(jwtOptions.TokenValidationParameters.ValidIssuer).IsEqualTo("CustomIssuer");
    }

    [Test]
    public async Task AddCustomAuthentication_ValidAudience_DefaultIndependentFromConfig()
    {
        var (_, jwtOptions) = BuildHostWithJwt(cfg =>
        {
            cfg["Jwt:Audience"] = "CustomAudience";
        });

        await Assert.That(jwtOptions.TokenValidationParameters.ValidAudience).IsEqualTo("CustomAudience");
    }

    [Test]
    public async Task AddCustomAuthentication_ValidateAudience_IsTrue()
    {
        var (_, jwtOptions) = BuildHostWithJwt();

        await Assert.That(jwtOptions.TokenValidationParameters.ValidateAudience).IsTrue();
    }

    [Test]
    public async Task AddCustomAuthentication_ValidAudience_FromConfig()
    {
        var (_, jwtOptions) = BuildHostWithJwt(cfg => cfg["Jwt:Audience"] = "TestAudience");

        await Assert.That(jwtOptions.TokenValidationParameters.ValidAudience).IsEqualTo("TestAudience");
    }

    [Test]
    public async Task AddCustomAuthentication_ValidAudience_DefaultWebApiTemplate()
    {
        var (_, jwtOptions) = BuildHostWithJwt();

        await Assert.That(jwtOptions.TokenValidationParameters.ValidAudience).IsEqualTo("WebApiTemplate");
    }

    [Test]
    public async Task AddCustomAuthentication_ValidateLifetime_IsTrue()
    {
        var (_, jwtOptions) = BuildHostWithJwt();

        await Assert.That(jwtOptions.TokenValidationParameters.ValidateLifetime).IsTrue();
    }

    [Test]
    public async Task AddCustomAuthentication_ValidateIssuerSigningKey_IsTrue()
    {
        var (_, jwtOptions) = BuildHostWithJwt();

        await Assert.That(jwtOptions.TokenValidationParameters.ValidateIssuerSigningKey).IsTrue();
    }

    [Test]
    public async Task AddCustomAuthentication_IssuerSigningKey_IsNotNull()
    {
        var (_, jwtOptions) = BuildHostWithJwt();

        await Assert.That(jwtOptions.TokenValidationParameters.IssuerSigningKey).IsNotNull();
    }

    [Test]
    public async Task AddCustomAuthentication_ClockSkew_IsOneMinute()
    {
        var (_, jwtOptions) = BuildHostWithJwt();

        await Assert.That(jwtOptions.TokenValidationParameters.ClockSkew).IsEqualTo(TimeSpan.FromMinutes(1));
    }

    [Test]
    public async Task AddCustomAuthentication_SigningKey_IsSymmetricAndSufficientLength()
    {
        var (_, jwtOptions) = BuildHostWithJwt();

        var key = jwtOptions.TokenValidationParameters.IssuerSigningKey;
        await Assert.That(key is SymmetricSecurityKey).IsTrue();

        var symmetricKey = (SymmetricSecurityKey)key;
        await Assert.That(symmetricKey.Key.Length).IsGreaterThanOrEqualTo(32);
    }
}
