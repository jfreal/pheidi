using Pheidi.Common.Models;

namespace Pheidi.Common.Engines;

public class PlanGenerationEngine
{
    private const decimal MinRunDistance = 2m;
    private static readonly TimeSpan DefaultWarmUp = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan DefaultCoolDown = TimeSpan.FromMinutes(10);

    public NewTrainingPlan Generate(RaceGoal raceGoal, UserProfile profile)
    {
        var plan = new NewTrainingPlan
        {
            RaceGoal = raceGoal,
            UserProfile = profile,
            ProgressionPattern = ProgressionPattern.ThreeUpOneDown
        };

        var totalWeeks = raceGoal.PlanWeeks;
        var phases = AllocatePhases(totalWeeks);
        var longRunDistances = GenerateLongRunProgression(
            plan.ProgressionPattern,
            raceGoal.Distance.PeakLongRunMiles(),
            phases,
            totalWeeks);

        var peakWeeklyMiles = profile.ExperienceLevel.PeakWeeklyMiles(raceGoal.Distance);
        var availableDays = profile.AvailableDays.Length > 0
            ? profile.AvailableDays
            : DefaultAvailableDays(profile.ExperienceLevel);

        for (int weekNum = 1; weekNum <= totalWeeks; weekNum++)
        {
            var phase = phases[weekNum - 1];
            var longRunDistance = longRunDistances[weekNum - 1];

            var weekDate = raceGoal.RaceDate.AddDays(-7 * (totalWeeks - weekNum));
            var mondayOfWeek = weekDate.AddDays(-(int)weekDate.DayOfWeek + (int)DayOfWeek.Monday);
            if (weekDate.DayOfWeek == DayOfWeek.Sunday)
                mondayOfWeek = mondayOfWeek.AddDays(-7);

            var taperMultiplier = GetTaperMultiplier(phase, phases, weekNum);
            var weeklyTarget = peakWeeklyMiles * GetPhaseVolumeMultiplier(phase) * taperMultiplier;

            var week = new TrainingWeek
            {
                WeekNumber = weekNum,
                Phase = phase
            };

            var workouts = DistributeWorkouts(
                availableDays, phase, profile.ExperienceLevel,
                longRunDistance, weeklyTarget, mondayOfWeek,
                weekNum, totalWeeks);

            week.Workouts = workouts;
            plan.Weeks.Add(week);
        }

        return plan;
    }

    public static Dictionary<TrainingPhase, int> AllocatePhaseCounts(int totalWeeks)
    {
        var taper = Math.Max(2, (int)Math.Round(totalWeeks * 0.20));
        var peak = Math.Max(1, (int)Math.Round(totalWeeks * 0.15));
        var basePhase = Math.Max(2, (int)Math.Round(totalWeeks * 0.25));
        var build = totalWeeks - taper - peak - basePhase;

        return new Dictionary<TrainingPhase, int>
        {
            [TrainingPhase.Base] = basePhase,
            [TrainingPhase.Build] = build,
            [TrainingPhase.Peak] = peak,
            [TrainingPhase.Taper] = taper
        };
    }

    internal static TrainingPhase[] AllocatePhases(int totalWeeks)
    {
        var counts = AllocatePhaseCounts(totalWeeks);
        var phases = new TrainingPhase[totalWeeks];
        var idx = 0;

        foreach (var phase in new[] { TrainingPhase.Base, TrainingPhase.Build, TrainingPhase.Peak, TrainingPhase.Taper })
        {
            for (int i = 0; i < counts[phase] && idx < totalWeeks; i++, idx++)
            {
                phases[idx] = phase;
            }
        }

        return phases;
    }

    internal static decimal[] GenerateLongRunProgression(
        ProgressionPattern pattern,
        decimal peakDistance,
        TrainingPhase[] phases,
        int totalWeeks)
    {
        var distances = new decimal[totalWeeks];
        var startDistance = Math.Max(MinRunDistance, peakDistance * 0.4m);
        var increment = (peakDistance - startDistance) / Math.Max(1, totalWeeks - 4);

        var current = startDistance;
        int upCount = 0;

        int cycleUp = pattern switch
        {
            ProgressionPattern.Linear => int.MaxValue,
            ProgressionPattern.TwoUpOneDown => 2,
            ProgressionPattern.ThreeUpOneDown => 3,
            ProgressionPattern.FourUpOneDown => 4,
            _ => 3
        };

        for (int i = 0; i < totalWeeks; i++)
        {
            if (phases[i] == TrainingPhase.Taper)
            {
                // Taper weeks get reduced long runs handled separately
                distances[i] = 0;
                continue;
            }

            distances[i] = Math.Min(Math.Round(current, 1), peakDistance);
            upCount++;

            if (upCount >= cycleUp && pattern != ProgressionPattern.Linear)
            {
                current -= increment * 1.5m;
                current = Math.Max(current, startDistance);
                upCount = 0;
            }
            else
            {
                current += increment;
            }
        }

        // Apply taper long run reduction
        int taperStart = Array.FindIndex(phases, p => p == TrainingPhase.Taper);
        if (taperStart > 0)
        {
            var preTaperLong = distances[taperStart - 1];
            var taperWeeks = totalWeeks - taperStart;
            for (int i = 0; i < taperWeeks; i++)
            {
                var reduction = (i + 1) switch
                {
                    1 => 0.75m,
                    2 => 0.60m,
                    _ => 0.40m
                };
                distances[taperStart + i] = Math.Round(preTaperLong * reduction, 1);
            }
        }

        return distances;
    }

    private static decimal GetPhaseVolumeMultiplier(TrainingPhase phase) => phase switch
    {
        TrainingPhase.Base => 0.65m,
        TrainingPhase.Build => 0.85m,
        TrainingPhase.Peak => 1.0m,
        TrainingPhase.Taper => 1.0m, // Taper multiplier handled separately
        _ => 0.7m
    };

    internal static decimal GetTaperMultiplier(TrainingPhase phase, TrainingPhase[] allPhases, int weekNum)
    {
        if (phase != TrainingPhase.Taper) return 1.0m;

        int taperStart = Array.FindIndex(allPhases, p => p == TrainingPhase.Taper);
        int taperWeekIndex = weekNum - 1 - taperStart;

        return taperWeekIndex switch
        {
            0 => 0.75m,
            1 => 0.60m,
            _ => 0.40m
        };
    }

    private List<ScheduledWorkout> DistributeWorkouts(
        DayOfWeek[] availableDays,
        TrainingPhase phase,
        ExperienceLevel level,
        decimal longRunDistance,
        decimal weeklyTarget,
        DateTime mondayOfWeek,
        int weekNum,
        int totalWeeks)
    {
        var workouts = new List<ScheduledWorkout>();
        var maxRunDays = level.MaxRunDaysPerWeek();
        var runDays = Math.Min(availableDays.Length, maxRunDays);

        // Create a workout for each day of the week
        for (int d = 0; d < 7; d++)
        {
            var dayOfWeek = (DayOfWeek)d;
            var date = mondayOfWeek.AddDays(d == 0 ? 6 : d - 1); // Monday = day 0 in our layout

            // Adjust for DayOfWeek enum (Sunday=0)
            date = mondayOfWeek.AddDays(((int)dayOfWeek - (int)DayOfWeek.Monday + 7) % 7);

            workouts.Add(new ScheduledWorkout
            {
                Date = date,
                Type = WorkoutType.Rest
            });
        }

        if (runDays == 0) return workouts;

        // Assign long run to preferred day (default Saturday) or last available day
        var longRunDay = availableDays.Contains(DayOfWeek.Saturday)
            ? DayOfWeek.Saturday
            : availableDays.Last();

        var longRunWorkout = workouts.First(w => w.DayOfWeek == longRunDay);
        longRunWorkout.Type = WorkoutType.LongRun;
        longRunWorkout.TargetDistanceMiles = Math.Max(longRunDistance, MinRunDistance);
        longRunWorkout.PaceZone = PaceZone.ForWorkoutType(WorkoutType.LongRun);
        longRunWorkout.CoolDownDuration = TimeSpan.FromMinutes(5);

        var remainingDistance = Math.Max(0, weeklyTarget - longRunWorkout.TargetDistanceMiles);
        var remainingRunDays = runDays - 1;
        var otherAvailableDays = availableDays.Where(d => d != longRunDay).ToList();

        // Assign quality workout if in Build/Peak and experience allows
        if (remainingRunDays > 0 && phase is TrainingPhase.Build or TrainingPhase.Peak
            && (level.AllowIntervalsFromStart() || weekNum > 4))
        {
            var qualityDay = PickQualityDay(otherAvailableDays, longRunDay);
            if (qualityDay.HasValue)
            {
                var qualityType = phase == TrainingPhase.Peak ? WorkoutType.Tempo : PickQualityWorkoutType(weekNum);
                var qualityDistance = Math.Round(remainingDistance * 0.35m, 1);
                qualityDistance = Math.Max(qualityDistance, MinRunDistance);

                var qualityWorkout = workouts.First(w => w.DayOfWeek == qualityDay.Value);
                qualityWorkout.Type = qualityType;
                qualityWorkout.TargetDistanceMiles = qualityDistance;
                qualityWorkout.PaceZone = PaceZone.ForWorkoutType(qualityType);
                qualityWorkout.WarmUpDuration = DefaultWarmUp;
                qualityWorkout.CoolDownDuration = DefaultCoolDown;

                remainingDistance -= qualityDistance;
                remainingRunDays--;
                otherAvailableDays.Remove(qualityDay.Value);
            }
        }

        // Fill remaining days with easy/recovery runs
        if (remainingRunDays > 0 && otherAvailableDays.Count > 0)
        {
            var easyDistance = remainingRunDays > 0
                ? Math.Max(MinRunDistance, Math.Round(remainingDistance / remainingRunDays, 1))
                : MinRunDistance;

            foreach (var day in otherAvailableDays.Take(remainingRunDays))
            {
                var easyWorkout = workouts.First(w => w.DayOfWeek == day);
                easyWorkout.Type = WorkoutType.Easy;
                easyWorkout.TargetDistanceMiles = easyDistance;
                easyWorkout.PaceZone = PaceZone.ForWorkoutType(WorkoutType.Easy);
            }
        }

        return workouts;
    }

    private static DayOfWeek? PickQualityDay(List<DayOfWeek> availableDays, DayOfWeek longRunDay)
    {
        // Pick a day with at least one rest/easy day between it and the long run
        foreach (var day in availableDays)
        {
            var gap = Math.Abs((int)day - (int)longRunDay);
            if (gap == 0) gap = 7;
            if (gap >= 2) return day;
        }
        return availableDays.FirstOrDefault();
    }

    private static WorkoutType PickQualityWorkoutType(int weekNum)
    {
        // Alternate between tempo, intervals, and hill repeats
        return (weekNum % 3) switch
        {
            0 => WorkoutType.Intervals,
            1 => WorkoutType.Tempo,
            _ => WorkoutType.HillRepeats
        };
    }

    private static DayOfWeek[] DefaultAvailableDays(ExperienceLevel level) => level switch
    {
        ExperienceLevel.Beginner => [DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday],
        ExperienceLevel.Intermediate => [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday],
        ExperienceLevel.Advanced => [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Saturday],
        _ => [DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday]
    };
}
