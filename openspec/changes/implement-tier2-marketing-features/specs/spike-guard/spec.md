## ADDED Requirements

### Requirement: Single-session spike guard during plan generation
The system SHALL cap any individual run at 110% of the longest run in the plan's preceding 4 weeks during plan generation.

#### Scenario: Spike detected during generation
- **WHEN** the plan generation engine creates a workout whose distance exceeds 110% of the longest run in the previous 4 weeks of the plan
- **THEN** the system caps the workout distance at 110% of that longest run

#### Scenario: First 4 weeks of plan
- **WHEN** generating workouts in the first 4 weeks of a plan (no prior history)
- **THEN** the spike guard uses the plan's starting base distance as the reference

### Requirement: Spike warning on scheduled workouts
The system SHALL display a warning badge on any upcoming workout that exceeds 110% of the user's longest logged run in the past 30 days.

#### Scenario: Workout exceeds 110% of recent max
- **WHEN** user views a scheduled workout whose target distance exceeds 110% of their longest completed run in the past 30 days
- **THEN** the system shows a warning icon with "This run is a big jump from your recent training. Consider taking it easy."

#### Scenario: Workout within safe range
- **WHEN** a scheduled workout is within 110% of the user's longest completed run in the past 30 days
- **THEN** no warning is shown

#### Scenario: No logged runs
- **WHEN** user has no completed runs in the past 30 days
- **THEN** the spike guard uses the plan's planned distances as the reference (no warning shown)
