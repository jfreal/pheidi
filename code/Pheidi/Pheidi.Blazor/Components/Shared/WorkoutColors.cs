using Pheidi.Common.Models;

namespace Pheidi.Blazor.Components.Shared;

public static class WorkoutColors
{
    public static string Get(WorkoutType type) => type switch
    {
        WorkoutType.Easy => "#4CAF50",        // green
        WorkoutType.Tempo => "#FF9800",       // orange
        WorkoutType.Intervals => "#F44336",   // red
        WorkoutType.LongRun => "#2196F3",     // blue
        WorkoutType.Recovery => "#81C784",    // light green
        WorkoutType.Fartlek => "#FF5722",     // deep orange
        WorkoutType.HillRepeats => "#795548", // brown
        WorkoutType.RacePace => "#FFC107",    // gold
        WorkoutType.CrossTraining => "#9C27B0", // purple
        WorkoutType.Strength => "#607D8B",    // blue grey
        WorkoutType.Rest => "#9E9E9E",        // grey
        _ => "#9E9E9E"
    };

    public static string GetText(WorkoutType type) => type switch
    {
        WorkoutType.RacePace or WorkoutType.Easy or WorkoutType.Recovery => "#333",
        _ => "#fff"
    };
}
