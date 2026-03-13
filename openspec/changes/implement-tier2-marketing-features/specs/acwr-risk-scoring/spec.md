## ADDED Requirements

### Requirement: Calculate ACWR from logged workouts
The system SHALL compute the Acute:Chronic Workload Ratio (ACWR) from logged workout distances. Acute load = total distance in the last 7 days. Chronic load = average weekly distance over the last 28 days.

#### Scenario: ACWR calculation
- **WHEN** user has 28+ days of logged workout data
- **THEN** the system computes ACWR = (7-day total distance) / (28-day average weekly distance) and classifies into a risk zone

#### Scenario: Insufficient data
- **WHEN** user has fewer than 28 days of logged data
- **THEN** the system does not display ACWR and shows "Building your baseline — ACWR available after 4 weeks"

### Requirement: ACWR risk zone classification
The system SHALL classify ACWR into three zones: Green (0.8–1.3), Yellow (1.3–1.5), Red (1.5+). Under-training (below 0.8) is shown as a separate informational state.

#### Scenario: Green zone
- **WHEN** ACWR is between 0.8 and 1.3
- **THEN** the system shows a green indicator with "Sweet spot — building fitness safely"

#### Scenario: Yellow zone
- **WHEN** ACWR is between 1.3 and 1.5
- **THEN** the system shows a yellow warning with "Caution — training load is ramping quickly. Consider an easier week."

#### Scenario: Red zone
- **WHEN** ACWR exceeds 1.5
- **THEN** the system shows a red alert with "High injury risk — your recent load is much higher than your baseline. Dial it back."

#### Scenario: Under-training
- **WHEN** ACWR is below 0.8
- **THEN** the system shows an informational note "Under-training zone — you can safely increase load"

### Requirement: ACWR displayed on calendar
The system SHALL display the current ACWR value and risk zone on the calendar page.

#### Scenario: ACWR badge on calendar
- **WHEN** user views the calendar with sufficient data
- **THEN** a small ACWR badge (colored by risk zone) is visible in the calendar header area
