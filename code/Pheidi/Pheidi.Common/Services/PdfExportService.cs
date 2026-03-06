using System.Text;
using Pheidi.Common.Models;

namespace Pheidi.Common.Services;

public class PdfExportService
{
    public string GeneratePrintableHtml(NewTrainingPlan plan)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:Arial,sans-serif;margin:20px;color:#333;}");
        sb.AppendLine("h1{text-align:center;margin-bottom:5px;}");
        sb.AppendLine("h2{color:#555;border-bottom:2px solid #ddd;padding-bottom:5px;}");
        sb.AppendLine("table{width:100%;border-collapse:collapse;margin-bottom:20px;}");
        sb.AppendLine("th,td{border:1px solid #ddd;padding:6px 8px;text-align:left;font-size:0.85rem;}");
        sb.AppendLine("th{background:#f5f5f5;font-weight:600;}");
        sb.AppendLine(".phase-base{color:#28a745;}.phase-build{color:#fd7e14;}");
        sb.AppendLine(".phase-peak{color:#dc3545;}.phase-taper{color:#6f42c1;}");
        sb.AppendLine(".summary{display:flex;gap:20px;margin-bottom:20px;}");
        sb.AppendLine(".summary div{background:#f8f9fa;padding:10px 15px;border-radius:6px;flex:1;text-align:center;}");
        sb.AppendLine(".summary div strong{display:block;font-size:1.2rem;}");
        sb.AppendLine("@media print{body{margin:0;}h2{page-break-before:auto;}}");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine($"<h1>{plan.RaceGoal.Distance.DisplayName()} Training Plan</h1>");
        sb.AppendLine($"<p style='text-align:center;color:#666;'>Race Date: {plan.RaceGoal.RaceDate:MMMM d, yyyy}</p>");

        // Summary stats
        sb.AppendLine("<div class='summary'>");
        sb.AppendLine($"<div><strong>{plan.TotalWeeks}</strong>Weeks</div>");
        sb.AppendLine($"<div><strong>{plan.TotalPlannedMiles:F0}</strong>Total Miles</div>");
        sb.AppendLine($"<div><strong>{plan.PeakWeeklyMiles:F0}</strong>Peak Week</div>");
        sb.AppendLine($"<div><strong>{plan.LongestRun:F0}</strong>Longest Run</div>");
        sb.AppendLine("</div>");

        // Week-by-week table
        foreach (var week in plan.Weeks)
        {
            var phaseClass = $"phase-{week.Phase.ToString().ToLower()}";
            sb.AppendLine($"<h2>Week {week.WeekNumber} — <span class='{phaseClass}'>{week.Phase}</span> ({week.TotalPlannedDistance:F1} mi)</h2>");
            sb.AppendLine("<table><tr><th>Day</th><th>Workout</th><th>Distance</th><th>Notes</th></tr>");

            foreach (var workout in week.Workouts.OrderBy(w => w.Date))
            {
                var notes = new List<string>();
                if (workout.WarmUpDuration.HasValue)
                    notes.Add($"WU: {workout.WarmUpDuration.Value.TotalMinutes:F0}min");
                if (workout.CoolDownDuration.HasValue)
                    notes.Add($"CD: {workout.CoolDownDuration.Value.TotalMinutes:F0}min");

                sb.AppendLine($"<tr><td>{workout.Date:ddd M/d}</td><td>{workout.Type}</td>" +
                    $"<td>{(workout.TargetDistanceMiles > 0 ? $"{workout.TargetDistanceMiles:F1} mi" : "—")}</td>" +
                    $"<td>{string.Join(", ", notes)}</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    public byte[] GeneratePdfBytes(NewTrainingPlan plan)
    {
        // Returns HTML bytes — browser can print-to-PDF or use a server-side converter
        var html = GeneratePrintableHtml(plan);
        return Encoding.UTF8.GetBytes(html);
    }
}
