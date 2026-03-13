using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pheidi.Common.Engines;

namespace Pheidi.Common.Tests.Engines;

[TestClass]
public class SpikeGuardTests
{
    [TestMethod]
    public void EmptyHistoryReturnsMaxValue()
    {
        var result = SpikeGuard.GetMaxSafeDistance([]);
        Assert.AreEqual(decimal.MaxValue, result);
    }

    [TestMethod]
    public void SingleValueReturns110Percent()
    {
        var result = SpikeGuard.GetMaxSafeDistance([10m]);
        Assert.AreEqual(11.0m, result);
    }

    [TestMethod]
    public void UsesMaxFromArray()
    {
        var result = SpikeGuard.GetMaxSafeDistance([5m, 8m, 6m, 10m]);
        Assert.AreEqual(11.0m, result);
    }

    [TestMethod]
    public void IsSpikeReturnsFalseForEmptyHistory()
    {
        Assert.IsFalse(SpikeGuard.IsSpike(15m, []));
    }

    [TestMethod]
    public void IsSpikeReturnsFalseWhenWithinThreshold()
    {
        Assert.IsFalse(SpikeGuard.IsSpike(10.5m, [10m]));
    }

    [TestMethod]
    public void IsSpikeReturnsFalseAtExactThreshold()
    {
        Assert.IsFalse(SpikeGuard.IsSpike(11.0m, [10m]));
    }

    [TestMethod]
    public void IsSpikeReturnsTrueWhenExceedingThreshold()
    {
        Assert.IsTrue(SpikeGuard.IsSpike(11.1m, [10m]));
    }

    [TestMethod]
    public void IsSpikeUsesMaxFromHistory()
    {
        // Max is 12, so 110% = 13.2
        Assert.IsFalse(SpikeGuard.IsSpike(13.0m, [8m, 10m, 12m, 9m]));
        Assert.IsTrue(SpikeGuard.IsSpike(13.3m, [8m, 10m, 12m, 9m]));
    }

    [TestMethod]
    public void SmallDistancesWorkCorrectly()
    {
        var result = SpikeGuard.GetMaxSafeDistance([2m]);
        Assert.AreEqual(2.20m, result);
    }

    [TestMethod]
    public void ZeroDistanceInHistory()
    {
        var result = SpikeGuard.GetMaxSafeDistance([0m]);
        Assert.AreEqual(0m, result);
        Assert.IsTrue(SpikeGuard.IsSpike(0.1m, [0m]));
    }
}
