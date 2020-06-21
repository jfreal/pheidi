using Pheidi.Common;

namespace Pheidi.Blazor.Shared
{
    public class DistanceSelectChangedArgs
    {
        public DistanceSelectChangedArgs(int dayNumber, DistanceType distanceType)
        {
            DayNumber = dayNumber;
            DistanceType = distanceType;
        }

        public int DayNumber { get; set; }
        public DistanceType DistanceType { get; set; }
    }
}
