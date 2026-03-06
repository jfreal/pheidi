## 1. Core Data Models (Pheidi.Common/Models)

- [ ] 1.1 Create RaceDistance enum (FiveK, TenK, HalfMarathon, FullMarathon) with distance conversion methods (km/miles)
- [ ] 1.2 Create WorkoutType enum (Easy, Tempo, Intervals, LongRun, Recovery, Fartlek, HillRepeats, RacePace, CrossTraining, Strength, Rest)
- [ ] 1.3 Create TrainingPhase enum (Base, Build, Peak, Taper)
- [ ] 1.4 Create ExperienceLevel enum (Beginner, Intermediate, Advanced) with constraints (max run days, restrictions)
- [ ] 1.5 Create ProgressionPattern enum (Linear, TwoUpOneDown, ThreeUpOneDown, FourUpOneDown)
- [ ] 1.6 Create PacePreference enum (VDOT, RPE) and PaceZone model with VDOT zone definitions and RPE scale
- [ ] 1.7 Create UserProfile model (ExperienceLevel, PacePreference, units, available days)
- [ ] 1.8 Create RaceGoal model (RaceDistance, race date, optional target time)
- [ ] 1.9 Create ScheduledWorkout model (date, WorkoutType, target distance/duration, PaceZone, warmup, cooldown, completion status, actual values, feedback)
- [ ] 1.10 Create TrainingWeek model (week number, TrainingPhase, list of ScheduledWorkouts, total planned distance)
- [ ] 1.11 Create updated TrainingPlan model (RaceGoal, UserProfile, list of TrainingWeeks, ProgressionPattern, plan status)
- [ ] 1.12 Write unit tests for model creation, distance conversions, and TrainingWeek total distance calculation

## 2. Plan Generation Engine (Pheidi.Common/Engines)

- [ ] 2.1 Create PlanGenerationEngine class with Generate(RaceGoal, UserProfile) method signature
- [ ] 2.2 Implement plan duration calculation from race distance (default midpoints: 5K→10wk, 10K→12wk, Half→14wk, Full→18wk)
- [ ] 2.3 Implement phase allocation algorithm (Base 25%, Build 40%, Peak 15%, Taper 20%, rounded to whole weeks)
- [ ] 2.4 Implement Linear progression pattern for long run distances
- [ ] 2.5 Implement 2-up-1-down, 3-up-1-down, and 4-up-1-down progression patterns
- [ ] 2.6 Implement long run peak distance caps by race distance (5K→10mi, 10K→14mi, Half→14mi, Full→22mi)
- [ ] 2.7 Implement weekly workout distribution — assign workout types to available days based on phase and experience
- [ ] 2.8 Implement experience-level volume scaling (Beginner peak ≤35mi, Intermediate ≤45mi, Advanced ≤55mi for marathon)
- [ ] 2.9 Implement taper algorithm (75% → 60% → 40% volume reduction)
- [ ] 2.10 Implement warm-up/cool-down auto-assignment for quality workouts
- [ ] 2.11 Implement backward date assignment from race date
- [ ] 2.12 Enforce one active plan at a time (archive previous plan on new generation)
- [ ] 2.13 Write unit tests for phase allocation, progression patterns, volume scaling, and taper

## 3. Pace Calculator Service (Pheidi.Common/Services)

- [ ] 3.1 Create PaceCalculator service with VDOT-based pace zone calculation
- [ ] 3.2 Implement RPE effort description mapping (1-10 scale to descriptive labels)
- [ ] 3.3 Implement VDOT lookup from recent race time
- [ ] 3.4 Write unit tests for pace calculations and VDOT lookup

## 4. Onboarding UI (Pheidi.Blazor/Components)

- [ ] 4.1 Create OnboardingLayout component with stepped flow navigation (back/next, step indicators)
- [ ] 4.2 Create RaceGoalStep component — distance selector, date picker, optional target time input
- [ ] 4.3 Add date validation (minimum weeks check based on race distance)
- [ ] 4.4 Create ExperienceLevelStep component — three cards with descriptions and constraint previews
- [ ] 4.5 Create PacePreferenceStep component — VDOT vs RPE toggle with explanations, optional VDOT input
- [ ] 4.6 Create AvailableDaysStep component — day-of-week checkboxes with minimum 3 validation, long run day suggestion
- [ ] 4.7 Create PlanGenerationStep component — summary of choices, "Generate Plan" button, loading state
- [ ] 4.8 Wire up PlanGenerationEngine to generate plan on button click and navigate to calendar

## 5. Calendar UI — Core Views (Pheidi.Blazor/Components)

- [ ] 5.1 Create CalendarLayout component with view switcher (Monthly, Weekly, Daily, Overview) and month/week navigation
- [ ] 5.2 Create MonthlyCalendarView — grid layout with day cells showing workout type icon, short description, target distance
- [ ] 5.3 Implement today highlight and current day visual emphasis in monthly view
- [ ] 5.4 Create WeeklyCalendarView — 7 day cards with full workout details (type, distance, pace, warmup/cooldown)
- [ ] 5.5 Create DailyView — full workout prescription with warmup, main set, cooldown, pace guidance, coaching notes
- [ ] 5.6 Create PlanOverview — condensed week-by-week view with phase color coding, weekly mileage, long run distances
- [ ] 5.7 Implement workout type color coding (Easy→green, Tempo→orange, Intervals→red, LongRun→blue, etc.)
- [ ] 5.8 Create WorkoutDetailPanel — expandable panel/modal for clicking on workouts in any view
- [ ] 5.9 Create PlanStateService (scoped) to hold active plan state and propagate changes to components
- [ ] 5.10 Update site.css with calendar styles, color coding, and responsive breakpoints

## 6. Schedule Flexibility (Pheidi.Common/Engines + Blazor UI)

- [ ] 6.1 Create ScheduleFlexibilityEngine class with reflow logic
- [ ] 6.2 Implement blocked day handling — redistribute workout to nearest available day in same week
- [ ] 6.3 Implement available days change — reflow remaining weeks when user modifies available days
- [ ] 6.4 Implement drag-and-drop workout swap within a week (Blazor UI + engine validation)
- [ ] 6.5 Implement consecutive hard days prevention — warn when quality workouts are adjacent
- [ ] 6.6 Implement taper lock enforcement — block volume additions during taper phase
- [ ] 6.7 Implement vacation handling (paid feature) — multi-day blocks with volume reduction strategies
- [ ] 6.8 Add UI for blocking days on the calendar (click to block/unblock)
- [ ] 6.9 Write unit tests for reflow, blocked days, taper lock, and consecutive day detection

## 7. Workout Logging (Blazor UI + Pheidi.Common)

- [ ] 7.1 Create WorkoutLoggingService for recording completed workouts
- [ ] 7.2 Create QuickCompleteButton component — one-tap "Done" that logs planned values
- [ ] 7.3 Create ManualEntryForm component — distance, time, effort (1-10) inputs, all optional
- [ ] 7.4 Create PostWorkoutFeedback component — "How did that feel?" with 4 options (Too Easy, Just Right, Tough, Too Hard)
- [ ] 7.5 Implement "Not Today" same-day rescheduling — move workout to available day or mark skipped
- [ ] 7.6 Create CompletionTracker component — progress bar toward 90% target, completed/remaining counts
- [ ] 7.7 Create EndOfPlanSummary component — total miles, completion %, longest run, phase breakdown
- [ ] 7.8 Implement positive-only messaging for all completion states and missed workouts
- [ ] 7.9 Write unit tests for completion percentage calculation and rescheduling logic

## 8. Injury System (Pheidi.Common + Blazor UI)

- [ ] 8.1 Create InjuryEngine with dormant activation model
- [ ] 8.2 Create InjuryReport model (body part, severity, date, status)
- [ ] 8.3 Create ReportInjuryFlow component — body part selector, severity slider, guidance display
- [ ] 8.4 Implement run/stop decision guidance based on severity thresholds (1-3, 4-6, 7-10)
- [ ] 8.5 Implement workout modification logic — reduce distance, lower intensity, substitute cross-training based on severity
- [ ] 8.6 Implement return-to-plan progression (50% → 70% → 85% → 100% over 4 weeks)
- [ ] 8.7 Implement pain recurrence handling — drop back to previous volume level
- [ ] 8.8 Add medical clearance prompt for severity 7+ or injuries lasting 14+ days
- [ ] 8.9 Write unit tests for modification logic and return-to-plan progression

## 9. Coaching & Retention Features

- [ ] 9.1 Create MessagingService with positive-only message templates for completion, misses, streaks, and milestones
- [ ] 9.2 Create RacePredictionService — estimated finish time range based on logged workout data
- [ ] 9.3 Implement reduced availability week compression — preserve long run and quality, drop easy runs first
- [ ] 9.4 Create BaseBuildingEngine — optional 4-6 week pre-plan ramp for beginners not currently running
- [ ] 9.5 Add base building option to onboarding flow for beginners
- [ ] 9.6 Write unit tests for race prediction calculation and week compression

## 10. Auth & Persistence

- [ ] 10.1 Add Entity Framework Core and SQLite NuGet packages to Pheidi.Blazor
- [ ] 10.2 Create AppDbContext with DbSets for UserProfile, TrainingPlan, TrainingWeek, ScheduledWorkout, InjuryReport
- [ ] 10.3 Create initial EF Core migration
- [ ] 10.4 Implement OtpAuthService — generate 6-digit codes, email sending (console logger for dev), 10-min expiry, rate limiting
- [ ] 10.5 Create SignIn page — email input, OTP code entry, session creation
- [ ] 10.6 Configure auth middleware and secure cookie sessions (30-day expiry)
- [ ] 10.7 Create UserService for profile CRUD operations
- [ ] 10.8 Create PlanRepository for plan CRUD and active plan retrieval
- [ ] 10.9 Create WorkoutRepository for workout log persistence
- [ ] 10.10 Wire up all services to use persistence instead of in-memory state
- [ ] 10.11 Implement monetization gate — check paid status before allowing vacation features

## 11. External Integrations

- [ ] 11.1 Create ICalExportService — generate .ics files from training plan workouts
- [ ] 11.2 Implement calendar subscription endpoint — user-specific token URL serving live iCal feed
- [ ] 11.3 Create export UI — "Export to Calendar" button, copy subscribe URL
- [ ] 11.4 Implement print-friendly plan layout with CSS @media print rules
- [ ] 11.5 Implement PDF generation service for plan download
- [ ] 11.6 Create calendar sharing — generate unique read-only plan URL
- [ ] 11.7 Create shared plan view page (read-only, no personal data)

## 12. Polish & Integration Testing

- [ ] 12.1 Update NavMenu with navigation to onboarding, calendar, and settings
- [ ] 12.2 Update MainLayout for authenticated vs unauthenticated flows
- [ ] 12.3 Add responsive design adjustments for mobile calendar views
- [ ] 12.4 End-to-end smoke test: onboarding → plan generation → calendar view → log workout → completion tracking
- [ ] 12.5 Review and update IconMapping.cs for new workout types and UI elements
