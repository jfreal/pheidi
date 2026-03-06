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
            AvailableDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                             DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday]
        };

        var plan = _engine.Generate(goal, profile);

        foreach (var week in plan.Weeks)
        {
            Assert.IsTrue(week.RunDayCount <= 4,
                $"Week {week.WeekNumber}: Beginner should have max 4 run days, got {week.RunDayCount}");
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
}
