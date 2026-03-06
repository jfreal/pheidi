## ADDED Requirements

### Requirement: Plan duration by race distance
The system SHALL support configurable plan durations: 5K (8-12 weeks), 10K (10-14 weeks), Half Marathon (12-16 weeks), Full Marathon (16-20 weeks). The default duration SHALL be the midpoint of each range.

#### Scenario: Default marathon plan duration
- **WHEN** a user creates a Full Marathon plan without specifying duration
- **THEN** the system generates an 18-week plan

#### Scenario: Custom duration within range
- **WHEN** a user creates a Half Marathon plan with 14 weeks specified
- **THEN** the system generates a 14-week plan

#### Scenario: Duration outside valid range
- **WHEN** a user attempts to create a 5K plan with 20 weeks
- **THEN** the system rejects the request and indicates the valid range is 8-12 weeks

### Requirement: Periodization phase allocation
The system SHALL allocate plan weeks into Base, Build, Peak, and Taper phases. Base phase SHALL be approximately 25% of total weeks, Build approximately 40%, Peak approximately 15%, and Taper approximately 20%. The system SHALL round to whole weeks.

#### Scenario: 16-week plan phase allocation
- **WHEN** a 16-week Full Marathon plan is generated
- **THEN** phases are allocated as: Base (4 weeks), Build (6 weeks), Peak (3 weeks), Taper (3 weeks)

#### Scenario: 10-week plan phase allocation
- **WHEN** a 10-week 10K plan is generated
- **THEN** phases are allocated as: Base (3 weeks), Build (4 weeks), Peak (1 week), Taper (2 weeks)

### Requirement: Progression patterns
The system SHALL support four long run progression patterns: Linear (increase every week), 2-up-1-down (increase 2 weeks then reduce 1), 3-up-1-down (increase 3 weeks then reduce 1), and 4-up-1-down (increase 4 weeks then reduce 1). The default pattern SHALL be 3-up-1-down.

#### Scenario: 3-up-1-down pattern
- **WHEN** a plan uses 3-up-1-down progression starting at 8 miles with 1-mile increments
- **THEN** the long run distances follow: 8, 9, 10, 8, 9, 10, 11, 9... (3 increases then drop back)

#### Scenario: Linear pattern
- **WHEN** a plan uses Linear progression starting at 6 miles with 1-mile increments
- **THEN** the long run distances follow: 6, 7, 8, 9, 10... (continuous increase)

### Requirement: Weekly workout distribution
The system SHALL distribute workouts across available days based on training phase and experience level. Each week SHALL include at most one long run, at most one quality session (tempo/intervals), and the remainder as easy/recovery runs.

#### Scenario: Beginner base phase week
- **WHEN** a Beginner plan generates a Base phase week with 4 available days
- **THEN** the week contains: 1 long run, 2 easy runs, 1 rest or cross-training day

#### Scenario: Advanced build phase week
- **WHEN** an Advanced plan generates a Build phase week with 6 available days
- **THEN** the week contains: 1 long run, 1 tempo or interval session, 3 easy/recovery runs, 1 rest day

### Requirement: Long run distance calculation
The system SHALL calculate long run peak distances based on race distance: 5K (8-10 miles max), 10K (12-14 miles max), Half Marathon (12-14 miles max), Full Marathon (20-22 miles max). Long runs SHALL not exceed the race distance for distances shorter than half marathon.

#### Scenario: Marathon long run peak
- **WHEN** a Full Marathon plan reaches its Peak phase
- **THEN** the longest long run is between 20 and 22 miles

#### Scenario: 5K long run cap
- **WHEN** a 5K plan reaches its Peak phase
- **THEN** the longest long run does not exceed 10 miles

### Requirement: Taper algorithm
The system SHALL reduce training volume during the Taper phase. Volume reduction SHALL follow: first taper week reduces to 75% of peak volume, second week to 60%, final week (race week) to 40%. Long run distance SHALL also decrease proportionally.

#### Scenario: Three-week taper for marathon
- **WHEN** a marathon plan enters its 3-week Taper phase with peak weekly volume of 40 miles
- **THEN** taper weeks have volumes of approximately 30, 24, and 16 miles

#### Scenario: Taper lock prevents volume addition
- **WHEN** a user attempts to add a workout during a taper week
- **THEN** the system prevents the addition and displays a message explaining taper lock

### Requirement: Experience-level volume scaling
The system SHALL scale weekly volume based on experience level. Beginner plans SHALL have lower peak weekly mileage than Intermediate, which SHALL be lower than Advanced. The system SHALL use minimum run distance of 2 miles for all levels.

#### Scenario: Beginner marathon peak volume
- **WHEN** a Beginner Full Marathon plan reaches peak volume
- **THEN** peak weekly mileage does not exceed 35 miles

#### Scenario: Advanced marathon peak volume
- **WHEN** an Advanced Full Marathon plan reaches peak volume
- **THEN** peak weekly mileage can reach up to 55 miles

### Requirement: Warm-up and cool-down auto-assignment
The system SHALL automatically add warm-up and cool-down segments to quality workouts (Tempo, Intervals, HillRepeats, RacePace). Easy and Recovery runs SHALL not include separate warm-up/cool-down. Long runs SHALL include a cool-down walk segment only.

#### Scenario: Tempo workout warm-up
- **WHEN** a Tempo workout is generated
- **THEN** it includes a 10-minute easy warm-up jog and a 10-minute easy cool-down jog

#### Scenario: Easy run has no separate warm-up
- **WHEN** an Easy run workout is generated
- **THEN** it has no separate warm-up or cool-down segments

### Requirement: Plan generation from race goal
The system SHALL generate a complete training plan when given a race goal (distance, date, optional target time) and a user profile (experience level, available days, pace preference). The plan SHALL work backward from the race date.

#### Scenario: Generate plan from race goal
- **WHEN** a user with Intermediate experience, 5 available days, and a Full Marathon goal on 2026-10-15 triggers plan generation
- **THEN** the system creates an 18-week plan ending on 2026-10-15 with properly phased weeks and distributed workouts

#### Scenario: One active plan at a time
- **WHEN** a user already has an active plan and generates a new one
- **THEN** the previous plan is archived and the new plan becomes the sole active plan
