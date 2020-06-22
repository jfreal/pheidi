using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pheidi.Common.Tests
{
    [TestClass]
    public class TrainingPlanTests
    {
        [TestMethod]
        public void TheBasicPlanHas2RunsOver16Miles()
        {
            var trainingPlan = new TrainingPlan();

            trainingPlan.Generate();

            Assert.AreEqual(trainingPlan.PlanMetrics.RunsOver16, 3);
        }
    }
}
