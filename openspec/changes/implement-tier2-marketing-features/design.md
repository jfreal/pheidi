## Context

The Pheidi Blazor app is a .NET 9 Blazor Server application for generating personalized running training plans. The marketing website promises 12 features, but an audit found 10 algorithmic features that are either missing or incomplete. These features require no external APIs — they are pure model, engine, and UI changes within the existing architecture.

**Current architecture:**
- **Models** in `Pheidi.Common/Models/` — `UserProfile`, `ScheduledWorkout`, `TrainingWeek`, `NewTrainingPlan`, `RaceGoal`
- **Engines** in `Pheidi.Common/Engines/` — `PlanGenerationEngine`, `ScheduleFlexibilityEngine`, `InjuryEngine`, `BaseBuildingEngine`
- **Services** in `Pheidi.Common/Services/` and `Pheidi.Blazor/Services/` — `MessagingService`, `WorkoutLoggingService`, `PaceCalculator`, `PlanStateService`
- **UI** in `Pheidi.Blazor/Components/` — Razor pages for onboarding, calendar, settings
- **Database** — SQLite via EF Core with `AppDbContext`

**Key constraints:**
- `ExperienceLevel` enum (Beginner/Intermediate/Advanced) currently controls both skill level AND volume (max days/week, peak mileage)
- `UserProfile` has no age/birthdate field
- `ScheduledWorkout` has no intensity zone tracking or run/walk structure
- `PlanGenerationEngine` uses fixed increment progression, not adaptive rates
- `MessagingService` has streak/milestone logic that is never called from UI

## Goals / Non-Goals

**Goals:**
- Implement all 10 Tier 2 features so the app delivers on every marketing promise that doesn't require external integrations
- Maintain backward compatibility — existing plans and user profiles must continue to work after migration
- Keep changes within the existing architectural patterns (engines, services, models, Razor components)

**Non-Goals:**
- External API integrations (Strava, Garmin, Apple Health, weather APIs, Google Calendar write-back)
- Mobile widgets or native app features
- Redesigning the existing UI — new features integrate into existing pages
- Performance optimization or caching changes

## Decisions

### 1. Volume Mode as a separate concept from Experience Level

**Decision:** Add a new `VolumeMode` enum (Minimal, Moderate, High, Elite) to `UserProfile`, independent of `ExperienceLevel`.

**Rationale:** Experience level determines workout complexity (run/walk vs intervals vs doubles), while volume mode determines how many days/week. A beginner might run 3 or 5 days/week; an advanced runner might run 3 (FIRST method) or 7. These are orthogonal.

**Alternative considered:** Expanding ExperienceLevel to 12 combinations — rejected because it conflates two separate concerns and makes the onboarding UI confusing.

**Impact:** `ExperienceLevelExtensions.MaxRunDaysPerWeek()` will read from VolumeMode instead of ExperienceLevel. A new onboarding step for volume mode selection is needed.

### 2. Age stored as birthdate, brackets computed at runtime

**Decision:** Add `DateTime? DateOfBirth` to `UserProfile`. Age brackets are computed dynamically, not stored.

**Rationale:** Storing a bracket would become stale. Birthdate allows accurate age calculation at plan generation time and supports edge cases (turning 40 mid-plan).

**Impact:** New optional field in Settings and Onboarding. `PlanGenerationEngine` reads age to scale recovery and warm-ups.

### 3. Run/walk intervals as properties on ScheduledWorkout

**Decision:** Add `bool IsRunWalk`, `int? RunMinutes`, `int? WalkMinutes` to `ScheduledWorkout`.

**Rationale:** Run/walk is a modification of an existing workout type (Easy, LongRun), not a new WorkoutType enum value. A long run can be either continuous or run/walk. This avoids splitting types.

**Alternative considered:** New WorkoutType values (RunWalkEasy, RunWalkLong) — rejected because it doubles the type space and complicates all switch expressions.

### 4. Intensity tracking via logged workout data, not plan data

**Decision:** Track intensity zone on `ScheduledWorkout` (already has `PaceZone`) and compute 80/20 distribution from completed workouts. Add `ActualIntensityZone` (Easy/Threshold/Hard) to ScheduledWorkout for logging.

**Rationale:** Plan-level intensity is already determined by workout type (Easy=Zone1, Tempo=Zone2, Intervals=Zone3). The gap is tracking what the user ACTUALLY ran vs what was planned. A new service `IntensityTrackingService` computes rolling distribution.

### 5. ACWR computed from rolling 4-week and 1-week load windows

**Decision:** ACWR = (sum of last 7 days distance) / (average weekly distance over last 28 days). Computed on-the-fly from logged workouts, not stored.

**Rationale:** Storing ACWR would require recalculation on every workout log anyway. Computing from logged data keeps the model clean and always accurate.

### 6. Spike guard as a validation step in plan generation and display

**Decision:** `PlanGenerationEngine` enforces the 110% cap during generation. `CalendarPage` shows a warning badge on workouts that exceed 110% of the user's longest logged run in the past 30 days.

**Rationale:** Dual enforcement — generation prevents spikes in new plans, display catches spikes that emerge from schedule changes or reflowed plans.

### 7. Adaptive progression replaces fixed increment in PlanGenerationEngine

**Decision:** Replace the fixed `increment` calculation in `GenerateLongRunProgression()` with a volume-calibrated rate. Below 30 miles/week peak: up to 15% increase. Above 50: 7%. Between: linear interpolation. Add hold weeks per Jack Daniels' equilibrium method.

**Impact:** `GenerateLongRunProgression()` signature unchanged but internal logic changes. Deload weeks continue to work as before.

### 8. Transition time as an enum preset on UserProfile

**Decision:** Add `TransitionTimePreset` enum (None=0min, HomeShower=20min, GymShower=25min, LunchBreak=30min, GymWithCommute=35min) and `Dictionary<DayOfWeek, TransitionTimePreset>` per-day storage on UserProfile.

**Simplified approach:** For V1, use a single preset applied to all days (per-day override is a future enhancement). Store as a single `TransitionTimePreset` on UserProfile.

### 9. Streak/milestone wired into CalendarPage via PlanStateService

**Decision:** Add streak calculation to `PlanStateService` (consecutive completed workout days). Display in CalendarPage header area. Call existing `MessagingService` methods.

**Rationale:** PlanStateService already holds the active plan and is injected into CalendarPage. Adding streak computation here is natural.

### 10. Database migration strategy

**Decision:** Single EF Core migration adding all new columns with nullable defaults for backward compatibility.

New columns on UserProfile: `DateOfBirth` (DateTime?), `VolumeMode` (int, default 1=Moderate), `TransitionTimePreset` (int, default 0=None).

New columns on ScheduledWorkout: `IsRunWalk` (bool, default false), `RunMinutes` (int?), `WalkMinutes` (int?), `ActualIntensityZone` (int?).

All nullable or with defaults — no data loss, no required migration of existing rows.

## Risks / Trade-offs

- **[Risk] Existing plans may have suboptimal progression after adaptive rates change** → Mitigation: Only apply adaptive rates to newly generated plans. Existing plans keep their current progression.
- **[Risk] VolumeMode decoupled from ExperienceLevel may confuse users** → Mitigation: Provide sensible defaults (Beginner→Moderate, Intermediate→Moderate, Advanced→High) and clear descriptions in UI.
- **[Risk] ACWR requires sufficient logged data to be meaningful** → Mitigation: Only show ACWR after 4+ weeks of logged data. Display "Not enough data" until then.
- **[Risk] Run/walk intervals add complexity to pace/duration calculations** → Mitigation: Run/walk is only available for Beginner experience level and Easy/LongRun workout types. Advanced users never see it.
- **[Risk] Single migration with many columns** → Mitigation: All columns are nullable or have defaults. Migration is additive-only, no destructive changes. Rollback = remove migration.
