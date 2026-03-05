# Running Training Plan App — Data Models & API Endpoints

> This module consolidates the core TypeScript interfaces and the complete API endpoint
> summary. Individual modules also contain domain-specific data models and API endpoints
> inline — this file provides the shared foundation.
> See `00-index.md` for the full file map.

## 7. Data Models

### 7.1 Core Entities

```typescript
interface TrainingPlan {
  id: string;
  user_id: string;
  race_distance: "5K" | "10K" | "half_marathon" | "marathon";
  skill_level: "beginner" | "intermediate" | "advanced";
  race_date: Date;
  start_date: Date;
  duration_weeks: number;
  weeks: TrainingWeek[];
  status: "active" | "paused" | "completed" | "abandoned";
  created_at: Date;
  updated_at: Date;
}

interface TrainingWeek {
  week_number: number;
  phase: "base" | "build" | "peak" | "taper";
  type: "build" | "deload" | "taper";
  target_mileage_km: number;
  actual_mileage_km: number | null;
  workouts: ScheduledWorkout[];
  notes: string[];
}

interface ScheduledWorkout {
  id: string;
  date: Date;
  type: WorkoutType;
  target_distance_km: number;
  target_duration_minutes: number;
  intensity_description: string;
  structure: string | null;  // e.g., "4 x 800m @ 5K pace, 400m jog recovery"
  status: "scheduled" | "completed" | "skipped" | "rescheduled";
  actual_distance_km: number | null;
  actual_duration_minutes: number | null;
  rescheduled_from: Date | null;
  rescheduled_to: Date | null;
  priority: number;  // 1 (highest) to 7 (lowest)
}

type WorkoutType =
  | "easy_run"
  | "long_run"
  | "tempo_run"
  | "interval_run"
  | "progression_run"
  | "recovery_run"
  | "race_pace_run"
  | "run_walk"
  | "rest"
  | "cross_training";

interface UserProfile {
  id: string;
  name: string;
  email: string;
  current_weekly_mileage_km: number;
  running_experience_months: number;
  recent_race_times: RaceTime[];
  injury_history: string[];
  available_days: DayOfWeek[];
  preferred_long_run_day: DayOfWeek;
  max_daily_minutes: number;
  timezone: string;
}

interface RaceTime {
  distance: string;
  time_seconds: number;
  date: Date;
}

interface ScheduleException {
  id: string;
  plan_id: string;
  type: "holiday" | "vacation" | "missed_day" | "illness" | "injury";
  start_date: Date;
  end_date: Date;
  resolution: "skipped" | "rescheduled" | "volume_reduced" | "plan_reset";
  notes: string;
}
```

---

---

## 16. API Endpoint Summary (Complete)

```
AUTH:
POST   /api/auth/request-otp                   — Send OTP to email or phone
POST   /api/auth/verify-otp                    — Validate OTP, return tokens
POST   /api/auth/refresh                       — Refresh access token
POST   /api/auth/logout                        — Revoke refresh token
GET    /api/auth/sessions                      — List active sessions
DELETE /api/auth/sessions/:id                  — Revoke a specific session

USERS:
POST   /api/users                              — Create user profile
PUT    /api/users/:id                          — Update user profile
GET    /api/users/:id/plans                    — List user's plans
PUT    /api/users/:id/settings                 — Update preferences (units, pace mode, etc.)
POST   /api/users/:id/auth-methods             — Link additional email/phone
DELETE /api/users/:id/auth-methods/:mid         — Remove an auth method

PLANS:
POST   /api/plans                              — Generate new training plan
GET    /api/plans/:id                          — Retrieve plan details
PUT    /api/plans/:id                          — Update plan settings
DELETE /api/plans/:id                          — Archive/delete plan
POST   /api/plans/:id/pause                    — Pause active plan
POST   /api/plans/:id/resume                   — Resume paused plan

WORKOUTS:
POST   /api/plans/:id/workouts/:wid/complete   — Mark workout done
POST   /api/plans/:id/workouts/:wid/skip       — Mark workout skipped
PUT    /api/plans/:id/workouts/:wid/reschedule  — Reschedule workout
POST   /api/plans/:id/workouts/:wid/readiness   — Submit pre-workout readiness check-in

CROSS-TRAINING:
POST   /api/plans/:id/cross-training           — Log a cross-training activity
GET    /api/plans/:id/cross-training           — List logged cross-training

INJURIES:
POST   /api/plans/:id/injuries                 — Report a new injury
PUT    /api/plans/:id/injuries/:iid            — Update injury (severity, status)
GET    /api/plans/:id/injuries                 — List injuries for current plan
GET    /api/users/:id/injury-history           — Full injury history across all plans
POST   /api/plans/:id/injuries/:iid/resolve    — Mark resolved, trigger return protocol

SCHEDULE:
POST   /api/plans/:id/exceptions               — Add holiday/blocked date
GET    /api/plans/:id/exceptions               — List all exceptions
DELETE /api/plans/:id/exceptions/:eid          — Remove an exception
POST   /api/plans/:id/vacations                — Add preplanned vacation with strategy choice
POST   /api/plans/:id/weeks/:wnum/reduce       — Flag a week as reduced availability
PUT    /api/plans/:id/weeks/:wnum/reduce       — Update reduced week parameters
DELETE /api/plans/:id/weeks/:wnum/reduce       — Revert to normal week

RETENTION & MOTIVATION:
GET    /api/plans/:id/streaks                    — Get current daily and weekly streaks
GET    /api/plans/:id/milestones                 — Get earned milestones
GET    /api/users/:id/milestones                 — Get all milestones across plans
GET    /api/plans/:id/completion                 — Get 90% completion status and breakdown
POST   /api/plans/:id/workouts/:wid/fallback     — Accept fallback workout for today

PREDICTIONS:
GET    /api/plans/:id/prediction                — Get current race time prediction
GET    /api/plans/:id/prediction/history         — Get prediction trend over weeks

PUBLIC PROFILE:
GET    /api/public/u/:slug                       — Public profile page data (no auth)
PUT    /api/users/:id/public-profile             — Update public profile settings
POST   /api/users/:id/public-profile/slug        — Claim/change slug
DELETE /api/users/:id/public-profile             — Disable public profile
GET    /api/plans/:id/share-card/:type           — Generate achievement card image

STATS & MARKETING:
GET    /api/plans/:id/stats                    — Get plan progress & marketing stats
GET    /api/stats/aggregate                    — Get aggregate marketing dashboard data
GET    /api/users/:id/achievements             — Get shareable achievement data

BILLING:
GET    /api/subscriptions                      — Get current subscription status
POST   /api/subscriptions                      — Create/upgrade subscription
DELETE /api/subscriptions                      — Cancel subscription
GET    /api/purchases                          — List plan purchases
```

---
