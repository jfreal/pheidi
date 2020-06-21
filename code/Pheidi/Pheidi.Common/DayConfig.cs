namespace Pheidi.Common
{
    public class DayConfig
    {
        public DistanceType DistanceType { get; set; }
        public Activity Activity { get; set; }

        public EffortType EffortType { get; set; }

        public DayConfig(DistanceType distanceType, Activity activity, EffortType effortType)
        {
            this.DistanceType = distanceType;
            this.Activity = activity;
            this.EffortType = effortType;
        }
    }
}
