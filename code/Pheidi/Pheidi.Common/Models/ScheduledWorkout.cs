namespace Pheidi.Common.Models;

public enum WorkoutStatus
{
    Pending,
    Completed,
    Skipped
}

public enum WorkoutFeedback
{
    None,
    TooEasy,
    JustRight,
    Tough,
    TooHard
}

public class ScheduledWorkout
{
    public int Id { get; set; }
    public int TrainingWeekId { get; set; }
    public DateTime Date { get; set; }
    public DayOfWeek DayOfWeek => Date.DayOfWeek;
    public WorkoutType Type { get; set; } = WorkoutType.Rest;
    public decimal TargetDistanceMiles { get; set; }
    public TimeSpan? TargetDuration { get; set; }
    public PaceZone? PaceZone { get; set; }

    // Warm-up / Cool-down
    public TimeSpan? WarmUpDuration { get; set; }
    public TimeSpan? CoolDownDuration { get; set; }

    // Completion tracking
    public WorkoutStatus Status { get; set; } = WorkoutStatus.Pending;
    public decimal? ActualDistanceMiles { get; set; }
    public TimeSpan? ActualDuration { get; set; }
    public int? ActualEffort { get; set; }
    public WorkoutFeedback Feedback { get; set; } = WorkoutFeedback.None;
    public int? CompletionPercent { get; set; }
    public int? ReadinessScore { get; set; }

    public string Description => Type switch
    {
        WorkoutType.Rest => "Rest Day",
        WorkoutType.Easy => $"Easy Run — {TargetDistanceMiles:F1} mi",
        WorkoutType.Tempo => $"Tempo Run — {TargetDistanceMiles:F1} mi",
        WorkoutType.Intervals => $"Intervals — {TargetDistanceMiles:F1} mi",
        WorkoutType.LongRun => $"Long Run — {TargetDistanceMiles:F1} mi",
        WorkoutType.Recovery => $"Recovery Run — {TargetDistanceMiles:F1} mi",
        WorkoutType.Fartlek => $"Fartlek — {TargetDistanceMiles:F1} mi",
        WorkoutType.HillRepeats => $"Hill Repeats — {TargetDistanceMiles:F1} mi",
        WorkoutType.RacePace => $"Race Pace — {TargetDistanceMiles:F1} mi",
        WorkoutType.CrossTraining => "Cross Training",
        WorkoutType.Strength => "Strength Training",
        _ => Type.ToString()
    };

    public bool IsQualityWorkout => Type is WorkoutType.Tempo or WorkoutType.Intervals
        or WorkoutType.HillRepeats or WorkoutType.RacePace;

    public bool IsRunWorkout => Type is not (WorkoutType.Rest or WorkoutType.CrossTraining or WorkoutType.Strength);
}
