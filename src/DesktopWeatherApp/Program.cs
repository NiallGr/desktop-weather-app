using System;
using Avalonia;
using DesktopWeatherApp.Core.Forecast;
using DesktopWeatherApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;

namespace DesktopWeatherApp;

internal static class Program
{
    public static IServiceProvider Services { get; private set; } = default!;

    [STAThread]
    public static void Main(string[] args)
    {
        var logDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DesktopWeatherApp", "logs");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
            .Enrich.WithProperty("Application", "DesktopWeatherApp")
            .WriteTo.File(new CompactJsonFormatter(),
                System.IO.Path.Combine(logDir, "app-.log"),
                rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .WriteTo.Debug()
            .CreateLogger();

        try
        {
            Services = BuildServices();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddSerilog(dispose: false));
        services.AddHttpClient(ForecastService.HttpClientName,
            c => c.BaseAddress = new Uri("https://api.open-meteo.com"));
        services.AddSingleton<IForecastService, ForecastService>();
        services.AddSingleton(Locations.London);
        services.AddTransient<MainViewModel>();
        return services.BuildServiceProvider();
    }

    // Avalonia configuration, don't remove; also used by the visual designer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
}
