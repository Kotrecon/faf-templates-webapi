using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;

namespace WebApiTemplate.Security;

// ============================================================================
// АВТОРИЗАЦИЯ (Policy-based)
// Определяет, ЧТО может делать пользователь (какие операции).
// Policies инкапсулируют бизнес-правила доступа.
// ============================================================================
public static class AuthorizationExtensions
{
    public static IHostApplicationBuilder AddCustomAuthorization(this IHostApplicationBuilder builder)
    {
        builder.Services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy("Operator", policy =>
                policy.RequireRole("Admin", "Operator"));

            options.AddPolicy("AuditViewer", policy =>
                policy.RequireRole("Admin", "Operator", "Auditor"));
        });

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        return builder;
    }
}
