# Running Training Plan App — Workout Logging, Completion & Feedback

> This module covers how runners log workouts, post-workout feedback, same-day rescheduling,
> surface/terrain preferences, plan restart/reset, end-of-plan summary, and offline support.
> See `00-index.md` for the full file map.

## 8B. Workout Logging, Completion & Feedback

This section defines how runners record completed workouts, how the app
captures what actually happened (vs what was planned), and how that data
feeds back into the plan generator. This is the core execution loop —
the plan generates workouts, the runner does them, the app records results,
and the plan adapts.

### 8B.1 Workout Completion Methods

```python
WORKOUT_COMPLETION_METHODS = {
    "method_1_auto_sync": {
        "label": "Auto-sync from connected platform",
        "description": (
            "The app detects a matching workout from Strava, Garmin, or "
            "Apple Health and auto-matches it to today's planned workout. "
            "No user action required."
        ),
        "platforms": ["strava", "garmin_connect", "apple_health"],
        "matching_logic": {
            "match_by": ["date", "activity_type", "approximate_distance"],
            "tolerance": "±20% distance, same day",
            "ambiguity_handling": (
                "If multiple activities on the same day, show the user "
                "a list and let them pick which maps to the planned workout."
            )
        },
        "auto_captured_data": [
            "actual_distance_km",
            "actual_duration_minutes",
            "actual_pace_per_km",
            "average_heart_rate",
            "max_heart_rate",
            "elevation_gain_m",
            "splits_per_km",
            "cadence",
            "calories_estimated",
            "route_map"            # from GPS data
        ]
    },

    "method_2_in_app_tracking": {
        "label": "Track with the app (GPS)",
        "description": (
            "User taps 'Start Workout' in the app and the phone tracks "
            "distance, pace, and duration via GPS. Includes in-run "
            "notifications (Section 12.6)."
        ),
        "captured_data": [
            "actual_distance_km",
            "actual_duration_minutes",
            "actual_pace_per_km",
            "splits_per_km",
            "route_map",
            "elevation_gain_m"
        ],
        "notes": (
            "The app is NOT trying to replace Strava or Garmin as a GPS "
            "tracker. This is a lightweight option for users who don't "
            "use another platform. It should work but doesn't need to be "
            "best-in-class GPS tracking."
        )
    },

    "method_3_manual_entry": {
        "label": "Log it manually",
        "description": (
            "User taps 'Mark Complete' and enters what they did. "
            "Quick entry: just tap 'Done'. Detailed entry: add distance, "
            "time, pace, and notes."
        ),
        "input_fields": {
            "required": [],          # nothing required — just tapping 'Done' is enough
            "optional": [
                "actual_distance_km",
                "actual_duration_minutes",
                "actual_pace_per_km",
                "heart_rate_average",
                "surface (road / trail / treadmill / track)",
                "notes"
            ]
        },
        "quick_complete": {
            "description": (
                "One-tap completion. User just taps 'Done' with no data entry. "
                "The app assumes the workout was completed as planned. This is "
                "the LOWEST FRICTION option — critical for retention."
            ),
            "when_to_use": (
                "Many runners, especially beginners, won't log detailed data. "
                "That's fine. A completed workout with no data is infinitely "
                "better than an uncompleted one."
            )
        }
    },

    "method_4_partial_completion": {
        "label": "I did some of it",
        "description": (
            "User ran but didn't complete the full planned workout. "
            "The app should make it easy to log partial workouts without "
            "making the user feel like they failed."
        ),
        "input": "Slider or quick-pick: 25% / 50% / 75% / custom",
        "message": "You got out there — that's what counts. Logging what you did.",
        "plan_effect": (
            "Partial completion is tracked. If a user consistently does "
            "~75% of planned volume, the plan generator may silently reduce "
            "future targets to match actual capacity."
        )
    }
}
```

### 8B.2 Post-Workout Feedback

After completing a workout, the app optionally asks for quick feedback.
This data feeds the adaptive plan engine.

```python
POST_WORKOUT_FEEDBACK = {
    "description": (
        "Brief, optional feedback after each workout. Should take "
        "less than 10 seconds. The app learns from this data to "
        "adjust future workouts."
    ),

    "prompt_timing": "Immediately after marking workout complete",
    "required": False,  # never force feedback
    "skip_option": True,  # always show 'Skip' button

    "questions": {
        "effort_rating": {
            "question": "How did that feel?",
            "options": [
                {"label": "Too easy", "value": -1,
                 "effect": "Plan may increase next similar workout by 5%"},
                {"label": "Just right", "value": 0,
                 "effect": "No adjustment — plan is calibrated well"},
                {"label": "Hard but doable", "value": 1,
                 "effect": "No adjustment — this is the target zone"},
                {"label": "Too hard", "value": 2,
                 "effect": "Plan may reduce next similar workout by 5-10%"}
            ],
            "display": "4 buttons, one tap"
        },

        "energy_level": {
            "question": "Energy level today?",
            "options": [
                {"label": "Low", "value": 1},
                {"label": "Normal", "value": 2},
                {"label": "High", "value": 3}
            ],
            "display": "3 buttons, shown only 2x per week to avoid fatigue",
            "frequency": "every_3rd_workout"
        },

        "free_notes": {
            "question": "Anything to note? (optional)",
            "input": "Free text, max 200 chars",
            "display": "Collapsible — hidden by default",
            "examples": "Course was hilly, felt great, legs heavy today"
        }
    },

    "adaptive_response": {
        "description": (
            "When multiple 'too hard' ratings accumulate (3+ in 2 weeks), "
            "the plan generator triggers a recalibration — silently reducing "
            "pace targets and/or volume by 5-10%. When multiple 'too easy' "
            "ratings accumulate, it may increase targets."
        ),
        "threshold_to_adjust": {
            "too_hard": "3 out of last 6 workouts rated 'too hard'",
            "too_easy": "4 out of last 6 workouts rated 'too easy'"
        },
        "adjustment_cap": "Never adjust more than 10% in a single recalibration",
        "user_notification": (
            "We've noticed your recent workouts have felt {hard/easy}. "
            "We've fine-tuned your upcoming targets to match your current fitness."
        )
    }
}
```

### 8B.3 Workout Result Data Model

```typescript
interface WorkoutResult {
  id: string;
  plan_id: string;
  scheduled_workout_id: string;
  user_id: string;
  completed_at: Date;

  // Completion method
  completion_method: "auto_sync" | "in_app_gps" | "manual" | "quick_complete";
  sync_source: "strava" | "garmin" | "apple_health" | null;
  external_activity_id: string | null;

  // Planned vs Actual
  planned_distance_km: number;
  actual_distance_km: number | null;    // null if quick-complete
  planned_duration_min: number;
  actual_duration_min: number | null;
  planned_pace_sec_per_km: number;
  actual_pace_sec_per_km: number | null;

  // Optional data (from sync or manual)
  average_heart_rate: number | null;
  max_heart_rate: number | null;
  elevation_gain_m: number | null;
  cadence_avg: number | null;
  splits: Split[] | null;
  route_polyline: string | null;       // encoded polyline from GPS
  surface: "road" | "trail" | "treadmill" | "track" | "grass" | null;

  // Feedback
  effort_rating: -1 | 0 | 1 | 2 | null;  // too easy / just right / hard / too hard
  energy_level: 1 | 2 | 3 | null;
  notes: string | null;

  // Weather (auto-captured at workout time)
  weather_at_workout: {
    temperature_c: number;
    humidity_pct: number;
    wind_speed_kmh: number;
    conditions: string;                 // "clear", "rain", "overcast", etc.
  } | null;

  // Partial completion
  completion_percentage: number;        // 1.0 = full, 0.75 = partial, etc.
  partial_reason: string | null;

  // Injury check
  pain_check_in: PainCheckIn | null;   // if injury was active
}

interface Split {
  km_number: number;
  pace_sec_per_km: number;
  heart_rate_avg: number | null;
  elevation_change_m: number | null;
}
```

### 8B.4 Ongoing Platform Sync (Auto-Completion) — DEFERRED TO V2

> **This feature has been moved to V2.** See `running-training-plan-v2-features.md`, Section V2-1 for full details.
>
> **V1 behavior**: Users log workouts via the 4 completion methods in Section 8B.1 (auto-sync from watch app, in-app GPS tracking, manual entry, or partial completion with one-tap quick-complete). One-time history import from Strava/Garmin/Apple Health is available at onboarding (Section 12.8).
>
> **V2 addition**: Always-on background sync that continuously watches connected platforms for new activities and auto-matches them to planned workouts.

### 8B.5 Same-Day Rescheduling ("Not Today" Flow)

```python
def handle_not_today(plan, today, user_reason=None):
    """
    The most common real-world scenario: user wakes up and can't/won't
    run today. They want to move today's workout to another day THIS WEEK
    proactively, before the day ends.

    This is DIFFERENT from the missed-day algorithm (Section 6.3), which
    runs after the fact. This is proactive rescheduling.
    """
    todays_workout = get_workout_on_date(plan, today)

    if todays_workout is None:
        return {"status": "already_rest", "message": "Today is already a rest day."}

    # Find available slots this week
    remaining_days = get_remaining_days_this_week(plan, today)
    available_slots = [
        day for day in remaining_days
        if day.workout is None or day.workout.type in ["rest_day", "easy_run"]
    ]

    if not available_slots:
        # No room to move — offer to skip or do a shorter version
        return {
            "status": "no_room",
            "options": [
                {
                    "label": "Skip it",
                    "action": "skip",
                    "message": "No worries — one missed workout won't affect your plan."
                },
                {
                    "label": "Do a shorter version",
                    "action": "shorten",
                    "message": (
                        "Even 15 minutes counts. We'll adapt the workout "
                        "to whatever time you have."
                    ),
                    "shortened_workout": fit_to_time_budget(todays_workout, 20)
                }
            ]
        }

    # Offer available days to move to
    move_options = []
    for day in available_slots:
        # Check swap rules (hard/easy spacing)
        valid = check_swap_rules(plan, today, day.date, todays_workout)
        move_options.append({
            "date": day.date,
            "day_name": day.day_name,
            "valid": valid["ok"],
            "warning": valid.get("warning"),
            "would_replace": day.workout.type if day.workout else "rest day"
        })

    return {
        "status": "can_move",
        "workout": todays_workout.summary,
        "options": move_options,
        "skip_option": {
            "label": "Just skip it",
            "message": "No worries — one day won't make or break your plan."
        },
        "message": "Can't make it today? No problem. Where should we move it?"
    }
```

### 8B.6 Surface & Terrain Preferences

```python
SURFACE_PREFERENCES = {
    "description": (
        "Where a runner trains affects workout selection and injury risk. "
        "The plan generator uses surface preferences to make smarter "
        "workout placement decisions."
    ),

    "user_config": {
        "primary_surface": {
            "options": ["road", "trail", "treadmill", "track", "mixed"],
            "default": "road"
        },
        "has_treadmill_access": {
            "type": "boolean",
            "default": False,
            "effect": (
                "If True, bad weather days suggest treadmill instead of skip. "
                "If False, bad weather = reschedule or skip."
            )
        },
        "has_track_access": {
            "type": "boolean",
            "default": False,
            "effect": (
                "If True, interval workouts can specify track. "
                "If False, intervals are road-based or treadmill-based."
            )
        },
        "has_hill_access": {
            "type": "boolean",
            "default": True,
            "effect": (
                "If False, hill repeats are replaced with treadmill incline "
                "or bridge/overpass repeats or removed from plan."
            )
        },
        "trail_experience": {
            "options": ["none", "some", "experienced"],
            "default": "none",
            "effect": (
                "Trail running requires different pacing (ignore pace targets, "
                "use effort/time instead). Trail long runs are slower but "
                "provide excellent strength benefits."
            )
        }
    },

    "surface_based_workout_placement": {
        "track": ["interval_800m", "interval_1600m", "ladder_intervals", "pyramid_intervals"],
        "road": ["easy_run", "tempo_run", "long_run", "fartlek"],
        "trail": ["easy_run", "long_run", "hill_repeats"],
        "treadmill": ["easy_run", "tempo_run", "interval sessions (any)", "hill_repeats (incline)"],
        "grass": ["strides", "fartlek", "recovery_run"]
    },

    "bad_weather_handling": {
        "has_treadmill": {
            "rain": "Suggest treadmill. User can override.",
            "extreme_heat": "Strongly suggest treadmill or reschedule.",
            "extreme_cold": "Suggest treadmill. Short easy runs outside still OK.",
            "ice_snow": "Treadmill only. Too dangerous outside."
        },
        "no_treadmill": {
            "rain": "Run as planned — rain doesn't hurt. Suggest visibility gear.",
            "extreme_heat": "Suggest early morning or reschedule.",
            "extreme_cold": "Layer up tips. Suggest shorter run.",
            "ice_snow": "Reschedule or skip. Safety first."
        }
    }
}
```

### 8B.7 Plan Restart & Reset

```python
PLAN_RESTART_OPTIONS = {
    "restart_from_beginning": {
        "description": "Start the entire plan over from Week 1.",
        "when": (
            "User feels they started too hard, lost too much time, "
            "or wants a fresh start."
        ),
        "preserves": ["schedule preferences", "injury history", "account data"],
        "resets": ["workout logs", "streaks", "plan progress", "mileage tracking"],
        "recalibrate": (
            "If restarting after completing 4+ weeks, use logged data "
            "to recalibrate starting mileage and pace targets."
        )
    },

    "restart_from_current_week": {
        "description": (
            "Keep all prior progress but regenerate remaining weeks "
            "based on current fitness level."
        ),
        "when": (
            "User came back from injury or break and wants the rest of "
            "the plan to reflect their current state, not their old plan."
        ),
        "preserves": ["all prior workout logs", "streaks", "schedule"],
        "regenerates": ["all future weeks from current week forward"],
        "uses": "Current weekly mileage and recent paces as new baseline"
    },

    "adjust_race_date": {
        "description": (
            "Move the goal race to a new date. The plan stretches or "
            "compresses accordingly."
        ),
        "stretch": "Add weeks — more base building, gentler progression",
        "compress": (
            "Remove weeks — steeper progression, validate that user's "
            "current fitness supports it. Warn if risky."
        ),
        "taper_fixed": "Taper length stays the same regardless of date change"
    },

    "abandon_plan": {
        "description": "Archive the plan without completing it.",
        "data_preserved": "All logs, history, and injury data kept in archive",
        "follow_up": "Offer to start a new plan or take a break",
        "no_shame_messaging": (
            "Plans change — that's life. Your training so far still "
            "made you fitter. Come back when you're ready."
        )
    }
}
```

### 8B.8 End-of-Plan Summary Report

```python
END_OF_PLAN_REPORT = {
    "trigger": "Recovery phase complete (Section 3, post-race recovery)",
    "description": (
        "A celebration of the entire training journey. Shows the runner "
        "how far they've come from Day 1 to Race Day."
    ),

    "report_contents": {
        "headline_stats": [
            "Total distance run during the plan (km/miles)",
            "Total workouts completed",
            "Plan completion rate (% of planned mileage achieved)",
            "Longest single run",
            "Peak weekly mileage",
            "Total training days",
            "Total hours spent running"
        ],

        "progression_charts": [
            "Weekly mileage over time (line chart)",
            "Easy pace improvement over time",
            "Long run distance progression",
            "Intensity distribution by phase"
        ],

        "milestones_achieved": (
            "List of all milestones hit during the plan (from Section 5G.6 "
            "and 11E): first double-digit week, longest run ever, peak week, etc."
        ),

        "race_day_recap": {
            "race_distance": "string",
            "finish_time": "if entered by user",
            "predicted_time": "from race time predictor (Section 11C)",
            "predicted_vs_actual": "comparison if both available",
            "weather_on_race_day": "auto-captured"
        },

        "plan_adaptation_summary": (
            "How many times the plan adapted around your life: "
            "X holidays handled, Y schedule changes, Z injury modifications. "
            "'Your plan bent around your life — and you still made it to the finish line.'"
        ),

        "what_next": {
            "options": [
                "Start a new plan for a different distance",
                "Run the same distance again with a faster goal",
                "Take a break — we'll keep your data for when you're ready",
                "Share your achievement (public profile)"
            ]
        }
    },

    "shareability": {
        "summary_card": (
            "Generate a shareable image card with key stats for social media. "
            "Respects the public profile visibility settings (Section 11D)."
        ),
        "formats": ["image_card_png", "link_to_public_profile"]
    }
}
```

### 8B.9 Offline Support

```python
OFFLINE_SUPPORT = {
    "description": (
        "Runners need their plan when they have no signal — on trails, "
        "in rural areas, or in airplane mode during a race."
    ),

    "cached_locally": [
        "Current week's workouts (always cached)",
        "Next week's workouts (pre-cached)",
        "Warm-up and cool-down protocols for scheduled workout types",
        "Injury rehab exercises (if active injury)",
        "Run/walk interval timers",
        "Pace targets for the day's workout"
    ],

    "works_offline": [
        "View today's workout and full week",
        "Start a workout with in-app GPS tracking",
        "Mark a workout as complete (manual entry)",
        "Run/walk interval timer",
        "View warm-up and cool-down instructions"
    ],

    "requires_connection": [
        "Syncing with Strava/Garmin/Apple Health",
        "Weather-adjusted pace targets",
        "Plan modifications (schedule changes, injury reporting)",
        "Notification delivery",
        "Public profile and sharing"
    ],

    "sync_on_reconnect": (
        "When connection is restored, all offline activity (completed "
        "workouts, manual logs) syncs automatically. Conflict resolution: "
        "if a workout was auto-synced from Strava AND manually logged "
        "offline, prefer the Strava data (richer) and discard the manual log."
    )
}
```

### 8B.10 Workout Logging API Endpoints

```
POST   /api/plans/:id/workouts/:wid/complete      — Mark workout complete (any method)
POST   /api/plans/:id/workouts/:wid/partial        — Log partial completion
POST   /api/plans/:id/workouts/:wid/skip           — Skip a workout
POST   /api/plans/:id/workouts/:wid/not-today      — Proactive same-day reschedule
POST   /api/plans/:id/workouts/:wid/feedback       — Submit post-workout feedback
GET    /api/plans/:id/workouts/:wid/result          — Get workout result data
POST   /api/plans/:id/workouts/unplanned            — Log a workout not in the plan
GET    /api/plans/:id/sync/status                   — Check sync status with platforms
POST   /api/plans/:id/sync/manual                   — Trigger manual sync
GET    /api/plans/:id/end-of-plan-report            — Generate end-of-plan summary
PUT    /api/users/:id/surface-preferences           — Update surface/terrain preferences
PUT    /api/plans/:id/restart                        — Restart plan (from beginning or current week)
PUT    /api/plans/:id/adjust-race-date              — Move race date and regenerate plan
POST   /api/plans/:id/archive                       — Archive plan without completing
```

---
