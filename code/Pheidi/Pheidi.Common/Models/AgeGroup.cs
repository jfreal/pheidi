namespace Pheidi.Common.Models;

public enum AgeGroup
{
    Under40,
    Forties,
    Fifties,
    SixtyPlus
}

public static class AgeGroupExtensions
{
    public static AgeGroup GetAgeGroup(DateTime? dateOfBirth)
    {
        if (dateOfBirth == null) return AgeGroup.Under40;

        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Value.Year;
        if (dateOfBirth.Value.Date > today.AddYears(-age)) age--;

        return age switch
        {
            >= 60 => AgeGroup.SixtyPlus,
            >= 50 => AgeGroup.Fifties,
            >= 40 => AgeGroup.Forties,
            _ => AgeGroup.Under40
        };
    }

    public static int GetMinRecoveryDays(this AgeGroup group) => group switch
    {
        AgeGroup.Under40 => 1,
        AgeGroup.Forties => 2,
        AgeGroup.Fifties => 2,
        AgeGroup.SixtyPlus => 3,
        _ => 1
    };

    public static TimeSpan GetWarmUpDuration(this AgeGroup group) => group switch
    {
        AgeGroup.SixtyPlus => TimeSpan.FromMinutes(15),
        AgeGroup.Fifties => TimeSpan.FromMinutes(12),
        _ => TimeSpan.FromMinutes(10)
    };

    public static string DisplayName(this AgeGroup group) => group switch
    {
        AgeGroup.Under40 => "Under 40",
        AgeGroup.Forties => "40–49",
        AgeGroup.Fifties => "50–59",
        AgeGroup.SixtyPlus => "60+",
        _ => "Unknown"
    };
}
