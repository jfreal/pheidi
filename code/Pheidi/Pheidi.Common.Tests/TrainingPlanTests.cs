using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pheidi.Common.Tests;

[TestClass]
public class TrainingPlanTests
{
    [TestMethod]
    public void TheBasicPlanHas4RunsOver16Miles()
    {
        var trainingPlan = new TrainingPlan();

        trainingPlan.Generate();

        Assert.AreEqual(4, trainingPlan.PlanMetrics.RunsOver16);
    }
}
