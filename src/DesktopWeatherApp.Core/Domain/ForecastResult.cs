namespace DesktopWeatherApp.Core.Domain;

/// <summary>Success-or-failure result of a Refresh. Never carries both a value and an error.</summary>
public sealed class ForecastResult
{
    private ForecastResult(bool isSuccess, CurrentConditions? conditions, ForecastError? error)
    {
        IsSuccess = isSuccess;
        Conditions = conditions;
        Error = error;
    }

    public bool IsSuccess { get; }
    public CurrentConditions? Conditions { get; }
    public ForecastError? Error { get; }

    public static ForecastResult Success(CurrentConditions conditions) =>
        new(true, conditions, null);

    public static ForecastResult Failure(ForecastError error) =>
        new(false, null, error);
}
