namespace Pheidi.Common;

public class TrainingPlan
{
    public DayConfig[] DayConfigs { get; }

    public List<Week> Weeks { get; private set; } = [];

    public int NumberOfWeeks { get; set; } = 16;

    public DateTime MarathonDate { get; set; }

    public int WeeksOfTaper { get; set; } = 3;

    public int LongRunMaxDistance { get; set; } = 20;

    public int MinRunDistance { get; set; } = 2;

    public int RampUp { get; private set; } = 1;

    public TrainingPlan()
    {
        DayConfigs =
        [
            new DayConfig(DistanceType.None, Activity.Rest, EffortType.None),
            new DayConfig(DistanceType.Quarter, Activity.Run, EffortType.Distance),
            new DayConfig(DistanceType.Half, Activity.Run, EffortType.Distance),
            new DayConfig(DistanceType.Quarter, Activity.Run, EffortType.Distance),
            new DayConfig(DistanceType.None, Activity.Rest, EffortType.None),
            new DayConfig(DistanceType.Long, Activity.Run, EffortType.Distance),
            new DayConfig(DistanceType.None, Activity.Rest, EffortType.None)
        ];
    }

    public void SetLong(Week week, decimal longRunDistance)
    {
        var longDistance = Math.Max(longRunDistance, MinRunDistance);

        week.Distances[DistanceType.Half] = longDistance / 2m;
        week.Distances[DistanceType.Long] = longDistance;
        week.Distances[DistanceType.Quarter] = longDistance / 4m;
    }

    public void Generate()
    {
        var lastLongRunWeekNumber = 1 + WeeksOfTaper;

        var newWeeks = new List<Week>();

        // Initial week generation
        for (var i = NumberOfWeeks; i != 0; i--)
        {
            var newWeek = new Week(i);
            newWeeks.Add(newWeek);

            if (i <= WeeksOfTaper)
            {
                newWeek.Taper = true;
            }
        }

        newWeeks.Single(w => w.WeekNumber == lastLongRunWeekNumber).LastLongRun = true;

        // Set long runs by starting at the longest week and then ramping down
        for (var i = lastLongRunWeekNumber; i <= NumberOfWeeks; i += 2)
        {
            var iteratedWeek = newWeeks.Single(w => w.WeekNumber == i);

            if (iteratedWeek.Taper)
            {
                continue;
            }

            SetLong(iteratedWeek, LongRunMaxDistance - (RampUp * (i - lastLongRunWeekNumber)));
        }

        // Set recovery runs by taking the first week and setting the long run three weeks out
        for (var i = NumberOfWeeks; i >= lastLongRunWeekNumber; i -= 2)
        {
            var iteratedWeek = newWeeks.Single(w => w.WeekNumber == i);
            var threeWeeksLater = newWeeks.Single(w => w.WeekNumber == i - 3);

            if (threeWeeksLater.Taper || iteratedWeek.Distances[DistanceType.Long] == 0)
            {
                continue;
            }

            SetLong(threeWeeksLater, iteratedWeek.Distances[DistanceType.Long]);
        }

        // Because of how the code works there will be a non-taper week without a long run.
        // Set that to the shortest long run, minus two ramps.
        var shortestLongRun = newWeeks
            .Where(w => !w.Taper && w.Distances[DistanceType.Long] != 0)
            .OrderBy(w => w.Distances[DistanceType.Long]);

        var zeroWeek = newWeeks.Single(w => !w.Taper && w.Distances[DistanceType.Long] == 0);
        SetLong(zeroWeek, shortestLongRun.First().Distances[DistanceType.Long] - (2 * RampUp));

        Weeks = newWeeks;
    }

    public PlanMetrics PlanMetrics
    {
        get
        {
            var runsOver16 = 0;
            var runsOver20 = 0;
            var milesOver16 = 0m;

            foreach (var week in Weeks)
            {
                for (var i = 0; i < 7; i++)
                {
                    if (DayConfigs[i].Activity != Activity.Run) continue;

                    var runDistance = week.Distances[DayConfigs[i].DistanceType];

                    if (runDistance >= 16)
                    {
                        runsOver16++;
                        milesOver16 += runDistance - 16;
                    }

                    if (runDistance >= 20)
                    {
                        runsOver20++;
                    }
                }
            }

            return new PlanMetrics(runsOver16, runsOver20, milesOver16);
        }
    }
}

public record PlanMetrics(int RunsOver16 = 0, int RunsOver20 = 0, decimal MilesOver16 = 0);
