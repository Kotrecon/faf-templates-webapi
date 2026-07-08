using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace WebApiTemplate.Tests.Integration.Infrastructure;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestJwtKey = "DevelopmentKeyAtLeast32CharactersLong!!";
    public const string TestJwtIssuer = "WebApiTemplate";
    public const string TestJwtAudience = "WebApiTemplate";

    private readonly Dictionary<string, string?>? _additionalConfig;

    public TestWebApplicationFactory(Dictionary<string, string?>? additionalConfig = null)
    {
        _additionalConfig = additionalConfig;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (_additionalConfig != null && _additionalConfig.TryGetValue("ASPNETCORE_ENVIRONMENT", out var env))
        {
            builder.UseEnvironment(env!);
        }

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:ServiceName"] = "TestService",
                ["AppSettings:Port"] = "8080",
                ["Jwt:Key"] = TestJwtKey,
                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["ApiMetadata:Title"] = "Test API",
                ["ApiMetadata:Version"] = "1.0.0",
                ["ApiMetadata:Description"] = "Test description",
                ["ApiMetadata:Developer:Name"] = "Test Dev",
                ["ApiMetadata:Developer:Url"] = "https://example.com",
                ["OpenTelemetry:Endpoint"] = "http://localhost:4317",
                ["ASPNETCORE_ENVIRONMENT"] = "Development"
            });

            if (_additionalConfig != null)
            {
                config.AddInMemoryCollection(_additionalConfig);
            }
        });

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
                options.TokenValidationParameters.NameClaimType = ClaimTypes.Name;
            });
        });
    }

    public string GenerateJwtToken(string username, string[] roles, TimeSpan? expires = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.NameIdentifier, username)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = TestJwtIssuer,
            Audience = TestJwtAudience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.Add(expires ?? TimeSpan.FromHours(1)),
            SigningCredentials = creds
        };

        var handler = new JwtSecurityTokenHandler();
        handler.OutboundClaimTypeMap.Clear();
        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }

    public string GenerateExpiredJwtToken(string username, string[] roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.NameIdentifier, username)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = TestJwtIssuer,
            Audience = TestJwtAudience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = DateTime.UtcNow.AddHours(-2),
            Expires = DateTime.UtcNow.AddHours(-1),
            SigningCredentials = creds
        };

        var handler = new JwtSecurityTokenHandler();
        handler.OutboundClaimTypeMap.Clear();
        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }

    public HttpClient CreateAuthenticatedClient(string username = "testuser", params string[] roles)
    {
        var client = CreateClient();
        var token = GenerateJwtToken(username, roles);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
