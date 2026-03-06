using Pheidi.Common.Models;

namespace Pheidi.Common.Services;

public static class MessagingService
{
    public static string GetStreakMessage(int streak) => streak switch
    {
        >= 21 => $"{streak} in a row! You're an unstoppable force!",
        >= 14 => $"{streak} consecutive workouts! That's incredible consistency.",
        >= 7 => $"{streak} in a row! You're building great consistency.",
        >= 3 => $"{streak} workouts straight — keep it rolling!",
        _ => "Great job getting out there!"
    };

    public static string GetMilestoneMessage(int totalWorkouts) => totalWorkouts switch
    {
        100 => "100 workouts logged! What an incredible journey.",
        50 => "50 workouts in the books! You're halfway to 100.",
        25 => "25 workouts complete — you're building a real habit!",
        10 => "Double digits! 10 workouts done.",
        1 => "Your first workout is in the books! Welcome to the journey.",
        _ => ""
    };

    public static string GetMissedWorkoutMessage() =>
        "No worries — rest is training too. Here's what's coming up next.";

    public static string GetWeekCompleteMessage(decimal weeklyCompletion) => weeklyCompletion switch
    {
        >= 100 => "Perfect week! Every workout completed.",
        >= 80 => "Strong week! You stayed consistent.",
        >= 50 => "Solid effort this week. Every run counts!",
        _ => "A new week brings new opportunities. Let's go!"
    };

    public static string GetPhaseTransitionMessage(TrainingPhase newPhase) => newPhase switch
    {
        TrainingPhase.Build => "Welcome to the Build phase! Time to layer on some quality work.",
        TrainingPhase.Peak => "You've reached Peak phase — your fitness is at its highest. Trust the work you've done!",
        TrainingPhase.Taper => "Taper time! Ease off and let your body absorb all that training. Trust the taper!",
        _ => "New phase, new opportunities. Keep it up!"
    };
}
