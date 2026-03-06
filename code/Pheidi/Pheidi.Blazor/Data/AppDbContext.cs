using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Pheidi.Common.Models;

namespace Pheidi.Blazor.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<NewTrainingPlan> TrainingPlans => Set<NewTrainingPlan>();
    public DbSet<TrainingWeek> TrainingWeeks => Set<TrainingWeek>();
    public DbSet<ScheduledWorkout> ScheduledWorkouts => Set<ScheduledWorkout>();
    public DbSet<InjuryReport> InjuryReports => Set<InjuryReport>();
    public DbSet<PainEntry> PainEntries => Set<PainEntry>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // AppUser
        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
        });

        // UserProfile — store AvailableDays as comma-separated string
        modelBuilder.Entity<UserProfile>(e =>
        {
            e.Property(p => p.AvailableDays)
                .HasConversion(
                    v => string.Join(',', v.Select(d => (int)d)),
                    v => string.IsNullOrEmpty(v)
                        ? Array.Empty<DayOfWeek>()
                        : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => (DayOfWeek)int.Parse(s))
                            .ToArray(),
                    new ValueComparer<DayOfWeek[]>(
                        (a, b) => a != null && b != null && a.SequenceEqual(b),
                        v => v.Aggregate(0, (hash, d) => HashCode.Combine(hash, d)),
                        v => v.ToArray()));
        });

        // RaceGoal — store TargetTime as ticks
        modelBuilder.Entity<RaceGoal>(e =>
        {
            e.Property(r => r.TargetTime)
                .HasConversion(
                    v => v.HasValue ? v.Value.Ticks : (long?)null,
                    v => v.HasValue ? TimeSpan.FromTicks(v.Value) : null);
        });

        // NewTrainingPlan
        modelBuilder.Entity<NewTrainingPlan>(e =>
        {
            e.HasOne(p => p.RaceGoal)
                .WithMany()
                .HasForeignKey(p => p.RaceGoalId);

            e.HasMany(p => p.Weeks)
                .WithOne()
                .HasForeignKey(w => w.TrainingPlanId);

            e.Ignore(p => p.UserProfile); // UserProfile loaded separately via UserId
        });

        // TrainingWeek
        modelBuilder.Entity<TrainingWeek>(e =>
        {
            e.HasMany(w => w.Workouts)
                .WithOne()
                .HasForeignKey(wo => wo.TrainingWeekId);
        });

        // ScheduledWorkout — ignore computed PaceZone, store TimeSpan as ticks
        modelBuilder.Entity<ScheduledWorkout>(e =>
        {
            e.Ignore(w => w.PaceZone);

            e.Property(w => w.TargetDuration)
                .HasConversion(
                    v => v.HasValue ? v.Value.Ticks : (long?)null,
                    v => v.HasValue ? TimeSpan.FromTicks(v.Value) : null);

            e.Property(w => w.WarmUpDuration)
                .HasConversion(
                    v => v.HasValue ? v.Value.Ticks : (long?)null,
                    v => v.HasValue ? TimeSpan.FromTicks(v.Value) : null);

            e.Property(w => w.CoolDownDuration)
                .HasConversion(
                    v => v.HasValue ? v.Value.Ticks : (long?)null,
                    v => v.HasValue ? TimeSpan.FromTicks(v.Value) : null);

            e.Property(w => w.ActualDuration)
                .HasConversion(
                    v => v.HasValue ? v.Value.Ticks : (long?)null,
                    v => v.HasValue ? TimeSpan.FromTicks(v.Value) : null);
        });

        // InjuryReport
        modelBuilder.Entity<InjuryReport>(e =>
        {
            e.HasMany(r => r.PainHistory)
                .WithOne()
                .HasForeignKey(p => p.InjuryReportId);
        });

        // OtpCode
        modelBuilder.Entity<OtpCode>(e =>
        {
            e.HasIndex(o => new { o.Email, o.Code });
        });
    }
}
