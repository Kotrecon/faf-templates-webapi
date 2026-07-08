using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace WebApiTemplate.Security;

// ============================================================================
// АУТЕНТИФИКАЦИЯ (JWT Bearer)
// Проверяет, КТО обращается к API (пользователь/сервис).
// Токен должен быть в заголовке: Authorization: Bearer <token>
// ============================================================================
public static class AuthenticationExtensions
{
    public static IHostApplicationBuilder AddCustomAuthentication(this IHostApplicationBuilder builder)
    {
        var jwtKey = builder.Configuration["Jwt:Key"]!;
        var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "WebApiTemplate";
        var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "WebApiTemplate";

        // --------------------------------------------------------------------
        // TokenValidationParameters — правила проверки JWT.
        // --------------------------------------------------------------------
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,

                    ValidateAudience = true,
                    ValidAudience = jwtAudience,

                    ValidateLifetime = true,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

                    ClockSkew = TimeSpan.FromMinutes(1),

                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.Name
                };
            });

        return builder;
    }
}
