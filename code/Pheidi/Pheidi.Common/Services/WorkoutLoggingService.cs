using Pheidi.Common.Engines;
using Pheidi.Common.Models;

namespace Pheidi.Common.Services;

public class WorkoutLoggingService
{
    /// <summary>
    /// Quick-complete: mark workout as done with planned values.
    /// Task 9.3: Auto-assign ActualIntensityZone based on workout type.
    /// </summary>
    public void QuickComplete(ScheduledWorkout workout)
    {
        workout.Status = WorkoutStatus.Completed;
        workout.ActualDistanceMiles = workout.TargetDistanceMiles;
        workout.ActualIntensityZone = IntensityTrackingService.MapEffortToZone(null, workout.Type);
    }

    /// <summary>
    /// Manual entry: log actual values for a workout.
    /// Task 9.4: Map ActualEffort to ActualIntensityZone.
    /// </summary>
    public void LogWorkout(ScheduledWorkout workout, decimal? distance, TimeSpan? duration, int? effort)
    {
        workout.Status = WorkoutStatus.Completed;
        workout.ActualDistanceMiles = distance;
        workout.ActualDuration = duration;
        workout.ActualEffort = effort;
        workout.ActualIntensityZone = IntensityTrackingService.MapEffortToZone(effort, workout.Type);
    }

    /// <summary>
    /// Record post-workout feedback.
    /// </summary>
    public void RecordFeedback(ScheduledWorkout workout, WorkoutFeedback feedback)
    {
        workout.Feedback = feedback;
    }

    /// <summary>
    /// Skip a workout with no penalty.
    /// </summary>
    public void SkipWorkout(ScheduledWorkout workout)
    {
        workout.Status = WorkoutStatus.Skipped;
    }

    /// <summary>
    /// "Not Today" — try to reschedule to another available day in the same week.
    /// Returns true if rescheduled, false if skipped.
    /// </summary>
    public bool RescheduleToday(NewTrainingPlan plan, ScheduledWorkout workout)
    {
        var week = plan.Weeks.FirstOrDefault(w => w.Workouts.Contains(workout));
        if (week == null)
        {
            SkipWorkout(workout);
            return false;
        }

        // Find a rest day later in the same week
        var targetDay = week.Workouts
            .Where(w => w.Type == WorkoutType.Rest && w.Date > workout.Date && w.Status == WorkoutStatus.Pending)
            .OrderBy(w => w.Date)
            .FirstOrDefault();

        if (targetDay != null)
        {
            ScheduleFlexibilityEngine.SwapWorkouts(workout, targetDay);
            return true;
        }

        // No available days — mark as skipped
        SkipWorkout(workout);
        return false;
    }

    /// <summary>
    /// Calculate completion percentage for run workouts only.
    /// </summary>
    public static decimal CalculateCompletionPercentage(NewTrainingPlan plan)
    {
        return plan.CompletionPercentage;
    }

    /// <summary>
    /// Get a positive-only message based on completion status.
    /// </summary>
    public static string GetCompletionMessage(decimal completionPercentage, int completedCount) => completionPercentage switch
    {
        >= 90 => "Outstanding! You're crushing your plan!",
        >= 80 => "Great consistency! You're on track for a strong race.",
        >= 70 => "Solid work! Keep building momentum.",
        >= 50 => "Every run counts! Let's build momentum this week.",
        > 0 => "You've started — that's what matters. One run at a time!",
        _ => completedCount == 0 ? "Ready to start? Your first workout is waiting!" : "Let's get back on track!"
    };

    /// <summary>
    /// Get a message after completing a workout.
    /// </summary>
    public static string GetPostWorkoutMessage(WorkoutType type) => type switch
    {
        WorkoutType.LongRun => "Long run done! That's a big confidence builder.",
        WorkoutType.Tempo => "Tempo complete! Your race pace is getting stronger.",
        WorkoutType.Intervals => "Intervals crushed! Speed work pays off on race day.",
        WorkoutType.HillRepeats => "Hills conquered! You're building serious strength.",
        WorkoutType.Easy => "Easy run logged. Recovery runs are the foundation!",
        WorkoutType.Recovery => "Smart recovery. Your body thanks you.",
        _ => "Another workout in the books! Great work."
    };

    /// <summary>
    /// Get a message for a missed workout (positive only, no shaming).
    /// </summary>
    public static string GetMissedWorkoutMessage() =>
        "No worries — rest is training too. Here's what's coming up next.";
}
