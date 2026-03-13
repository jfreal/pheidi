## ADDED Requirements

### Requirement: Transition time presets
The system SHALL offer 5 presets for commute/shower overhead that reduce the available training time per session: None (0 min), Home Shower (20 min), Gym Shower (25 min), Lunch Break (30 min), Gym With Commute (35 min).

#### Scenario: Selecting a preset in settings
- **WHEN** user selects "Gym Shower (25 min)" as their transition time preset
- **THEN** the system stores this on UserProfile and subtracts 25 minutes from each day's available training window when generating workouts

#### Scenario: No preset selected
- **WHEN** user does not select a transition time preset (default: None)
- **THEN** no time is subtracted from available training windows

### Requirement: Transition time affects workout duration
The system SHALL factor transition time into workout duration calculations so that the total time commitment (workout + transition) fits within the user's available window.

#### Scenario: Workout fits within window
- **WHEN** user has 60 minutes available and 25 minutes transition time
- **THEN** the system generates a workout of no more than 35 minutes duration

#### Scenario: Workout too long for window
- **WHEN** a planned workout duration plus transition time would exceed the available window
- **THEN** the system reduces the workout distance/duration to fit or moves it to a day with more time

### Requirement: Transition time selection during onboarding
The system SHALL present transition time selection during onboarding alongside the schedule/availability step.

#### Scenario: Onboarding preset selection
- **WHEN** user reaches the schedule step in onboarding
- **THEN** the system shows the 5 transition time presets with descriptions and the default is None

### Requirement: Transition time stored on user profile
The system SHALL persist the selected transition time preset on UserProfile.

#### Scenario: Changing preset in settings
- **WHEN** user changes their transition time preset in Settings
- **THEN** the system updates the profile and reflows remaining plan weeks with the new time constraint
