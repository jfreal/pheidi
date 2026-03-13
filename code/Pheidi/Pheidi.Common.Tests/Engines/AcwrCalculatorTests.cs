using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pheidi.Common.Engines;
using Pheidi.Common.Models;

namespace Pheidi.Common.Tests.Engines;

[TestClass]
public class AcwrCalculatorTests
{
    private static ScheduledWorkout MakeCompleted(DateTime date, decimal miles) => new()
    {
        Date = date,
        Status = WorkoutStatus.Completed,
        Type = WorkoutType.Easy,
        ActualDistanceMiles = miles
    };

    [TestMethod]
    public void ReturnsNullWhenNoWorkouts()
    {
        var result = AcwrCalculator.Calculate([]);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ReturnsNullWhenInsufficientData()
    {
        var today = DateTime.Today;
        var workouts = new List<ScheduledWorkout>
        {
            MakeCompleted(today.AddDays(-5), 5),
            MakeCompleted(today.AddDays(-3), 6),
            MakeCompleted(today.AddDays(-1), 4)
        };

        var result = AcwrCalculator.Calculate(workouts);
        Assert.IsNull(result, "Should return null with <21 days of data span");
    }

    [TestMethod]
    public void CalculatesAcwrWithSufficientData()
    {
        var today = DateTime.Today;
        var workouts = new List<ScheduledWorkout>();

        // 4 weeks of consistent 5-mile runs, 3x/week
        for (int i = 0; i < 28; i += 2)
        {
            workouts.Add(MakeCompleted(today.AddDays(-i), 5));
        }

        var result = AcwrCalculator.Calculate(workouts);
        Assert.IsNotNull(result);
        Assert.IsTrue(result > 0, "ACWR should be positive");
    }

    [TestMethod]
    public void HighAcuteLoadProducesHighRatio()
    {
        var today = DateTime.Today;
        var workouts = new List<ScheduledWorkout>();

        // Low chronic load: 3mi runs every 3 days for first 3 weeks
        for (int i = 28; i > 7; i -= 3)
        {
            workouts.Add(MakeCompleted(today.AddDays(-i), 3));
        }

        // High acute load: 10mi runs every day for past week
        for (int i = 0; i < 7; i++)
        {
            workouts.Add(MakeCompleted(today.AddDays(-i), 10));
        }

        var result = AcwrCalculator.Calculate(workouts);
        Assert.IsNotNull(result);
        Assert.IsTrue(result > 1.3m, $"ACWR {result} should be elevated with high recent load");
    }

    [TestMethod]
    public void ReturnsNullWhenChronicLoadIsZero()
    {
        // Only non-completed workouts with zero actual distance
        var result = AcwrCalculator.Calculate([]);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void IgnoresNonCompletedWorkouts()
    {
        var today = DateTime.Today;
        var workouts = new List<ScheduledWorkout>
        {
            new() { Date = today.AddDays(-25), Status = WorkoutStatus.Pending, Type = WorkoutType.Easy, ActualDistanceMiles = 10 },
            new() { Date = today.AddDays(-20), Status = WorkoutStatus.Skipped, Type = WorkoutType.Easy, ActualDistanceMiles = 10 }
        };

        var result = AcwrCalculator.Calculate(workouts);
        Assert.IsNull(result);
    }

    // --- ClassifyRisk ---

    [TestMethod]
    [DataRow(0.5, AcwrRiskZone.UnderTraining)]
    [DataRow(0.79, AcwrRiskZone.UnderTraining)]
    [DataRow(0.8, AcwrRiskZone.Green)]
    [DataRow(1.0, AcwrRiskZone.Green)]
    [DataRow(1.3, AcwrRiskZone.Green)]
    [DataRow(1.31, AcwrRiskZone.Yellow)]
    [DataRow(1.5, AcwrRiskZone.Yellow)]
    [DataRow(1.51, AcwrRiskZone.Red)]
    [DataRow(2.0, AcwrRiskZone.Red)]
    public void ClassifyRiskReturnsCorrectZone(double acwr, AcwrRiskZone expected)
    {
        Assert.AreEqual(expected, AcwrCalculator.ClassifyRisk((decimal)acwr));
    }

    [TestMethod]
    public void GetRiskMessageIsNotEmpty()
    {
        foreach (var zone in Enum.GetValues<AcwrRiskZone>())
        {
            var message = AcwrCalculator.GetRiskMessage(zone);
            Assert.IsFalse(string.IsNullOrEmpty(message), $"Risk message for {zone} should not be empty");
        }
    }

    [TestMethod]
    public void GetRiskColorIsNotEmpty()
    {
        foreach (var zone in Enum.GetValues<AcwrRiskZone>())
        {
            var color = AcwrCalculator.GetRiskColor(zone);
            Assert.IsTrue(color.StartsWith("#"), $"Risk color for {zone} should be a hex color");
        }
    }
}
