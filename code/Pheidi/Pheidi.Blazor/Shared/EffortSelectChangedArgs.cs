using Pheidi.Common;

namespace Pheidi.Blazor.Shared
{
    public class EffortSelectChangedArgs
    {
        public EffortSelectChangedArgs(int dayNumber, EffortType effortType)
        {
            DayNumber = dayNumber;
            EffortType = effortType;
        }

        public int DayNumber { get; set; }
        public EffortType EffortType { get; set; }
    }
}
