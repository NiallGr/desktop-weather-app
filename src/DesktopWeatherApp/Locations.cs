using DesktopWeatherApp.Core.Domain;

namespace DesktopWeatherApp;

/// <summary>The single hard-coded Supported City for Feature 1. Core has no notion of a default.</summary>
public static class Locations
{
    public static readonly Location London =
        new(new Coordinate(51.5074, -0.1278), "London");
}
