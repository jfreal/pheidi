using Pheidi.Common.Models;

namespace Pheidi.Common.Engines;

public class PlanGenerationEngine
{
    private const decimal MinRunDistance = 2m;
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

        // Task 5.2/5.3: Apply adaptive progression rates
        ApplyAdaptiveProgression(longRunDistances, phases);

        // Task 6.1/6.2/6.3/6.4: Apply beginner 50% increase cap with transition week insertion
        if (profile.ExperienceLevel == ExperienceLevel.Beginner)
        {
            (longRunDistances, phases, totalWeeks) = ApplyBeginnerIncreaseCapWithTransitions(
                longRunDistances, phases, raceGoal.Distance.PeakLongRunMiles());
        }

        // Task 7.2: Apply spike guard (110% cap over 4-week window)
        ApplySpikeGuard(longRunDistances, phases);

        var peakWeeklyMiles = profile.ExperienceLevel.PeakWeeklyMiles(raceGoal.Distance);
        var availableDays = profile.AvailableDays.Length > 0
            ? profile.AvailableDays
            : DefaultAvailableDays(profile.ExperienceLevel);

        // Task 2.2: Use VolumeMode for max run days instead of ExperienceLevel
        var volumeMode = profile.VolumeMode;

        // Task 3.3/3.4: Age-adjusted training
        var ageGroup = AgeGroupExtensions.GetAgeGroup(profile.DateOfBirth);
        var warmUp = ageGroup.GetWarmUpDuration();

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
                weekNum, totalWeeks,
                volumeMode, ageGroup, warmUp, profile.TransitionTimePreset, profile);

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

    /// <summary>
    /// Task 5.2/5.3: Replace fixed increment with adaptive rates from AdaptiveProgressionCalculator.
    /// Implements equilibrium hold: after an increase, hold for 3 weeks before next increase.
    /// </summary>
    internal static void ApplyAdaptiveProgression(decimal[] distances, TrainingPhase[] phases)
    {
        int holdWeeksRemaining = 0;

        for (int i = 1; i < distances.Length; i++)
        {
            if (phases[i] == TrainingPhase.Taper) continue;

            if (holdWeeksRemaining > 0)
            {
                // Equilibrium hold: keep distance at previous level
                distances[i] = distances[i - 1];
                holdWeeksRemaining--;
                continue;
            }

            if (distances[i] > distances[i - 1] && phases[i - 1] != TrainingPhase.Taper)
            {
                // Apply adaptive rate cap
                var rate = AdaptiveProgressionCalculator.GetIncreaseRate(distances[i - 1]);
                var maxIncrease = distances[i - 1] * rate;
                var actualIncrease = distances[i] - distances[i - 1];

                if (actualIncrease > maxIncrease)
                {
                    distances[i] = Math.Round(distances[i - 1] + maxIncrease, 1);
                }

                // Start 3-week equilibrium hold after an increase
                holdWeeksRemaining = 3;
            }
        }
    }

    /// <summary>
    /// Task 6.1/6.2: Cap increase at 50% for beginners.
    /// Task 6.3/6.4: Insert transition weeks when capping creates a gap to the target peak.
    /// Returns potentially expanded arrays and updated total week count.
    /// </summary>
    internal static (decimal[] distances, TrainingPhase[] phases, int totalWeeks) ApplyBeginnerIncreaseCapWithTransitions(
        decimal[] distances, TrainingPhase[] phases, decimal peakTarget)
    {
        var distList = new List<decimal>(distances);
        var phaseList = new List<TrainingPhase>(phases);

        // First pass: apply the 50% cap
        for (int i = 1; i < distList.Count; i++)
        {
            if (phaseList[i] == TrainingPhase.Taper) continue;

            var prevDistance = 0m;
            for (int j = i - 1; j >= 0; j--)
            {
                if (phaseList[j] != TrainingPhase.Taper && distList[j] > 0)
                {
                    prevDistance = distList[j];
                    break;
                }
            }

            if (prevDistance > 0 && distList[i] > prevDistance * 1.5m)
            {
                distList[i] = Math.Round(prevDistance * 1.5m, 1);
            }
        }

        // Second pass: check if capping left a gap to the peak target before taper.
        // If so, insert transition weeks to bridge the gap.
        int taperStart = phaseList.IndexOf(TrainingPhase.Taper);
        if (taperStart < 0) taperStart = distList.Count;

        var preTaperDist = taperStart > 0 ? distList[taperStart - 1] : 0m;
        if (preTaperDist < peakTarget * 0.9m && preTaperDist > 0)
        {
            // Calculate how many transition weeks we need (max 4 to avoid bloat)
            var transitionWeeks = new List<decimal>();
            var current = preTaperDist;
            while (current < peakTarget * 0.95m && transitionWeeks.Count < 4)
            {
                current = Math.Min(Math.Round(current * 1.5m, 1), peakTarget);
                transitionWeeks.Add(current);
            }

            if (transitionWeeks.Count > 0)
            {
                // Insert transition weeks before the taper
                for (int i = 0; i < transitionWeeks.Count; i++)
                {
                    // Use Build phase for transition weeks (they're building toward peak)
                    distList.Insert(taperStart + i, transitionWeeks[i]);
                    phaseList.Insert(taperStart + i, TrainingPhase.Build);
                }
            }
        }

        return (distList.ToArray(), phaseList.ToArray(), distList.Count);
    }

    /// <summary>
    /// Task 7.2: Cap any single run at 110% of the max in the preceding 4 plan weeks.
    /// </summary>
    internal static void ApplySpikeGuard(decimal[] distances, TrainingPhase[] phases)
    {
        for (int i = 1; i < distances.Length; i++)
        {
            if (phases[i] == TrainingPhase.Taper) continue;

            var windowStart = Math.Max(0, i - 4);
            var recentMax = 0m;
            for (int j = windowStart; j < i; j++)
            {
                if (distances[j] > recentMax) recentMax = distances[j];
            }

            if (recentMax > 0)
            {
                var maxSafe = SpikeGuard.GetMaxSafeDistance(new[] { recentMax });
                if (distances[i] > maxSafe)
                {
                    distances[i] = Math.Round(maxSafe, 1);
                }
            }
        }
    }

    private static decimal GetPhaseVolumeMultiplier(TrainingPhase phase) => phase switch
    {
        TrainingPhase.Base => 0.65m,
        TrainingPhase.Build => 0.85m,
        TrainingPhase.Peak => 1.0m,
        TrainingPhase.Taper => 1.0m,
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
        int totalWeeks,
        VolumeMode volumeMode,
        AgeGroup ageGroup,
        TimeSpan warmUp,
        TransitionTimePreset transitionPreset,
        UserProfile profile)
    {
        var workouts = new List<ScheduledWorkout>();
        // Task 2.2: Use VolumeMode for max run days
        var maxRunDays = volumeMode.MaxRunDaysPerWeek();
        var runDays = Math.Min(availableDays.Length, maxRunDays);

        // Create a workout for each day of the week
        for (int d = 0; d < 7; d++)
        {
            var dayOfWeek = (DayOfWeek)d;
            var date = mondayOfWeek.AddDays(((int)dayOfWeek - (int)DayOfWeek.Monday + 7) % 7);

            workouts.Add(new ScheduledWorkout
            {
                Date = date,
                Type = WorkoutType.Rest
            });
        }

        if (runDays == 0) return workouts;

        // Assign long run to preferred day or last available day
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

        // Task 3.3: Age-adjusted recovery — enforce min recovery days between hard sessions
        var minRecoveryDays = ageGroup.GetMinRecoveryDays();

        // Assign quality workout if in Build/Peak and experience allows
        if (remainingRunDays > 0 && phase is TrainingPhase.Build or TrainingPhase.Peak
            && (level.AllowIntervalsFromStart() || weekNum > 4))
        {
            var qualityDay = PickQualityDay(otherAvailableDays, longRunDay, minRecoveryDays);
            if (qualityDay.HasValue)
            {
                var qualityType = phase == TrainingPhase.Peak ? WorkoutType.Tempo : PickQualityWorkoutType(weekNum);
                var qualityDistance = Math.Round(remainingDistance * 0.35m, 1);
                qualityDistance = Math.Max(qualityDistance, MinRunDistance);

                var qualityWorkout = workouts.First(w => w.DayOfWeek == qualityDay.Value);
                qualityWorkout.Type = qualityType;
                qualityWorkout.TargetDistanceMiles = qualityDistance;
                qualityWorkout.PaceZone = PaceZone.ForWorkoutType(qualityType);
                // Task 3.4: Age-adjusted warm-up duration
                qualityWorkout.WarmUpDuration = warmUp;
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

        // Task 2.3: Elite mode — add PM easy run on the busiest day
        if (volumeMode.SupportsDoubles() && phase is TrainingPhase.Build or TrainingPhase.Peak)
        {
            var busiestRunDay = workouts
                .Where(w => w.IsRunWorkout && w.Type != WorkoutType.LongRun)
                .OrderByDescending(w => w.TargetDistanceMiles)
                .FirstOrDefault();

            if (busiestRunDay != null)
            {
                // Add a second easy run on the same day (PM session) by splitting distance
                var pmDistance = Math.Round(busiestRunDay.TargetDistanceMiles * 0.4m, 1);
                pmDistance = Math.Max(pmDistance, MinRunDistance);
                busiestRunDay.TargetDistanceMiles = Math.Round(busiestRunDay.TargetDistanceMiles * 0.6m, 1);

                // Add PM workout as a recovery run on the same date
                var pmWorkout = new ScheduledWorkout
                {
                    Date = busiestRunDay.Date,
                    Type = WorkoutType.Recovery,
                    TargetDistanceMiles = pmDistance,
                    PaceZone = PaceZone.ForWorkoutType(WorkoutType.Recovery)
                };
                workouts.Add(pmWorkout);
            }
        }

        // Task 4.1/4.2/4.3: Apply run/walk intervals for beginner Easy/LongRun workouts
        if (level == ExperienceLevel.Beginner)
        {
            ApplyRunWalk(workouts, profile, phase);
        }

        // Task 10.3: Apply transition time — estimate workout durations and cap distances
        // if transition time significantly reduces available time
        if (transitionPreset != TransitionTimePreset.None)
        {
            ApplyTransitionTime(workouts, transitionPreset);
        }

        return workouts;
    }

    /// <summary>
    /// Task 4.1-4.3: Set run/walk intervals on beginner Easy and LongRun workouts.
    /// Default ratio 4:1, custom from profile if set. Progresses by phase.
    /// </summary>
    private static void ApplyRunWalk(List<ScheduledWorkout> workouts, UserProfile profile, TrainingPhase phase)
    {
        // Task 4.2: Default 4:1, read custom from profile
        var (runMin, walkMin) = GetRunWalkRatio(profile, phase);

        foreach (var workout in workouts.Where(w => w.Type is WorkoutType.Easy or WorkoutType.LongRun))
        {
            workout.IsRunWalk = true;
            workout.RunMinutes = runMin;
            workout.WalkMinutes = walkMin;
        }
    }

    /// <summary>
    /// Task 4.3: Run/walk progression by phase. Base→4:1, Build→6:1, Peak→8:1.
    /// </summary>
    private static (int runMin, int walkMin) GetRunWalkRatio(UserProfile profile, TrainingPhase phase)
    {
        // Use custom ratio from profile if set
        if (profile.RunWalkRunMinutes.HasValue && profile.RunWalkWalkMinutes.HasValue)
            return (profile.RunWalkRunMinutes.Value, profile.RunWalkWalkMinutes.Value);

        return phase switch
        {
            TrainingPhase.Base => (4, 1),
            TrainingPhase.Build => (6, 1),
            TrainingPhase.Peak => (8, 1),
            TrainingPhase.Taper => (8, 1),
            _ => (4, 1)
        };
    }

    /// <summary>
    /// Task 10.3: Set TargetDuration on workouts factoring in transition time.
    /// Estimates duration from distance at ~10 min/mile pace plus warm-up/cool-down,
    /// then adds transition overhead so the user knows total time commitment.
    /// </summary>
    private static void ApplyTransitionTime(List<ScheduledWorkout> workouts, TransitionTimePreset preset)
    {
        var transitionMinutes = preset.Minutes();

        foreach (var workout in workouts.Where(w => w.IsRunWorkout))
        {
            // Estimate running time: ~10 min/mile for easy, ~9 min/mile for quality
            var paceMinPerMile = workout.IsQualityWorkout ? 9m : 10m;
            var runMinutes = workout.TargetDistanceMiles * paceMinPerMile;
            var warmUpMin = workout.WarmUpDuration?.TotalMinutes ?? 0;
            var coolDownMin = workout.CoolDownDuration?.TotalMinutes ?? 0;

            // Total workout time including transition
            var totalMinutes = (int)(runMinutes + (decimal)warmUpMin + (decimal)coolDownMin + transitionMinutes);
            workout.TargetDuration = TimeSpan.FromMinutes(totalMinutes);
        }
    }

    /// <summary>
    /// Task 3.3: Pick quality day respecting minimum recovery days from age group.
    /// </summary>
    private static DayOfWeek? PickQualityDay(List<DayOfWeek> availableDays, DayOfWeek longRunDay, int minRecoveryDays)
    {
        foreach (var day in availableDays)
        {
            var gap = Math.Abs((int)day - (int)longRunDay);
            if (gap == 0) gap = 7;
            if (gap >= minRecoveryDays + 1) return day;
        }
        // Fallback: use original 2-day gap logic
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
