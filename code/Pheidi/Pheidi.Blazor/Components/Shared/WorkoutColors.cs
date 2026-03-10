using Pheidi.Common.Models;

namespace Pheidi.Blazor.Components.Shared;

public static class WorkoutColors
{
    public static string Get(WorkoutType type) => type switch
    {
        WorkoutType.Easy => "#C8E6C9",        // pastel green
        WorkoutType.Tempo => "#FFE0B2",       // pastel orange
        WorkoutType.Intervals => "#FFCDD2",   // pastel red
        WorkoutType.LongRun => "#BBDEFB",     // pastel blue
        WorkoutType.Recovery => "#DCEDC8",    // pale lime
        WorkoutType.Fartlek => "#FFCCBC",     // pastel deep orange
        WorkoutType.HillRepeats => "#D7CCC8", // pastel brown
        WorkoutType.RacePace => "#FFF9C4",    // pastel gold
        WorkoutType.CrossTraining => "#E1BEE7", // pastel purple
        WorkoutType.Strength => "#CFD8DC",    // pastel blue grey
        WorkoutType.Rest => "#E0E0E0",        // light grey
        _ => "#E0E0E0"
    };

    public static string GetText(WorkoutType type) => type switch
    {
        WorkoutType.Rest => "#666",
        _ => "#333"
    };
}
