using Moriyama.PreviewAuth.Options;

namespace Moriyama.PreviewAuth.Setup;

public static class BuilderExtensions
{
    public static IHostApplicationBuilder AddPreviewAuth(this IHostApplicationBuilder builder)
    {
        builder.Services.AddPreviewAuth();
        return builder;
    }

    public static IWebHostBuilder AddPreviewAuth(this IWebHostBuilder builder)
    {
        builder.ConfigureServices(s => s.AddPreviewAuth());
        return builder;
    }

    public static IServiceCollection AddPreviewAuth(this IServiceCollection services)
    {
        services.AddSingleton<IDatetimeProvider, DatetimeProvider>();
        services.AddOptions<PreviewAuthOptions>()
            .BindConfiguration(PreviewAuthOptions.Key)
            .ValidateDataAnnotations();

        return services;
    }
}
