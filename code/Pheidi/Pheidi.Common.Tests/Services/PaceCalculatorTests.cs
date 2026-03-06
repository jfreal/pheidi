using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pheidi.Common.Models;
using Pheidi.Common.Services;

namespace Pheidi.Common.Tests.Services;

[TestClass]
public class PaceCalculatorTests
{
    private readonly PaceCalculator _calculator = new();

    [TestMethod]
    public void GetPaceZoneReturnsValidPaceForVdot45()
    {
        var zone = _calculator.GetPaceZone(VdotZone.Easy, 45);

        Assert.IsNotNull(zone);
        Assert.AreEqual(VdotZone.Easy, zone.Zone);
        Assert.IsTrue(zone.MinPacePerMile > 0);
        Assert.IsTrue(zone.MaxPacePerMile > zone.MinPacePerMile);
    }

    [TestMethod]
    public void TempoZoneIsFasterThanEasyZone()
    {
        var easy = _calculator.GetPaceZone(VdotZone.Easy, 45);
        var tempo = _calculator.GetPaceZone(VdotZone.Tempo, 45);

        Assert.IsTrue(tempo.MinPacePerMile < easy.MinPacePerMile,
            "Tempo pace should be faster (lower min/mile) than easy pace");
    }

    [TestMethod]
    public void EstimateVdotFrom5KTime()
    {
        // ~25 minute 5K should be around VDOT 30
        var vdot = _calculator.EstimateVdot(RaceDistance.FiveK, TimeSpan.FromMinutes(25));
        Assert.AreEqual(30, vdot);
    }

    [TestMethod]
    public void EstimateVdotFrom10KTime()
    {
        // ~41 minute 10K should be around VDOT 40
        var vdot = _calculator.EstimateVdot(RaceDistance.TenK, TimeSpan.FromMinutes(41));
        Assert.AreEqual(40, vdot);
    }

    [TestMethod]
    public void EstimateVdotFromMarathonTime()
    {
        // ~3:45 marathon should be around VDOT 40
        var vdot = _calculator.EstimateVdot(RaceDistance.FullMarathon, TimeSpan.FromHours(3).Add(TimeSpan.FromMinutes(12)));
        Assert.AreEqual(40, vdot);
    }

    [TestMethod]
    public void FormatPaceReturnsCorrectFormat()
    {
        Assert.AreEqual("8:30", PaceCalculator.FormatPace(8.5m));
        Assert.AreEqual("7:00", PaceCalculator.FormatPace(7.0m));
    }

    [TestMethod]
    public void RpeDescriptionReturnsForAllValues()
    {
        for (int i = 1; i <= 10; i++)
        {
            var desc = PaceCalculator.GetRpeDescription(i);
            Assert.IsFalse(string.IsNullOrEmpty(desc), $"RPE {i} should have a description");
            Assert.AreNotEqual("Unknown effort level", desc);
        }
    }

    [TestMethod]
    public void PaceZoneIncludesRpeInfo()
    {
        var zone = _calculator.GetPaceZone(VdotZone.Tempo, 50);

        Assert.IsTrue(zone.RpeMin.HasValue);
        Assert.IsTrue(zone.RpeMax.HasValue);
        Assert.IsFalse(string.IsNullOrEmpty(zone.RpeDescription));
    }
}
