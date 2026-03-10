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
        db.ScheduledWorkouts.Update(workout);
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
            db.InjuryReports.Update(report);
        }
        await db.SaveChangesAsync();
    }
}
