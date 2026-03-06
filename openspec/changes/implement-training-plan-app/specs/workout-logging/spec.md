## ADDED Requirements

### Requirement: Quick-complete logging
The system SHALL provide a one-tap "Done" button to mark a workout as completed without entering details. Quick-complete SHALL record the workout as completed with planned values assumed.

#### Scenario: Quick-complete a workout
- **WHEN** a user taps "Done" on a 5-mile easy run
- **THEN** the system marks the workout as completed with actual distance = 5 miles (planned value) and no additional feedback required

### Requirement: Manual entry logging
The system SHALL allow users to manually log workout details: actual distance, actual duration, and perceived effort (1-10 scale). All fields SHALL be optional — partial logging is allowed.

#### Scenario: Log with full details
- **WHEN** a user logs a tempo run with distance 4.2 miles, time 32:15, and effort 7
- **THEN** the system stores all values and marks the workout as completed

#### Scenario: Log with distance only
- **WHEN** a user logs only the distance (5 miles) without time or effort
- **THEN** the system stores the distance, marks as completed, and leaves time and effort as null

### Requirement: Post-workout feedback
The system SHALL prompt users with "How did that feel?" after logging a workout. Options SHALL be: "Too Easy", "Just Right", "Tough", "Too Hard". The feedback SHALL inform future plan adjustments.

#### Scenario: Feedback indicates too hard
- **WHEN** a user selects "Too Hard" after a tempo run
- **THEN** the system stores the feedback and may reduce intensity in the next similar workout

#### Scenario: Feedback is optional
- **WHEN** a user dismisses the feedback prompt without selecting an option
- **THEN** the system records no feedback and does not penalize the user

### Requirement: Same-day rescheduling
The system SHALL provide a "Not Today" option that moves the current day's workout to another available day within the same week. If no days are available, the workout is marked as skipped.

#### Scenario: Move workout to later in the week
- **WHEN** a user selects "Not Today" on a Tuesday tempo run and Thursday is available
- **THEN** the system moves the tempo run to Thursday and swaps Thursday's original workout to Tuesday (or marks Tuesday as rest)

#### Scenario: No available days remaining
- **WHEN** a user selects "Not Today" on Friday and Saturday/Sunday are the only remaining days (both occupied)
- **THEN** the system marks the workout as skipped with a supportive message

### Requirement: Completion tracking toward 90% target
The system SHALL track workout completion percentage against the plan target of 90%. The completion dashboard SHALL display current percentage, completed workouts, and remaining workouts.

#### Scenario: View completion progress
- **WHEN** a user has completed 18 of 24 planned workouts (75%)
- **THEN** the system displays "75% complete — on track for your 90% goal" with a progress indicator

#### Scenario: Below 90% threshold warning
- **WHEN** a user's completion rate drops below 70% with significant plan time remaining
- **THEN** the system displays an encouraging message without shame (e.g., "Every run counts! Let's build momentum this week")

### Requirement: End-of-plan summary
The system SHALL generate a summary report when a training plan is completed or the race date passes. The report SHALL include: total miles logged, completion percentage, longest run, total training days, and phase-by-phase breakdown.

#### Scenario: Plan completion report
- **WHEN** a user's plan reaches the race date
- **THEN** the system generates and displays a summary with total miles, completion %, longest run achieved, and a congratulatory message

### Requirement: Positive-only completion messaging
The system SHALL use positive, encouraging language in all completion tracking and feedback. The system SHALL NEVER grade, rank, or shame users for missed workouts.

#### Scenario: Missed workout messaging
- **WHEN** a user misses a workout
- **THEN** the system does not display negative messages and instead focuses on the next upcoming workout
