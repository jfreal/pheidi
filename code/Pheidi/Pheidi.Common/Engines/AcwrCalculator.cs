using Pheidi.Common.Models;

namespace Pheidi.Common.Engines;

public enum AcwrRiskZone
{
    UnderTraining,
    Green,
    Yellow,
    Red
}

public static class AcwrCalculator
{
    /// <summary>
    /// Calculate ACWR from completed workouts.
    /// Acute = total distance in last 7 days.
    /// Chronic = average weekly distance over last 28 days.
    /// Returns null if insufficient data (&lt;28 days).
    /// </summary>
    public static decimal? Calculate(List<ScheduledWorkout> completedWorkouts)
    {
        var today = DateTime.Today;
        var last28Days = completedWorkouts
            .Where(w => w.Status == WorkoutStatus.Completed
                && w.Date.Date >= today.AddDays(-28)
                && w.Date.Date <= today
                && w.ActualDistanceMiles.HasValue)
            .ToList();

        if (last28Days.Count == 0) return null;

        // Check if we have at least 28 days of span
        var earliest = last28Days.Min(w => w.Date.Date);
        if ((today - earliest).TotalDays < 21) return null; // Need ~3 weeks minimum

        var acuteLoad = last28Days
            .Where(w => w.Date.Date >= today.AddDays(-7))
            .Sum(w => w.ActualDistanceMiles ?? 0);

        var chronicLoad = last28Days.Sum(w => w.ActualDistanceMiles ?? 0) / 4m;

        if (chronicLoad == 0) return null;

        return Math.Round(acuteLoad / chronicLoad, 2);
    }

    public static AcwrRiskZone ClassifyRisk(decimal acwr) => acwr switch
    {
        < 0.8m => AcwrRiskZone.UnderTraining,
        <= 1.3m => AcwrRiskZone.Green,
        <= 1.5m => AcwrRiskZone.Yellow,
        _ => AcwrRiskZone.Red
    };

    public static string GetRiskMessage(AcwrRiskZone zone) => zone switch
    {
        AcwrRiskZone.Green => "Sweet spot — building fitness safely",
        AcwrRiskZone.Yellow => "Caution — training load is ramping quickly. Consider an easier week.",
        AcwrRiskZone.Red => "High injury risk — your recent load is much higher than your baseline. Dial it back.",
        AcwrRiskZone.UnderTraining => "Under-training zone — you can safely increase load",
        _ => ""
    };

    public static string GetRiskColor(AcwrRiskZone zone) => zone switch
    {
        AcwrRiskZone.Green => "#4caf50",
        AcwrRiskZone.Yellow => "#ff9800",
        AcwrRiskZone.Red => "#f44336",
        AcwrRiskZone.UnderTraining => "#9e9e9e",
        _ => "#9e9e9e"
    };
}
