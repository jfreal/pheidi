using Pheidi.Common.Models;

namespace Pheidi.Common.Services;

public static class IntensityTrackingService
{
    /// <summary>
    /// Get intensity distribution over last 4 weeks of completed workouts.
    /// Returns percentages for Easy, Threshold, Hard zones.
    /// </summary>
    public static (decimal Easy, decimal Threshold, decimal Hard) GetDistribution(List<ScheduledWorkout> completedWorkouts)
    {
        var today = DateTime.Today;
        var recent = completedWorkouts
            .Where(w => w.Status == WorkoutStatus.Completed
                && w.IsRunWorkout
                && w.Date.Date >= today.AddDays(-28)
                && w.Date.Date <= today)
            .ToList();

        if (recent.Count == 0) return (0, 0, 0);

        var total = (decimal)recent.Count;
        var easy = recent.Count(w => GetEffectiveZone(w) == IntensityZone.Easy);
        var threshold = recent.Count(w => GetEffectiveZone(w) == IntensityZone.Threshold);
        var hard = recent.Count(w => GetEffectiveZone(w) == IntensityZone.Hard);

        return (
            Math.Round(easy / total * 100, 1),
            Math.Round(threshold / total * 100, 1),
            Math.Round(hard / total * 100, 1)
        );
    }

    /// <summary>
    /// Returns true if gray zone (Threshold) exceeds 20% of training volume.
    /// </summary>
    public static bool IsInGrayZone(List<ScheduledWorkout> completedWorkouts)
    {
        var (_, threshold, _) = GetDistribution(completedWorkouts);
        return threshold > 20;
    }

    public static string GetGrayZoneNudge() =>
        "You're spending too much time in the gray zone. Try slowing down on easy days — it builds more fitness than you think.";

    /// <summary>
    /// Maps effort to IntensityZone for auto-assignment during logging.
    /// </summary>
    public static IntensityZone MapEffortToZone(int? effort, WorkoutType type)
    {
        if (effort.HasValue) return IntensityZoneExtensions.FromEffort(effort);
        return IntensityZoneExtensions.FromWorkoutType(type);
    }

    private static IntensityZone GetEffectiveZone(ScheduledWorkout workout)
    {
        return workout.ActualIntensityZone ?? IntensityZoneExtensions.FromWorkoutType(workout.Type);
    }
}
