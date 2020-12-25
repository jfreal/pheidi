using System.Collections.Generic;

namespace Pheidi.Common
{
    public class Week
    {
        public Week(int weekNumber)
        {
            WeekNumber = weekNumber;

            this.Distances[DistanceType.Half] = 0;
            this.Distances[DistanceType.Long] = 0;
            this.Distances[DistanceType.None] = 0;
            this.Distances[DistanceType.Quarter] = 0;
        }

        public int WeekNumber { get; }

        public bool LastLongRun { get; set; }

        public Dictionary<DistanceType, decimal> Distances { get; private set; } = new Dictionary<DistanceType, decimal>();
        public bool Taper { get; internal set; }

     

        public override string ToString()
        {
            return $"{this.WeekNumber} + {this.Distances[DistanceType.Long]}";
        }
    }
}
