using System.Text;
using Pheidi.Common.Models;

namespace Pheidi.Common.Services;

public class ICalExportService
{
    public string GenerateICalendar(NewTrainingPlan plan)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//Pheidi//Training Plan//EN");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:PUBLISH");
        sb.AppendLine($"X-WR-CALNAME:Pheidi - {plan.RaceGoal.Distance.DisplayName()} Plan");

        foreach (var week in plan.Weeks)
        {
            foreach (var workout in week.Workouts)
            {
                if (workout.Type == WorkoutType.Rest) continue;

                sb.AppendLine("BEGIN:VEVENT");
                sb.AppendLine($"DTSTART;VALUE=DATE:{workout.Date:yyyyMMdd}");
                sb.AppendLine($"DTEND;VALUE=DATE:{workout.Date.AddDays(1):yyyyMMdd}");
                sb.AppendLine($"SUMMARY:{workout.Description}");
                sb.AppendLine($"DESCRIPTION:{BuildDescription(workout, week)}");
                sb.AppendLine($"UID:{Guid.NewGuid()}@pheidi");
                sb.AppendLine($"CATEGORIES:{workout.Type}");
                sb.AppendLine("END:VEVENT");
            }
        }

        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }

    public byte[] GenerateIcsFile(NewTrainingPlan plan)
    {
        var content = GenerateICalendar(plan);
        return Encoding.UTF8.GetBytes(content);
    }

    private static string BuildDescription(ScheduledWorkout workout, TrainingWeek week)
    {
        var parts = new List<string>
        {
            $"Week {week.WeekNumber} - {week.Phase} Phase"
        };

        if (workout.TargetDistanceMiles > 0)
            parts.Add($"Distance: {workout.TargetDistanceMiles:F1} miles");

        if (workout.WarmUpDuration.HasValue)
            parts.Add($"Warm-up: {workout.WarmUpDuration.Value.TotalMinutes:F0} min");

        if (workout.CoolDownDuration.HasValue)
            parts.Add($"Cool-down: {workout.CoolDownDuration.Value.TotalMinutes:F0} min");

        return string.Join("\\n", parts);
    }
}
