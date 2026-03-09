using Pheidi.Common.Engines;
using Pheidi.Common.Models;

namespace Pheidi.Blazor.Services;

public class PlanStateService
{
    private readonly PlanGenerationEngine _engine = new();
    private readonly PlanRepository _planRepo;
    private readonly AuthStateService _auth;

    public UserProfile UserProfile { get; set; } = new();
    public RaceGoal RaceGoal { get; set; } = new();
    public NewTrainingPlan? ActivePlan { get; private set; }

    public event Action? OnPlanChanged;

    public PlanStateService(PlanRepository planRepo, AuthStateService auth)
    {
        _planRepo = planRepo;
        _auth = auth;
    }

    public async Task LoadActivePlanAsync()
    {
        if (_auth.CurrentUser == null) return;
        ActivePlan = await _planRepo.GetActivePlanAsync(_auth.CurrentUser.Id);
        OnPlanChanged?.Invoke();
    }

    public async Task<NewTrainingPlan> GeneratePlanAsync()
    {
        if (_auth.CurrentUser == null)
            throw new InvalidOperationException("User must be signed in to generate a plan.");

        // Archive existing active plans
        await _planRepo.ArchiveActivePlansAsync(_auth.CurrentUser.Id);

        ActivePlan = _engine.Generate(RaceGoal, UserProfile);
        ActivePlan.UserId = _auth.CurrentUser.Id;
        await _planRepo.SavePlanAsync(ActivePlan);

        OnPlanChanged?.Invoke();
        return ActivePlan;
    }

    public bool HasActivePlan => ActivePlan is { Status: PlanStatus.Active };

    public bool IsOnboardingComplete =>
        RaceGoal.RaceDate > DateTime.Today &&
        UserProfile.AvailableDays.Length >= 3;
}
