﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace Pheidi.Common
{
    public class TrainingPlan
    {
        public void SetLong(Week week, decimal longRunDistance)
        {
            var longDistance = Math.Max(longRunDistance, this.MinRunDistance);           

            week.Distances[DistanceType.Half] = longDistance / 2m;
            week.Distances[DistanceType.Long] = longDistance;
            week.Distances[DistanceType.Quarter] = longDistance / 4m;
        }

        public DayConfig[] DayConfigs { get; }

        public TrainingPlan()
        {
            this.DayConfigs = new[] {
               new DayConfig(DistanceType.None, Activity.Rest, EffortType.None),
               new DayConfig(DistanceType.Quarter, Activity.Run, EffortType.Distance),
               new DayConfig(DistanceType.Half, Activity.Run, EffortType.Distance),
               new DayConfig(DistanceType.Quarter, Activity.Run, EffortType.Distance),
               new DayConfig(DistanceType.None, Activity.Rest, EffortType.None),
               new DayConfig(DistanceType.Long, Activity.Run, EffortType.Distance),
               new DayConfig(DistanceType.None, Activity.Rest, EffortType.None)
            };
        }

        public List<Week> Weeks { get; set; } = new List<Week>();

        public int NumberOfWeeks { get; set; } = 16;

        public DateTime MarathonDate { get; set; }

        public int WeeksOfTaper { get; set; } = 3;

        public int LongRunMaxDistance { get; set; } = 20;

        public int MinRunDistance { get; set; } = 2;

        public void Generate()
        {
            Weeks.Clear();

            var lastLongRunWeekNumber = 1 + this.WeeksOfTaper;

            var newWeeks = new List<Week>();

            //initial week generation
            for (int i = this.NumberOfWeeks; i != 0; i--)
            {
                var newWeek = new Week(i);

                newWeeks.Add(newWeek);

                if(i <= this.WeeksOfTaper)
                {
                    newWeek.Taper = true;
                }
            }

            newWeeks.Single(_ => _.WeekNumber == lastLongRunWeekNumber).LastLongRun = true;
            
            //Set long runs by starting at the longest week and then ramping down.
            for (int i = lastLongRunWeekNumber; i <= this.NumberOfWeeks; i += 2)
            {
                var iteratedWeek = newWeeks.Single(_ => _.WeekNumber == i);

                if (iteratedWeek.Taper)
                {
                    continue;
                }

                SetLong(iteratedWeek, (this.LongRunMaxDistance - (this.RampUp * (i - lastLongRunWeekNumber))));
            }

            //var numberOfRampDownWeeks = this.RampUp 

            //set recovery runs by taking the first week and setting the long run three weeks out
            for (int i = this.NumberOfWeeks; i >= lastLongRunWeekNumber; i -= 2)
            {
                var iteratedWeek = newWeeks.Single(_ => _.WeekNumber == i);              

                var threeWeeksLater = newWeeks.Single(_ => _.WeekNumber == i - 3);

                if (threeWeeksLater.Taper || iteratedWeek.Distances[DistanceType.Long] == 0)
                {
                    continue;
                }

                SetLong(threeWeeksLater,iteratedWeek.Distances[DistanceType.Long]);
            }

            //because of how the code works there will be a non taper week without a long run
            //lets set that to the shortest long run, minus two ramps
            var shortestLongRun = newWeeks.Where(_ => !_.Taper && _.Distances[DistanceType.Long] != 0).OrderBy(_ => _.Distances[DistanceType.Long]);

            var zeroWeek = newWeeks.Single(_ => !_.Taper && _.Distances[DistanceType.Long] == 0);
            this.SetLong(zeroWeek, shortestLongRun.First().Distances[DistanceType.Long] - (2 * this.RampUp));


            //for (int i = this.NumberOfWeeks - 1 - this.WeeksOfTaper; i >= 0; i--)
            //{
            //    var week = new Week(i + 1);

            //    var nextWeek = Weeks.FirstOrDefault(w => w.WeekNumber == week.WeekNumber + 1);

            //   var weeksUntilLongRun = lastLongRunWeekNumber - week.WeekNumber;

            //   var longWeek = weeksUntilLongRun % 2 == 0;

            //    // -1 is marathon week
            //    if (week.WeekNumber == lastLongRunWeekNumber)
            //    {
            //        week.LastLongRun = true;
            //        week.Distances[DistanceType.Long] = this.LongRunMaxDistance;
            //    }
            //    else
            //    {

            //        if (longWeek && week.WeekNumber < lastLongRunWeekNumber)
            //        {
            //            week.Distances[DistanceType.Long] = this.LongRunMaxDistance - weeksUntilLongRun;
            //        }

            //        //longest run generation
            //        if (!longWeek && week.WeekNumber < lastLongRunWeekNumber)
            //        {
            //           var recoveryDistance = 6;

            //            if (week.WeekNumber < this.NumberOfWeeks / 2)
            //            {
            //                recoveryDistance = 5;
            //            }


            //            week.Distances[DistanceType.Long] = nextWeek.Distances[DistanceType.Long] - recoveryDistance;
            //        }
            //    }

            //    //mid week generation
            //    if (week.WeekNumber == lastLongRunWeekNumber)
            //    {
            //        week.Distances[DistanceType.Half] = this.LongRunMaxDistance / 2;
            //        week.Distances[DistanceType.Quarter] = (int)Math.Max(this.LongRunMaxDistance / 2m / 2m, this.MinRunDistance);
            //    }
            //    else
            //    {

            //       var incrementedHalf = (int)Math.Ceiling((week.WeekNumber / 2m) + 2);

            //        week.Distances[DistanceType.Half] = incrementedHalf;
            //        week.Distances[DistanceType.Quarter] = Math.Max(incrementedHalf / 2, this.MinRunDistance);
            //        week.Distances[DistanceType.QuarterUp] = Math.Max(incrementedHalf / 2, this.MinRunDistance);
            //    }

            //    Weeks.Add(week);
            //}

            Weeks = newWeeks;

           // //TAPER
           //var halfWay = this.NumberOfWeeks - this.WeeksOfTaper;
           // for (var i = 0; i < this.WeeksOfTaper; i++)
           // {

           //    var halfOfTheLongestRun = (this.LongRunMaxDistance / 2);

           //    var midWeekRun = halfOfTheLongestRun - ((i + 1) * 2);

           //     halfWay = halfWay / 2;

           //    var halfWayWeek = Weeks[halfWay - 1];

           //    var taperWeek = Weeks[this.NumberOfWeeks - this.WeeksOfTaper + i];

           //     taperWeek.Taper = true;

           //     //0 2 1
           //     //1 4 2
           //     //2 6 3

           //     taperWeek.Distances[DistanceType.Half] = midWeekRun;
           //     taperWeek.Distances[DistanceType.QuarterUp] = (int)Math.Round(midWeekRun / 2m);
           //     taperWeek.Distances[DistanceType.Quarter] = (int)Math.Ceiling(midWeekRun / 2m);

           //     if (halfWayWeek.Distances.ContainsKey(DistanceType.Long))
           //     {
           //         taperWeek.Distances[DistanceType.Long] = halfWayWeek.Distances[DistanceType.Long];
           //     }
           // }

            //if (this.marathonDate)
            //{
            //   var now = new Date();
            //    now.setDate(this.marathonDate.getDate() - this.numberOfWeeks * 7);
            //}
        }


        public PlanMetrics PlanMetrics
        {
            get
            {
                var metrics = new PlanMetrics();

                foreach (var week in Weeks)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        if (DayConfigs[i].Activity == Activity.Run)
                        {
                            var runDistance = week.Distances[DayConfigs[i].DistanceType];

                            if (runDistance >= 16)
                            {
                                metrics.RunsOver16++;

                                metrics.MilesOver16 += runDistance - 16;
                            }

                            if (runDistance >= 20)
                            {
                                metrics.RunsOver20++;
                            }                            
                        }
                    }
                }

                return metrics;
            }            
        }

        public int RampUp { get; private set; } = 1;
    }

    public class PlanMetrics
    {
        public int RunsOver16 { get; set; }

        public int RunsOver20 { get; set; }

        public decimal MilesOver16 { get; set; }

        public int StartingMileage { get; set; }

        public int Week16ToMax { get; set; }

        public int ToRace16 { get; set; }

        public int MaxToRace { get; set; }
    }
}
