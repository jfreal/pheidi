using Pheidi.Common.Models;

namespace Pheidi.Common.Services;

public class RacePredictionService
{
    private const int MinWeeksForPrediction = 4;

    /// <summary>
    /// Estimate race finish time based on logged workout data.
    /// Returns null if insufficient data.
    /// </summary>
    public (TimeSpan Optimistic, TimeSpan Conservative)? PredictFinishTime(NewTrainingPlan plan)
    {
        var loggedWorkouts = plan.Weeks
            .SelectMany(w => w.Workouts)
            .Where(w => w.Status == WorkoutStatus.Completed &&
                        w.ActualDistanceMiles.HasValue &&
                        w.ActualDuration.HasValue &&
                        w.ActualDistanceMiles > 0)
            .ToList();

        // Need at least 4 weeks of data
        var weeksWithData = plan.Weeks.Count(w =>
            w.Workouts.Any(wo => wo.Status == WorkoutStatus.Completed));
        if (weeksWithData < MinWeeksForPrediction)
            return null;

        // Calculate average pace from recent workouts (last 3 weeks)
        var recentWorkouts = loggedWorkouts
            .OrderByDescending(w => w.Date)
            .Take(15)
            .ToList();

        if (recentWorkouts.Count == 0) return null;

        var totalMiles = recentWorkouts.Sum(w => w.ActualDistanceMiles!.Value);
        var totalMinutes = recentWorkouts.Sum(w => w.ActualDuration!.Value.TotalMinutes);

        if (totalMiles == 0) return null;

        var avgPaceMinPerMile = (decimal)(totalMinutes / (double)totalMiles);
        var raceDistanceMiles = plan.RaceGoal.Distance.ToMiles();

        // Adjust for race day (slower due to distance fatigue)
        var racePaceFactor = plan.RaceGoal.Distance switch
        {
            RaceDistance.FiveK => 0.95m,    // Race pace is faster than training
            RaceDistance.TenK => 0.97m,
            RaceDistance.HalfMarathon => 1.02m,
            RaceDistance.FullMarathon => 1.08m, // Marathon is slower due to fatigue
            _ => 1.0m
        };

        var estimatedPace = avgPaceMinPerMile * racePaceFactor;
        var estimatedMinutes = estimatedPace * raceDistanceMiles;

        var optimistic = TimeSpan.FromMinutes((double)(estimatedMinutes * 0.95m));
        var conservative = TimeSpan.FromMinutes((double)(estimatedMinutes * 1.05m));

        return (optimistic, conservative);
    }

    /// <summary>
    /// Get prediction status message.
    /// </summary>
    public string GetPredictionMessage(NewTrainingPlan plan)
    {
        var prediction = PredictFinishTime(plan);
        if (prediction == null)
        {
            var weeksWithData = plan.Weeks.Count(w =>
                w.Workouts.Any(wo => wo.Status == WorkoutStatus.Completed));
            var remaining = MinWeeksForPrediction - weeksWithData;
            return remaining > 0
                ? $"Keep logging workouts — race prediction will appear after {remaining} more week(s) of data."
                : "Log workouts with time data to enable race predictions.";
        }

        var (opt, con) = prediction.Value;
        return $"Estimated finish: {FormatTime(opt)} - {FormatTime(con)}";
    }

    private static string FormatTime(TimeSpan time) =>
        time.Hours > 0
            ? $"{time.Hours}:{time.Minutes:D2}:{time.Seconds:D2}"
            : $"{time.Minutes}:{time.Seconds:D2}";
}
