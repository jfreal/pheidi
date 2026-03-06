using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pheidi.Common.Models;

namespace Pheidi.Common.Tests.Models;

[TestClass]
public class ScheduledWorkoutTests
{
    [TestMethod]
    public void TempoIsQualityWorkout()
    {
        var workout = new ScheduledWorkout { Type = WorkoutType.Tempo };
        Assert.IsTrue(workout.IsQualityWorkout);
    }

    [TestMethod]
    public void EasyRunIsNotQualityWorkout()
    {
        var workout = new ScheduledWorkout { Type = WorkoutType.Easy };
        Assert.IsFalse(workout.IsQualityWorkout);
    }

    [TestMethod]
    public void RestIsNotRunWorkout()
    {
        var workout = new ScheduledWorkout { Type = WorkoutType.Rest };
        Assert.IsFalse(workout.IsRunWorkout);
    }

    [TestMethod]
    public void EasyRunIsRunWorkout()
    {
        var workout = new ScheduledWorkout { Type = WorkoutType.Easy };
        Assert.IsTrue(workout.IsRunWorkout);
    }

    [TestMethod]
    public void DescriptionFormatsCorrectly()
    {
        var workout = new ScheduledWorkout { Type = WorkoutType.Tempo, TargetDistanceMiles = 4.0m };
        Assert.AreEqual("Tempo Run — 4.0 mi", workout.Description);
    }

    [TestMethod]
    public void RestDayDescriptionIsSimple()
    {
        var workout = new ScheduledWorkout { Type = WorkoutType.Rest };
        Assert.AreEqual("Rest Day", workout.Description);
    }
}
