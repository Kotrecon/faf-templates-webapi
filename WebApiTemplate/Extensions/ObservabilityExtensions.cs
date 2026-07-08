using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WebApiTemplate.Configuration.Options;
using Serilog;
using Serilog.Events;

namespace WebApiTemplate.Extensions;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddCustomLogging(this WebApplicationBuilder builder)
    {
        var rootLevelSwitch = new Serilog.Core.LoggingLevelSwitch(
            Enum.TryParse<LogEventLevel>(
                builder.Configuration["Serilog:MinimumLevel:Default"],
                true,
                out var lvl) ? lvl : LogEventLevel.Information);

        var overrideSwitches = new Dictionary<string, Serilog.Core.LoggingLevelSwitch>();

        var overrideSection = builder.Configuration.GetSection("Serilog:MinimumLevel:Override");
        foreach (var kvp in overrideSection.GetChildren())
        {
            if (Enum.TryParse<LogEventLevel>(kvp.Value, true, out var olvl))
                overrideSwitches[kvp.Key] = new Serilog.Core.LoggingLevelSwitch(olvl);
        }

        builder.Services.AddSingleton(rootLevelSwitch);
        builder.Services.AddSingleton<IDictionary<string, Serilog.Core.LoggingLevelSwitch>>(overrideSwitches);

        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", context.Configuration["AppSettings:ServiceName"])
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .MinimumLevel.ControlledBy(rootLevelSwitch);

            foreach (var kvp in overrideSwitches)
                loggerConfiguration.MinimumLevel.Override(kvp.Key, kvp.Value);
        });

        return builder;
    }

    public static WebApplicationBuilder AddCustomOpenTelemetry(this WebApplicationBuilder builder)
    {
        var serviceName = builder.Configuration.GetSection(AppSettings.SectionName).Get<AppSettings>()!.ServiceName;
        var otelOptions = builder.Configuration.GetSection(OpenTelemetryOptions.SectionName).Get<OpenTelemetryOptions>()!;

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.ParseStateValues = true;

            logging.SetResourceBuilder(
                ResourceBuilder.CreateDefault().AddService(serviceName));

            logging.AddOtlpExporter(otlp =>
            {
                otlp.Endpoint = new Uri(otelOptions.Endpoint);
                otlp.Protocol = otelOptions.Protocol;
                if (otelOptions.Headers.Count > 0)
                    otlp.Headers = string.Join(",", otelOptions.Headers.Select(x => $"{x.Key}={x.Value}"));
            });

            if (otelOptions.UseConsoleExporter)
                logging.AddConsoleExporter();
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing.AddSource(serviceName)
                       .AddHttpClientInstrumentation()
                       .AddOtlpExporter(otlp =>
                       {
                           otlp.Endpoint = new Uri(otelOptions.Endpoint);
                           otlp.Protocol = otelOptions.Protocol;
                           if (otelOptions.Headers.Count > 0)
                               otlp.Headers = string.Join(",", otelOptions.Headers.Select(x => $"{x.Key}={x.Value}"));
                       });

                if (otelOptions.UseConsoleExporter)
                    tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics.AddRuntimeInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddOtlpExporter(otlp =>
                       {
                           otlp.Endpoint = new Uri(otelOptions.Endpoint);
                           otlp.Protocol = otelOptions.Protocol;
                           if (otelOptions.Headers.Count > 0)
                               otlp.Headers = string.Join(",", otelOptions.Headers.Select(x => $"{x.Key}={x.Value}"));
                       });

                if (otelOptions.UseConsoleExporter)
                    metrics.AddConsoleExporter();
            });

        builder.Services.Configure<LoggerFilterOptions>(filter =>
        {
            foreach (var kvp in otelOptions.LogLevel)
            {
                if (Enum.TryParse<LogLevel>(kvp.Value, true, out var level))
                    filter.AddFilter(kvp.Key, level);
            }
        });

        return builder;
    }
}