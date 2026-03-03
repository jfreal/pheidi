namespace Pheidi.Common;

public class Week(int weekNumber)
{
    public int WeekNumber { get; } = weekNumber;

    public bool LastLongRun { get; set; }

    public Dictionary<DistanceType, decimal> Distances { get; private set; } = new()
    {
        [DistanceType.Half] = 0,
        [DistanceType.Long] = 0,
        [DistanceType.None] = 0,
        [DistanceType.Quarter] = 0
    };

    public bool Taper { get; internal set; }

    public override string ToString() => $"{WeekNumber} + {Distances[DistanceType.Long]}";
}
