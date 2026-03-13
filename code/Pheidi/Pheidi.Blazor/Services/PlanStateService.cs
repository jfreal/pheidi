using Microsoft.JSInterop;
using Pheidi.Common.Engines;
using Pheidi.Common.Models;

namespace Pheidi.Blazor.Services;

public class PlanStateService
{
    private readonly PlanGenerationEngine _engine = new();
    private readonly PlanRepository _planRepo;
    private readonly UserService _userService;
    private readonly AuthStateService _auth;

    public UserProfile UserProfile { get; private set; } = new();
    public RaceGoal RaceGoal { get; set; } = new();
    public NewTrainingPlan? ActivePlan { get; private set; }

    public event Func<Task>? OnPlanChanged;

    public PlanStateService(PlanRepository planRepo, UserService userService, AuthStateService auth)
    {
        _planRepo = planRepo;
        _userService = userService;
        _auth = auth;
    }

    public void UpdateUserProfile(UserProfile profile)
    {
        UserProfile = profile;
    }

    public async Task LoadActivePlanAsync()
    {
        if (_auth.CurrentUser == null) return;
        ActivePlan = await _planRepo.GetActivePlanAsync(_auth.CurrentUser.Id);
        UserProfile = await _userService.GetOrCreateProfileAsync(_auth.CurrentUser.Id);
        await NotifyPlanChanged();
    }

    public async Task<NewTrainingPlan> GeneratePlanAsync()
    {
        if (_auth.CurrentUser == null)
            throw new InvalidOperationException("User must be signed in to generate a plan.");

        // Archive existing active plans
        await _planRepo.ArchiveActivePlansAsync(_auth.CurrentUser.Id);

        // Persist user profile settings from onboarding
        UserProfile.UserId = _auth.CurrentUser.Id;
        await _userService.SaveProfileAsync(UserProfile);

        // Reset RaceGoal Id so EF creates a new row instead of
        // trying to INSERT with the old plan's RaceGoal Id
        RaceGoal.Id = 0;

        ActivePlan = _engine.Generate(RaceGoal, UserProfile);
        ActivePlan.UserId = _auth.CurrentUser.Id;
        await _planRepo.SavePlanAsync(ActivePlan);

        await NotifyPlanChanged();
        return ActivePlan;
    }

    public async Task PausePlanAsync()
    {
        if (ActivePlan == null || _auth.CurrentUser == null) return;
        ActivePlan.Status = PlanStatus.Paused;
        await _planRepo.SavePlanAsync(ActivePlan);
        await NotifyPlanChanged();
    }

    public async Task ResumePlanAsync()
    {
        if (ActivePlan == null || _auth.CurrentUser == null) return;
        ActivePlan.Status = PlanStatus.Active;
        await _planRepo.SavePlanAsync(ActivePlan);
        await NotifyPlanChanged();
    }

    public async Task AbandonPlanAsync()
    {
        if (ActivePlan == null || _auth.CurrentUser == null) return;
        ActivePlan.Status = PlanStatus.Archived;
        await _planRepo.SavePlanAsync(ActivePlan);
        ActivePlan = null;
        await NotifyPlanChanged();
    }

    public bool HasActivePlan => ActivePlan is { Status: PlanStatus.Active };
    public bool HasPausedPlan => ActivePlan is { Status: PlanStatus.Paused };

    /// <summary>
    /// Task 11.1: Count consecutive completed scheduled workout days from today backward.
    /// </summary>
    public static int CalculateCurrentStreak(NewTrainingPlan plan)
    {
        var today = DateTime.Today;
        var completedDates = plan.Weeks
            .SelectMany(w => w.Workouts)
            .Where(w => w.Status == WorkoutStatus.Completed && w.IsRunWorkout)
            .Select(w => w.Date.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        if (completedDates.Count == 0) return 0;

        int streak = 0;
        // Start from the most recent completed date
        var checkDate = completedDates[0];
        // Only count if the most recent completed date is today or yesterday
        if ((today - checkDate).TotalDays > 1) return 0;

        foreach (var date in completedDates)
        {
            if (date == checkDate)
            {
                streak++;
                checkDate = checkDate.AddDays(-1);
            }
            else break;
        }

        return streak;
    }

    /// <summary>
    /// Task 11.2: Scan all completed workouts for longest consecutive run streak.
    /// </summary>
    public static int CalculateBestStreak(NewTrainingPlan plan)
    {
        var completedDates = plan.Weeks
            .SelectMany(w => w.Workouts)
            .Where(w => w.Status == WorkoutStatus.Completed && w.IsRunWorkout)
            .Select(w => w.Date.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        if (completedDates.Count == 0) return 0;

        int best = 1, current = 1;
        for (int i = 1; i < completedDates.Count; i++)
        {
            if ((completedDates[i] - completedDates[i - 1]).TotalDays == 1)
            {
                current++;
                if (current > best) best = current;
            }
            else
            {
                current = 1;
            }
        }

        return best;
    }

    /// <summary>
    /// Task 11.3: Get total completed workout count for milestone tracking.
    /// </summary>
    public static int GetTotalCompletedWorkouts(NewTrainingPlan plan)
    {
        return plan.Weeks
            .SelectMany(w => w.Workouts)
            .Count(w => w.Status == WorkoutStatus.Completed && w.IsRunWorkout);
    }

    public bool IsOnboardingComplete =>
        RaceGoal.RaceDate > DateTime.Today &&
        UserProfile.AvailableDays.Length >= 3;

    private async Task NotifyPlanChanged()
    {
        if (OnPlanChanged != null)
        {
            foreach (var handler in OnPlanChanged.GetInvocationList().Cast<Func<Task>>())
            {
                try
                {
                    await handler();
                }
                catch (JSDisconnectedException)
                {
                    // Circuit disposed — safe to ignore
                }
            }
        }
    }
}
