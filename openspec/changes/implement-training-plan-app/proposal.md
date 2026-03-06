## Why

The Pheidi app has a comprehensive product specification (.research/spec/) for a personalized running training plan generator, but the current implementation is minimal — basic linear progression with a simple table UI. The core value proposition (a running plan calendar that works around people's schedules) is unbuilt. This change implements the full app from spec to working product inside Pheidi.Blazor.

## What Changes

- **Expand data models** in Pheidi.Common to support the full workout taxonomy, pace zones, periodization, user profiles, and race goals
- **Rewrite the plan generation engine** with proper periodization (Base/Build/Peak/Taper), multiple progression patterns, experience-level scaling, and workout type distribution
- **Build onboarding UI** for race goal setup, experience assessment, pace preference, and schedule configuration
- **Implement calendar views** (monthly, weekly, daily, plan overview) as the primary app interface with color coding and workout detail panels
- **Add schedule flexibility** — blocked days, vacations, drag-and-drop swaps, mid-plan changes, taper lock enforcement
- **Build workout logging** — quick-complete, manual entry, post-workout feedback, completion tracking, end-of-plan summary
- **Implement injury system** — dormant by default, activates on user report with pain tracking and workout modifications
- **Add coaching and retention features** — positive-only messaging, race time prediction, reduced availability compression
- **Implement auth and persistence** — passwordless OTP, database layer, API endpoints, monetization gate for holiday/vacation handling
- **Add external integrations** — calendar sync (Google/Apple/Outlook via iCal), print/PDF export, calendar sharing

## Capabilities

### New Capabilities
- `data-models`: Core domain models — user profile, race goal, workout taxonomy, pace zones, periodization phases, ScheduledWorkout, TrainingWeek
- `plan-generation`: Training plan engine — progression patterns, phase allocation, experience scaling, workout distribution, taper algorithm
- `onboarding-ui`: User setup flow — race goal, experience level, pace preference, available days, plan generation trigger
- `calendar-ui`: Calendar views (monthly/weekly/daily/overview), color coding, today highlight, workout detail panels
- `schedule-flexibility`: Schedule adaptation — blocked days, vacations, drag-and-drop swap, mid-plan changes, taper lock
- `workout-logging`: Workout tracking — quick-complete, manual entry, post-workout feedback, completion tracking, end-of-plan summary
- `injury-system`: Injury management — dormant activation, pain tracking, run/stop decisions, workout modifications, return-to-plan
- `coaching-retention`: Motivation engine — positive messaging, race prediction, availability compression, base building
- `auth-persistence`: Authentication and storage — passwordless OTP, database layer, API endpoints
- `external-integrations`: Calendar sync, print/PDF export, calendar sharing

### Modified Capabilities
<!-- No existing capabilities to modify — this is a greenfield implementation -->

## Impact

- **Pheidi.Common**: Major expansion of models and business logic (new files + rewrite of TrainingPlan.cs)
- **Pheidi.Blazor**: New pages, components, layouts, and services throughout
- **Pheidi.Common.Tests**: Significant new test coverage for plan generation and schedule flexibility
- **Dependencies**: Will need to add packages for auth (OTP), database (EF Core or similar), PDF generation, and iCal export
- **Breaking**: Existing `TrainingPlan`, `Week`, `DayConfig` models will be replaced with richer equivalents — **BREAKING** for any code depending on current shapes
