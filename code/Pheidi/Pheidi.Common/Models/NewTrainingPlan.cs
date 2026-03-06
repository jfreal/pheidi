namespace Pheidi.Common.Models;

public enum PlanStatus
{
    Active,
    Completed,
    Archived
}

public class NewTrainingPlan
{
    public int Id { get; set; }
    public RaceGoal RaceGoal { get; set; } = new();
    public UserProfile UserProfile { get; set; } = new();
    public ProgressionPattern ProgressionPattern { get; set; } = ProgressionPattern.ThreeUpOneDown;
    public List<TrainingWeek> Weeks { get; set; } = [];
    public PlanStatus Status { get; set; } = PlanStatus.Active;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public int TotalWeeks => Weeks.Count;
    public decimal TotalPlannedMiles => Weeks.Sum(w => w.TotalPlannedDistance);
    public decimal PeakWeeklyMiles => Weeks.Count > 0 ? Weeks.Max(w => w.TotalPlannedDistance) : 0;
    public decimal LongestRun => Weeks.Count > 0 ? Weeks.Max(w => w.LongRunDistance) : 0;

    public int CompletedWorkouts => Weeks
        .SelectMany(w => w.Workouts)
        .Count(w => w.Status == WorkoutStatus.Completed);

    public int TotalRunWorkouts => Weeks
        .SelectMany(w => w.Workouts)
        .Count(w => w.IsRunWorkout);

    public decimal CompletionPercentage => TotalRunWorkouts > 0
        ? (decimal)CompletedWorkouts / TotalRunWorkouts * 100
        : 0;

    public TrainingWeek? CurrentWeek
    {
        get
        {
            var today = DateTime.Today;
            return Weeks.FirstOrDefault(w =>
                w.Workouts.Any(wo => wo.Date.Date <= today) &&
                w.Workouts.Any(wo => wo.Date.Date >= today));
        }
    }

    public ScheduledWorkout? TodaysWorkout
    {
        get
        {
            var today = DateTime.Today;
            return Weeks
                .SelectMany(w => w.Workouts)
                .FirstOrDefault(w => w.Date.Date == today);
        }
    }
}
