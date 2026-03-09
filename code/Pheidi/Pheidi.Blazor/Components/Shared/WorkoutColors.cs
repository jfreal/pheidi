using Pheidi.Common.Models;

namespace Pheidi.Blazor.Components.Shared;

public static class WorkoutColors
{
    public static string Get(WorkoutType type) => type switch
    {
        WorkoutType.Easy => "#81C784",        // soft green
        WorkoutType.Tempo => "#FFB74D",       // soft orange
        WorkoutType.Intervals => "#E57373",   // soft red
        WorkoutType.LongRun => "#64B5F6",     // soft blue
        WorkoutType.Recovery => "#A5D6A7",    // pale green
        WorkoutType.Fartlek => "#FF8A65",     // soft deep orange
        WorkoutType.HillRepeats => "#A1887F", // soft brown
        WorkoutType.RacePace => "#FFD54F",    // soft gold
        WorkoutType.CrossTraining => "#BA68C8", // soft purple
        WorkoutType.Strength => "#90A4AE",    // soft blue grey
        WorkoutType.Rest => "#BDBDBD",        // light grey
        _ => "#BDBDBD"
    };

    public static string GetText(WorkoutType type) => type switch
    {
        WorkoutType.Rest => "#666",
        _ => "#333"
    };
}
