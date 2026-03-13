## 1. Database Schema & Model Changes

- [x] 1.1 Add `VolumeMode` enum to `Pheidi.Common/Models/` with values: Minimal, Moderate, High, Elite
- [x] 1.2 Add `TransitionTimePreset` enum to `Pheidi.Common/Models/` with values: None(0), HomeShower(20), GymShower(25), LunchBreak(30), GymWithCommute(35)
- [x] 1.3 Add `AgeGroup` enum to `Pheidi.Common/Models/` with values: Under40, Forties, Fifties, SixtyPlus and static helper to compute from birthdate
- [x] 1.4 Add `IntensityZone` enum to `Pheidi.Common/Models/` with values: Easy, Threshold, Hard
- [x] 1.5 Add new properties to `UserProfile`: `DateTime? DateOfBirth`, `VolumeMode VolumeMode` (default Moderate), `TransitionTimePreset TransitionTimePreset` (default None), `int? RunWalkRunMinutes`, `int? RunWalkWalkMinutes`
- [x] 1.6 Add new properties to `ScheduledWorkout`: `bool IsRunWalk` (default false), `int? RunMinutes`, `int? WalkMinutes`, `IntensityZone? ActualIntensityZone`
- [x] 1.7 Create EF Core migration for all new columns (all nullable or with defaults for backward compat)
- [x] 1.8 Apply migration and verify existing data is preserved

## 2. Volume Modes

- [x] 2.1 Create `VolumeModeExtensions` static class with `MaxRunDaysPerWeek(VolumeMode)` returning Minimal:3, Moderate:5, High:7, Elite:7 and `SupportsDoubles(VolumeMode)` returning true only for Elite
- [x] 2.2 Update `PlanGenerationEngine.Generate()` to read `VolumeMode` from UserProfile for max run days instead of `ExperienceLevel.MaxRunDaysPerWeek()`
- [x] 2.3 Update `PlanGenerationEngine.DistributeWorkouts()` to support Elite mode with AM/PM double-run days
- [x] 2.4 Update `ScheduleFlexibilityEngine.ReflowRemainingWeeks()` and `CompressWeek()` to use VolumeMode for day count
- [x] 2.5 Add `VolumeModeStep.razor` onboarding component with 4 options, descriptions, and recommended default based on experience level
- [x] 2.6 Add VolumeMode step to `Setup.razor` wizard between Experience Level and Pace Preference steps
- [x] 2.7 Add VolumeMode selector to `Settings.razor` with reflow on change

## 3. Age-Adjusted Training

- [x] 3.1 Add `GetAgeGroup(DateTime? dateOfBirth)` static method to `AgeGroup` enum computing bracket from birthdate
- [x] 3.2 Add `GetMinRecoveryDays(AgeGroup)` static method returning Under40:1, Forties:2, Fifties:2, SixtyPlus:3
- [x] 3.3 Update `PlanGenerationEngine.DistributeWorkouts()` to enforce minimum recovery days between hard sessions based on age group
- [x] 3.4 Update `PlanGenerationEngine` warm-up duration: Under50→10min, Fifties→12min, SixtyPlus→15min based on age group
- [x] 3.5 Add optional DateOfBirth field to `Settings.razor` displaying computed age bracket
- [x] 3.6 Add optional DateOfBirth field to onboarding (in the experience level step or a new step)

## 4. Run/Walk Intervals

- [x] 4.1 Update `PlanGenerationEngine.DistributeWorkouts()` to set `IsRunWalk=true`, `RunMinutes`, `WalkMinutes` on Easy and LongRun workouts when ExperienceLevel is Beginner
- [x] 4.2 Default run:walk ratio to 4:1, read custom ratio from UserProfile if set
- [x] 4.3 Implement run/walk progression: increase run portion by phase (Base→4:1, Build→6:1, Peak→8:1)
- [x] 4.4 Update `ScheduledWorkout.Description` computed property to include run/walk interval info (e.g., "Easy Run · 5km · Run 4min / Walk 1min")
- [x] 4.5 Add run/walk ratio settings to `Settings.razor` (only visible for Beginner experience level)

## 5. Adaptive Mileage Progression

- [x] 5.1 Create `AdaptiveProgressionCalculator` static class with `GetIncreaseRate(decimal peakWeeklyMiles)` returning 15% at <30mi, 7% at >50mi, interpolated between
- [x] 5.2 Refactor `PlanGenerationEngine.GenerateLongRunProgression()` to use adaptive rates from `AdaptiveProgressionCalculator` instead of fixed increment
- [x] 5.3 Implement equilibrium hold: after a mileage increase, hold for 3 weeks before next increase (integrate with existing deload pattern)
- [x] 5.4 Verify backward compatibility: existing ProgressionPattern enum values still work correctly with adaptive rates

## 6. Beginner Increase Cap

- [x] 6.1 Add post-processing step in `PlanGenerationEngine.Generate()` for Beginner plans that checks consecutive week long-run distances
- [x] 6.2 Enforce 50% max increase cap: if Week N+1 long run exceeds 150% of Week N, cap it at 150%
- [x] 6.3 Implement transition week insertion: when capped distance creates a gap to the target, insert intermediate weeks with progressive distances
- [x] 6.4 Update total plan week count when transition weeks are inserted

## 7. Single-Session Spike Guard

- [x] 7.1 Add `SpikeGuard` static class with `GetMaxSafeDistance(decimal[] recentDistances)` returning 110% of the max from the input array
- [x] 7.2 Integrate spike guard into `PlanGenerationEngine.GenerateLongRunProgression()` to cap any single run at 110% of the max in the preceding 4 plan weeks
- [x] 7.3 Add spike detection in `CalendarPage.razor` or `DailyView.razor`: compare upcoming workout distance against 110% of max completed run in last 30 days
- [x] 7.4 Display warning badge on workouts that exceed the spike threshold

## 8. ACWR Injury Risk Scoring

- [x] 8.1 Create `AcwrCalculator` static class with `Calculate(List<ScheduledWorkout> completedWorkouts)` returning ACWR ratio from last 7 days / avg of last 28 days
- [x] 8.2 Add `ClassifyRisk(decimal acwr)` method returning Green/Yellow/Red/UnderTraining classification
- [x] 8.3 Add `GetRiskMessage(AcwrRiskZone zone)` returning user-facing messages per spec
- [x] 8.4 Add ACWR badge component to `CalendarPage.razor` header showing current ACWR value and colored risk zone
- [x] 8.5 Handle insufficient data case: show "Building baseline" message when < 28 days of data

## 9. Intensity Tracking & Gray Zone Detection

- [x] 9.1 Create `IntensityTrackingService` with `GetDistribution(List<ScheduledWorkout> completed)` returning percentage breakdown of Easy/Threshold/Hard over last 4 weeks
- [x] 9.2 Add `MapEffortToZone(int? effort, WorkoutType type)` method for auto-mapping logged effort to IntensityZone
- [x] 9.3 Update `WorkoutLoggingService.QuickComplete()` to auto-assign ActualIntensityZone based on workout type
- [x] 9.4 Update `WorkoutLoggingService.LogWorkout()` to map ActualEffort to ActualIntensityZone
- [x] 9.5 Create `IntensityDashboard.razor` component showing 4-week rolling distribution with visual bars and 80/20 target comparison
- [x] 9.6 Add gray zone detection: when Threshold exceeds 20%, show coaching nudge
- [x] 9.7 Integrate IntensityDashboard into CalendarPage (e.g., in PlanOverviewView or as a collapsible panel)

## 10. Transition Time Calculator

- [x] 10.1 Add TransitionTimePreset selector to `Settings.razor` with 5 options and minute descriptions
- [x] 10.2 Add TransitionTimePreset to the schedule step in `Setup.razor` onboarding
- [x] 10.3 Update `PlanGenerationEngine.DistributeWorkouts()` to subtract transition time from available duration when calculating max workout duration
- [x] 10.4 Update `ScheduleFlexibilityEngine.ReflowRemainingWeeks()` to respect transition time when redistributing

## 11. Streak & Milestone Display

- [x] 11.1 Add `CalculateCurrentStreak(NewTrainingPlan plan)` method to `PlanStateService` counting consecutive completed scheduled workout days from today backward
- [x] 11.2 Add `CalculateBestStreak(NewTrainingPlan plan)` method scanning all completed workouts for longest consecutive run
- [x] 11.3 Add `GetTotalCompletedWorkouts(NewTrainingPlan plan)` method for milestone counting
- [x] 11.4 Add streak badge to `CalendarPage.razor` header calling `MessagingService.GetStreakMessage()` when streak ≥ 3
- [x] 11.5 Add best streak display as secondary text ("Best: X days") when current streak < best streak
- [x] 11.6 Add milestone toast/banner in `CalendarPage.razor` when total completed workouts hits 1, 10, 25, 50, or 100 using `MessagingService.GetMilestoneMessage()`

## 12. Integration Testing & Verification

- [x] 12.1 Generate a new Beginner plan and verify: run/walk intervals present, 50% increase cap enforced, adaptive progression applied, volume mode respected
- [x] 12.2 Generate a new plan for a 55-year-old user and verify: extended warm-ups (12 min), extra recovery days between hard sessions
- [x] 12.3 Generate a new Elite plan and verify: double-run days present, 7 runs/week, appropriate mileage
- [x] 12.4 Log 5+ weeks of workouts and verify: ACWR badge appears with correct zone, intensity dashboard shows distribution, streak badge displays
- [x] 12.5 Set transition time to GymShower and verify workouts have reduced durations
- [x] 12.6 Verify existing plans still load and function correctly after migration (backward compat)
