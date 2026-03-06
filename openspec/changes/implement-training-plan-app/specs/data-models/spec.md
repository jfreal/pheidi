## ADDED Requirements

### Requirement: User profile model
The system SHALL maintain a UserProfile with experience level (Beginner, Intermediate, Advanced), pace preference (VDOT or RPE), preferred units (miles or kilometers), and available training days per week.

#### Scenario: Create user profile with defaults
- **WHEN** a new user is created
- **THEN** the system creates a UserProfile with default values: Beginner experience, RPE pace preference, miles units, and no available days configured

#### Scenario: Update pace preference
- **WHEN** a user changes their pace preference from RPE to VDOT
- **THEN** the system updates the UserProfile and all displayed pace guidance switches to VDOT-based values

### Requirement: Race goal model
The system SHALL support race goals with distance type (5K, 10K, Half Marathon, Full Marathon), race date, and optional target finish time.

#### Scenario: Create race goal for half marathon
- **WHEN** a user sets a race goal with distance "Half Marathon", date "2026-09-15", and target time "1:45:00"
- **THEN** the system stores a RaceGoal with DistanceType.HalfMarathon, the specified date, and target TimeSpan of 1:45:00

#### Scenario: Race goal without target time
- **WHEN** a user sets a race goal without specifying a target finish time
- **THEN** the system stores the RaceGoal with a null target time and the plan generation engine uses experience-based defaults

### Requirement: Workout type taxonomy
The system SHALL define workout types: Easy, Tempo, Intervals, LongRun, Recovery, Fartlek, HillRepeats, RacePace, CrossTraining, Strength, and Rest.

#### Scenario: Workout type has pace zone association
- **WHEN** the system creates a workout of type Tempo
- **THEN** the workout is associated with the Tempo pace zone (VDOT Zone 3 or RPE 7-8)

#### Scenario: Rest type has no distance or pace
- **WHEN** the system creates a workout of type Rest
- **THEN** the workout has zero distance, zero duration, and no pace zone

### Requirement: Pace zone definitions
The system SHALL define pace zones for both VDOT and RPE modes. VDOT zones SHALL include Easy, Marathon, Tempo, Interval, and Repetition. RPE zones SHALL use a 1-10 scale with descriptive labels.

#### Scenario: VDOT pace zone calculation
- **WHEN** a user has VDOT value of 45
- **THEN** the system calculates specific pace ranges (min/km or min/mile) for each VDOT zone

#### Scenario: RPE zone display
- **WHEN** a user is in RPE mode
- **THEN** workout pace guidance displays effort descriptions (e.g., "Conversational pace", "Comfortably hard") instead of pace numbers

### Requirement: Periodization phase model
The system SHALL define training phases: Base, Build, Peak, and Taper. Each TrainingWeek SHALL be assigned to exactly one phase.

#### Scenario: Phase assignment for 16-week marathon plan
- **WHEN** a 16-week marathon plan is generated
- **THEN** weeks are allocated across phases with Base (~4 weeks), Build (~6 weeks), Peak (~3 weeks), and Taper (~3 weeks)

### Requirement: ScheduledWorkout model
The system SHALL represent each planned workout as a ScheduledWorkout with: date, workout type, target distance or duration, pace zone, warm-up specification, cool-down specification, and completion status.

#### Scenario: ScheduledWorkout with warmup and cooldown
- **WHEN** a Tempo workout is scheduled
- **THEN** the ScheduledWorkout includes a warm-up (e.g., 10 min easy), main set (e.g., 4 miles at tempo pace), and cool-down (e.g., 10 min easy)

#### Scenario: ScheduledWorkout completion tracking
- **WHEN** a ScheduledWorkout is created
- **THEN** it has a completion status of Pending, with fields for actual distance, actual duration, and effort feedback

### Requirement: TrainingWeek model
The system SHALL represent each training week with: week number, phase assignment, list of ScheduledWorkouts (one per day), total planned distance, and weekly volume target.

#### Scenario: TrainingWeek contains 7 days
- **WHEN** a TrainingWeek is created
- **THEN** it contains exactly 7 ScheduledWorkout slots (Monday through Sunday), some of which may be Rest days

#### Scenario: TrainingWeek calculates total distance
- **WHEN** a TrainingWeek contains workouts with distances 3, 5, 0, 4, 0, 12, 0 miles
- **THEN** the TotalPlannedDistance property returns 24 miles

### Requirement: Distance type enumeration
The system SHALL support race distances: FiveK, TenK, HalfMarathon, and FullMarathon with their metric equivalents (5.0km, 10.0km, 21.1km, 42.2km).

#### Scenario: Distance conversion to miles
- **WHEN** the system needs the mile equivalent of HalfMarathon
- **THEN** it returns 13.1 miles

### Requirement: Experience level definitions
The system SHALL define experience levels with associated constraints: Beginner (max 3-4 run days/week, no intervals in first 4 weeks), Intermediate (4-5 run days/week, full workout variety), Advanced (5-6 run days/week, higher intensity allowance).

#### Scenario: Beginner constraints applied
- **WHEN** a plan is generated for a Beginner
- **THEN** the plan limits run days to a maximum of 4 per week and excludes Interval workouts from the first 4 weeks
