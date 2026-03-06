using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pheidi.Common.Engines;
using Pheidi.Common.Models;
using Pheidi.Common.Services;

namespace Pheidi.Common.Tests.Services;

[TestClass]
public class CoachingRetentionTests
{
    [TestMethod]
    public void StreakOf7ReturnsConsistencyMessage()
    {
        var msg = MessagingService.GetStreakMessage(7);
        Assert.IsTrue(msg.Contains("7 in a row"));
    }

    [TestMethod]
    public void MissedWorkoutMessageIsPositive()
    {
        var msg = MessagingService.GetMissedWorkoutMessage();
        Assert.IsTrue(msg.Contains("No worries"));
    }

    [TestMethod]
    public void PhaseTransitionMessageForTaper()
    {
        var msg = MessagingService.GetPhaseTransitionMessage(TrainingPhase.Taper);
        Assert.IsTrue(msg.Contains("Taper") || msg.Contains("taper"));
    }

    [TestMethod]
    public void RacePredictionReturnsNullWithInsufficientData()
    {
        var plan = new NewTrainingPlan
        {
            RaceGoal = new RaceGoal { Distance = RaceDistance.FullMarathon },
            Weeks = [new TrainingWeek { WeekNumber = 1 }]
        };

        var service = new RacePredictionService();
        var result = service.PredictFinishTime(plan);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void RacePredictionMessageIndicatesDataNeeded()
    {
        var plan = new NewTrainingPlan
        {
            RaceGoal = new RaceGoal { Distance = RaceDistance.HalfMarathon },
            Weeks = [new TrainingWeek { WeekNumber = 1 }]
        };

        var service = new RacePredictionService();
        var msg = service.GetPredictionMessage(plan);

        Assert.IsTrue(msg.Contains("Keep logging") || msg.Contains("Log workouts"));
    }

    [TestMethod]
    public void BaseBuildingShouldBeOfferedToBeginners()
    {
        Assert.IsTrue(BaseBuildingEngine.ShouldOfferBaseBuilding(ExperienceLevel.Beginner));
    }

    [TestMethod]
    public void BaseBuildingShouldNotBeOfferedToAdvanced()
    {
        Assert.IsFalse(BaseBuildingEngine.ShouldOfferBaseBuilding(ExperienceLevel.Advanced));
    }

    [TestMethod]
    public void BaseBuildingGenerates4Weeks()
    {
        var engine = new BaseBuildingEngine();
        var profile = new UserProfile
        {
            ExperienceLevel = ExperienceLevel.Beginner,
            AvailableDays = [DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday]
        };

        var weeks = engine.GenerateBaseBuildingPhase(profile, DateTime.Today, 4);

        Assert.AreEqual(4, weeks.Count);
    }

    [TestMethod]
    public void BaseBuildingWeeksHaveEasyRuns()
    {
        var engine = new BaseBuildingEngine();
        var profile = new UserProfile
        {
            ExperienceLevel = ExperienceLevel.Beginner,
            AvailableDays = [DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday]
        };

        var weeks = engine.GenerateBaseBuildingPhase(profile, DateTime.Today, 4);

        foreach (var week in weeks)
        {
            Assert.IsTrue(week.Workouts.Any(w => w.Type == WorkoutType.Easy),
                $"Week {week.WeekNumber} should have easy runs");
        }
    }
}
