namespace Pheidi.Common.Models;

public class RaceGoal
{
    public int Id { get; set; }
    public RaceDistance Distance { get; set; }
    public DateTime RaceDate { get; set; }
    public TimeSpan? TargetTime { get; set; }
    public int? CustomPlanWeeks { get; set; }

    public int PlanWeeks
    {
        get
        {
            if (CustomPlanWeeks.HasValue)
            {
                var (min, max) = Distance.PlanWeekRange();
                return Math.Clamp(CustomPlanWeeks.Value, min, max);
            }
            return Distance.DefaultPlanWeeks();
        }
    }
}
