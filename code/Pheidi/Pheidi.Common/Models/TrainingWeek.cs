namespace Pheidi.Common.Models;

public class TrainingWeek
{
    public int WeekNumber { get; set; }
    public TrainingPhase Phase { get; set; }
    public List<ScheduledWorkout> Workouts { get; set; } = [];

    public decimal TotalPlannedDistance => Workouts.Sum(w => w.TargetDistanceMiles);
    public decimal LongRunDistance => Workouts
        .Where(w => w.Type == WorkoutType.LongRun)
        .Select(w => w.TargetDistanceMiles)
        .FirstOrDefault();

    public int RunDayCount => Workouts.Count(w => w.IsRunWorkout);

    public DateTime WeekStartDate => Workouts.Count > 0
        ? Workouts.Min(w => w.Date)
        : DateTime.MinValue;

    public ScheduledWorkout? GetWorkout(DayOfWeek day) =>
        Workouts.FirstOrDefault(w => w.DayOfWeek == day);
}
