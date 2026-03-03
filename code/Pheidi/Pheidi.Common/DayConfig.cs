namespace Pheidi.Common;

public class DayConfig(DistanceType distanceType, Activity activity, EffortType effortType)
{
    public DistanceType DistanceType { get; set; } = distanceType;
    public Activity Activity { get; set; } = activity;
    public EffortType EffortType { get; set; } = effortType;
}
