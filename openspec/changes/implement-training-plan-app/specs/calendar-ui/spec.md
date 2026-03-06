## ADDED Requirements

### Requirement: Monthly calendar view
The system SHALL display a monthly calendar grid showing all planned workouts. Each day cell SHALL display the workout type icon, short description, and target distance/duration. The current day SHALL be visually highlighted.

#### Scenario: View monthly calendar with workouts
- **WHEN** a user navigates to the monthly calendar view
- **THEN** the system displays a calendar grid for the current month with workout summaries in each day cell

#### Scenario: Navigate between months
- **WHEN** a user clicks the next/previous month arrows
- **THEN** the calendar updates to show the selected month with its workouts

### Requirement: Weekly calendar view
The system SHALL display a detailed weekly view showing each day as a card with full workout information: workout type, distance/duration, pace zone guidance, and warm-up/cool-down details.

#### Scenario: View weekly detail
- **WHEN** a user switches to weekly view
- **THEN** the system displays 7 day cards for the current week, each showing complete workout details

#### Scenario: Navigate between weeks
- **WHEN** a user clicks next/previous week arrows
- **THEN** the view updates to show the selected week's workouts

### Requirement: Daily workout view
The system SHALL provide a detailed daily view showing the full workout prescription: warm-up instructions, main set details (distance, pace zone, splits if applicable), cool-down instructions, and coaching notes.

#### Scenario: View daily tempo workout
- **WHEN** a user opens the daily view for a Tempo workout day
- **THEN** the system displays: warm-up (10 min easy), main set (4 miles at tempo pace with zone guidance), cool-down (10 min easy), and coaching tips

#### Scenario: View rest day
- **WHEN** a user opens the daily view for a Rest day
- **THEN** the system displays "Rest Day" with optional recovery tips (stretching, foam rolling)

### Requirement: Plan overview
The system SHALL provide a high-level plan overview showing all weeks in a condensed format with phase labels, weekly mileage totals, and long run distances. Each phase SHALL be color-coded.

#### Scenario: View plan overview
- **WHEN** a user switches to the plan overview
- **THEN** the system displays all plan weeks in a compact list/grid showing: week number, phase (color-coded), total mileage, long run distance, and key workout

### Requirement: Workout type color coding
The system SHALL assign distinct colors to each workout type: Easy (green), Tempo (orange), Intervals (red), LongRun (blue), Recovery (light green), Rest (gray), CrossTraining (purple), HillRepeats (brown), RacePace (gold).

#### Scenario: Color-coded calendar cells
- **WHEN** the monthly calendar displays a week with different workout types
- **THEN** each day cell has a color indicator matching its workout type

### Requirement: Today highlight
The system SHALL visually emphasize the current day in all calendar views. The today view SHALL be the default landing page after onboarding.

#### Scenario: Current day emphasized
- **WHEN** a user opens the calendar
- **THEN** today's date is visually highlighted (e.g., bold border, background color) and the view scrolls to show today

### Requirement: Workout detail panel
The system SHALL display a detail panel or modal when a user clicks on a workout in any calendar view. The panel SHALL show full workout details and provide access to logging actions.

#### Scenario: Open workout detail from monthly view
- **WHEN** a user clicks on a workout day in the monthly calendar
- **THEN** a detail panel opens showing the full workout prescription with a "Log Workout" action button

### Requirement: View switching
The system SHALL provide navigation controls to switch between Monthly, Weekly, Daily, and Overview views. The selected view SHALL persist across navigation within the calendar.

#### Scenario: Switch from monthly to weekly
- **WHEN** a user clicks "Weekly" in the view switcher while viewing March 2026 monthly
- **THEN** the view changes to weekly, showing the week containing the currently highlighted/selected day
