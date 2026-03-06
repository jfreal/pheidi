using Microsoft.EntityFrameworkCore;
using Pheidi.Blazor.Data;
using Pheidi.Common.Models;

namespace Pheidi.Blazor.Services;

public class WorkoutRepository
{
    private readonly AppDbContext _db;

    public WorkoutRepository(AppDbContext db) => _db = db;

    public async Task<ScheduledWorkout?> GetWorkoutAsync(int workoutId)
    {
        return await _db.ScheduledWorkouts.FindAsync(workoutId);
    }

    public async Task<List<ScheduledWorkout>> GetWorkoutsForDateAsync(int planId, DateTime date)
    {
        return await _db.TrainingWeeks
            .Where(w => w.TrainingPlanId == planId)
            .SelectMany(w => w.Workouts)
            .Where(wo => wo.Date.Date == date.Date)
            .ToListAsync();
    }

    public async Task UpdateWorkoutAsync(ScheduledWorkout workout)
    {
        _db.ScheduledWorkouts.Update(workout);
        await _db.SaveChangesAsync();
    }

    public async Task<List<InjuryReport>> GetActiveInjuriesAsync(int userId)
    {
        return await _db.InjuryReports
            .Include(r => r.PainHistory)
            .Where(r => r.UserId == userId && r.Status != InjuryStatus.Resolved)
            .ToListAsync();
    }

    public async Task SaveInjuryReportAsync(InjuryReport report)
    {
        if (report.Id == 0)
        {
            _db.InjuryReports.Add(report);
        }
        else
        {
            _db.InjuryReports.Update(report);
        }
        await _db.SaveChangesAsync();
    }
}
