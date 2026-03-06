using Pheidi.Common;
using Pheidi.Common.Models;

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

    public static readonly Dictionary<WorkoutType, IconAndFriendlyName> WorkoutTypes = new()
    {
        [WorkoutType.Rest] = new("fas fa-bed fa-fw", "Rest"),
        [WorkoutType.Easy] = new("fas fa-walking fa-fw", "Easy"),
        [WorkoutType.Tempo] = new("fas fa-tachometer-alt fa-fw", "Tempo"),
        [WorkoutType.Intervals] = new("fas fa-bolt fa-fw", "Intervals"),
        [WorkoutType.LongRun] = new("fas fa-road fa-fw", "Long Run"),
        [WorkoutType.Recovery] = new("fas fa-heartbeat fa-fw", "Recovery"),
        [WorkoutType.Fartlek] = new("fas fa-chart-line fa-fw", "Fartlek"),
        [WorkoutType.HillRepeats] = new("fas fa-mountain fa-fw", "Hills"),
        [WorkoutType.RacePace] = new("fas fa-flag-checkered fa-fw", "Race Pace"),
        [WorkoutType.CrossTraining] = new("fas fa-swimmer fa-fw", "Cross Train"),
        [WorkoutType.Strength] = new("fas fa-dumbbell fa-fw", "Strength"),
    };

    public static readonly Dictionary<TrainingPhase, IconAndFriendlyName> Phases = new()
    {
        [TrainingPhase.Base] = new("fas fa-layer-group fa-fw", "Base"),
        [TrainingPhase.Build] = new("fas fa-hammer fa-fw", "Build"),
        [TrainingPhase.Peak] = new("fas fa-mountain fa-fw", "Peak"),
        [TrainingPhase.Taper] = new("fas fa-feather fa-fw", "Taper"),
    };
}
