using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pheidi.Common.Models;

namespace Pheidi.Common.Tests.Models;

[TestClass]
public class RaceDistanceTests
{
    [TestMethod]
    public void HalfMarathonToMilesReturns13Point1()
    {
        Assert.AreEqual(13.1m, RaceDistance.HalfMarathon.ToMiles());
    }

    [TestMethod]
    public void FullMarathonToKilometersReturns42Point2()
    {
        Assert.AreEqual(42.2m, RaceDistance.FullMarathon.ToKilometers());
    }

    [TestMethod]
    public void FiveKDefaultPlanWeeksReturns10()
    {
        Assert.AreEqual(10, RaceDistance.FiveK.DefaultPlanWeeks());
    }

    [TestMethod]
    public void FullMarathonDefaultPlanWeeksReturns18()
    {
        Assert.AreEqual(18, RaceDistance.FullMarathon.DefaultPlanWeeks());
    }

    [TestMethod]
    public void PlanWeekRangeForHalfMarathonIs12To16()
    {
        var (min, max) = RaceDistance.HalfMarathon.PlanWeekRange();
        Assert.AreEqual(12, min);
        Assert.AreEqual(16, max);
    }

    [TestMethod]
    public void PeakLongRunForMarathonIs22()
    {
        Assert.AreEqual(22m, RaceDistance.FullMarathon.PeakLongRunMiles());
    }

    [TestMethod]
    public void PeakLongRunFor5KIs10()
    {
        Assert.AreEqual(10m, RaceDistance.FiveK.PeakLongRunMiles());
    }
}
