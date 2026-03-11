using Pheidi.Common.Models;

namespace Pheidi.Common.Engines;

public class InjuryEngine
{
    /// <summary>
    /// Get run/stop guidance based on pain severity.
    /// </summary>
    public static string GetGuidance(int severity) => severity switch
    {
        <= 0 => "No pain reported. You're good to go!",
        <= 3 => "You can proceed — consider reducing pace and distance by 20%. Stop if pain increases.",
        <= 6 => "Moderate pain — reduce today's volume by 50% and keep intensity easy. Skip quality workouts.",
        <= 9 => "Rest today and consider seeing a healthcare provider. Your plan will adjust automatically.",
        _ => "Severe pain — please rest and consult a healthcare provider before resuming training."
    };

    /// <summary>
    /// Modify a workout based on active injury severity.
    /// </summary>
    public static void ModifyWorkout(ScheduledWorkout workout, int severity)
    {
        workout.Modifier = WorkoutModifier.InjuryReduced;

        if (severity <= 3)
        {
            // Mild: reduce distance by 20%
            workout.TargetDistanceMiles = Math.Round(workout.TargetDistanceMiles * 0.8m, 1);
            if (workout.IsQualityWorkout)
            {
                workout.Type = WorkoutType.Easy;
                workout.PaceZone = PaceZone.ForWorkoutType(WorkoutType.Easy);
                workout.WarmUpDuration = null;
                workout.CoolDownDuration = null;
            }
        }
        else if (severity <= 6)
        {
            // Moderate: convert to easy at 50% distance
            workout.Type = WorkoutType.Easy;
            workout.TargetDistanceMiles = Math.Round(workout.TargetDistanceMiles * 0.5m, 1);
            workout.PaceZone = PaceZone.ForWorkoutType(WorkoutType.Easy);
            workout.WarmUpDuration = null;
            workout.CoolDownDuration = null;
        }
        else
        {
            // Severe: convert to rest or optional cross-training
            workout.Type = WorkoutType.Rest;
            workout.TargetDistanceMiles = 0;
            workout.PaceZone = null;
            workout.WarmUpDuration = null;
            workout.CoolDownDuration = null;
        }
    }

    /// <summary>
    /// Apply injury modifications to all future workouts in a plan.
    /// </summary>
    public static void ApplyToFutureWorkouts(NewTrainingPlan plan, int severity)
    {
        var today = DateTime.Today;
        foreach (var week in plan.Weeks)
        {
            foreach (var workout in week.Workouts)
            {
                if (workout.Date.Date >= today && workout.Status == WorkoutStatus.Pending && workout.Type != WorkoutType.Rest)
                {
                    ModifyWorkout(workout, severity);
                }
            }
        }
    }

    /// <summary>
    /// Generate return-to-plan progression over 4 weeks.
    /// Returns volume multipliers for each return week.
    /// </summary>
    public static decimal[] GetReturnProgression() => [0.50m, 0.70m, 0.85m, 1.00m];

    /// <summary>
    /// Apply return-to-plan progression starting from the current week.
    /// </summary>
    public static void ApplyReturnProgression(NewTrainingPlan plan, int startingWeekNumber)
    {
        var progression = GetReturnProgression();

        for (int i = 0; i < progression.Length; i++)
        {
            var weekNum = startingWeekNumber + i;
            var week = plan.Weeks.FirstOrDefault(w => w.WeekNumber == weekNum);
            if (week == null) break;

            foreach (var workout in week.Workouts.Where(w => w.IsRunWorkout))
            {
                workout.TargetDistanceMiles = Math.Round(workout.TargetDistanceMiles * progression[i], 1);
            }
        }
    }

    /// <summary>
    /// Check if medical clearance should be recommended.
    /// </summary>
    public static bool ShouldRecommendMedicalClearance(InjuryReport injury)
    {
        if (injury.Severity >= 7) return true;
        if ((DateTime.UtcNow - injury.ReportDate).TotalDays > 14) return true;
        return false;
    }

    /// <summary>
    /// Get medical clearance message.
    /// </summary>
    public static string GetMedicalClearanceMessage(InjuryReport injury)
    {
        if (injury.Severity >= 7)
            return "This level of pain warrants medical attention. We recommend consulting a healthcare provider before continuing training.";

        if ((DateTime.UtcNow - injury.ReportDate).TotalDays > 14)
            return "This injury has persisted for 2+ weeks. We recommend consulting a healthcare provider before continuing training.";

        return string.Empty;
    }

    /// <summary>
    /// Get safe cross-training activities based on injured body part.
    /// </summary>
    public static string[] GetSafeCrossTraining(BodyPart bodyPart) => bodyPart switch
    {
        BodyPart.Knee => ["Swimming", "Upper body strength", "Cycling (low resistance)"],
        BodyPart.Ankle => ["Swimming", "Upper body strength", "Seated exercises"],
        BodyPart.Shin => ["Swimming", "Cycling", "Elliptical (if pain-free)"],
        BodyPart.Hip => ["Swimming", "Upper body strength", "Core work"],
        BodyPart.Foot => ["Swimming", "Upper body strength", "Seated cycling"],
        BodyPart.Calf => ["Swimming", "Upper body strength", "Cycling (low resistance)"],
        BodyPart.Hamstring => ["Swimming", "Upper body strength", "Walking (if pain-free)"],
        BodyPart.ITBand => ["Swimming", "Core work", "Upper body strength"],
        BodyPart.Back => ["Swimming (gentle)", "Walking", "Light stretching"],
        _ => ["Swimming", "Light cycling", "Core work"]
    };

    /// <summary>
    /// Check if the same body part has been injured multiple times (recurring injury).
    /// </summary>
    public static bool IsRecurring(BodyPart bodyPart, List<InjuryReport> injuries)
    {
        return injuries.Count(i => i.BodyPart == bodyPart) >= 2;
    }

    /// <summary>
    /// Get a warning message for recurring injuries.
    /// </summary>
    public static string GetRecurringMessage(BodyPart bodyPart) =>
        $"This is a recurring {FormatBodyPart(bodyPart)} injury. Consider consulting a sports medicine specialist for a biomechanical assessment.";

    /// <summary>
    /// Analyze pain trend from pain history entries.
    /// </summary>
    public static string AnalyzeTrend(List<PainEntry> painHistory)
    {
        if (painHistory.Count < 2) return "Insufficient data";

        var recent = painHistory.OrderByDescending(p => p.Date).Take(3).ToList();
        if (recent.Count < 2) return "Insufficient data";

        var latest = recent[0].Severity;
        var previous = recent[^1].Severity;

        if (latest < previous - 1) return "Improving";
        if (latest > previous + 1) return "Worsening";
        return "Stable";
    }

    private static string FormatBodyPart(BodyPart part) => part switch
    {
        BodyPart.ITBand => "IT Band",
        _ => part.ToString()
    };
}
