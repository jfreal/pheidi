## ADDED Requirements

### Requirement: iCal calendar export
The system SHALL generate iCal (.ics) files containing all planned workouts. Each workout SHALL be an iCal event with title (workout type + distance), description (full workout details), and duration estimate.

#### Scenario: Export full plan as iCal
- **WHEN** a user clicks "Export to Calendar"
- **THEN** the system generates an .ics file containing all plan workouts as calendar events and downloads it

#### Scenario: iCal event content
- **WHEN** an iCal file is generated for a Tempo workout of 4 miles
- **THEN** the event has title "Tempo Run - 4mi", description with pace guidance and warm-up/cool-down, and estimated duration of 45 minutes

### Requirement: Calendar subscription URL
The system SHALL provide a subscribe URL that external calendar apps can use for live sync. The URL SHALL serve an up-to-date iCal feed reflecting current plan state including any modifications.

#### Scenario: Google Calendar subscription
- **WHEN** a user copies the subscribe URL and adds it to Google Calendar
- **THEN** Google Calendar displays all plan workouts and updates when the plan changes

#### Scenario: URL authentication
- **WHEN** the subscribe URL is accessed
- **THEN** it uses a user-specific token in the URL for authentication (no login required for calendar apps)

### Requirement: Print/PDF export
The system SHALL generate a print-friendly version of the training plan. The print layout SHALL include a week-by-week table with workout summaries, phase labels, and total mileage per week.

#### Scenario: Print plan overview
- **WHEN** a user clicks "Print Plan"
- **THEN** the system generates a print-friendly layout and opens the browser print dialog

#### Scenario: PDF download
- **WHEN** a user clicks "Download PDF"
- **THEN** the system generates a PDF file with the plan overview and downloads it

### Requirement: Calendar sharing
The system SHALL allow users to share a read-only view of their training plan via a shareable link. The shared view SHALL show the plan calendar without personal details.

#### Scenario: Generate share link
- **WHEN** a user clicks "Share Plan"
- **THEN** the system generates a unique URL that displays a read-only calendar view of the plan

#### Scenario: Shared view is read-only
- **WHEN** someone accesses a shared plan link
- **THEN** they can view the calendar and workouts but cannot modify, log, or access personal data
