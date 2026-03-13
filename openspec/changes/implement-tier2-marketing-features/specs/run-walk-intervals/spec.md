## ADDED Requirements

### Requirement: Run/walk intervals for beginner plans
The system SHALL support run/walk intervals as a first-class workout structure for beginner-level plans using the Galloway method.

#### Scenario: Beginner Easy run with run/walk
- **WHEN** generating an Easy workout for a Beginner user
- **THEN** the workout includes run/walk intervals (e.g., run 4 min / walk 1 min) displayed in the workout description

#### Scenario: Beginner Long run with run/walk
- **WHEN** generating a Long Run workout for a Beginner user
- **THEN** the workout includes run/walk intervals with the same ratio as easy runs

#### Scenario: Non-beginner workouts
- **WHEN** generating workouts for Intermediate or Advanced users
- **THEN** the system does NOT add run/walk intervals

### Requirement: Configurable run/walk ratios
The system SHALL allow beginners to configure their run:walk ratio.

#### Scenario: Default ratio
- **WHEN** a beginner plan is generated without a custom ratio
- **THEN** the default run:walk ratio is 4 minutes run / 1 minute walk

#### Scenario: Custom ratio in settings
- **WHEN** a beginner user sets a custom run:walk ratio (e.g., 2:1 or 5:1) in Settings
- **THEN** all future run/walk workouts use that ratio

### Requirement: Run/walk progression
The system SHALL progressively increase the run portion of run/walk intervals as the plan advances.

#### Scenario: Base phase run/walk
- **WHEN** the plan is in the Base phase for a beginner
- **THEN** run/walk intervals use the configured ratio

#### Scenario: Build/Peak phase progression
- **WHEN** the plan advances to Build and Peak phases for a beginner
- **THEN** the run portion increases (e.g., 4:1 → 6:1 → 8:1) to build toward continuous running

### Requirement: Run/walk data stored on workout
The system SHALL store run/walk status and intervals on the ScheduledWorkout model.

#### Scenario: Run/walk workout properties
- **WHEN** a run/walk workout is created
- **THEN** the ScheduledWorkout has IsRunWalk=true, RunMinutes and WalkMinutes populated, and the Description reflects the interval structure
