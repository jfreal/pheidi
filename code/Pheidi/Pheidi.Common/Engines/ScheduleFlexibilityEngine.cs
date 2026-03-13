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
        // Clone PaceZones to avoid EF Core owned-entity tracking conflicts
        var pzA = a.PaceZone?.Clone();
        var pzB = b.PaceZone?.Clone();

        (a.Type, b.Type) = (b.Type, a.Type);
        (a.TargetDistanceMiles, b.TargetDistanceMiles) = (b.TargetDistanceMiles, a.TargetDistanceMiles);
        (a.TargetDuration, b.TargetDuration) = (b.TargetDuration, a.TargetDuration);
        a.PaceZone = pzB;
        b.PaceZone = pzA;
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
        ReflowRemainingWeeks(plan, newAvailableDays, TransitionTimePreset.None);
    }

    /// <summary>
    /// Task 10.4: Reflows remaining weeks respecting transition time when redistributing.
    /// </summary>
    public void ReflowRemainingWeeks(NewTrainingPlan plan, DayOfWeek[] newAvailableDays, TransitionTimePreset transitionPreset)
    {
        var today = DateTime.Today;

        foreach (var week in plan.Weeks)
        {
            if (week.Workouts.All(w => w.Date.Date < today || w.Status == WorkoutStatus.Completed))
                continue;

            RedistributeWeek(week, newAvailableDays);

            // Re-apply transition time durations after redistribution
            if (transitionPreset != TransitionTimePreset.None)
            {
                var transitionMinutes = transitionPreset.Minutes();
                foreach (var workout in week.Workouts.Where(w => w.IsRunWorkout && w.Status != WorkoutStatus.Completed))
                {
                    var paceMinPerMile = workout.IsQualityWorkout ? 9m : 10m;
                    var runMinutes = workout.TargetDistanceMiles * paceMinPerMile;
                    var warmUpMin = workout.WarmUpDuration?.TotalMinutes ?? 0;
                    var coolDownMin = workout.CoolDownDuration?.TotalMinutes ?? 0;
                    var totalMinutes = (int)(runMinutes + (decimal)warmUpMin + (decimal)coolDownMin + transitionMinutes);
                    workout.TargetDuration = TimeSpan.FromMinutes(totalMinutes);
                }
            }
        }
    }

    private void RedistributeWeek(TrainingWeek week, DayOfWeek[] availableDays)
    {
        // Snapshot workout data before resetting — clone PaceZone to avoid EF owned-entity conflicts
        var snapshots = week.Workouts
            .Where(w => w.Type != WorkoutType.Rest && w.Status != WorkoutStatus.Completed)
            .OrderByDescending(w => w.Type == WorkoutType.LongRun)
            .ThenByDescending(w => w.IsQualityWorkout)
            .Select(w => (w.Type, w.TargetDistanceMiles, PaceZone: w.PaceZone?.Clone(), w.WarmUpDuration, w.CoolDownDuration))
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

        // Reassign from snapshots to new available day slots
        var availableSlots = week.Workouts
            .Where(w => availableDays.Contains(w.DayOfWeek) && w.Status != WorkoutStatus.Completed)
            .ToList();

        for (int i = 0; i < Math.Min(snapshots.Count, availableSlots.Count); i++)
        {
            var src = snapshots[i];
            var slot = availableSlots[i];

            slot.Type = src.Type;
            slot.TargetDistanceMiles = src.TargetDistanceMiles;
            slot.PaceZone = src.PaceZone;
            slot.WarmUpDuration = src.WarmUpDuration;
            slot.CoolDownDuration = src.CoolDownDuration;
        }
    }

    /// <summary>
    /// Compresses a week to fit fewer available days.
    /// Preserves long run + quality sessions, drops easy/recovery runs first.
    /// Task 2.4: Uses VolumeMode max run days as the cap.
    /// </summary>
    public static void CompressWeek(TrainingWeek week, DayOfWeek[] availableDays, VolumeMode? volumeMode = null)
    {
        var available = new HashSet<DayOfWeek>(availableDays);
        var futureWorkouts = week.Workouts
            .Where(w => w.Status != WorkoutStatus.Completed && w.Type != WorkoutType.Rest)
            .ToList();

        var availableSlots = week.Workouts
            .Where(w => available.Contains(w.DayOfWeek) && w.Status != WorkoutStatus.Completed)
            .OrderBy(w => w.Date)
            .ToList();

        // Task 2.4: Cap run days by VolumeMode if provided
        var maxSlots = volumeMode.HasValue
            ? Math.Min(availableSlots.Count, volumeMode.Value.MaxRunDaysPerWeek())
            : availableSlots.Count;

        if (futureWorkouts.Count <= maxSlots)
            return; // Enough room, no compression needed

        // Rank workouts by priority: long run > quality > easy/recovery
        var ranked = futureWorkouts
            .OrderByDescending(w => w.Type == WorkoutType.LongRun ? 2 : w.IsQualityWorkout ? 1 : 0)
            .ToList();

        // Keep only as many as we have slots
        var keep = ranked.Take(maxSlots).ToList();
        var drop = ranked.Skip(maxSlots).ToList();

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

                workout.Modifier = WorkoutModifier.Vacation;

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
                    else
                    {
                        workout.Type = WorkoutType.Rest;
                        workout.TargetDistanceMiles = 0;
                        workout.PaceZone = null;
                    }
                }
            }
        }

        return true;
    }
}
