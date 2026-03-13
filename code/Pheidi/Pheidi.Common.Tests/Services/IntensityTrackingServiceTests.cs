using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pheidi.Common.Models;
using Pheidi.Common.Services;

namespace Pheidi.Common.Tests.Services;

[TestClass]
public class IntensityTrackingServiceTests
{
    private static ScheduledWorkout MakeCompleted(WorkoutType type, int daysAgo, IntensityZone? zone = null) => new()
    {
        Date = DateTime.Today.AddDays(-daysAgo),
        Status = WorkoutStatus.Completed,
        Type = type,
        ActualIntensityZone = zone
    };

    // --- GetDistribution ---

    [TestMethod]
    public void EmptyListReturnsZeros()
    {
        var (easy, threshold, hard) = IntensityTrackingService.GetDistribution([]);
        Assert.AreEqual(0m, easy);
        Assert.AreEqual(0m, threshold);
        Assert.AreEqual(0m, hard);
    }

    [TestMethod]
    public void AllEasyReturns100PercentEasy()
    {
        var workouts = new List<ScheduledWorkout>
        {
            MakeCompleted(WorkoutType.Easy, 1),
            MakeCompleted(WorkoutType.Easy, 3),
            MakeCompleted(WorkoutType.Recovery, 5),
            MakeCompleted(WorkoutType.LongRun, 7)
        };

        var (easy, threshold, hard) = IntensityTrackingService.GetDistribution(workouts);
        Assert.AreEqual(100m, easy);
        Assert.AreEqual(0m, threshold);
        Assert.AreEqual(0m, hard);
    }

    [TestMethod]
    public void MixedDistributionCalculatesCorrectly()
    {
        var workouts = new List<ScheduledWorkout>
        {
            MakeCompleted(WorkoutType.Easy, 1),      // Easy
            MakeCompleted(WorkoutType.Easy, 2),      // Easy
            MakeCompleted(WorkoutType.Easy, 3),      // Easy
            MakeCompleted(WorkoutType.Easy, 4),      // Easy
            MakeCompleted(WorkoutType.Tempo, 5),     // Threshold
            MakeCompleted(WorkoutType.Intervals, 6), // Hard
            MakeCompleted(WorkoutType.Easy, 7),      // Easy
            MakeCompleted(WorkoutType.Easy, 8),      // Easy
            MakeCompleted(WorkoutType.Easy, 9),      // Easy
            MakeCompleted(WorkoutType.Easy, 10),     // Easy — 8 easy, 1 threshold, 1 hard
        };

        var (easy, threshold, hard) = IntensityTrackingService.GetDistribution(workouts);
        Assert.AreEqual(80m, easy);
        Assert.AreEqual(10m, threshold);
        Assert.AreEqual(10m, hard);
    }

    [TestMethod]
    public void IgnoresWorkoutsOlderThan28Days()
    {
        var workouts = new List<ScheduledWorkout>
        {
            MakeCompleted(WorkoutType.Intervals, 30), // Should be excluded
            MakeCompleted(WorkoutType.Easy, 1)
        };

        var (easy, _, hard) = IntensityTrackingService.GetDistribution(workouts);
        Assert.AreEqual(100m, easy);
        Assert.AreEqual(0m, hard);
    }

    [TestMethod]
    public void IgnoresRestAndCrossTraining()
    {
        var workouts = new List<ScheduledWorkout>
        {
            MakeCompleted(WorkoutType.Easy, 1),
            new() { Date = DateTime.Today.AddDays(-2), Status = WorkoutStatus.Completed, Type = WorkoutType.Rest },
            new() { Date = DateTime.Today.AddDays(-3), Status = WorkoutStatus.Completed, Type = WorkoutType.CrossTraining }
        };

        var (easy, _, _) = IntensityTrackingService.GetDistribution(workouts);
        Assert.AreEqual(100m, easy, "Only the Easy run should be counted");
    }

    [TestMethod]
    public void UsesActualIntensityZoneWhenSet()
    {
        // Easy workout type but logged as Hard effort
        var workouts = new List<ScheduledWorkout>
        {
            MakeCompleted(WorkoutType.Easy, 1, IntensityZone.Hard)
        };

        var (easy, _, hard) = IntensityTrackingService.GetDistribution(workouts);
        Assert.AreEqual(0m, easy);
        Assert.AreEqual(100m, hard);
    }

    // --- IsInGrayZone ---

    [TestMethod]
    public void NotInGrayZoneWhenThresholdBelow20()
    {
        var workouts = new List<ScheduledWorkout>
        {
            MakeCompleted(WorkoutType.Easy, 1),
            MakeCompleted(WorkoutType.Easy, 2),
            MakeCompleted(WorkoutType.Easy, 3),
            MakeCompleted(WorkoutType.Easy, 4),
            MakeCompleted(WorkoutType.Tempo, 5) // 20% — not above 20
        };

        Assert.IsFalse(IntensityTrackingService.IsInGrayZone(workouts));
    }

    [TestMethod]
    public void InGrayZoneWhenThresholdAbove20()
    {
        var workouts = new List<ScheduledWorkout>
        {
            MakeCompleted(WorkoutType.Easy, 1),
            MakeCompleted(WorkoutType.Easy, 2),
            MakeCompleted(WorkoutType.Tempo, 3),
            MakeCompleted(WorkoutType.Tempo, 4) // 50% threshold
        };

        Assert.IsTrue(IntensityTrackingService.IsInGrayZone(workouts));
    }

    // --- MapEffortToZone ---

    [TestMethod]
    [DataRow(null, WorkoutType.Easy, IntensityZone.Easy)]
    [DataRow(null, WorkoutType.Tempo, IntensityZone.Threshold)]
    [DataRow(null, WorkoutType.Intervals, IntensityZone.Hard)]
    [DataRow(3, WorkoutType.Intervals, IntensityZone.Easy)]
    [DataRow(5, WorkoutType.Easy, IntensityZone.Threshold)]
    [DataRow(8, WorkoutType.Easy, IntensityZone.Hard)]
    public void MapEffortToZoneReturnsCorrectZone(int? effort, WorkoutType type, IntensityZone expected)
    {
        Assert.AreEqual(expected, IntensityTrackingService.MapEffortToZone(effort, type));
    }
}
