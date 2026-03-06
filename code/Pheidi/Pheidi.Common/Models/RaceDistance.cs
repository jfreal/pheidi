namespace Pheidi.Common.Models;

public enum RaceDistance
{
    FiveK,
    TenK,
    HalfMarathon,
    FullMarathon
}

public static class RaceDistanceExtensions
{
    public static decimal ToKilometers(this RaceDistance distance) => distance switch
    {
        RaceDistance.FiveK => 5.0m,
        RaceDistance.TenK => 10.0m,
        RaceDistance.HalfMarathon => 21.1m,
        RaceDistance.FullMarathon => 42.2m,
        _ => throw new ArgumentOutOfRangeException(nameof(distance))
    };

    public static decimal ToMiles(this RaceDistance distance) => distance switch
    {
        RaceDistance.FiveK => 3.1m,
        RaceDistance.TenK => 6.2m,
        RaceDistance.HalfMarathon => 13.1m,
        RaceDistance.FullMarathon => 26.2m,
        _ => throw new ArgumentOutOfRangeException(nameof(distance))
    };

    public static string DisplayName(this RaceDistance distance) => distance switch
    {
        RaceDistance.FiveK => "5K",
        RaceDistance.TenK => "10K",
        RaceDistance.HalfMarathon => "Half Marathon",
        RaceDistance.FullMarathon => "Full Marathon",
        _ => throw new ArgumentOutOfRangeException(nameof(distance))
    };

    public static (int Min, int Max) PlanWeekRange(this RaceDistance distance) => distance switch
    {
        RaceDistance.FiveK => (8, 12),
        RaceDistance.TenK => (10, 14),
        RaceDistance.HalfMarathon => (12, 16),
        RaceDistance.FullMarathon => (16, 20),
        _ => throw new ArgumentOutOfRangeException(nameof(distance))
    };

    public static int DefaultPlanWeeks(this RaceDistance distance)
    {
        var (min, max) = distance.PlanWeekRange();
        return (min + max) / 2;
    }

    public static decimal PeakLongRunMiles(this RaceDistance distance) => distance switch
    {
        RaceDistance.FiveK => 10m,
        RaceDistance.TenK => 14m,
        RaceDistance.HalfMarathon => 14m,
        RaceDistance.FullMarathon => 22m,
        _ => throw new ArgumentOutOfRangeException(nameof(distance))
    };
}
