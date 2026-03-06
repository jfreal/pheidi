## ADDED Requirements

### Requirement: Available days configuration
The system SHALL allow users to configure which days of the week they are available for training. Changes to available days SHALL trigger a reflow of remaining plan weeks.

#### Scenario: Change available days mid-plan
- **WHEN** a user removes Wednesday from their available days in week 5 of a 16-week plan
- **THEN** the system redistributes workouts in weeks 5-16 to exclude Wednesdays while preserving weekly volume targets

### Requirement: Blocked days
The system SHALL allow users to mark specific dates as blocked (holidays, events). Workouts on blocked days SHALL be automatically redistributed to adjacent available days within the same week.

#### Scenario: Block a single day
- **WHEN** a user blocks Thursday March 19 which has a Tempo workout
- **THEN** the system moves the Tempo workout to the nearest available day in the same week

#### Scenario: Block day with no alternatives in week
- **WHEN** a user blocks all remaining available days in a week
- **THEN** the system marks the week as a rest week and adjusts subsequent weeks to compensate

### Requirement: Vacation handling
The system SHALL allow users to mark multi-day vacation periods. During vacation, the system SHALL offer volume reduction strategies: maintain base easy runs, or fully rest. Vacation handling SHALL be a paid feature.

#### Scenario: One-week vacation
- **WHEN** a user marks a 7-day vacation period
- **THEN** the system offers options: "Light maintenance runs" (3 easy runs at 50% volume) or "Full rest" (no workouts), and adjusts the return week with a gradual ramp-back

#### Scenario: Vacation during taper
- **WHEN** a user marks vacation during the taper phase
- **THEN** the system treats the vacation as compatible with taper and reduces volume accordingly

### Requirement: Drag-and-drop workout swap
The system SHALL allow users to drag a workout from one day to another within the same week. The system SHALL prevent moving workouts to rest days that would create consecutive hard days.

#### Scenario: Swap workout within week
- **WHEN** a user drags a Tempo workout from Tuesday to Thursday within the same week
- **THEN** the workouts on Tuesday and Thursday swap positions

#### Scenario: Prevent consecutive hard days
- **WHEN** a user drags an Interval workout to the day immediately before or after a Long Run
- **THEN** the system warns about consecutive hard days and asks for confirmation

### Requirement: Mid-plan schedule changes
The system SHALL support changing available training days after plan generation. The system SHALL reflow all remaining (future) weeks while preserving completed week history.

#### Scenario: Add a training day
- **WHEN** a user adds Friday as a new available day starting from the current week
- **THEN** the system regenerates remaining weeks with the additional day, potentially adding recovery runs on Fridays

### Requirement: Taper lock enforcement
The system SHALL prevent users from adding workout volume during Taper phase weeks. Users MAY reduce or skip workouts during taper but SHALL NOT add distance or intensity.

#### Scenario: Attempt to add workout during taper
- **WHEN** a user tries to add a 5-mile run to a taper week rest day
- **THEN** the system blocks the addition and displays: "Taper phase — volume cannot be increased. Trust the taper!"

#### Scenario: Skip workout during taper allowed
- **WHEN** a user marks a taper week workout as skipped
- **THEN** the system allows the skip without penalty or rescheduling
