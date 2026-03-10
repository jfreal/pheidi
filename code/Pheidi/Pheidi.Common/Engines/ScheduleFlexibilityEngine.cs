using Pheidi.Common.Models;

namespace Pheidi.Common.Engines;

public class ScheduleFlexibilityEngine
{
    /// <summary>
    /// Blocks a specific date and redistributes the workout to the nearest available day in the same week.
    /// </summary>
    public bool BlockDay(NewTrainingPlan plan, DateTime blockedDate)
    {
        var week = plan.Weeks.FirstOrDefault(w => w.Workouts.Any(wo => wo.Date.Date == blockedDate.Date));
        if (week == null) return false;

        var workout = week.Workouts.First(w => w.Date.Date == blockedDate.Date);
        if (workout.Type == WorkoutType.Rest) return true; // Already rest, nothing to do

        // Find nearest available rest day in the same week
        var restDays = week.Workouts
            .Where(w => w.Type == WorkoutType.Rest && w.Date.Date != blockedDate.Date)
            .OrderBy(w => Math.Abs((w.Date - blockedDate).TotalDays))
            .ToList();

        if (restDays.Count > 0)
        {
            var target = restDays.First();
            SwapWorkouts(workout, target);
            return true;
        }

        // No rest days available — mark week as reduced, skip the workout
        workout.Type = WorkoutType.Rest;
        workout.TargetDistanceMiles = 0;
        workout.PaceZone = null;
        workout.WarmUpDuration = null;
        workout.CoolDownDuration = null;
        return true;
    }

    /// <summary>
    /// Swaps two workouts within the same week.
    /// </summary>
    public static void SwapWorkouts(ScheduledWorkout a, ScheduledWorkout b)
    {
        (a.Type, b.Type) = (b.Type, a.Type);
        (a.TargetDistanceMiles, b.TargetDistanceMiles) = (b.TargetDistanceMiles, a.TargetDistanceMiles);
        (a.TargetDuration, b.TargetDuration) = (b.TargetDuration, a.TargetDuration);
        (a.PaceZone, b.PaceZone) = (b.PaceZone, a.PaceZone);
        (a.WarmUpDuration, b.WarmUpDuration) = (b.WarmUpDuration, a.WarmUpDuration);
        (a.CoolDownDuration, b.CoolDownDuration) = (b.CoolDownDuration, a.CoolDownDuration);
        (a.Status, b.Status) = (b.Status, a.Status);
        (a.Feedback, b.Feedback) = (b.Feedback, a.Feedback);
    }

    /// <summary>
    /// Checks if two workouts are on consecutive days and both are hard/quality workouts.
    /// Returns true if placing them adjacent is problematic.
    /// </summary>
    public static bool AreConsecutiveHardDays(ScheduledWorkout a, ScheduledWorkout b)
    {
        if (!a.IsQualityWorkout && a.Type != WorkoutType.LongRun) return false;
        if (!b.IsQualityWorkout && b.Type != WorkoutType.LongRun) return false;

        var gap = Math.Abs((a.Date - b.Date).TotalDays);
        return gap <= 1;
    }

    /// <summary>
    /// Validates whether a swap would create consecutive hard days.
    /// </summary>
    public static bool ValidateSwap(TrainingWeek week, ScheduledWorkout source, ScheduledWorkout target)
    {
        // Simulate the swap and check for consecutive hard days
        var sourceType = source.Type;
        var targetType = target.Type;

        // After swap, check if target position would be adjacent to another hard workout
        foreach (var workout in week.Workouts)
        {
            if (workout == source || workout == target) continue;

            // Check if source's new position (target's day) is adjacent to a hard workout
            if (AreConsecutiveHardDaysAfterSwap(target.Date, sourceType, workout))
                return false;

            // Check if target's new position (source's day) is adjacent to a hard workout
            if (AreConsecutiveHardDaysAfterSwap(source.Date, targetType, workout))
                return false;
        }

        return true;
    }

    private static bool AreConsecutiveHardDaysAfterSwap(DateTime newDate, WorkoutType movedType, ScheduledWorkout neighbor)
    {
        var isMovedHard = movedType is WorkoutType.Tempo or WorkoutType.Intervals
            or WorkoutType.HillRepeats or WorkoutType.RacePace or WorkoutType.LongRun;
        var isNeighborHard = neighbor.IsQualityWorkout || neighbor.Type == WorkoutType.LongRun;

        if (!isMovedHard || !isNeighborHard) return false;

        var gap = Math.Abs((newDate - neighbor.Date).TotalDays);
        return gap <= 1;
    }

    /// <summary>
    /// Checks if a taper week allows adding volume. Returns false during taper.
    /// </summary>
    public static bool CanAddVolume(TrainingWeek week)
    {
        return week.Phase != TrainingPhase.Taper;
    }

    /// <summary>
    /// Reflows remaining weeks when available days change.
    /// Preserves completed weeks, regenerates future weeks.
    /// </summary>
    public void ReflowRemainingWeeks(NewTrainingPlan plan, DayOfWeek[] newAvailableDays)
    {
        var engine = new PlanGenerationEngine();
        var today = DateTime.Today;

        foreach (var week in plan.Weeks)
        {
            // Skip completed weeks (all workouts in the past or completed)
            if (week.Workouts.All(w => w.Date.Date < today || w.Status == WorkoutStatus.Completed))
                continue;

            // Redistribute workouts for future weeks based on new available days
            RedistributeWeek(week, newAvailableDays);
        }
    }

    private void RedistributeWeek(TrainingWeek week, DayOfWeek[] availableDays)
    {
        // Collect existing non-rest workouts
        var workouts = week.Workouts
            .Where(w => w.Type != WorkoutType.Rest && w.Status != WorkoutStatus.Completed)
            .OrderByDescending(w => w.Type == WorkoutType.LongRun) // Long run first
            .ThenByDescending(w => w.IsQualityWorkout) // Quality second
            .ToList();

        // Reset all future uncompleted workouts to rest
        foreach (var w in week.Workouts.Where(w => w.Status != WorkoutStatus.Completed))
        {
            w.Type = WorkoutType.Rest;
            w.TargetDistanceMiles = 0;
            w.PaceZone = null;
            w.WarmUpDuration = null;
            w.CoolDownDuration = null;
        }

        // Reassign workouts to new available days
        var availableSlots = week.Workouts
            .Where(w => availableDays.Contains(w.DayOfWeek) && w.Status != WorkoutStatus.Completed)
            .ToList();

        for (int i = 0; i < Math.Min(workouts.Count, availableSlots.Count); i++)
        {
            var source = workouts[i];
            var slot = availableSlots[i];

            slot.Type = source.Type;
            slot.TargetDistanceMiles = source.TargetDistanceMiles;
            slot.PaceZone = source.PaceZone;
            slot.WarmUpDuration = source.WarmUpDuration;
            slot.CoolDownDuration = source.CoolDownDuration;
        }
    }

    /// <summary>
    /// Compresses a week to fit fewer available days.
    /// Preserves long run + quality sessions, drops easy/recovery runs first.
    /// </summary>
    public static void CompressWeek(TrainingWeek week, DayOfWeek[] availableDays)
    {
        var available = new HashSet<DayOfWeek>(availableDays);
        var futureWorkouts = week.Workouts
            .Where(w => w.Status != WorkoutStatus.Completed && w.Type != WorkoutType.Rest)
            .ToList();

        var availableSlots = week.Workouts
            .Where(w => available.Contains(w.DayOfWeek) && w.Status != WorkoutStatus.Completed)
            .OrderBy(w => w.Date)
            .ToList();

        if (futureWorkouts.Count <= availableSlots.Count)
            return; // Enough room, no compression needed

        // Rank workouts by priority: long run > quality > easy/recovery
        var ranked = futureWorkouts
            .OrderByDescending(w => w.Type == WorkoutType.LongRun ? 2 : w.IsQualityWorkout ? 1 : 0)
            .ToList();

        // Keep only as many as we have slots
        var keep = ranked.Take(availableSlots.Count).ToList();
        var drop = ranked.Skip(availableSlots.Count).ToList();

        // Convert dropped workouts to rest
        foreach (var w in drop)
        {
            w.Type = WorkoutType.Rest;
            w.TargetDistanceMiles = 0;
            w.PaceZone = null;
            w.WarmUpDuration = null;
            w.CoolDownDuration = null;
        }
    }

    /// <summary>
    /// Handles vacation by reducing volume for the specified date range.
    /// Returns false if the feature requires paid access.
    /// </summary>
    public bool HandleVacation(NewTrainingPlan plan, DateTime startDate, DateTime endDate, bool isPaidUser, bool fullRest)
    {
        if (!isPaidUser) return false;

        var vacationDays = (endDate - startDate).Days + 1;

        foreach (var week in plan.Weeks)
        {
            foreach (var workout in week.Workouts)
            {
                if (workout.Date.Date < startDate.Date || workout.Date.Date > endDate.Date)
                    continue;

                if (fullRest)
                {
                    workout.Type = WorkoutType.Rest;
                    workout.TargetDistanceMiles = 0;
                    workout.PaceZone = null;
                    workout.WarmUpDuration = null;
                    workout.CoolDownDuration = null;
                }
                else
                {
                    // Light maintenance: convert to easy runs at 50% volume
                    if (workout.IsRunWorkout)
                    {
                        workout.Type = WorkoutType.Easy;
                        workout.TargetDistanceMiles = Math.Round(workout.TargetDistanceMiles * 0.5m, 1);
                        workout.PaceZone = PaceZone.ForWorkoutType(WorkoutType.Easy);
                        workout.WarmUpDuration = null;
                        workout.CoolDownDuration = null;
                    }
                }
            }
        }

        return true;
    }
}
