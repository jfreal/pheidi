using System.Collections.Generic;
using System.Linq;

namespace Pheidi.Common
{

    public enum RunDistance
    {
        Long,
        Half,
        Quarter,
        QuarterUp,
        None = 0
    }

    public class Week
    {
        public Week(int weekNumber)
        {
            WeekNumber = weekNumber;
        }

        public int WeekNumber { get; }

        public bool LastLongRun { get; set; }

        public Dictionary<RunDistance, int> Distances { get; set; } = new Dictionary<RunDistance, int>();
        public bool Taper { get; internal set; }
    }

    public enum DayType
    {
        Rest,
        Run,
        Sprint,
        Cross,
        Strength,
        Fartlek
    }

    public class DayConfig
    {
        public RunDistance RunDistance { get; }
        public DayType DayType { get; }

        public DayConfig(RunDistance distanceType, DayType daysTYpe)
        {
            this.RunDistance = distanceType;
            this.DayType = daysTYpe;
        }
    }
}
