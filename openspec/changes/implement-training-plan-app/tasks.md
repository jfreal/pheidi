## 1. Core Data Models (Pheidi.Common/Models)

- [x] 1.1 Create RaceDistance enum (FiveK, TenK, HalfMarathon, FullMarathon) with distance conversion methods (km/miles)
- [x] 1.2 Create WorkoutType enum (Easy, Tempo, Intervals, LongRun, Recovery, Fartlek, HillRepeats, RacePace, CrossTraining, Strength, Rest)
- [x] 1.3 Create TrainingPhase enum (Base, Build, Peak, Taper)
- [x] 1.4 Create ExperienceLevel enum (Beginner, Intermediate, Advanced) with constraints (max run days, restrictions)
- [x] 1.5 Create ProgressionPattern enum (Linear, TwoUpOneDown, ThreeUpOneDown, FourUpOneDown)
- [x] 1.6 Create PacePreference enum (VDOT, RPE) and PaceZone model with VDOT zone definitions and RPE scale
- [x] 1.7 Create UserProfile model (ExperienceLevel, PacePreference, units, available days)
- [x] 1.8 Create RaceGoal model (RaceDistance, race date, optional target time)
- [x] 1.9 Create ScheduledWorkout model (date, WorkoutType, target distance/duration, PaceZone, warmup, cooldown, completion status, actual values, feedback)
- [x] 1.10 Create TrainingWeek model (week number, TrainingPhase, list of ScheduledWorkouts, total planned distance)
- [x] 1.11 Create updated TrainingPlan model (RaceGoal, UserProfile, list of TrainingWeeks, ProgressionPattern, plan status)
- [x] 1.12 Write unit tests for model creation, distance conversions, and TrainingWeek total distance calculation

## 2. Plan Generation Engine (Pheidi.Common/Engines)

- [x] 2.1 Create PlanGenerationEngine class with Generate(RaceGoal, UserProfile) method signature
- [x] 2.2 Implement plan duration calculation from race distance (default midpoints: 5K→10wk, 10K→12wk, Half→14wk, Full→18wk)
- [x] 2.3 Implement phase allocation algorithm (Base 25%, Build 40%, Peak 15%, Taper 20%, rounded to whole weeks)
- [x] 2.4 Implement Linear progression pattern for long run distances
- [x] 2.5 Implement 2-up-1-down, 3-up-1-down, and 4-up-1-down progression patterns
- [x] 2.6 Implement long run peak distance caps by race distance (5K→10mi, 10K→14mi, Half→14mi, Full→22mi)
- [x] 2.7 Implement weekly workout distribution — assign workout types to available days based on phase and experience
- [x] 2.8 Implement experience-level volume scaling (Beginner peak ≤35mi, Intermediate ≤45mi, Advanced ≤55mi for marathon)
- [x] 2.9 Implement taper algorithm (75% → 60% → 40% volume reduction)
- [x] 2.10 Implement warm-up/cool-down auto-assignment for quality workouts
- [x] 2.11 Implement backward date assignment from race date
- [x] 2.12 Enforce one active plan at a time (archive previous plan on new generation)
- [x] 2.13 Write unit tests for phase allocation, progression patterns, volume scaling, and taper

## 3. Pace Calculator Service (Pheidi.Common/Services)

- [x] 3.1 Create PaceCalculator service with VDOT-based pace zone calculation
- [x] 3.2 Implement RPE effort description mapping (1-10 scale to descriptive labels)
- [x] 3.3 Implement VDOT lookup from recent race time
- [x] 3.4 Write unit tests for pace calculations and VDOT lookup

## 4. Onboarding UI (Pheidi.Blazor/Components)

- [x] 4.1 Create OnboardingLayout component with stepped flow navigation (back/next, step indicators)
- [x] 4.2 Create RaceGoalStep component — distance selector, date picker, optional target time input
- [x] 4.3 Add date validation (minimum weeks check based on race distance)
- [x] 4.4 Create ExperienceLevelStep component — three cards with descriptions and constraint previews
- [x] 4.5 Create PacePreferenceStep component — VDOT vs RPE toggle with explanations, optional VDOT input
- [x] 4.6 Create AvailableDaysStep component — day-of-week checkboxes with minimum 3 validation, long run day suggestion
- [x] 4.7 Create PlanGenerationStep component — summary of choices, "Generate Plan" button, loading state
- [x] 4.8 Wire up PlanGenerationEngine to generate plan on button click and navigate to calendar

## 5. Calendar UI — Core Views (Pheidi.Blazor/Components)

- [x] 5.1 Create CalendarLayout component with view switcher (Monthly, Weekly, Daily, Overview) and month/week navigation
- [x] 5.2 Create MonthlyCalendarView — grid layout with day cells showing workout type icon, short description, target distance
- [x] 5.3 Implement today highlight and current day visual emphasis in monthly view
- [x] 5.4 Create WeeklyCalendarView — 7 day cards with full workout details (type, distance, pace, warmup/cooldown)
- [x] 5.5 Create DailyView — full workout prescription with warmup, main set, cooldown, pace guidance, coaching notes
- [x] 5.6 Create PlanOverview — condensed week-by-week view with phase color coding, weekly mileage, long run distances
- [x] 5.7 Implement workout type color coding (Easy→green, Tempo→orange, Intervals→red, LongRun→blue, etc.)
- [x] 5.8 Create WorkoutDetailPanel — expandable panel/modal for clicking on workouts in any view
- [x] 5.9 Create PlanStateService (scoped) to hold active plan state and propagate changes to components
- [x] 5.10 Update site.css with calendar styles, color coding, and responsive breakpoints

## 6. Schedule Flexibility (Pheidi.Common/Engines + Blazor UI)

- [x] 6.1 Create ScheduleFlexibilityEngine class with reflow logic
- [x] 6.2 Implement blocked day handling — redistribute workout to nearest available day in same week
- [x] 6.3 Implement available days change — reflow remaining weeks when user modifies available days
- [x] 6.4 Implement drag-and-drop workout swap within a week (Blazor UI + engine validation)
- [x] 6.5 Implement consecutive hard days prevention — warn when quality workouts are adjacent
- [x] 6.6 Implement taper lock enforcement — block volume additions during taper phase
- [x] 6.7 Implement vacation handling (paid feature) — multi-day blocks with volume reduction strategies
- [x] 6.8 Add UI for blocking days on the calendar (click to block/unblock)
- [x] 6.9 Write unit tests for reflow, blocked days, taper lock, and consecutive day detection

## 7. Workout Logging (Blazor UI + Pheidi.Common)

- [x] 7.1 Create WorkoutLoggingService for recording completed workouts
- [x] 7.2 Create QuickCompleteButton component — one-tap "Done" that logs planned values
- [x] 7.3 Create ManualEntryForm component — distance, time, effort (1-10) inputs, all optional
- [x] 7.4 Create PostWorkoutFeedback component — "How did that feel?" with 4 options (Too Easy, Just Right, Tough, Too Hard)
- [x] 7.5 Implement "Not Today" same-day rescheduling — move workout to available day or mark skipped
- [x] 7.6 Create CompletionTracker component — progress bar toward 90% target, completed/remaining counts
- [x] 7.7 Create EndOfPlanSummary component — total miles, completion %, longest run, phase breakdown
- [x] 7.8 Implement positive-only messaging for all completion states and missed workouts
- [x] 7.9 Write unit tests for completion percentage calculation and rescheduling logic

## 8. Injury System (Pheidi.Common + Blazor UI)

- [x] 8.1 Create InjuryEngine with dormant activation model
- [x] 8.2 Create InjuryReport model (body part, severity, date, status)
- [x] 8.3 Create ReportInjuryFlow component — body part selector, severity slider, guidance display
- [x] 8.4 Implement run/stop decision guidance based on severity thresholds (1-3, 4-6, 7-10)
- [x] 8.5 Implement workout modification logic — reduce distance, lower intensity, substitute cross-training based on severity
- [x] 8.6 Implement return-to-plan progression (50% → 70% → 85% → 100% over 4 weeks)
- [x] 8.7 Implement pain recurrence handling — drop back to previous volume level
- [x] 8.8 Add medical clearance prompt for severity 7+ or injuries lasting 14+ days
- [x] 8.9 Write unit tests for modification logic and return-to-plan progression

## 9. Coaching & Retention Features

- [x] 9.1 Create MessagingService with positive-only message templates for completion, misses, streaks, and milestones
- [x] 9.2 Create RacePredictionService — estimated finish time range based on logged workout data
- [x] 9.3 Implement reduced availability week compression — preserve long run and quality, drop easy runs first
- [x] 9.4 Create BaseBuildingEngine — optional 4-6 week pre-plan ramp for beginners not currently running
- [x] 9.5 Add base building option to onboarding flow for beginners
- [x] 9.6 Write unit tests for race prediction calculation and week compression

## 10. Auth & Persistence

- [x] 10.1 Add Entity Framework Core and SQLite NuGet packages to Pheidi.Blazor
- [x] 10.2 Create AppDbContext with DbSets for UserProfile, TrainingPlan, TrainingWeek, ScheduledWorkout, InjuryReport
- [x] 10.3 Create initial EF Core migration
- [x] 10.4 Implement OtpAuthService — generate 6-digit codes, email sending (console logger for dev), 10-min expiry, rate limiting
- [x] 10.5 Create SignIn page — email input, OTP code entry, session creation
- [x] 10.6 Configure auth middleware and secure cookie sessions (30-day expiry)
- [x] 10.7 Create UserService for profile CRUD operations
- [x] 10.8 Create PlanRepository for plan CRUD and active plan retrieval
- [x] 10.9 Create WorkoutRepository for workout log persistence
- [x] 10.10 Wire up all services to use persistence instead of in-memory state
- [x] 10.11 Implement monetization gate — check paid status before allowing vacation features

## 11. External Integrations

- [x] 11.1 Create ICalExportService — generate .ics files from training plan workouts
- [x] 11.2 Implement calendar subscription endpoint — user-specific token URL serving live iCal feed
- [x] 11.3 Create export UI — "Export to Calendar" button, copy subscribe URL
- [x] 11.4 Implement print-friendly plan layout with CSS @media print rules
- [x] 11.5 Implement PDF generation service for plan download
- [x] 11.6 Create calendar sharing — generate unique read-only plan URL
- [x] 11.7 Create shared plan view page (read-only, no personal data)

## 12. Polish & Integration Testing

- [x] 12.1 Update NavMenu with navigation to onboarding, calendar, and settings
- [x] 12.2 Update MainLayout for authenticated vs unauthenticated flows
- [x] 12.3 Add responsive design adjustments for mobile calendar views
- [x] 12.4 End-to-end smoke test: onboarding → plan generation → calendar view → log workout → completion tracking
- [x] 12.5 Review and update IconMapping.cs for new workout types and UI elements
