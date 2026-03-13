## Why

The marketing website promises 12 core features, but an audit reveals that several algorithmic features are missing from the Blazor app. These are features that require no external API integrations — they are pure logic, models, and UI that can be built entirely within the existing codebase. Shipping the app without these features would undermine the credibility of the marketing claims and the product's core value proposition of being science-backed and schedule-first.

## What Changes

- **Age-adjusted training**: Add birthdate to user profile, implement age brackets (Under 40, 40–49, 50–59, 60+), scale recovery windows and warm-up durations by age, support non-standard 9–10 day training cycles for 60+ runners.
- **Volume modes**: Separate volume mode from experience level. Introduce 4 distinct modes (Minimal 3/week, Moderate 4–5/week, High 6–7/week, Elite with doubles) with independent selection during onboarding.
- **Run/walk intervals**: Add Galloway-method run/walk intervals as a first-class workout structure for beginner plans with configurable run:walk ratios.
- **Intensity tracking & gray zone detection**: Track actual workout intensity distribution (Easy/Threshold/Hard), compare against 80/20 targets, detect gray zone drift, and surface coaching nudges.
- **ACWR injury risk scoring**: Calculate acute:chronic workload ratio from logged data, classify into risk zones (green/yellow/red), and surface warnings when ratio enters danger territory.
- **Single-session spike guard**: Cap any individual scheduled run at 110% of the longest run in the past 30 days, with warnings on workouts that exceed the threshold.
- **Adaptive mileage progression**: Replace fixed increments with volume-calibrated rates (up to 15% at low volume, down to 7% at high volume) and implement Jack Daniels' equilibrium method (increase, hold 3–4 weeks, repeat).
- **Beginner increase cap**: Enforce a 50% max increase in continuous running duration between any two weeks for beginner plans, auto-inserting transition weeks when gaps are too large.
- **Transition time calculator**: Add 5 presets for commute/shower overhead (10–35 min) that factor into available training time per day.
- **Streak & milestone display**: Wire existing `MessagingService` streak/milestone logic into the UI so users actually see their streaks and celebrations.

## Capabilities

### New Capabilities
- `age-adjusted-training`: Age bracket detection, recovery scaling, warm-up scaling, and 60+ non-standard cycles
- `volume-modes`: Four distinct volume modes independent of experience level, including Elite doubles
- `run-walk-intervals`: Galloway-method run/walk workout type with configurable ratios
- `intensity-tracking`: 80/20 intensity distribution tracking, dashboard, and gray zone detection
- `acwr-risk-scoring`: Acute:chronic workload ratio calculation and risk zone classification
- `spike-guard`: Single-session spike detection and 110% cap enforcement
- `adaptive-progression`: Volume-calibrated mileage increase rates and equilibrium hold periods
- `beginner-increase-cap`: 50% max continuous running duration increase with auto transition weeks
- `transition-time-calculator`: Shower/commute time presets factored into available training time
- `streak-milestone-display`: UI integration for streak tracking and milestone celebrations

### Modified Capabilities
<!-- No existing specs to modify -->

## Impact

- **Models**: `UserProfile` (add birthdate, volume mode, transition time preset), `ScheduledWorkout` (add run/walk structure, intensity zone), new `VolumeMode` enum, new `TransitionTimePreset` enum, new `AgeGroup` enum
- **Engines**: `PlanGenerationEngine` (adaptive progression, beginner caps, volume modes, run/walk, age adjustments), `ScheduleFlexibilityEngine` (spike guard, transition time), `InjuryEngine` (ACWR scoring)
- **Services**: `WorkoutLoggingService` (intensity tracking, ACWR data collection), `MessagingService` (wire streak/milestone display), new `IntensityTrackingService`
- **Components**: `Settings.razor` (age, volume mode, transition time), onboarding steps (volume mode selection), `CalendarPage.razor` (streak display, ACWR badge, spike warnings), new `IntensityDashboard.razor`
- **Database**: New EF Core migration for schema changes (birthdate, volume mode, transition preset columns)
