## ADDED Requirements

### Requirement: Streak calculation and display
The system SHALL calculate the user's current streak (consecutive days with a completed workout) and display it on the calendar page.

#### Scenario: Active streak display
- **WHEN** user has completed workouts on 3+ consecutive scheduled workout days
- **THEN** the calendar page header shows a streak badge (e.g., "7-day streak") with the motivational message from MessagingService.GetStreakMessage()

#### Scenario: No active streak
- **WHEN** user has no consecutive completed workout days (streak < 3)
- **THEN** no streak badge is displayed

#### Scenario: Streak broken
- **WHEN** user skips or misses a scheduled workout day
- **THEN** the streak resets to 0 and the badge disappears

### Requirement: Milestone celebrations
The system SHALL celebrate workout milestones (1st, 10th, 25th, 50th, 100th completed workout) with a visible message.

#### Scenario: Reaching a milestone
- **WHEN** user completes their 10th workout
- **THEN** the system displays the milestone message from MessagingService.GetMilestoneMessage() as a toast or banner

#### Scenario: Between milestones
- **WHEN** user completes a workout that is not a milestone number
- **THEN** no milestone message is shown

### Requirement: Best streak tracking
The system SHALL track the user's all-time best streak and display it alongside the current streak.

#### Scenario: New best streak
- **WHEN** user's current streak exceeds their previous best streak
- **THEN** the system updates the best streak and displays "New record!" alongside the streak badge

#### Scenario: Below best streak
- **WHEN** user's current streak is below their best streak
- **THEN** the system shows "Best: X days" as secondary text under the current streak
