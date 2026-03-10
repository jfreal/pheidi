namespace Pheidi.Common.Models;

public enum PacePreference
{
    VDOT,
    RPE
}

public enum VdotZone
{
    Easy,
    Marathon,
    Tempo,
    Interval,
    Repetition
}

public class PaceZone
{
    public VdotZone Zone { get; init; }
    public decimal? MinPacePerMile { get; init; }
    public decimal? MaxPacePerMile { get; init; }
    public int? RpeMin { get; init; }
    public int? RpeMax { get; init; }
    public string RpeDescription { get; init; } = string.Empty;

    public static readonly Dictionary<VdotZone, (int Min, int Max, string Description)> RpeMapping = new()
    {
        [VdotZone.Easy] = (3, 4, "Conversational pace"),
        [VdotZone.Marathon] = (5, 6, "Steady, controlled effort"),
        [VdotZone.Tempo] = (7, 8, "Comfortably hard"),
        [VdotZone.Interval] = (8, 9, "Hard, breathing heavy"),
        [VdotZone.Repetition] = (9, 10, "All-out effort")
    };

    public PaceZone Clone() => new()
    {
        Zone = Zone,
        MinPacePerMile = MinPacePerMile,
        MaxPacePerMile = MaxPacePerMile,
        RpeMin = RpeMin,
        RpeMax = RpeMax,
        RpeDescription = RpeDescription
    };

    public static PaceZone ForWorkoutType(WorkoutType type) => type switch
    {
        WorkoutType.Easy or WorkoutType.Recovery => new PaceZone
        {
            Zone = VdotZone.Easy,
            RpeMin = 3, RpeMax = 4,
            RpeDescription = "Conversational pace"
        },
        WorkoutType.LongRun => new PaceZone
        {
            Zone = VdotZone.Easy,
            RpeMin = 4, RpeMax = 5,
            RpeDescription = "Conversational to steady"
        },
        WorkoutType.Tempo => new PaceZone
        {
            Zone = VdotZone.Tempo,
            RpeMin = 7, RpeMax = 8,
            RpeDescription = "Comfortably hard"
        },
        WorkoutType.Intervals => new PaceZone
        {
            Zone = VdotZone.Interval,
            RpeMin = 8, RpeMax = 9,
            RpeDescription = "Hard, breathing heavy"
        },
        WorkoutType.RacePace => new PaceZone
        {
            Zone = VdotZone.Marathon,
            RpeMin = 5, RpeMax = 6,
            RpeDescription = "Steady, controlled effort"
        },
        WorkoutType.HillRepeats => new PaceZone
        {
            Zone = VdotZone.Interval,
            RpeMin = 8, RpeMax = 9,
            RpeDescription = "Hard uphill effort"
        },
        WorkoutType.Fartlek => new PaceZone
        {
            Zone = VdotZone.Tempo,
            RpeMin = 5, RpeMax = 8,
            RpeDescription = "Varies — easy to hard"
        },
        _ => new PaceZone
        {
            Zone = VdotZone.Easy,
            RpeMin = 1, RpeMax = 3,
            RpeDescription = "Rest or light activity"
        }
    };
}
