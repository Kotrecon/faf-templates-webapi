using WebApiTemplate.Configuration.Options;

namespace WebApiTemplate.Extensions;

public static class ConfigurationExtensions
{
    public static bool AddAppSettings(this IHostApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(AppSettings.SectionName);
        if (!section.Exists())
            return false;

        builder.Services
            .AddOptions<AppSettings>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return true;
    }

    public static bool AddApiMetadata(this IHostApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(ApiMetadataOptions.SectionName);
        if (!section.Exists())
            return false;

        builder.Services
            .AddOptions<ApiMetadataOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return true;
    }

    public static bool AddJwt(this IHostApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(JwtOptions.SectionName);
        if (!section.Exists())
            return false;

        builder.Services
            .AddOptions<JwtOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return true;
    }

    public static bool AddOpenTelemetryOptions(this IHostApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(OpenTelemetryOptions.SectionName);
        if (!section.Exists())
            return false;

        builder.Services
            .AddOptions<OpenTelemetryOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return true;
    }
}