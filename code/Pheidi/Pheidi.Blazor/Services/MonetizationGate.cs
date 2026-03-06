using Pheidi.Common.Models;

namespace Pheidi.Blazor.Services;

public static class MonetizationGate
{
    public static bool CanUseVacationHandling(AppUser? user) => user?.IsPaidUser ?? false;

    public static bool CanExportCalendar(AppUser? user) => true; // Free feature

    public static bool CanSharePlan(AppUser? user) => true; // Free feature

    public static string GetUpgradeMessage(string feature) =>
        $"{feature} is a premium feature. Upgrade to unlock it!";
}
