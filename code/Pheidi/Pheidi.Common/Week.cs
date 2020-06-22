using System.Collections.Generic;

namespace Pheidi.Common
{
    public class Week
    {
        public Week(int weekNumber)
        {
            WeekNumber = weekNumber;
        }

        public int WeekNumber { get; }

        public bool LastLongRun { get; set; }

        public Dictionary<DistanceType, decimal> Distances { get; set; } = new Dictionary<DistanceType, decimal>();
        public bool Taper { get; internal set; }        
    }
}
