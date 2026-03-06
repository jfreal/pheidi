namespace Pheidi.Common.Models;

public enum ExperienceLevel
{
    Beginner,
    Intermediate,
    Advanced
}

public static class ExperienceLevelExtensions
{
    public static int MaxRunDaysPerWeek(this ExperienceLevel level) => level switch
    {
        ExperienceLevel.Beginner => 4,
        ExperienceLevel.Intermediate => 5,
        ExperienceLevel.Advanced => 6,
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };

    public static decimal PeakWeeklyMiles(this ExperienceLevel level, RaceDistance distance) => (level, distance) switch
    {
        (ExperienceLevel.Beginner, RaceDistance.FullMarathon) => 35m,
        (ExperienceLevel.Intermediate, RaceDistance.FullMarathon) => 45m,
        (ExperienceLevel.Advanced, RaceDistance.FullMarathon) => 55m,
        (ExperienceLevel.Beginner, RaceDistance.HalfMarathon) => 25m,
        (ExperienceLevel.Intermediate, RaceDistance.HalfMarathon) => 35m,
        (ExperienceLevel.Advanced, RaceDistance.HalfMarathon) => 45m,
        (ExperienceLevel.Beginner, _) => 20m,
        (ExperienceLevel.Intermediate, _) => 30m,
        (ExperienceLevel.Advanced, _) => 40m,
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };

    public static bool AllowIntervalsFromStart(this ExperienceLevel level) => level != ExperienceLevel.Beginner;

    public static string Description(this ExperienceLevel level) => level switch
    {
        ExperienceLevel.Beginner => "New to running or returning after a long break",
        ExperienceLevel.Intermediate => "Running regularly for 6+ months",
        ExperienceLevel.Advanced => "Competitive runner with 2+ years experience",
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };
}
