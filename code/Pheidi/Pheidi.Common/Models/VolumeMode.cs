namespace Pheidi.Common.Models;

public enum VolumeMode
{
    Minimal,
    Moderate,
    High,
    Elite
}

public static class VolumeModeExtensions
{
    public static int MaxRunDaysPerWeek(this VolumeMode mode) => mode switch
    {
        VolumeMode.Minimal => 3,
        VolumeMode.Moderate => 5,
        VolumeMode.High => 7,
        VolumeMode.Elite => 7,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    public static bool SupportsDoubles(this VolumeMode mode) => mode == VolumeMode.Elite;

    public static string Description(this VolumeMode mode) => mode switch
    {
        VolumeMode.Minimal => "3 runs/week — long run, tempo, speed (FIRST method)",
        VolumeMode.Moderate => "4–5 runs/week — the sweet spot for most runners",
        VolumeMode.High => "6–7 runs/week — for daily runners",
        VolumeMode.Elite => "Doubles — AM/PM sessions for 80+ km/week",
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    public static VolumeMode RecommendedDefault(this ExperienceLevel level) => level switch
    {
        ExperienceLevel.Beginner => VolumeMode.Moderate,
        ExperienceLevel.Intermediate => VolumeMode.Moderate,
        ExperienceLevel.Advanced => VolumeMode.High,
        _ => VolumeMode.Moderate
    };
}
