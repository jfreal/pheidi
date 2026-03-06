## ADDED Requirements

### Requirement: Passwordless OTP authentication
The system SHALL support passwordless sign-in via email OTP (one-time password). Users SHALL enter their email, receive a 6-digit code, and enter the code to authenticate. OTP codes SHALL expire after 10 minutes.

#### Scenario: Successful email OTP sign-in
- **WHEN** a user enters their email and receives a 6-digit OTP code
- **THEN** entering the correct code within 10 minutes authenticates the user and creates a session

#### Scenario: Expired OTP
- **WHEN** a user enters an OTP code after 10 minutes have elapsed
- **THEN** the system rejects the code and prompts the user to request a new one

#### Scenario: Rate limiting
- **WHEN** a user requests more than 5 OTP codes within 15 minutes
- **THEN** the system rate-limits further requests and displays a cooldown message

### Requirement: User session management
The system SHALL maintain authenticated sessions using secure cookies. Sessions SHALL expire after 30 days of inactivity. Users SHALL be able to sign out explicitly.

#### Scenario: Session persistence
- **WHEN** an authenticated user returns within 30 days
- **THEN** the system recognizes the session and does not require re-authentication

#### Scenario: Explicit sign out
- **WHEN** a user clicks "Sign Out"
- **THEN** the session is invalidated and the user is redirected to the sign-in page

### Requirement: Data persistence layer
The system SHALL persist all user data (profiles, plans, workouts, injuries) to a database using Entity Framework Core. Data SHALL be associated with the authenticated user's account.

#### Scenario: Plan survives session restart
- **WHEN** a user generates a plan, signs out, and signs back in
- **THEN** the user's active plan is loaded and displayed as it was before sign-out

#### Scenario: Workout history persistence
- **WHEN** a user logs workouts over multiple sessions
- **THEN** all workout logs are stored and retrievable in chronological order

### Requirement: API endpoint structure
The system SHALL expose API endpoints for core operations following RESTful conventions: plans (CRUD), workouts (log, list, update), user profile (read, update), injuries (report, update, list).

#### Scenario: Get active plan
- **WHEN** an authenticated request is made to GET /api/plans/active
- **THEN** the system returns the user's active training plan with all weeks and workouts

#### Scenario: Log a workout
- **WHEN** an authenticated request is made to POST /api/workouts with workout data
- **THEN** the system records the workout log and returns the updated workout with completion status

### Requirement: Monetization gate for vacation handling
The system SHALL gate vacation/holiday schedule handling behind a paid feature. Free users SHALL be able to block individual days but SHALL NOT access multi-day vacation handling with volume strategies.

#### Scenario: Free user blocks single day
- **WHEN** a free user blocks a single date on the calendar
- **THEN** the system allows the block and redistributes the workout

#### Scenario: Free user attempts vacation feature
- **WHEN** a free user attempts to mark a multi-day vacation period
- **THEN** the system displays the vacation feature as a paid upgrade with description of benefits
