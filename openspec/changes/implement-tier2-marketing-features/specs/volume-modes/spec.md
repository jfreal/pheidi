## ADDED Requirements

### Requirement: Four distinct volume modes
The system SHALL offer four volume modes independent of experience level: Minimal (3 runs/week), Moderate (4–5 runs/week), High (6–7 runs/week), Elite (doubles, 80+ km/week).

#### Scenario: Selecting Minimal mode
- **WHEN** user selects Minimal volume mode
- **THEN** the plan generates with exactly 3 runs per week (1 long, 1 tempo, 1 speed session per the FIRST/Furman method)

#### Scenario: Selecting Moderate mode
- **WHEN** user selects Moderate volume mode
- **THEN** the plan generates with 4–5 runs per week including easy runs for aerobic development

#### Scenario: Selecting High mode
- **WHEN** user selects High volume mode
- **THEN** the plan generates with 6–7 runs per week where every day has purpose

#### Scenario: Selecting Elite mode
- **WHEN** user selects Elite volume mode
- **THEN** the plan generates with double-run days (AM/PM scheduling) for runners at 80+ km/week

### Requirement: Volume mode selection during onboarding
The system SHALL present volume mode selection as a step in the onboarding wizard after experience level selection.

#### Scenario: Onboarding volume mode step
- **WHEN** user completes experience level selection
- **THEN** the system shows volume mode options with descriptions and a recommended default based on experience level (Beginner→Moderate, Intermediate→Moderate, Advanced→High)

### Requirement: Volume mode stored on user profile
The system SHALL persist the volume mode on UserProfile and allow changes in Settings.

#### Scenario: Changing volume mode in settings
- **WHEN** user changes volume mode in Settings with an active plan
- **THEN** the system reflows remaining weeks to match the new volume mode

### Requirement: Volume mode independent of experience level
The system SHALL allow any combination of experience level and volume mode.

#### Scenario: Beginner selects Minimal
- **WHEN** a beginner selects Minimal (3 runs/week)
- **THEN** the plan uses beginner workout types (easy, long run, one quality session) with 3 runs per week

#### Scenario: Advanced selects Minimal
- **WHEN** an advanced runner selects Minimal (3 runs/week)
- **THEN** the plan uses advanced workout types (intervals, tempo, long run with race-pace segments) with 3 runs per week
