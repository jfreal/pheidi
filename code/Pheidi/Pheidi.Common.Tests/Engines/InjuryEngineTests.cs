using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pheidi.Common.Engines;
using Pheidi.Common.Models;

namespace Pheidi.Common.Tests.Engines;

[TestClass]
public class InjuryEngineTests
{
    [TestMethod]
    public void MildPainReducesDistanceBy20Percent()
    {
        var workout = new ScheduledWorkout { Type = WorkoutType.Easy, TargetDistanceMiles = 5 };

        InjuryEngine.ModifyWorkout(workout, 3);

        Assert.AreEqual(4.0m, workout.TargetDistanceMiles);
        Assert.AreEqual(WorkoutType.Easy, workout.Type);
    }

    [TestMethod]
    public void MildPainConvertsQualityToEasy()
    {
        var workout = new ScheduledWorkout { Type = WorkoutType.Tempo, TargetDistanceMiles = 5 };

        InjuryEngine.ModifyWorkout(workout, 3);

        Assert.AreEqual(WorkoutType.Easy, workout.Type);
    }

    [TestMethod]
    public void ModeratePainConvertsToEasyAt50Percent()
    {
        var workout = new ScheduledWorkout { Type = WorkoutType.Tempo, TargetDistanceMiles = 6 };

        InjuryEngine.ModifyWorkout(workout, 5);

        Assert.AreEqual(WorkoutType.Easy, workout.Type);
        Assert.AreEqual(3.0m, workout.TargetDistanceMiles);
    }

    [TestMethod]
    public void SeverePainConvertsToRest()
    {
        var workout = new ScheduledWorkout { Type = WorkoutType.LongRun, TargetDistanceMiles = 16 };

        InjuryEngine.ModifyWorkout(workout, 8);

        Assert.AreEqual(WorkoutType.Rest, workout.Type);
        Assert.AreEqual(0m, workout.TargetDistanceMiles);
    }

    [TestMethod]
    public void ReturnProgressionHasFourWeeks()
    {
        var progression = InjuryEngine.GetReturnProgression();

        Assert.AreEqual(4, progression.Length);
        Assert.AreEqual(0.50m, progression[0]);
        Assert.AreEqual(0.70m, progression[1]);
        Assert.AreEqual(0.85m, progression[2]);
        Assert.AreEqual(1.00m, progression[3]);
    }

    [TestMethod]
    public void MedicalClearanceRecommendedForSeverity7Plus()
    {
        var injury = new InjuryReport { Severity = 7 };

        Assert.IsTrue(InjuryEngine.ShouldRecommendMedicalClearance(injury));
    }

    [TestMethod]
    public void MedicalClearanceNotRecommendedForMild()
    {
        var injury = new InjuryReport { Severity = 3, ReportDate = DateTime.UtcNow };

        Assert.IsFalse(InjuryEngine.ShouldRecommendMedicalClearance(injury));
    }

    [TestMethod]
    public void MedicalClearanceRecommendedAfter14Days()
    {
        var injury = new InjuryReport { Severity = 4, ReportDate = DateTime.UtcNow.AddDays(-15) };

        Assert.IsTrue(InjuryEngine.ShouldRecommendMedicalClearance(injury));
    }

    [TestMethod]
    public void GuidanceReturnsDifferentMessagesPerSeverity()
    {
        var mild = InjuryEngine.GetGuidance(2);
        var moderate = InjuryEngine.GetGuidance(5);
        var severe = InjuryEngine.GetGuidance(8);

        Assert.AreNotEqual(mild, moderate);
        Assert.AreNotEqual(moderate, severe);
    }
}
