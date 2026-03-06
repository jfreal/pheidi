using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pheidi.Common.Engines;
using Pheidi.Common.Models;

namespace Pheidi.Common.Tests.Engines;

[TestClass]
public class ScheduleFlexibilityEngineTests
{
    private readonly ScheduleFlexibilityEngine _engine = new();

    private static TrainingWeek CreateTestWeek()
    {
        var monday = new DateTime(2026, 3, 9); // Monday
        return new TrainingWeek
        {
            WeekNumber = 1,
            Phase = TrainingPhase.Build,
            Workouts =
            [
                new ScheduledWorkout { Date = monday, Type = WorkoutType.Rest },
                new ScheduledWorkout { Date = monday.AddDays(1), Type = WorkoutType.Easy, TargetDistanceMiles = 4 },
                new ScheduledWorkout { Date = monday.AddDays(2), Type = WorkoutType.Tempo, TargetDistanceMiles = 5, WarmUpDuration = TimeSpan.FromMinutes(10), CoolDownDuration = TimeSpan.FromMinutes(10) },
                new ScheduledWorkout { Date = monday.AddDays(3), Type = WorkoutType.Easy, TargetDistanceMiles = 3 },
                new ScheduledWorkout { Date = monday.AddDays(4), Type = WorkoutType.Rest },
                new ScheduledWorkout { Date = monday.AddDays(5), Type = WorkoutType.LongRun, TargetDistanceMiles = 12 },
                new ScheduledWorkout { Date = monday.AddDays(6), Type = WorkoutType.Rest }
            ]
        };
    }

    [TestMethod]
    public void BlockDayRedistributesWorkoutToRestDay()
    {
        var week = CreateTestWeek();
        var plan = new NewTrainingPlan { Weeks = [week] };
        var wednesday = new DateTime(2026, 3, 11); // Tempo day

        var result = _engine.BlockDay(plan, wednesday);

        Assert.IsTrue(result);
        // Wednesday should now be rest
        var wed = week.Workouts.First(w => w.Date.Date == wednesday.Date);
        Assert.AreEqual(WorkoutType.Rest, wed.Type);
    }

    [TestMethod]
    public void BlockRestDayIsNoOp()
    {
        var week = CreateTestWeek();
        var plan = new NewTrainingPlan { Weeks = [week] };
        var monday = new DateTime(2026, 3, 9); // Already rest

        var result = _engine.BlockDay(plan, monday);

        Assert.IsTrue(result);
        Assert.AreEqual(WorkoutType.Rest, week.Workouts[0].Type);
    }

    [TestMethod]
    public void SwapWorkoutsExchangesTypes()
    {
        var a = new ScheduledWorkout { Date = DateTime.Today, Type = WorkoutType.Easy, TargetDistanceMiles = 4 };
        var b = new ScheduledWorkout { Date = DateTime.Today.AddDays(1), Type = WorkoutType.Rest, TargetDistanceMiles = 0 };

        ScheduleFlexibilityEngine.SwapWorkouts(a, b);

        Assert.AreEqual(WorkoutType.Rest, a.Type);
        Assert.AreEqual(WorkoutType.Easy, b.Type);
        Assert.AreEqual(0m, a.TargetDistanceMiles);
        Assert.AreEqual(4m, b.TargetDistanceMiles);
    }

    [TestMethod]
    public void ConsecutiveHardDaysDetected()
    {
        var a = new ScheduledWorkout { Date = DateTime.Today, Type = WorkoutType.Tempo };
        var b = new ScheduledWorkout { Date = DateTime.Today.AddDays(1), Type = WorkoutType.LongRun };

        Assert.IsTrue(ScheduleFlexibilityEngine.AreConsecutiveHardDays(a, b));
    }

    [TestMethod]
    public void NonConsecutiveHardDaysNotFlagged()
    {
        var a = new ScheduledWorkout { Date = DateTime.Today, Type = WorkoutType.Tempo };
        var b = new ScheduledWorkout { Date = DateTime.Today.AddDays(3), Type = WorkoutType.LongRun };

        Assert.IsFalse(ScheduleFlexibilityEngine.AreConsecutiveHardDays(a, b));
    }

    [TestMethod]
    public void EasyAndHardNotConsecutiveIssue()
    {
        var a = new ScheduledWorkout { Date = DateTime.Today, Type = WorkoutType.Easy };
        var b = new ScheduledWorkout { Date = DateTime.Today.AddDays(1), Type = WorkoutType.LongRun };

        Assert.IsFalse(ScheduleFlexibilityEngine.AreConsecutiveHardDays(a, b));
    }

    [TestMethod]
    public void TaperWeekCannotAddVolume()
    {
        var week = new TrainingWeek { Phase = TrainingPhase.Taper };
        Assert.IsFalse(ScheduleFlexibilityEngine.CanAddVolume(week));
    }

    [TestMethod]
    public void BuildWeekCanAddVolume()
    {
        var week = new TrainingWeek { Phase = TrainingPhase.Build };
        Assert.IsTrue(ScheduleFlexibilityEngine.CanAddVolume(week));
    }

    [TestMethod]
    public void VacationRequiresPaidUser()
    {
        var plan = new NewTrainingPlan();
        var result = _engine.HandleVacation(plan, DateTime.Today, DateTime.Today.AddDays(7), isPaidUser: false, fullRest: true);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VacationFullRestSetsAllToRest()
    {
        var week = CreateTestWeek();
        var plan = new NewTrainingPlan { Weeks = [week] };
        var start = new DateTime(2026, 3, 9);
        var end = new DateTime(2026, 3, 15);

        var result = _engine.HandleVacation(plan, start, end, isPaidUser: true, fullRest: true);

        Assert.IsTrue(result);
        Assert.IsTrue(week.Workouts.All(w => w.Type == WorkoutType.Rest));
    }
}
