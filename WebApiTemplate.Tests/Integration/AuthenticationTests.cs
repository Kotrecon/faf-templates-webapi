using System.Net;
using System.Net.Http.Headers;
using WebApiTemplate.Tests.Integration.Infrastructure;

namespace WebApiTemplate.Tests.Integration;

public class AuthenticationTests
{
    [Test]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/logging/categories");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ProtectedEndpoint_WithInvalidToken_Returns401()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid.token.here");

        var response = await client.GetAsync("/api/v1/logging/categories");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ProtectedEndpoint_WithExpiredToken_Returns401()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();
        var expiredToken = factory.GenerateExpiredJwtToken("user", ["Admin"]);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await client.GetAsync("/api/v1/logging/categories");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ProtectedEndpoint_WithWrongIssuer_Returns401()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        // Генерируем токен с неправильным issuer
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(TestWebApplicationFactory.TestJwtKey));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var descriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Issuer = "WrongIssuer",
            Audience = TestWebApplicationFactory.TestJwtAudience,
            Claims = new Dictionary<string, object>
            {
                [System.Security.Claims.ClaimTypes.Name] = "user",
                [System.Security.Claims.ClaimTypes.Role] = "Admin"
            },
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = creds
        };

        var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
        var wrongToken = handler.CreateToken(descriptor);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", wrongToken);

        var response = await client.GetAsync("/api/v1/logging/categories");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ProtectedEndpoint_WithWrongAudience_Returns401()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(TestWebApplicationFactory.TestJwtKey));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var descriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Issuer = TestWebApplicationFactory.TestJwtIssuer,
            Audience = "WrongAudience",
            Claims = new Dictionary<string, object>
            {
                [System.Security.Claims.ClaimTypes.Name] = "user",
                [System.Security.Claims.ClaimTypes.Role] = "Admin"
            },
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = creds
        };

        var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
        var wrongToken = handler.CreateToken(descriptor);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", wrongToken);

        var response = await client.GetAsync("/api/v1/logging/categories");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
