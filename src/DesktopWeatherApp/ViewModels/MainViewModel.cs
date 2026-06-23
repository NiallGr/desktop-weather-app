using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopWeatherApp.Core.Domain;
using DesktopWeatherApp.Core.Forecast;

namespace DesktopWeatherApp.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IForecastService _forecastService;
    private readonly Location _location;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private string? _summary;

    public MainViewModel(IForecastService forecastService, Location location)
    {
        _forecastService = forecastService;
        _location = location;
    }

    /// <summary>Fired once when the window opens. No user-triggered re-entry in Feature 1.</summary>
    public async Task InitializeAsync()
    {
        IsLoading = true;
        Error = null;
        try
        {
            var result = await _forecastService.GetCurrentAsync(_location, CancellationToken.None);
            if (result.IsSuccess)
            {
                var c = result.Conditions!;
                Summary = $"{_location.DisplayName} · {c.TemperatureCelsius:0.#}°C · " +
                          $"{c.Condition} · as of {c.ObservedAt:HH:mm}";
                HasData = true;
            }
            else
            {
                Error = "couldn't reach open-meteo.";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
