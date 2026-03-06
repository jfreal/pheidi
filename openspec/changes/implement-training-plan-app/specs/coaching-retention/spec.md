## ADDED Requirements

### Requirement: Positive-only messaging system
The system SHALL use positive, encouraging language in all user-facing messages. The system SHALL NEVER use grades, scores, penalties, or negative framing. Missed workouts SHALL be acknowledged without shame.

#### Scenario: Missed workout message
- **WHEN** a user misses a scheduled workout
- **THEN** the system displays a message like "No worries — rest is training too. Here's what's coming up next." and focuses on the next workout

#### Scenario: Streak recognition
- **WHEN** a user completes 7 consecutive planned workouts
- **THEN** the system displays a celebratory message (e.g., "7 in a row! You're building great consistency.")

### Requirement: Race time prediction
The system SHALL provide an estimated race finish time based on training data (logged workouts, paces, and completion rate). The prediction SHALL update as more data is logged. Predictions SHALL include a range (optimistic to conservative).

#### Scenario: Prediction with sufficient data
- **WHEN** a user has logged at least 4 weeks of workouts with pace data
- **THEN** the system displays a predicted race finish time range (e.g., "Estimated finish: 3:45-3:55")

#### Scenario: Insufficient data for prediction
- **WHEN** a user has logged fewer than 4 weeks of workouts
- **THEN** the system displays "Keep logging workouts — race prediction will appear after 4 weeks of data"

### Requirement: Reduced availability week compression
The system SHALL handle weeks where a user marks fewer available days than normal. The compression algorithm SHALL preserve the long run, prioritize quality sessions, and drop easy runs first.

#### Scenario: Compress 5-day week to 3 days
- **WHEN** a user marks only 3 available days for a week that normally has 5 workouts
- **THEN** the system keeps the long run and quality session, drops 2 easy runs, and slightly increases the remaining easy run distance to partially compensate for lost volume

### Requirement: Pre-plan base building period
The system SHALL offer an optional base building phase for users who are not currently running regularly. Base building SHALL be 4-6 weeks of gradually increasing easy runs before the formal plan begins.

#### Scenario: Beginner requests base building
- **WHEN** a Beginner user indicates they are not currently running regularly during onboarding
- **THEN** the system offers a 4-6 week base building phase starting with 3 easy runs per week at 1-2 miles each, gradually increasing

#### Scenario: Experienced runner skips base building
- **WHEN** an Advanced user with recent running history starts onboarding
- **THEN** the system does not offer base building and proceeds directly to plan generation
