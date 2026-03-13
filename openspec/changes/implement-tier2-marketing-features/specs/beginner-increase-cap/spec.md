## ADDED Requirements

### Requirement: 50% max increase cap on continuous running duration for beginners
The system SHALL cap the increase in the longest continuous running duration between any two consecutive weeks at 50% for Beginner-level plans.

#### Scenario: Duration increase within cap
- **WHEN** a beginner plan has Week N with 8 minutes continuous running and Week N+1 would be 11 minutes
- **THEN** the system allows the increase (37.5% < 50%)

#### Scenario: Duration increase exceeds cap
- **WHEN** a beginner plan has Week N with 8 minutes continuous running and Week N+1 would be 14 minutes
- **THEN** the system caps Week N+1 at 12 minutes (50% of 8) and inserts transition progression

### Requirement: Automatic transition week insertion
The system SHALL insert transition weeks when the gap between two planned running durations exceeds the 50% cap.

#### Scenario: Transition week inserted
- **WHEN** the planned jump from Week N to Week N+2 exceeds what can be bridged in one 50% increase
- **THEN** the system inserts one or more transition weeks with intermediate durations to bridge the gap smoothly

#### Scenario: No transition needed
- **WHEN** consecutive weeks already respect the 50% cap
- **THEN** no transition weeks are inserted

### Requirement: Cap only applies to beginner experience level
The system SHALL only enforce the 50% increase cap for users with Beginner experience level.

#### Scenario: Intermediate user
- **WHEN** generating a plan for an Intermediate user
- **THEN** the 50% cap is NOT enforced and standard progression applies

#### Scenario: Advanced user
- **WHEN** generating a plan for an Advanced user
- **THEN** the 50% cap is NOT enforced and standard progression applies
