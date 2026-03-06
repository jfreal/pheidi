using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pheidi.Common.Models;

namespace Pheidi.Common.Tests.Models;

[TestClass]
public class TrainingWeekTests
{
    [TestMethod]
    public void TotalPlannedDistanceSumsAllWorkouts()
    {
        var week = new TrainingWeek
        {
            WeekNumber = 1,
            Phase = TrainingPhase.Base,
            Workouts =
            [
                new ScheduledWorkout { Type = WorkoutType.Easy, TargetDistanceMiles = 3 },
                new ScheduledWorkout { Type = WorkoutType.Tempo, TargetDistanceMiles = 5 },
                new ScheduledWorkout { Type = WorkoutType.Rest, TargetDistanceMiles = 0 },
                new ScheduledWorkout { Type = WorkoutType.Easy, TargetDistanceMiles = 4 },
                new ScheduledWorkout { Type = WorkoutType.Rest, TargetDistanceMiles = 0 },
                new ScheduledWorkout { Type = WorkoutType.LongRun, TargetDistanceMiles = 12 },
                new ScheduledWorkout { Type = WorkoutType.Rest, TargetDistanceMiles = 0 }
            ]
        };

        Assert.AreEqual(24m, week.TotalPlannedDistance);
    }

    [TestMethod]
    public void LongRunDistanceReturnsLongRunWorkout()
    {
        var week = new TrainingWeek
        {
            WeekNumber = 1,
            Phase = TrainingPhase.Build,
            Workouts =
            [
                new ScheduledWorkout { Type = WorkoutType.Easy, TargetDistanceMiles = 3 },
                new ScheduledWorkout { Type = WorkoutType.LongRun, TargetDistanceMiles = 14 }
            ]
        };

        Assert.AreEqual(14m, week.LongRunDistance);
    }

    [TestMethod]
    public void RunDayCountExcludesRestAndCrossTraining()
    {
        var week = new TrainingWeek
        {
            WeekNumber = 1,
            Phase = TrainingPhase.Base,
            Workouts =
            [
                new ScheduledWorkout { Type = WorkoutType.Easy },
                new ScheduledWorkout { Type = WorkoutType.Rest },
                new ScheduledWorkout { Type = WorkoutType.CrossTraining },
                new ScheduledWorkout { Type = WorkoutType.LongRun },
                new ScheduledWorkout { Type = WorkoutType.Strength },
                new ScheduledWorkout { Type = WorkoutType.Tempo },
                new ScheduledWorkout { Type = WorkoutType.Rest }
            ]
        };

        Assert.AreEqual(3, week.RunDayCount);
    }
}
