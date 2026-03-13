using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pheidi.Common.Engines;

namespace Pheidi.Common.Tests.Engines;

[TestClass]
public class AdaptiveProgressionCalculatorTests
{
    [TestMethod]
    public void LowVolumeReturns15Percent()
    {
        Assert.AreEqual(0.15m, AdaptiveProgressionCalculator.GetIncreaseRate(20m));
        Assert.AreEqual(0.15m, AdaptiveProgressionCalculator.GetIncreaseRate(30m));
    }

    [TestMethod]
    public void HighVolumeReturns7Percent()
    {
        Assert.AreEqual(0.07m, AdaptiveProgressionCalculator.GetIncreaseRate(50m));
        Assert.AreEqual(0.07m, AdaptiveProgressionCalculator.GetIncreaseRate(80m));
    }

    [TestMethod]
    public void MidVolumeInterpolates()
    {
        // At 40mi: 0.15 - (40-30)/20 * 0.08 = 0.15 - 0.04 = 0.11
        var rate = AdaptiveProgressionCalculator.GetIncreaseRate(40m);
        Assert.AreEqual(0.11m, rate);
    }

    [TestMethod]
    public void InterpolationIsLinear()
    {
        var rate35 = AdaptiveProgressionCalculator.GetIncreaseRate(35m);
        var rate40 = AdaptiveProgressionCalculator.GetIncreaseRate(40m);
        var rate45 = AdaptiveProgressionCalculator.GetIncreaseRate(45m);

        // Rate should decrease monotonically
        Assert.IsTrue(rate35 > rate40);
        Assert.IsTrue(rate40 > rate45);

        // Steps should be equal (linear)
        var step1 = rate35 - rate40;
        var step2 = rate40 - rate45;
        Assert.AreEqual(step1, step2, $"Steps should be equal: {step1} vs {step2}");
    }

    [TestMethod]
    public void ZeroVolumeReturns15Percent()
    {
        Assert.AreEqual(0.15m, AdaptiveProgressionCalculator.GetIncreaseRate(0m));
    }

    [TestMethod]
    public void BoundaryValues()
    {
        // Just inside interpolation range
        var rate31 = AdaptiveProgressionCalculator.GetIncreaseRate(31m);
        Assert.IsTrue(rate31 < 0.15m && rate31 > 0.07m);

        var rate49 = AdaptiveProgressionCalculator.GetIncreaseRate(49m);
        Assert.IsTrue(rate49 < 0.15m && rate49 > 0.07m);
    }
}
