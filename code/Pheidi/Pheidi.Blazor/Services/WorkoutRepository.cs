using Microsoft.EntityFrameworkCore;
using Pheidi.Blazor.Data;
using Pheidi.Common.Models;

namespace Pheidi.Blazor.Services;

public class WorkoutRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public WorkoutRepository(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<ScheduledWorkout?> GetWorkoutAsync(int workoutId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.ScheduledWorkouts.FindAsync(workoutId);
    }

    public async Task<List<ScheduledWorkout>> GetWorkoutsForDateAsync(int planId, DateTime date)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.TrainingWeeks
            .Where(w => w.TrainingPlanId == planId)
            .SelectMany(w => w.Workouts)
            .Where(wo => wo.Date.Date == date.Date)
            .ToListAsync();
    }

    public async Task UpdateWorkoutAsync(ScheduledWorkout workout)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.ScheduledWorkouts.FindAsync(workout.Id);
        if (existing == null) return;

        db.Entry(existing).CurrentValues.SetValues(workout);

        // Handle owned PaceZone manually — SetValues doesn't cover owned types
        if (workout.PaceZone != null)
        {
            if (existing.PaceZone != null)
                db.Entry(existing.PaceZone).CurrentValues.SetValues(workout.PaceZone);
            else
                existing.PaceZone = workout.PaceZone.Clone();
        }
        else
        {
            existing.PaceZone = null;
        }

        await db.SaveChangesAsync();
    }

    public async Task<List<InjuryReport>> GetActiveInjuriesAsync(int userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.InjuryReports
            .Include(r => r.PainHistory)
            .Where(r => r.UserId == userId && r.Status != InjuryStatus.Resolved)
            .ToListAsync();
    }

    public async Task<List<InjuryReport>> GetAllInjuriesAsync(int userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.InjuryReports
            .Where(r => r.UserId == userId)
            .ToListAsync();
    }

    public async Task SaveInjuryReportAsync(InjuryReport report)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        if (report.Id == 0)
        {
            db.InjuryReports.Add(report);
        }
        else
        {
            var existing = await db.InjuryReports
                .Include(r => r.PainHistory)
                .FirstOrDefaultAsync(r => r.Id == report.Id);
            if (existing != null)
            {
                db.Entry(existing).CurrentValues.SetValues(report);

                // Add any new PainEntry items not yet in the database
                foreach (var entry in report.PainHistory)
                {
                    if (entry.Id == 0)
                        existing.PainHistory.Add(new PainEntry
                        {
                            Severity = entry.Severity,
                            Date = entry.Date,
                            InjuryReportId = existing.Id
                        });
                }
            }
            else
            {
                db.InjuryReports.Add(report);
            }
        }
        await db.SaveChangesAsync();
    }

    public async Task UpdateWorkoutsBatchAsync(IEnumerable<ScheduledWorkout> workouts)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        foreach (var workout in workouts)
        {
            var existing = await db.ScheduledWorkouts.FindAsync(workout.Id);
            if (existing == null) continue;

            db.Entry(existing).CurrentValues.SetValues(workout);

            if (workout.PaceZone != null)
            {
                if (existing.PaceZone != null)
                    db.Entry(existing.PaceZone).CurrentValues.SetValues(workout.PaceZone);
                else
                    existing.PaceZone = workout.PaceZone.Clone();
            }
            else
            {
                existing.PaceZone = null;
            }
        }
        await db.SaveChangesAsync();
    }
}
