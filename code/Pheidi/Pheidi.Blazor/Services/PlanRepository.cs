using Microsoft.EntityFrameworkCore;
using Pheidi.Blazor.Data;
using Pheidi.Common.Models;

namespace Pheidi.Blazor.Services;

public class PlanRepository
{
    private readonly AppDbContext _db;

    public PlanRepository(AppDbContext db) => _db = db;

    public async Task<NewTrainingPlan?> GetActivePlanAsync(int userId)
    {
        return await _db.TrainingPlans
            .Include(p => p.RaceGoal)
            .Include(p => p.Weeks)
                .ThenInclude(w => w.Workouts)
            .Where(p => p.UserId == userId && p.Status == PlanStatus.Active)
            .FirstOrDefaultAsync();
    }

    public async Task<List<NewTrainingPlan>> GetAllPlansAsync(int userId)
    {
        return await _db.TrainingPlans
            .Include(p => p.RaceGoal)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync();
    }

    public async Task SavePlanAsync(NewTrainingPlan plan)
    {
        if (plan.Id == 0)
        {
            _db.TrainingPlans.Add(plan);
        }
        else
        {
            _db.TrainingPlans.Update(plan);
        }
        await _db.SaveChangesAsync();
    }

    public async Task ArchiveActivePlansAsync(int userId)
    {
        var activePlans = await _db.TrainingPlans
            .Where(p => p.UserId == userId && p.Status == PlanStatus.Active)
            .ToListAsync();

        foreach (var plan in activePlans)
        {
            plan.Status = PlanStatus.Archived;
        }

        await _db.SaveChangesAsync();
    }
}
