using Pheidi.Common.Engines;
using Pheidi.Common.Models;

namespace Pheidi.Blazor.Services;

public class PlanStateService
{
    private readonly PlanGenerationEngine _engine = new();

    public UserProfile UserProfile { get; set; } = new();
    public RaceGoal RaceGoal { get; set; } = new();
    public NewTrainingPlan? ActivePlan { get; private set; }

    public event Action? OnPlanChanged;

    public NewTrainingPlan GeneratePlan()
    {
        if (ActivePlan != null)
        {
            ActivePlan.Status = PlanStatus.Archived;
        }

        ActivePlan = _engine.Generate(RaceGoal, UserProfile);
        OnPlanChanged?.Invoke();
        return ActivePlan;
    }

    public bool HasActivePlan => ActivePlan is { Status: PlanStatus.Active };

    public bool IsOnboardingComplete =>
        RaceGoal.RaceDate > DateTime.Today &&
        UserProfile.AvailableDays.Length >= 3;
}
