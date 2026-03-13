## ADDED Requirements

### Requirement: Volume-calibrated mileage increase rates
The system SHALL use adaptive increase rates calibrated to current weekly volume instead of fixed increments. Up to 15% increase at low volume (under 30 miles/week peak), down to 7% at high volume (over 50 miles/week peak), with linear interpolation between.

#### Scenario: Low volume progression
- **WHEN** generating a plan with peak weekly mileage under 30 miles
- **THEN** weekly mileage increases by up to 15% during non-deload weeks

#### Scenario: High volume progression
- **WHEN** generating a plan with peak weekly mileage over 50 miles
- **THEN** weekly mileage increases by no more than 7% during non-deload weeks

#### Scenario: Mid-range volume
- **WHEN** generating a plan with peak weekly mileage between 30 and 50 miles
- **THEN** the increase rate is linearly interpolated between 15% and 7%

### Requirement: Equilibrium hold periods
The system SHALL implement Jack Daniels' equilibrium method: after a mileage increase, hold the new volume for 3–4 weeks before the next increase (independent of deload pattern).

#### Scenario: Increase then hold
- **WHEN** weekly mileage increases from one block to the next
- **THEN** the system holds the new mileage level for 3 weeks (adjustable by deload pattern) before applying the next increase

#### Scenario: Deload within hold period
- **WHEN** a deload week falls within a hold period
- **THEN** the deload reduces volume as normal, and the hold resumes at the held level after the deload

### Requirement: Backward compatibility with existing progression patterns
The system SHALL continue to support the existing ProgressionPattern enum (Linear, TwoUpOneDown, ThreeUpOneDown, FourUpOneDown) while applying adaptive rates within each pattern.

#### Scenario: ThreeUpOneDown with adaptive rates
- **WHEN** generating a plan with ThreeUpOneDown pattern and adaptive rates
- **THEN** three weeks increase at the adaptive rate, followed by one deload week, repeating through the plan
