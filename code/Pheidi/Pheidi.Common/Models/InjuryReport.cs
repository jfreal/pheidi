namespace Pheidi.Common.Models;

public enum BodyPart
{
    Knee,
    Shin,
    Hip,
    Ankle,
    Foot,
    Calf,
    Hamstring,
    ITBand,
    Back
}

public enum InjuryStatus
{
    Active,
    Recovering,
    Resolved
}

public class InjuryReport
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public BodyPart BodyPart { get; set; }
    public int Severity { get; set; } // 1-10
    public DateTime ReportDate { get; set; } = DateTime.UtcNow;
    public InjuryStatus Status { get; set; } = InjuryStatus.Active;
    public List<PainEntry> PainHistory { get; set; } = [];
}

public class PainEntry
{
    public int Id { get; set; }
    public int InjuryReportId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public int Severity { get; set; }
    public string? Notes { get; set; }
}
