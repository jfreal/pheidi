## ADDED Requirements

### Requirement: User can set date of birth
The system SHALL allow users to optionally set their date of birth in their profile. The system SHALL compute age brackets dynamically from the birthdate.

#### Scenario: Setting birthdate in settings
- **WHEN** user enters their date of birth in Settings
- **THEN** the system stores the birthdate on UserProfile and displays the computed age bracket (Under 40, 40–49, 50–59, 60+)

#### Scenario: Setting birthdate during onboarding
- **WHEN** user reaches the profile step in onboarding
- **THEN** the system offers an optional date of birth field

#### Scenario: No birthdate provided
- **WHEN** user does not provide a date of birth
- **THEN** the system defaults to Under 40 behavior for all age-dependent calculations

### Requirement: Recovery windows scale by age bracket
The system SHALL scale recovery time between hard sessions based on the user's age bracket.

#### Scenario: Under 40 recovery
- **WHEN** generating a plan for a user under 40
- **THEN** the system allows hard sessions with 24–36 hours of recovery between them (minimum 1 rest/easy day)

#### Scenario: 40–49 recovery
- **WHEN** generating a plan for a user aged 40–49
- **THEN** the system enforces 36–48 hours of recovery between hard sessions (minimum 2 rest/easy days)

#### Scenario: 50–59 recovery
- **WHEN** generating a plan for a user aged 50–59
- **THEN** the system enforces 48–60 hours of recovery between hard sessions (minimum 2 rest/easy days)

#### Scenario: 60+ recovery
- **WHEN** generating a plan for a user aged 60+
- **THEN** the system enforces 60–72+ hours of recovery between hard sessions (minimum 3 rest/easy days)

### Requirement: Warm-up duration scales by age bracket
The system SHALL extend warm-up durations for older runners.

#### Scenario: Under 50 warm-up
- **WHEN** generating workouts for a user under 50
- **THEN** warm-up duration is 10 minutes (current default)

#### Scenario: 50–59 warm-up
- **WHEN** generating workouts for a user aged 50–59
- **THEN** warm-up duration is 12 minutes

#### Scenario: 60+ warm-up
- **WHEN** generating workouts for a user aged 60+
- **THEN** warm-up duration is 15 minutes

### Requirement: Non-standard training cycles for 60+ runners
The system SHALL support 9–10 day training cycles for runners aged 60+ to allow more recovery without reducing quality sessions.

#### Scenario: 60+ user with standard 7-day cycle
- **WHEN** a user aged 60+ generates a plan with default settings
- **THEN** the system suggests a 9-day training cycle with appropriate recovery spacing

#### Scenario: 60+ user opts to keep 7-day cycle
- **WHEN** a user aged 60+ explicitly selects a 7-day cycle
- **THEN** the system generates a standard 7-day plan with extended recovery between hard sessions
