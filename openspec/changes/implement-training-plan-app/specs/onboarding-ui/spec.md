## ADDED Requirements

### Requirement: Race goal selection page
The system SHALL provide a page where users select their race distance (5K, 10K, Half Marathon, Full Marathon), enter their race date, and optionally enter a target finish time.

#### Scenario: Select half marathon with date
- **WHEN** a user selects "Half Marathon" and enters race date "2026-09-15"
- **THEN** the system stores the race goal and enables navigation to the next onboarding step

#### Scenario: Date validation
- **WHEN** a user enters a race date less than 8 weeks from today
- **THEN** the system warns that the minimum plan duration may not be met and suggests a later date

### Requirement: Experience level assessment
The system SHALL provide an experience level selector with three options: Beginner ("New to running or returning after a long break"), Intermediate ("Running regularly for 6+ months"), Advanced ("Competitive runner with 2+ years experience"). Each option SHALL display a brief description.

#### Scenario: Select beginner experience
- **WHEN** a user selects "Beginner"
- **THEN** the system sets experience level to Beginner and displays associated training constraints (e.g., "3-4 run days/week, gradual build-up")

### Requirement: Pace guidance preference
The system SHALL allow users to choose between VDOT (pace numbers) and RPE (effort-based) guidance during onboarding. A brief explanation of each mode SHALL be displayed. The user SHALL be able to change this preference at any time.

#### Scenario: Select VDOT mode
- **WHEN** a user selects VDOT pace guidance
- **THEN** the system prompts for a recent race time or estimated fitness level to calculate VDOT value

#### Scenario: Select RPE mode
- **WHEN** a user selects RPE pace guidance
- **THEN** the system confirms the choice and explains effort descriptions will be used instead of pace numbers

### Requirement: Available days configuration
The system SHALL allow users to select which days of the week they are available to train. Users MUST select at least 3 days for any plan. The system SHALL recommend placing the long run on a weekend day.

#### Scenario: Select 4 training days
- **WHEN** a user selects Monday, Wednesday, Friday, and Saturday as available days
- **THEN** the system stores these as available days and suggests Saturday for the long run

#### Scenario: Fewer than 3 days selected
- **WHEN** a user selects only 2 days
- **THEN** the system displays a message requiring at least 3 training days

### Requirement: Plan generation trigger
The system SHALL provide a "Generate Plan" button after all onboarding steps are complete. Upon clicking, the system generates the training plan and navigates to the calendar view.

#### Scenario: Generate plan from completed onboarding
- **WHEN** a user has completed all onboarding steps and clicks "Generate Plan"
- **THEN** the system generates a training plan using the PlanGenerationEngine and navigates to the monthly calendar view

#### Scenario: Onboarding incomplete
- **WHEN** a user has not selected a race distance and attempts to generate a plan
- **THEN** the "Generate Plan" button is disabled and a message indicates which steps are incomplete

### Requirement: Onboarding flow navigation
The system SHALL present onboarding as a stepped flow (race goal → experience → pace preference → available days → generate). Users SHALL be able to navigate back to previous steps to modify their choices.

#### Scenario: Navigate back to change race distance
- **WHEN** a user is on the pace preference step and clicks "Back"
- **THEN** the system navigates to the experience level step with the user's previous selection preserved
