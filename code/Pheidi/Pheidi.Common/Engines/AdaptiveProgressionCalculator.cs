namespace Pheidi.Common.Engines;

public static class AdaptiveProgressionCalculator
{
    /// <summary>
    /// Returns the adaptive increase rate based on current peak weekly mileage.
    /// Up to 15% at low volume (&lt;30mi), down to 7% at high volume (&gt;50mi),
    /// linearly interpolated between.
    /// </summary>
    public static decimal GetIncreaseRate(decimal peakWeeklyMiles) => peakWeeklyMiles switch
    {
        <= 30m => 0.15m,
        >= 50m => 0.07m,
        _ => 0.15m - (peakWeeklyMiles - 30m) / 20m * 0.08m
    };
}
