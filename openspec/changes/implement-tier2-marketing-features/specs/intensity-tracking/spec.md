## ADDED Requirements

### Requirement: Track actual intensity zone per workout
The system SHALL record the actual intensity zone (Easy, Threshold, Hard) when a workout is logged.

#### Scenario: Quick-complete intensity
- **WHEN** user quick-completes a workout
- **THEN** the system auto-assigns the planned intensity zone based on workout type (Easy/Recovery/LongRun→Easy, Tempo/RacePace/Fartlek→Threshold, Intervals/HillRepeats→Hard)

#### Scenario: Manual log intensity
- **WHEN** user manually logs a workout with effort level
- **THEN** the system maps the effort to an intensity zone (RPE 1-4→Easy, RPE 5-6→Threshold, RPE 7-10→Hard)

### Requirement: Intensity distribution dashboard
The system SHALL display a rolling intensity distribution showing the percentage of Easy, Threshold, and Hard workouts over the last 4 weeks.

#### Scenario: Dashboard display
- **WHEN** user views the calendar with 4+ weeks of logged data
- **THEN** the system shows a percentage breakdown (e.g., "Easy 75% | Threshold 15% | Hard 10%") with a visual indicator comparing against the 80/20 target

#### Scenario: Insufficient data
- **WHEN** user has fewer than 4 weeks of logged workouts
- **THEN** the dashboard shows "Log more workouts to see your intensity balance"

### Requirement: Gray zone detection
The system SHALL detect when a user's training falls into the "gray zone" (excessive Threshold work) and provide coaching nudges.

#### Scenario: Gray zone warning
- **WHEN** Threshold zone exceeds 20% of total training volume over 4 weeks
- **THEN** the system displays a coaching nudge: "You're spending too much time in the gray zone. Try slowing down on easy days."

#### Scenario: Healthy distribution
- **WHEN** Easy zone is 75-85% and Hard zone is 10-20%
- **THEN** no coaching nudge is shown and the dashboard indicates the distribution is healthy
