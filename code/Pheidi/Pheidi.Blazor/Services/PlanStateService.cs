using Pheidi.Common.Engines;
using Pheidi.Common.Models;

namespace Pheidi.Blazor.Services;

public class PlanStateService
{
    private readonly PlanGenerationEngine _engine = new();
    private readonly PlanRepository _planRepo;
    private readonly UserService _userService;
    private readonly AuthStateService _auth;

    public UserProfile UserProfile { get; set; } = new();
    public RaceGoal RaceGoal { get; set; } = new();
    public NewTrainingPlan? ActivePlan { get; private set; }

    public event Action? OnPlanChanged;

    public PlanStateService(PlanRepository planRepo, UserService userService, AuthStateService auth)
    {
        _planRepo = planRepo;
        _userService = userService;
        _auth = auth;
    }

    public async Task LoadActivePlanAsync()
    {
        if (_auth.CurrentUser == null) return;
        ActivePlan = await _planRepo.GetActivePlanAsync(_auth.CurrentUser.Id);
        UserProfile = await _userService.GetOrCreateProfileAsync(_auth.CurrentUser.Id);
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

    public async Task PausePlanAsync()
    {
        if (ActivePlan == null || _auth.CurrentUser == null) return;
        ActivePlan.Status = PlanStatus.Paused;
        await _planRepo.SavePlanAsync(ActivePlan);
        OnPlanChanged?.Invoke();
    }

    public async Task ResumePlanAsync()
    {
        if (ActivePlan == null || _auth.CurrentUser == null) return;
        ActivePlan.Status = PlanStatus.Active;
        await _planRepo.SavePlanAsync(ActivePlan);
        OnPlanChanged?.Invoke();
    }

    public async Task AbandonPlanAsync()
    {
        if (ActivePlan == null || _auth.CurrentUser == null) return;
        ActivePlan.Status = PlanStatus.Archived;
        await _planRepo.SavePlanAsync(ActivePlan);
        ActivePlan = null;
        OnPlanChanged?.Invoke();
    }

    public bool HasActivePlan => ActivePlan is { Status: PlanStatus.Active };
    public bool HasPausedPlan => ActivePlan is { Status: PlanStatus.Paused };

    public bool IsOnboardingComplete =>
        RaceGoal.RaceDate > DateTime.Today &&
        UserProfile.AvailableDays.Length >= 3;
}
