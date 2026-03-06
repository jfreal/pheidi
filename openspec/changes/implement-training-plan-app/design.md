## Context

Pheidi is a .NET 9.0 Blazor Web App (Interactive Server rendermode) for generating personalized running training plans. The solution has three projects:

- **Pheidi.Blazor** — UI layer (Razor components, Bootstrap 5, FontAwesome)
- **Pheidi.Common** — Domain models and business logic
- **Pheidi.Common.Tests** — Unit tests (MSTest)

Current state: Basic `TrainingPlan` class with linear progression and simple taper. UI is a sidebar + table showing weekly distances. Models are minimal (`Activity` enum, `DayConfig`, `Week`).

The product spec at `.research/spec/` defines a full-featured running plan app with calendar-centric UX, schedule flexibility, workout logging, injury management, and coaching. This design covers implementing that spec.

## Goals / Non-Goals

**Goals:**
- Implement the full training plan generation engine with periodization, multiple progression patterns, and experience-level scaling
- Build a calendar-centric UI as the primary interface (monthly, weekly, daily, overview views)
- Support schedule flexibility (blocked days, vacations, drag-and-drop, mid-plan changes)
- Enable workout logging with completion tracking and feedback
- Implement dormant injury system that activates only when needed
- Add passwordless auth and data persistence
- Maintain the existing project structure (Blazor + Common + Tests)

**Non-Goals:**
- V2 features (platform sync, wearable integration, altitude/terrain adjustments, voice coaching, roster import) — deferred per spec
- Native mobile apps — Blazor Server is the target platform
- Real-time GPS tracking during runs — manual/quick-complete logging only for V1
- Social features beyond basic calendar sharing

## Decisions

### 1. Keep Blazor Interactive Server (no WASM)
**Decision**: Stay with Interactive Server rendermode.
**Rationale**: Simpler deployment, no need for offline-first architecture in V1. Server-side state management is sufficient. The spec mentions offline support but that's better addressed in V2 with a WASM migration if needed.
**Alternative considered**: Blazor WebAssembly — rejected because it adds complexity for offline sync, API layer duplication, and larger bundle sizes.

### 2. Entity Framework Core with SQLite for persistence
**Decision**: Use EF Core with SQLite for local development, with migration path to SQL Server/PostgreSQL for production.
**Rationale**: EF Core is the natural choice for .NET 9. SQLite keeps local dev simple with no external database dependency. Code-first migrations make schema evolution straightforward.
**Alternative considered**: Dapper — lighter weight but loses migration tooling and change tracking benefits.

### 3. Domain-driven model structure in Pheidi.Common
**Decision**: Organize Pheidi.Common into domain folders: Models/, Services/, Engines/.
**Rationale**: Clear separation between data shapes (Models), business logic (Engines like PlanGenerationEngine, ScheduleFlexibilityEngine), and application services. Keeps the domain layer framework-agnostic.
**Alternative considered**: Feature-based folders — better for larger codebases but premature for this project size.

### 4. Component-based UI architecture
**Decision**: Build calendar views as composable Razor components with a shared state service.
**Rationale**: Blazor's component model naturally fits the calendar UI. A `PlanStateService` (scoped per circuit) holds the active plan and propagates changes. Components subscribe to state changes via events.
**Alternative considered**: Flux/Redux pattern — too heavyweight for Blazor Server where state is already server-side.

### 5. Pace calculation as a standalone service
**Decision**: Implement `PaceCalculator` as a stateless service supporting both VDOT and RPE modes.
**Rationale**: Pace calculations are used across plan generation, workout display, and coaching. A single service with clear inputs/outputs keeps this logic testable and reusable.

### 6. Phase-based implementation order
**Decision**: Implement in dependency order: Models → Engine → Onboarding UI → Calendar UI → Schedule Flexibility → Logging → Injury → Coaching → Auth → Integrations.
**Rationale**: Each phase builds on the previous. The plan generation engine needs models. The calendar needs a generated plan. Logging needs the calendar. This avoids rework.

### 7. Passwordless auth via email OTP
**Decision**: Implement email-based OTP using a custom auth service (not ASP.NET Identity).
**Rationale**: The spec requires passwordless auth. ASP.NET Identity is password-centric and heavy for OTP-only flow. A lightweight custom implementation with rate limiting is simpler and matches the spec exactly.
**Alternative considered**: ASP.NET Identity with custom token provider — adds unnecessary complexity for a passwordless-only system.

### 8. iCal format for calendar export/sync
**Decision**: Use iCal (.ics) format for Google Calendar, Apple Calendar, and Outlook integration.
**Rationale**: iCal is the universal standard supported by all major calendar apps. A single export format covers all three targets. Subscribe URLs enable live sync.

## Risks / Trade-offs

- **[Blazor Server scalability]** → Each user holds a SignalR circuit with server-side state. Mitigation: acceptable for V1 scale; migrate to WASM if user count warrants it.
- **[Plan generation complexity]** → The spec defines extensive periodization and progression rules. Mitigation: implement core algorithm first, add refinements iteratively. Comprehensive unit tests validate each rule.
- **[Breaking existing models]** → Current `TrainingPlan`, `Week`, `DayConfig` will be replaced. Mitigation: the existing app is pre-release with no external consumers. Clean break is acceptable.
- **[OTP email delivery]** → Requires email sending infrastructure. Mitigation: use a transactional email service (SendGrid/Resend). For dev, log OTP to console.
- **[Large spec surface area]** → 10 capabilities across ~100K tokens of spec. Mitigation: phased implementation with clear task boundaries. Each phase is independently testable and deployable.
