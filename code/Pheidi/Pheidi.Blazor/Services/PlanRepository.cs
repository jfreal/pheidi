using Microsoft.EntityFrameworkCore;
using Pheidi.Blazor.Data;
using Pheidi.Common.Models;

namespace Pheidi.Blazor.Services;

public class PlanRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public PlanRepository(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<NewTrainingPlan?> GetActivePlanAsync(int userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.TrainingPlans
            .Include(p => p.RaceGoal)
            .Include(p => p.Weeks)
                .ThenInclude(w => w.Workouts)
            .Where(p => p.UserId == userId && p.Status == PlanStatus.Active)
            .FirstOrDefaultAsync();
    }

    public async Task<List<NewTrainingPlan>> GetAllPlansAsync(int userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.TrainingPlans
            .Include(p => p.RaceGoal)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync();
    }

    public async Task SavePlanAsync(NewTrainingPlan plan)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        if (plan.Id == 0)
        {
            db.TrainingPlans.Add(plan);
        }
        else
        {
            // Attach the detached entity and mark only the root as modified
            // to avoid FK issues with owned types (PaceZone) in child entities
            var existing = await db.TrainingPlans.FindAsync(plan.Id);
            if (existing != null)
                db.Entry(existing).CurrentValues.SetValues(plan);
        }
        await db.SaveChangesAsync();
    }

    public async Task ArchiveActivePlansAsync(int userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var activePlans = await db.TrainingPlans
            .Where(p => p.UserId == userId && p.Status == PlanStatus.Active)
            .ToListAsync();

        foreach (var plan in activePlans)
        {
            plan.Status = PlanStatus.Archived;
        }

        await db.SaveChangesAsync();
    }
}
