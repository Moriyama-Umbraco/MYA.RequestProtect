using MYA.RequestProtect.Options;

namespace MYA.RequestProtect.Setup;

public static class BuilderExtensions
{
    public static IHostApplicationBuilder AddRequestProtect(this IHostApplicationBuilder builder)
    {
        builder.Services.AddRequestProtect();
        return builder;
    }

    public static IWebHostBuilder AddRequestProtect(this IWebHostBuilder builder)
    {
        builder.ConfigureServices(s => s.AddRequestProtect());
        return builder;
    }

    public static IServiceCollection AddRequestProtect(this IServiceCollection services)
    {
        services.AddSingleton<IDatetimeProvider, DatetimeProvider>();
        services.AddOptions<RequestProtectOptions>()
            .BindConfiguration(RequestProtectOptions.Key)
            .ValidateDataAnnotations();

        return services;
    }
}
