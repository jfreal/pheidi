using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pheidi.Common.Models;

namespace Pheidi.Common.Tests.Models;

[TestClass]
public class AgeGroupTests
{
    [TestMethod]
    public void NullBirthdateReturnsUnder40()
    {
        Assert.AreEqual(AgeGroup.Under40, AgeGroupExtensions.GetAgeGroup(null));
    }

    [TestMethod]
    public void Age25ReturnsUnder40()
    {
        var dob = DateTime.Today.AddYears(-25);
        Assert.AreEqual(AgeGroup.Under40, AgeGroupExtensions.GetAgeGroup(dob));
    }

    [TestMethod]
    public void Age39ReturnsUnder40()
    {
        var dob = DateTime.Today.AddYears(-39);
        Assert.AreEqual(AgeGroup.Under40, AgeGroupExtensions.GetAgeGroup(dob));
    }

    [TestMethod]
    public void Age40ReturnsForties()
    {
        // Exactly 40 today
        var dob = DateTime.Today.AddYears(-40);
        Assert.AreEqual(AgeGroup.Forties, AgeGroupExtensions.GetAgeGroup(dob));
    }

    [TestMethod]
    public void Age49ReturnsForties()
    {
        var dob = DateTime.Today.AddYears(-49);
        Assert.AreEqual(AgeGroup.Forties, AgeGroupExtensions.GetAgeGroup(dob));
    }

    [TestMethod]
    public void Age50ReturnsFifties()
    {
        var dob = DateTime.Today.AddYears(-50);
        Assert.AreEqual(AgeGroup.Fifties, AgeGroupExtensions.GetAgeGroup(dob));
    }

    [TestMethod]
    public void Age60ReturnsSixtyPlus()
    {
        var dob = DateTime.Today.AddYears(-60);
        Assert.AreEqual(AgeGroup.SixtyPlus, AgeGroupExtensions.GetAgeGroup(dob));
    }

    [TestMethod]
    public void BirthdayNotYetThisYear()
    {
        // Person turns 40 tomorrow — should still be Under40
        var dob = DateTime.Today.AddYears(-40).AddDays(1);
        Assert.AreEqual(AgeGroup.Under40, AgeGroupExtensions.GetAgeGroup(dob));
    }

    [TestMethod]
    public void BirthdayAlreadyPassedThisYear()
    {
        // Person turned 40 yesterday — should be Forties
        var dob = DateTime.Today.AddYears(-40).AddDays(-1);
        Assert.AreEqual(AgeGroup.Forties, AgeGroupExtensions.GetAgeGroup(dob));
    }

    // --- Recovery days ---

    [TestMethod]
    public void RecoveryDaysScaleWithAge()
    {
        Assert.AreEqual(1, AgeGroup.Under40.GetMinRecoveryDays());
        Assert.AreEqual(2, AgeGroup.Forties.GetMinRecoveryDays());
        Assert.AreEqual(2, AgeGroup.Fifties.GetMinRecoveryDays());
        Assert.AreEqual(3, AgeGroup.SixtyPlus.GetMinRecoveryDays());
    }

    // --- Warm-up duration ---

    [TestMethod]
    public void WarmUpIncreasesWithAge()
    {
        Assert.AreEqual(10, AgeGroup.Under40.GetWarmUpDuration().TotalMinutes);
        Assert.AreEqual(10, AgeGroup.Forties.GetWarmUpDuration().TotalMinutes);
        Assert.AreEqual(12, AgeGroup.Fifties.GetWarmUpDuration().TotalMinutes);
        Assert.AreEqual(15, AgeGroup.SixtyPlus.GetWarmUpDuration().TotalMinutes);
    }

    // --- Display name ---

    [TestMethod]
    public void DisplayNameIsReadable()
    {
        Assert.AreEqual("Under 40", AgeGroup.Under40.DisplayName());
        Assert.AreEqual("60+", AgeGroup.SixtyPlus.DisplayName());
    }
}
