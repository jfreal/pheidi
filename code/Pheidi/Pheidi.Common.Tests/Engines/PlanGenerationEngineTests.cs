using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pheidi.Common.Engines;
using Pheidi.Common.Models;

namespace Pheidi.Common.Tests.Engines;

[TestClass]
public class PlanGenerationEngineTests
{
    private readonly PlanGenerationEngine _engine = new();

    [TestMethod]
    public void PhaseAllocation16WeeksPlan()
    {
        var counts = PlanGenerationEngine.AllocatePhaseCounts(16);

        Assert.AreEqual(4, counts[TrainingPhase.Base]);
        Assert.AreEqual(7, counts[TrainingPhase.Build]); // 16 - 4(base) - 2(peak) - 3(taper)
        Assert.AreEqual(3, counts[TrainingPhase.Taper]);
        Assert.AreEqual(16, counts.Values.Sum());
    }

    [TestMethod]
    public void PhaseAllocation10WeeksPlan()
    {
        var counts = PlanGenerationEngine.AllocatePhaseCounts(10);

        Assert.AreEqual(10, counts.Values.Sum());
        Assert.IsTrue(counts[TrainingPhase.Base] >= 2);
        Assert.IsTrue(counts[TrainingPhase.Taper] >= 2);
        Assert.IsTrue(counts[TrainingPhase.Peak] >= 1);
    }

    [TestMethod]
    public void AllocatePhasesReturnsCorrectLength()
    {
        var phases = PlanGenerationEngine.AllocatePhases(18);
        Assert.AreEqual(18, phases.Length);
    }

    [TestMethod]
    public void PhasesAreInOrder()
    {
        var phases = PlanGenerationEngine.AllocatePhases(16);

        // First phase should be Base, last should be Taper
        Assert.AreEqual(TrainingPhase.Base, phases[0]);
        Assert.AreEqual(TrainingPhase.Taper, phases[^1]);

        // Phases should be in order (no going backward)
        for (int i = 1; i < phases.Length; i++)
        {
            Assert.IsTrue(phases[i] >= phases[i - 1],
                $"Phase at index {i} ({phases[i]}) should be >= phase at index {i - 1} ({phases[i - 1]})");
        }
    }

    [TestMethod]
    public void LinearProgressionAlwaysIncreases()
    {
        var phases = PlanGenerationEngine.AllocatePhases(12);
        var distances = PlanGenerationEngine.GenerateLongRunProgression(
            ProgressionPattern.Linear, 14m, phases, 12);

        // Non-taper distances should never decrease
        decimal prev = 0;
        for (int i = 0; i < distances.Length; i++)
        {
            if (phases[i] == TrainingPhase.Taper) continue;
            Assert.IsTrue(distances[i] >= prev,
                $"Week {i + 1}: {distances[i]} should be >= {prev}");
            prev = distances[i];
        }
    }

    [TestMethod]
    public void ThreeUpOneDownHasDropBackWeeks()
    {
        var phases = PlanGenerationEngine.AllocatePhases(16);
        var distances = PlanGenerationEngine.GenerateLongRunProgression(
            ProgressionPattern.ThreeUpOneDown, 20m, phases, 16);

        // There should be at least one drop-back (non-taper week where distance decreases)
        bool hasDropBack = false;
        for (int i = 1; i < distances.Length; i++)
        {
            if (phases[i] == TrainingPhase.Taper || phases[i - 1] == TrainingPhase.Taper) continue;
            if (distances[i] < distances[i - 1])
            {
                hasDropBack = true;
                break;
            }
        }
        Assert.IsTrue(hasDropBack, "3-up-1-down pattern should have at least one drop-back week");
    }

    [TestMethod]
    public void LongRunDoesNotExceedPeakCap()
    {
        var phases = PlanGenerationEngine.AllocatePhases(18);
        var peakDistance = RaceDistance.FullMarathon.PeakLongRunMiles(); // 22
        var distances = PlanGenerationEngine.GenerateLongRunProgression(
            ProgressionPattern.Linear, peakDistance, phases, 18);

        foreach (var d in distances)
        {
            Assert.IsTrue(d <= peakDistance,
                $"Long run distance {d} should not exceed peak cap {peakDistance}");
        }
    }

    [TestMethod]
    public void TaperWeeksReduceVolume()
    {
        var multiplier1 = PlanGenerationEngine.GetTaperMultiplier(
            TrainingPhase.Taper,
            PlanGenerationEngine.AllocatePhases(16),
            14); // First taper week

        Assert.AreEqual(0.75m, multiplier1);
    }

    [TestMethod]
    public void NonTaperWeekHasNoReduction()
    {
        var multiplier = PlanGenerationEngine.GetTaperMultiplier(
            TrainingPhase.Build,
            PlanGenerationEngine.AllocatePhases(16),
            8);

        Assert.AreEqual(1.0m, multiplier);
    }

    [TestMethod]
    public void GenerateMarathonPlanProducesCorrectWeekCount()
    {
        var goal = new RaceGoal
        {
            Distance = RaceDistance.FullMarathon,
            RaceDate = DateTime.Today.AddDays(18 * 7)
        };
        var profile = new UserProfile
        {
            ExperienceLevel = ExperienceLevel.Intermediate,
            AvailableDays = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Saturday]
        };

        var plan = _engine.Generate(goal, profile);

        Assert.AreEqual(18, plan.TotalWeeks);
    }

    [TestMethod]
    public void GeneratedPlanHasWorkoutsEveryWeek()
    {
        var goal = new RaceGoal
        {
            Distance = RaceDistance.HalfMarathon,
            RaceDate = DateTime.Today.AddDays(14 * 7)
        };
        var profile = new UserProfile
        {
            ExperienceLevel = ExperienceLevel.Beginner,
            AvailableDays = [DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday]
        };

        var plan = _engine.Generate(goal, profile);

        foreach (var week in plan.Weeks)
        {
            Assert.IsTrue(week.Workouts.Count == 7,
                $"Week {week.WeekNumber} should have 7 workout slots (including rest days)");
            Assert.IsTrue(week.RunDayCount > 0,
                $"Week {week.WeekNumber} should have at least 1 run day");
        }
    }

    [TestMethod]
    public void BeginnerPlanDoesNotExceedMaxRunDays()
    {
        var goal = new RaceGoal
        {
            Distance = RaceDistance.TenK,
            RaceDate = DateTime.Today.AddDays(12 * 7)
        };
        var profile = new UserProfile
        {
            ExperienceLevel = ExperienceLevel.Beginner,
            VolumeMode = VolumeMode.Moderate,
            AvailableDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                             DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday]
        };

        var plan = _engine.Generate(goal, profile);

        // VolumeMode.Moderate allows up to 5 run days per week
        foreach (var week in plan.Weeks)
        {
            Assert.IsTrue(week.RunDayCount <= 5,
                $"Week {week.WeekNumber}: Moderate volume should have max 5 run days, got {week.RunDayCount}");
        }
    }

    [TestMethod]
    public void QualityWorkoutsHaveWarmUpAndCoolDown()
    {
        var goal = new RaceGoal
        {
            Distance = RaceDistance.FullMarathon,
            RaceDate = DateTime.Today.AddDays(18 * 7)
        };
        var profile = new UserProfile
        {
            ExperienceLevel = ExperienceLevel.Advanced,
            AvailableDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday]
        };

        var plan = _engine.Generate(goal, profile);

        var qualityWorkouts = plan.Weeks
            .SelectMany(w => w.Workouts)
            .Where(w => w.IsQualityWorkout);

        foreach (var w in qualityWorkouts)
        {
            Assert.IsNotNull(w.WarmUpDuration,
                $"{w.Type} on {w.Date:d} should have warm-up");
            Assert.IsNotNull(w.CoolDownDuration,
                $"{w.Type} on {w.Date:d} should have cool-down");
        }
    }

    [TestMethod]
    public void PlanStatusIsActiveOnGeneration()
    {
        var goal = new RaceGoal
        {
            Distance = RaceDistance.FiveK,
            RaceDate = DateTime.Today.AddDays(10 * 7)
        };
        var profile = new UserProfile { ExperienceLevel = ExperienceLevel.Beginner };

        var plan = _engine.Generate(goal, profile);

        Assert.AreEqual(PlanStatus.Active, plan.Status);
    }

    // --- Tier 2 Integration Tests ---

    [TestMethod]
    public void BeginnerPlanHasRunWalkIntervals()
    {
        var goal = new RaceGoal
        {
            Distance = RaceDistance.TenK,
            RaceDate = DateTime.Today.AddDays(12 * 7)
        };
        var profile = new UserProfile { ExperienceLevel = ExperienceLevel.Beginner };

        var plan = _engine.Generate(goal, profile);

        var easyWorkouts = plan.Weeks.SelectMany(w => w.Workouts)
            .Where(w => w.Type is WorkoutType.Easy or WorkoutType.LongRun)
            .ToList();

        Assert.IsTrue(easyWorkouts.Count > 0, "Should have easy/long run workouts");
        Assert.IsTrue(easyWorkouts.All(w => w.IsRunWalk), "All easy/long run workouts should be run/walk for beginners");
        Assert.IsTrue(easyWorkouts.All(w => w.RunMinutes > 0 && w.WalkMinutes > 0), "Run and walk minutes should be set");
    }

    [TestMethod]
    public void BeginnerPlanEnforces50PercentIncreaseCap()
    {
        var goal = new RaceGoal
        {
            Distance = RaceDistance.HalfMarathon,
            RaceDate = DateTime.Today.AddDays(16 * 7)
        };
        var profile = new UserProfile { ExperienceLevel = ExperienceLevel.Beginner };

        var plan = _engine.Generate(goal, profile);

        var longRuns = plan.Weeks
            .Where(w => w.Phase != TrainingPhase.Taper)
            .Select(w => w.Workouts.Where(wo => wo.Type == WorkoutType.LongRun).Max(wo => wo.TargetDistanceMiles))
            .Where(d => d > 0)
            .ToList();

        for (int i = 1; i < longRuns.Count; i++)
        {
            if (longRuns[i - 1] > 0)
            {
                Assert.IsTrue(longRuns[i] <= longRuns[i - 1] * 1.51m, // tiny tolerance for rounding
                    $"Week {i + 1} long run ({longRuns[i]}) exceeds 150% of week {i} ({longRuns[i - 1]})");
            }
        }
    }

    [TestMethod]
    public void AgeAdjustedPlanHasExtendedWarmUps()
    {
        var goal = new RaceGoal
        {
            Distance = RaceDistance.HalfMarathon,
            RaceDate = DateTime.Today.AddDays(16 * 7)
        };
        var profile = new UserProfile
        {
            ExperienceLevel = ExperienceLevel.Intermediate,
            DateOfBirth = DateTime.Today.AddYears(-55) // Age 55 → Fifties bracket
        };

        var plan = _engine.Generate(goal, profile);

        var qualityWorkouts = plan.Weeks.SelectMany(w => w.Workouts)
            .Where(w => w.WarmUpDuration.HasValue)
            .ToList();

        if (qualityWorkouts.Count > 0)
        {
            Assert.IsTrue(qualityWorkouts.All(w => w.WarmUpDuration!.Value.TotalMinutes >= 12),
                "Fifties age group should have at least 12-minute warm-ups");
        }
    }

    [TestMethod]
    public void ElitePlanHasDoubleRunDays()
    {
        var goal = new RaceGoal
        {
            Distance = RaceDistance.FullMarathon,
            RaceDate = DateTime.Today.AddDays(18 * 7)
        };
        var profile = new UserProfile
        {
            ExperienceLevel = ExperienceLevel.Advanced,
            VolumeMode = VolumeMode.Elite,
            AvailableDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                             DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday]
        };

        var plan = _engine.Generate(goal, profile);

        // Elite mode should produce some days with double runs (> 7 run workouts in build/peak weeks)
        var buildPeakWeeks = plan.Weeks.Where(w => w.Phase is TrainingPhase.Build or TrainingPhase.Peak).ToList();
        var hasDoubles = buildPeakWeeks.Any(w => w.Workouts.Count(wo => wo.IsRunWorkout) > 7);

        Assert.IsTrue(hasDoubles || buildPeakWeeks.Count == 0,
            "Elite volume mode should produce double-run days in build/peak weeks");
    }

    [TestMethod]
    public void TransitionTimeSetsDuration()
    {
        var goal = new RaceGoal
        {
            Distance = RaceDistance.TenK,
            RaceDate = DateTime.Today.AddDays(12 * 7)
        };
        var profile = new UserProfile
        {
            ExperienceLevel = ExperienceLevel.Intermediate,
            TransitionTimePreset = TransitionTimePreset.GymShower // 25 min
        };

        var plan = _engine.Generate(goal, profile);

        var runWorkouts = plan.Weeks.SelectMany(w => w.Workouts)
            .Where(w => w.IsRunWorkout)
            .ToList();

        Assert.IsTrue(runWorkouts.Count > 0, "Should have run workouts");
        Assert.IsTrue(runWorkouts.All(w => w.TargetDuration.HasValue),
            "All run workouts should have TargetDuration set when transition time is configured");
        Assert.IsTrue(runWorkouts.All(w => w.TargetDuration!.Value.TotalMinutes >= 25),
            "All workout durations should include at least the transition time");
    }
}
