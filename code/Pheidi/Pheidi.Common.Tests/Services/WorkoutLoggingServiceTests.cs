using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pheidi.Common.Models;
using Pheidi.Common.Services;

namespace Pheidi.Common.Tests.Services;

[TestClass]
public class WorkoutLoggingServiceTests
{
    private readonly WorkoutLoggingService _service = new();

    [TestMethod]
    public void QuickCompleteSetsStatusAndActualDistance()
    {
        var workout = new ScheduledWorkout { Type = WorkoutType.Easy, TargetDistanceMiles = 5 };

        _service.QuickComplete(workout);

        Assert.AreEqual(WorkoutStatus.Completed, workout.Status);
        Assert.AreEqual(5m, workout.ActualDistanceMiles);
    }

    [TestMethod]
    public void ManualEntryRecordsValues()
    {
        var workout = new ScheduledWorkout { Type = WorkoutType.Tempo, TargetDistanceMiles = 4 };

        _service.LogWorkout(workout, 4.2m, TimeSpan.FromMinutes(32), 7);

        Assert.AreEqual(WorkoutStatus.Completed, workout.Status);
        Assert.AreEqual(4.2m, workout.ActualDistanceMiles);
        Assert.AreEqual(TimeSpan.FromMinutes(32), workout.ActualDuration);
        Assert.AreEqual(7, workout.ActualEffort);
    }

    [TestMethod]
    public void ManualEntryAllowsPartialData()
    {
        var workout = new ScheduledWorkout { Type = WorkoutType.Easy };

        _service.LogWorkout(workout, 5m, null, null);

        Assert.AreEqual(WorkoutStatus.Completed, workout.Status);
        Assert.AreEqual(5m, workout.ActualDistanceMiles);
        Assert.IsNull(workout.ActualDuration);
        Assert.IsNull(workout.ActualEffort);
    }

    [TestMethod]
    public void RecordFeedbackStoresValue()
    {
        var workout = new ScheduledWorkout();

        _service.RecordFeedback(workout, WorkoutFeedback.TooHard);

        Assert.AreEqual(WorkoutFeedback.TooHard, workout.Feedback);
    }

    [TestMethod]
    public void SkipWorkoutSetsSkippedStatus()
    {
        var workout = new ScheduledWorkout();

        _service.SkipWorkout(workout);

        Assert.AreEqual(WorkoutStatus.Skipped, workout.Status);
    }

    [TestMethod]
    public void RescheduleTodayMovesToRestDay()
    {
        var monday = new DateTime(2026, 3, 9);
        var plan = new NewTrainingPlan
        {
            Weeks =
            [
                new TrainingWeek
                {
                    WeekNumber = 1,
                    Workouts =
                    [
                        new ScheduledWorkout { Date = monday, Type = WorkoutType.Tempo, TargetDistanceMiles = 5 },
                        new ScheduledWorkout { Date = monday.AddDays(1), Type = WorkoutType.Rest }
                    ]
                }
            ]
        };

        var result = _service.RescheduleToday(plan, plan.Weeks[0].Workouts[0]);

        Assert.IsTrue(result);
        Assert.AreEqual(WorkoutType.Rest, plan.Weeks[0].Workouts[0].Type);
        Assert.AreEqual(WorkoutType.Tempo, plan.Weeks[0].Workouts[1].Type);
    }

    [TestMethod]
    public void RescheduleWithNoAvailableDaySkips()
    {
        var monday = new DateTime(2026, 3, 9);
        var plan = new NewTrainingPlan
        {
            Weeks =
            [
                new TrainingWeek
                {
                    WeekNumber = 1,
                    Workouts =
                    [
                        new ScheduledWorkout { Date = monday, Type = WorkoutType.Tempo, TargetDistanceMiles = 5 },
                        new ScheduledWorkout { Date = monday.AddDays(1), Type = WorkoutType.Easy, TargetDistanceMiles = 3 }
                    ]
                }
            ]
        };

        var result = _service.RescheduleToday(plan, plan.Weeks[0].Workouts[0]);

        Assert.IsFalse(result);
        Assert.AreEqual(WorkoutStatus.Skipped, plan.Weeks[0].Workouts[0].Status);
    }

    [TestMethod]
    public void CompletionMessageAt90PercentIsOutstanding()
    {
        var msg = WorkoutLoggingService.GetCompletionMessage(92, 20);
        Assert.IsTrue(msg.Contains("Outstanding") || msg.Contains("crushing"));
    }

    [TestMethod]
    public void MissedWorkoutMessageIsPositive()
    {
        var msg = WorkoutLoggingService.GetMissedWorkoutMessage();
        Assert.IsTrue(msg.Contains("No worries"));
        Assert.IsFalse(msg.Contains("failed") || msg.Contains("missed") || msg.Contains("bad"));
    }
}
