using Pheidi.Common;

namespace Pheidi.Blazor.Components.Shared;

public record IconAndFriendlyName(string Icon = "", string Name = "");

public static class IconMapping
{
    public static readonly Dictionary<Activity, IconAndFriendlyName> Activity = new()
    {
        [Common.Activity.Rest] = new("fas fa-bed fa-fw", "REST"),
        [Common.Activity.Run] = new("fas fa-running fa-fw", "RUN"),
        [Common.Activity.Sprint] = new("fas fa-tachometer-alt fa-fw", "SPRINT"),
        [Common.Activity.Cross] = new("fas fa-times fa-fw", "CROSS"),
        [Common.Activity.Strength] = new("fas fa-dumbbell fa-fw", "STRENGTH"),
        [Common.Activity.Fartlek] = new("fas fa-chart-line fa-fw", "FARTLEK"),
    };

    public static readonly Dictionary<EffortType, IconAndFriendlyName> EffortType = new()
    {
        [Common.EffortType.Distance] = new("fas fa-road", "Distance"),
        [Common.EffortType.Time] = new("far fa-clock", "Time"),
        [Common.EffortType.Reps] = new("fas fa-tachometer-alt fa-fw", "Reps"),
    };

    public static readonly Dictionary<DistanceType, IconAndFriendlyName> DistanceType = new()
    {
        [Common.DistanceType.Long] = new(Name: "L"),
        [Common.DistanceType.Half] = new(Name: "½ L"),
        [Common.DistanceType.Quarter] = new(Name: "¼ L"),
        [Common.DistanceType.QuarterUp] = new(Name: "¼ L↑"),
        [Common.DistanceType.None] = new(Name: "-"),
    };
}
