using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pheidi.Blazor.Shared
{
    public class ActivitySelectChangedArgs
    {
        public ActivitySelectChangedArgs(int dayNumber, Pheidi.Common.Activity activity)
        {
            DayNumber = dayNumber;
            Activity = activity;
        }

        public int DayNumber { get; set; }
        public Pheidi.Common.Activity Activity { get; set; }
    }
}
