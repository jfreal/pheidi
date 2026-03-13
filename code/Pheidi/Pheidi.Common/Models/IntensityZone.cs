namespace Pheidi.Common.Models;

public enum IntensityZone
{
    Easy,
    Threshold,
    Hard
}

public static class IntensityZoneExtensions
{
    public static IntensityZone FromWorkoutType(WorkoutType type) => type switch
    {
        WorkoutType.Easy or WorkoutType.Recovery or WorkoutType.LongRun => IntensityZone.Easy,
        WorkoutType.Tempo or WorkoutType.RacePace or WorkoutType.Fartlek => IntensityZone.Threshold,
        WorkoutType.Intervals or WorkoutType.HillRepeats => IntensityZone.Hard,
        _ => IntensityZone.Easy
    };

    public static IntensityZone FromEffort(int? effort) => effort switch
    {
        null => IntensityZone.Easy,
        <= 4 => IntensityZone.Easy,
        <= 6 => IntensityZone.Threshold,
        _ => IntensityZone.Hard
    };
}
