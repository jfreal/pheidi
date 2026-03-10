namespace Pheidi.Common.Models;

public class UserProfile
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public ExperienceLevel ExperienceLevel { get; set; } = ExperienceLevel.Beginner;
    public PacePreference PacePreference { get; set; } = PacePreference.RPE;
    public bool UseMiles { get; set; } = true;
    public DayOfWeek[] AvailableDays { get; set; } = [];
    public DayOfWeek PreferredLongRunDay { get; set; } = DayOfWeek.Saturday;
    public decimal? VdotValue { get; set; }
    public decimal? CurrentWeeklyMileage { get; set; }
    public int? RunningExperienceMonths { get; set; }
}
