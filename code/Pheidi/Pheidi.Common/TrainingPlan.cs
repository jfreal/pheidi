using System;
using System.Collections.Generic;
using System.Linq;

namespace Pheidi.Common
{
    public class TrainingPlan
    {
        public DayConfig[] DayConfigs { get; }

        public TrainingPlan()
        {
            this.DayConfigs = new[] {
               new DayConfig(RunDistance.None, DayType.Rest),
               new DayConfig(RunDistance.Quarter, DayType.Run),
               new DayConfig(RunDistance.Half, DayType.Run),
               new DayConfig(RunDistance.Quarter, DayType.Run),
               new DayConfig(RunDistance.None, DayType.Rest),
               new DayConfig(RunDistance.Long, DayType.Run),
               new DayConfig(RunDistance.None, DayType.Rest)
            };
        }

        public List<Week> Weeks { get; set; } = new List<Week>();

        public int NumberOfWeeks { get; set; } = 16;

        public DateTime MarathonDate { get; set; }

        public int WeeksOfTaper { get; set; } = 3;

        public int LongRunMaxDistance { get; set; } = 20;

        public int MinRunDistance { get; set; } = 2;

    //    dayConfigs: DayConfig[];
    //marathonDate: Date;
    //numberOfWeeks: number = 18;
    //generatedWeeks: Week[];
    //weeksOfTaper: number = 3;
    //lastLongRunDistance: number = 20;
    //longRunDay: number = 6;
    //minimumRunDistance: number = 3;


        public void Generate()
        {
            var lastLongRunWeek = this.NumberOfWeeks - this.WeeksOfTaper;


            for (int i = this.NumberOfWeeks - 1; i >= 0; i--)
            {

                var week = new Week(i + 1);
    
                var nextWeek = Weeks.FirstOrDefault(w => w.WeekNumber == week.WeekNumber + 1);

               var weeksUntilLongRun = lastLongRunWeek - week.WeekNumber;

               var longWeek = weeksUntilLongRun % 2 == 0;

                // -1 is marathon week
                if (week.WeekNumber == lastLongRunWeek)
                {
                    week.LastLongRun = true;
                    week.Distances[RunDistance.Long] = this.LongRunMaxDistance;
                }
                else
                {

                    if (longWeek && week.WeekNumber < lastLongRunWeek)
                    {
                        week.Distances[RunDistance.Long] = this.LongRunMaxDistance - weeksUntilLongRun;
                    }

                    //longest run generation
                    if (!longWeek && week.WeekNumber < lastLongRunWeek)
                    {
                       var recoveryDistance = 6;

                        if (week.WeekNumber < this.NumberOfWeeks / 2)
                        {
                            recoveryDistance = 5;
                        }

    
                        week.Distances[RunDistance.Long] = nextWeek.Distances[RunDistance.Long] - recoveryDistance;
                    }
                }

                //mid week generation
                if (week.WeekNumber == lastLongRunWeek)
                {
                    week.Distances[RunDistance.Half] = this.LongRunMaxDistance / 2;
                    week.Distances[RunDistance.Quarter] = (int)Math.Max(this.LongRunMaxDistance / 2m / 2m, this.MinRunDistance);
                }
                else
                {

                   var incrementedHalf = (int)Math.Ceiling((week.WeekNumber / 2m) + 2);

                    week.Distances[RunDistance.Half] = incrementedHalf;
                    week.Distances[RunDistance.Quarter] = Math.Max(incrementedHalf / 2, this.MinRunDistance);
                    week.Distances[RunDistance.QuarterUp] = Math.Max(incrementedHalf / 2, this.MinRunDistance);
                }

                Weeks.Add(week);
            }

            Weeks.Reverse();

            //TAPER
           var halfWay = this.NumberOfWeeks - this.WeeksOfTaper;
            for (var i = 0; i < this.WeeksOfTaper; i++)
            {

               var halfOfTheLongestRun = (this.LongRunMaxDistance / 2);

               var midWeekRun = halfOfTheLongestRun - ((i + 1) * 2);

                // var taperDistance = Math.floor(lastLongRunWeek / 2) + 1;

                //var indexOfWeekToClone = Math.floor((lastLongRunWeek / 2) - 1);
                // var weekToClone = weeks[indexOfWeekToClone];

                halfWay = halfWay / 2;

               var halfWayWeek = Weeks[halfWay - 1];

               var taperWeek = Weeks[this.NumberOfWeeks - this.WeeksOfTaper + i];

                taperWeek.Taper = true;

                //0 2 1
                //1 4 2
                //2 6 3

                // week.distances[DistanceType.Half, midWeekRun);
                // week.distances[DistanceType.QuarterUp, Math.floor(midWeekRun / 2) + 1);
                // week.distances[DistanceType.Quarter, Math.floor(midWeekRun / 2));

                if (halfWayWeek.Distances.ContainsKey(RunDistance.Long))
                {
                    taperWeek.Distances[RunDistance.Long] = halfWayWeek.Distances[RunDistance.Long];
                }
            }

            //if (this.marathonDate)
            //{
            //   var now = new Date();
            //    now.setDate(this.marathonDate.getDate() - this.numberOfWeeks * 7);
            //}
        }
    }
}
