using Pheidi.Common.Models;

namespace Pheidi.Common.Engines;

public class BaseBuildingEngine
{
    /// <summary>
    /// Generate a base building phase (4-6 weeks) for beginners not currently running.
    /// </summary>
    public List<TrainingWeek> GenerateBaseBuildingPhase(
        UserProfile profile,
        DateTime planStartDate,
        int weeks = 4)
    {
        weeks = Math.Clamp(weeks, 4, 6);
        var basePlan = new List<TrainingWeek>();
        var availableDays = profile.AvailableDays.Length >= 3
            ? profile.AvailableDays
            : new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday };

        var runsPerWeek = Math.Min(availableDays.Length, 3);

        for (int weekNum = 1; weekNum <= weeks; weekNum++)
        {
            var mondayOfWeek = planStartDate.AddDays(-7 * (weeks - weekNum));
            var week = new TrainingWeek
            {
                WeekNumber = -weekNum, // Negative numbers for pre-plan weeks
                Phase = TrainingPhase.Base
            };

            // Start at 1 mile, increase by 0.5 each week
            var easyDistance = Math.Round(1.0m + (weekNum - 1) * 0.5m, 1);

            for (int d = 0; d < 7; d++)
            {
                var dayOfWeek = (DayOfWeek)d;
                var date = mondayOfWeek.AddDays(((int)dayOfWeek - (int)DayOfWeek.Monday + 7) % 7);

                var isRunDay = availableDays.Contains(dayOfWeek) && runsPerWeek > 0;
                var assignedRuns = week.Workouts.Count(w => w.IsRunWorkout);

                week.Workouts.Add(new ScheduledWorkout
                {
                    Date = date,
                    Type = isRunDay && assignedRuns < runsPerWeek ? WorkoutType.Easy : WorkoutType.Rest,
                    TargetDistanceMiles = isRunDay && assignedRuns < runsPerWeek ? easyDistance : 0,
                    PaceZone = isRunDay && assignedRuns < runsPerWeek ? PaceZone.ForWorkoutType(WorkoutType.Easy) : null
                });
            }

            basePlan.Add(week);
        }

        return basePlan;
    }

    /// <summary>
    /// Determine if a user should be offered base building.
    /// </summary>
    public static bool ShouldOfferBaseBuilding(ExperienceLevel level) =>
        level == ExperienceLevel.Beginner;
}
