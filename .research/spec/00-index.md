# Running Training Plan App — Specification Index

This specification has been split into domain-specific modules for easier consumption by LLMs and development teams. Each file is self-contained and can be loaded independently.

## Architecture Overview

The app generates personalized running training plans for 5K, 10K, Half Marathon, and Full Marathon distances. The **primary goal is a running plan calendar that works around people's schedules**. The core loop is:

1. User sets a race goal → Plan Generation Engine creates a week-by-week plan
2. Plan is placed on a Calendar that integrates with the user's life schedule
3. User logs workouts → Feedback loop adjusts future weeks
4. Injury/schedule disruptions → Schedule Flexibility Engine adapts the plan

## File Map

| File | Domain | Sections | Est. Tokens |
|------|--------|----------|-------------|
| `01-overview-and-product-decisions.md` | Product foundation | 1, 1.1 | ~1,500 |
| `02-plan-generation-engine.md` | Training plan creation | 2, 3, 4, 5, 5B-5G, 8 | ~30,000 |
| `03-schedule-flexibility-engine.md` | Schedule adaptation | 6 (6.1-6.12) | ~15,000 |
| `04-workout-logging-and-feedback.md` | Workout tracking | 8B (8B.1-8B.10) | ~8,000 |
| `05-calendar-and-ui.md` | Calendar views & sync | 9, 9B (9B.1-9B.10) | ~12,000 |
| `06-injury-system.md` | Injury management | 11 (11.1-11.13) | ~15,000 |
| `07-coaching-retention-and-edge-cases.md` | Motivation, edge cases | 11B-11E, 12 (12.1-12.9) | ~12,000 |
| `08-data-models-and-api.md` | TypeScript interfaces, APIs | 7, 16 | ~3,000 |
| `09-auth-monetization-localization.md` | Auth, payments, i18n | 13, 14, 15 | ~3,000 |
| `10-references.md` | Research sources | 17 | ~500 |
| `v2-features.md` | Deferred features | V2-1 through V2-5 | ~5,000 |

## Key Product Decisions (Quick Reference)

These decisions are defined in `01-overview-and-product-decisions.md` and apply across ALL modules:

- **Pace guidance**: User chooses VDOT (pace numbers) or RPE (effort-based). Both always available.
- **Monetization**: Almost everything FREE. Only paid feature is holiday/vacation handling.
- **One active plan** at a time (product decision, not paywall).
- **Positive-only messaging**: Never grade, penalize, or shame runners.
- **Injury system**: Dormant by default, activates only when user reports an injury.
- **Plan completion target**: 90% of mileage (not 100%).
- **Auth**: Passwordless (email/phone OTP).
- **Taper lock**: Users cannot add volume during taper or recovery phases.

## Cross-Module Dependencies

```
01-overview ──────────────────────────────────────────────────────────────
      │
      ▼
02-plan-generation ──► 03-schedule-flexibility ──► 04-workout-logging
      │                        │                         │
      │                        │                         ▼
      │                        └──────────────────► 05-calendar-and-ui
      │                                                  │
      ▼                                                  │
06-injury-system ◄───────────────────────────────────────┘
      │
      ▼
07-coaching-retention-edge-cases
      │
      ▼
08-data-models-and-api (consolidates all interfaces)
      │
      ▼
09-auth-monetization-localization
```

## How to Use These Files

**For building a specific feature**: Load the relevant module + `08-data-models-and-api.md` for interfaces.

**For understanding the full system**: Start with `00-index.md` (this file) → `01-overview` → then the module you need.

**For an LLM coding session**: Load at most 2-3 modules at a time. Each module is sized to fit comfortably within a single context window with room for conversation.

## Marketing Data

Marketing data points and schemas are defined in `05-calendar-and-ui.md` (Section 10) as they relate to calendar-driven user engagement metrics. The full schema is also available in `marketing-data-schema.json`.
