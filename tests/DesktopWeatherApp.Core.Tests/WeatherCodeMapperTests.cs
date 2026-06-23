using DesktopWeatherApp.Core.Domain;
using DesktopWeatherApp.Core.Weather;
using FluentAssertions;
using Xunit;

namespace DesktopWeatherApp.Core.Tests;

public class WeatherCodeMapperTests
{
    [Theory]
    [InlineData(0, WeatherCondition.Clear)]
    [InlineData(1, WeatherCondition.MainlyClear)]
    [InlineData(3, WeatherCondition.Cloudy)]
    [InlineData(45, WeatherCondition.Fog)]
    [InlineData(61, WeatherCondition.Rain)]
    [InlineData(71, WeatherCondition.Snow)]
    [InlineData(95, WeatherCondition.Thunderstorm)]
    public void Maps_known_wmo_codes_to_conditions(int code, WeatherCondition expected)
    {
        WeatherCodeMapper.Map(code).Should().Be(expected);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(1000)]
    [InlineData(-1)]
    public void Maps_unknown_codes_to_Unknown(int code)
    {
        WeatherCodeMapper.Map(code).Should().Be(WeatherCondition.Unknown);
    }
}
