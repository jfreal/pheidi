using Pheidi.Common.Models;

namespace Pheidi.Blazor.Components.Shared;

public static class WorkoutColors
{
    public static string Get(WorkoutType type) => type switch
    {
        WorkoutType.Easy => "#E8F5E9",        // very light green
        WorkoutType.Tempo => "#FFF3E0",       // very light orange
        WorkoutType.Intervals => "#FFEBEE",   // very light red
        WorkoutType.LongRun => "#E3F2FD",     // very light blue
        WorkoutType.Recovery => "#F1F8E9",    // very pale lime
        WorkoutType.Fartlek => "#FBE9E7",     // very light deep orange
        WorkoutType.HillRepeats => "#EFEBE9", // very light brown
        WorkoutType.RacePace => "#FFFDE7",    // very light gold
        WorkoutType.CrossTraining => "#F3E5F5", // very light purple
        WorkoutType.Strength => "#ECEFF1",    // very light blue grey
        WorkoutType.Rest => "#F5F5F5",        // near white grey
        _ => "#F5F5F5"
    };

    public static string GetText(WorkoutType type) => type switch
    {
        WorkoutType.Rest => "#999",
        _ => "#555"
    };

    public static string GetModifier(WorkoutModifier modifier) => modifier switch
    {
        WorkoutModifier.Vacation => "#E1F5FE",       // very light blue
        WorkoutModifier.InjuryReduced => "#FFEBEE",  // very light pink
        WorkoutModifier.DayOff => "#F5F5F5",         // near white grey
        _ => ""
    };

    public static string GetModifierText(WorkoutModifier modifier) => modifier switch
    {
        WorkoutModifier.Vacation => "#0288D1",        // medium blue
        WorkoutModifier.InjuryReduced => "#D32F2F",   // medium red
        WorkoutModifier.DayOff => "#888",             // medium grey
        _ => "#888"
    };

    public static string GetModifierIcon(WorkoutModifier modifier) => modifier switch
    {
        WorkoutModifier.Vacation => "fas fa-umbrella-beach",
        WorkoutModifier.InjuryReduced => "fas fa-band-aid",
        WorkoutModifier.DayOff => "fas fa-ban",
        _ => ""
    };
}
