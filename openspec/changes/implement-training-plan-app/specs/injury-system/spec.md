## ADDED Requirements

### Requirement: Dormant injury system activation
The injury system SHALL be invisible and dormant by default. It SHALL only activate when a user explicitly reports an injury via a "Report Injury" action. No injury-related UI SHALL appear until activated.

#### Scenario: No injury reported
- **WHEN** a user has not reported any injury
- **THEN** no injury-related UI elements, warnings, or prompts are displayed

#### Scenario: First injury report
- **WHEN** a user taps "Report Injury" for the first time
- **THEN** the injury tracking system activates and presents the pain assessment flow

### Requirement: Pain tracking
The system SHALL allow users to report pain by selecting a body part (knee, shin, hip, ankle, foot, calf, hamstring, IT band, back) and pain severity (1-10 scale). The system SHALL store pain history over time.

#### Scenario: Report knee pain
- **WHEN** a user reports pain in "Right Knee" with severity 5
- **THEN** the system records the pain entry with date, body part, and severity

#### Scenario: Pain trend tracking
- **WHEN** a user has reported pain 3 times over 2 weeks with decreasing severity (7, 5, 3)
- **THEN** the system displays a trend showing improvement

### Requirement: Run/stop decision guidance
The system SHALL provide a guided decision flow when a user reports pain before a scheduled workout. Based on severity: 1-3 (mild) suggests proceeding with modifications, 4-6 (moderate) suggests reducing volume/intensity, 7-10 (severe) recommends rest and medical consultation.

#### Scenario: Mild pain guidance
- **WHEN** a user reports pain severity 3 before a scheduled workout
- **THEN** the system suggests: "You can proceed — consider reducing pace and distance by 20%. Stop if pain increases."

#### Scenario: Severe pain guidance
- **WHEN** a user reports pain severity 8 before a scheduled workout
- **THEN** the system recommends: "Rest today and consider seeing a healthcare provider. Your plan will adjust automatically."

### Requirement: Workout modifications for injury
The system SHALL automatically modify scheduled workouts when an active injury is reported. Modifications SHALL include: reducing distance, lowering intensity, substituting cross-training, or converting to rest days based on injury severity.

#### Scenario: Moderate injury modifies tempo to easy
- **WHEN** a user has an active injury with severity 5 and a Tempo workout is scheduled
- **THEN** the system modifies the workout to an Easy run at reduced distance (50-75% of planned)

#### Scenario: Severe injury converts to rest
- **WHEN** a user has an active injury with severity 8
- **THEN** the system converts all workouts to rest or optional cross-training until pain is re-assessed

### Requirement: Return-to-plan progression
The system SHALL implement a gradual return to full training volume after injury. The return SHALL follow: Week 1 at 50% volume, Week 2 at 70% volume, Week 3 at 85% volume, Week 4 back to 100%. The system SHALL prompt for pain check-ins during return.

#### Scenario: Return after one-week injury
- **WHEN** a user reports pain resolved after 1 week of modified training
- **THEN** the system generates a 4-week return progression starting at 50% of the pre-injury volume

#### Scenario: Pain recurrence during return
- **WHEN** a user reports pain recurrence during return-to-plan progression
- **THEN** the system pauses the return, drops back to the previous volume level, and asks for updated pain assessment

### Requirement: Medical clearance prompt
The system SHALL recommend users seek medical clearance for injuries with severity 7+ or injuries lasting more than 2 weeks. The system SHALL NOT provide medical advice — only general guidance and recommendations to consult professionals.

#### Scenario: Persistent injury warning
- **WHEN** a user's injury has been active for more than 14 days
- **THEN** the system displays: "This injury has persisted for 2+ weeks. We recommend consulting a healthcare provider before continuing training."
