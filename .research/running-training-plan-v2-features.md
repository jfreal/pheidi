# Running Training Plan App — V2 Features

This document describes features deferred from the V1 specification (`running-training-plan-spec.md`) to a future V2 release. Each feature includes the original spec context, rationale for deferral, and implementation notes.

---

## V2-1: Ongoing Platform Sync (Auto-Completion)

**Original spec location**: Section 8B.4

**Summary**: Unlike the one-time history import (Section 12.8 in V1), ongoing sync continuously watches for new activities on connected platforms and auto-matches them to planned workouts. V1 supports 4 manual completion methods (auto-sync from watch, in-app GPS, manual entry, partial completion). V2 adds always-on background sync.

### Supported Platforms

```python
ONGOING_SYNC_CONFIG = {
    "supported_platforms": {
        "strava": {
            "method": "Webhook subscription (Strava API v3)",
            "triggers_on": "New activity created",
            "delay": "Usually 1-5 minutes after upload"
        },
        "garmin_connect": {
            "method": "Push notifications API or periodic polling",
            "triggers_on": "New activity synced from watch",
            "delay": "Usually 1-10 minutes after watch sync"
        },
        "apple_health": {
            "method": "HealthKit background observer",
            "triggers_on": "New workout sample added",
            "delay": "Near-instant on iOS"
        }
    },

    "auto_match_flow": [
        "1. New activity detected from connected platform",
        "2. Check if activity date matches a planned workout date",
        "3. Check if activity type is 'Run' (or matching type)",
        "4. Check if distance is within ±20% of planned distance",
        "5. If all match → auto-mark workout as complete with synced data",
        "6. If ambiguous (multiple matches) → send notification asking user to confirm",
        "7. If no match → offer to log as an unplanned workout (extra run)"
    ],

    "unplanned_workouts": {
        "description": (
            "If a user runs on a rest day or does an extra run not in the plan, "
            "log it but don't penalize OR reward. The extra volume is noted in "
            "the weekly summary and factored into ACWR calculations."
        ),
        "plan_effect": "Counts toward weekly volume for ACWR but doesn't change plan"
    }
}
```

### Why Deferred

- Requires maintaining webhook subscriptions and handling platform API rate limits at scale
- Strava API requires app review and approval for webhook access
- Garmin Connect API access requires a partnership agreement
- Apple HealthKit background observers require careful iOS permission handling
- V1's manual completion methods (including one-tap quick-complete) cover the core use case

### V2 Implementation Notes

- Build a background job queue for webhook event processing
- Handle edge cases: user uploads same run to multiple platforms, delayed syncs, retroactive uploads
- Consider a "sync status" indicator in the calendar view showing last sync time per platform
- Unplanned workout logging should integrate with the existing ACWR injury risk scoring (Section 11.12)

---

## V2-2: Wearable Integration (HRV, Resting HR, Sleep Quality)

**Original spec location**: Section 1.1 (Readiness and Auto-Adjustment)

**Summary**: V1 uses a pre-workout subjective check-in on a 1-5 scale (Fresh / Good / Normal / Tired / Exhausted) to gauge readiness. V2 adds objective physiological data from wearables to improve readiness assessment and auto-adjustment accuracy.

### Data Model Fields (Already in V1 Schema)

The V1 data model already includes placeholder fields for these metrics to ensure forward compatibility:

```typescript
interface ReadinessData {
  // V1 fields (active)
  subjective_rating: 1 | 2 | 3 | 4 | 5;

  // V2 fields (stored but unused in V1)
  hrv_milliseconds?: number;         // Heart rate variability (rMSSD)
  resting_heart_rate_bpm?: number;    // Morning resting HR
  sleep_quality_score?: number;       // 0-100 from wearable
  sleep_duration_hours?: number;      // Total sleep time
  recovery_score?: number;            // Platform-specific (Garmin Body Battery, Whoop Recovery, etc.)
}
```

### V2 Readiness Algorithm

```python
V2_READINESS_CONFIG = {
    "data_sources": {
        "garmin": ["Body Battery", "HRV Status", "Sleep Score", "Resting HR"],
        "apple_watch": ["HRV (rMSSD via HealthKit)", "Resting HR", "Sleep Duration"],
        "whoop": ["Recovery Score", "HRV", "Sleep Performance"],
        "oura": ["Readiness Score", "HRV", "Sleep Score", "Resting HR"],
        "polar": ["Nightly Recharge", "HRV", "Sleep Score"]
    },

    "composite_readiness_score": {
        "description": (
            "Combine subjective check-in with wearable data for a composite score. "
            "Subjective always takes priority if user feels bad but data looks good — "
            "trust the runner."
        ),
        "weights": {
            "subjective_rating": 0.4,
            "hrv_trend": 0.25,
            "sleep_quality": 0.20,
            "resting_hr_trend": 0.15
        },
        "override_rule": (
            "If subjective_rating <= 2 (Tired/Exhausted), always downgrade "
            "regardless of wearable data. Runner's feel > numbers."
        )
    },

    "auto_adjustments": [
        "If composite score < 40: suggest rest day or easy walk",
        "If composite score 40-60: downgrade hard sessions to moderate",
        "If composite score 60-80: proceed as planned",
        "If composite score > 80: optional stretch goal offered (not pushed)"
    ]
}
```

### Why Deferred

- Each wearable platform has its own API with different data formats, access requirements, and rate limits
- HRV interpretation requires establishing a personal baseline (7-14 days of data) before it's useful
- Risk of over-relying on wearable data vs. runner's subjective feel — needs careful UX research
- V1's subjective check-in covers the primary readiness use case effectively

### V2 Implementation Notes

- Start with Garmin and Apple Watch (largest running user bases)
- Always show both subjective and objective readiness — never hide the subjective option
- Consider a "learning period" where V2 shows wearable data but doesn't auto-adjust until baseline is established
- Display simple trend arrows (↑ ↓ →) rather than raw numbers to avoid data overload

---

## V2-3: Altitude and Terrain Adjustments

**Original spec location**: Section 1.1 (Climate Adjustments)

**Summary**: V1 handles heat and humidity pace adjustments (approximately 1-2 sec/km per °C above 15°C). V2 adds altitude acclimatization and terrain-based effort adjustments.

### Altitude Adjustment

```python
ALTITUDE_ADJUSTMENT_CONFIG = {
    "description": (
        "Running at altitude increases physiological stress due to reduced oxygen "
        "availability. Pace adjustments are needed for runners training or racing "
        "above ~1,200m (4,000ft)."
    ),

    "adjustment_model": {
        "baseline_altitude_m": 0,
        "threshold_m": 1200,
        "pace_penalty": {
            "1200_1800m": "3-5% slower per km",
            "1800_2400m": "6-10% slower per km",
            "2400_3000m": "12-18% slower per km",
            "above_3000m": "20%+ slower — recommend altitude-specific coaching"
        }
    },

    "acclimatization": {
        "description": (
            "Runners moving to altitude need 1-3 weeks to acclimatize. "
            "The app should gradually reduce pace penalties as acclimatization progresses."
        ),
        "timeline": {
            "day_1_3": "Full pace penalty applied",
            "day_4_7": "Reduce penalty by 25%",
            "day_8_14": "Reduce penalty by 50%",
            "day_15_21": "Reduce penalty by 75%",
            "day_22_plus": "Minimal penalty (fully acclimatized)"
        }
    },

    "user_input": {
        "training_altitude_m": "User's typical training location elevation",
        "race_altitude_m": "Race venue elevation (if different)",
        "arrived_at_altitude_date": "For acclimatization tracking"
    }
}
```

### Terrain-Based Effort Adjustment

```python
TERRAIN_ADJUSTMENT_CONFIG = {
    "surfaces": {
        "road": {"effort_multiplier": 1.0, "description": "Baseline"},
        "track": {"effort_multiplier": 0.95, "description": "Slightly faster due to surface"},
        "trail_groomed": {"effort_multiplier": 1.10, "description": "Well-maintained trail"},
        "trail_technical": {"effort_multiplier": 1.25, "description": "Rocky, rooty, uneven"},
        "sand": {"effort_multiplier": 1.30, "description": "Beach running"},
        "treadmill": {"effort_multiplier": 0.95, "description": "No wind resistance, set 1% incline"}
    },

    "elevation_gain_adjustment": {
        "description": (
            "For hilly routes, adjust expected pace based on elevation gain. "
            "Roughly 12-15 seconds per km added per 100m of elevation gain."
        ),
        "formula": "adjusted_pace = base_pace + (elevation_gain_per_km * 0.13)"
    }
}
```

### Why Deferred

- Requires elevation data for user's training routes (GPS + elevation API)
- Acclimatization tracking is complex and individual-specific
- Terrain detection from GPS data is unreliable — would need user input or route database integration
- V1's treadmill/trail/road surface preference (Section 8B.6) covers basic terrain needs

### V2 Implementation Notes

- Integrate with Google Elevation API or similar for route elevation profiles
- Consider partnership with route databases (Strava segments, AllTrails) for terrain data
- For altitude, start with a simple "What altitude do you train at?" onboarding question
- Terrain adjustments can initially be user-declared per workout rather than auto-detected

---

## V2-4: Voice Coaching During Runs

**Original spec location**: Section 12.6 (In-Run Notifications & Audio Cues)

**Summary**: V1 provides audio cues for pace alerts, interval timing, walk break prompts, and hydration reminders. V2 adds spoken motivational coaching and real-time verbal feedback during runs.

### Voice Coaching Features

```python
VOICE_COACHING_CONFIG = {
    "description": (
        "Real-time spoken feedback during runs, overlaid on the runner's music. "
        "Goes beyond V1 audio cues (beeps/chimes) to actual voice guidance."
    ),

    "coaching_types": {
        "pace_guidance": {
            "examples": [
                "You're right on target pace. Great job.",
                "You've drifted about 15 seconds fast. Let's ease back a touch.",
                "This is your recovery pace — nice and easy."
            ]
        },
        "interval_coaching": {
            "examples": [
                "Interval 3 of 6. Pick it up to your target pace now.",
                "30 seconds left in this hard effort. Strong finish.",
                "Recovery jog. Catch your breath before the next one."
            ]
        },
        "motivation": {
            "examples": [
                "You've completed 4 miles. More than halfway there.",
                "This is your longest run yet. You're building real endurance.",
                "Great week of training. Enjoy this easy finish."
            ]
        },
        "form_reminders": {
            "examples": [
                "Quick posture check — shoulders down, eyes forward.",
                "Relax your hands. No tight fists.",
                "Short quick steps on this hill. Don't overstride."
            ]
        }
    },

    "user_preferences": {
        "voice_frequency": ["minimal", "moderate", "chatty"],
        "voice_gender": ["male", "female", "neutral"],
        "coaching_focus": ["pace_only", "motivation_only", "full_coaching"],
        "quiet_periods": "User can set 'no coaching' for certain segments (e.g., last mile)"
    },

    "technical_requirements": {
        "tts_engine": "On-device TTS preferred for offline support",
        "music_ducking": "Lower music volume during coaching, restore after",
        "language_support": "Match user's app language setting"
    }
}
```

### Why Deferred

- Voice coaching requires high-quality text-to-speech (TTS) or pre-recorded audio
- Motivational coaching is subjective — some runners find it helpful, others find it annoying
- Requires significant UX testing to get tone and frequency right
- V1 audio cues (beeps, chimes, simple notifications) cover essential in-run guidance

### V2 Implementation Notes

- Consider using on-device TTS for offline support (critical for trail runners)
- Start with pace-only coaching as the simplest and most universally useful mode
- Allow users to preview and customize coaching voice before enabling
- Music ducking (lowering music volume during coaching) is essential — test with major music apps

---

## V2-5: Work Roster Import

**Original spec location**: Section 12.7a (Non-Standard Week Lengths)

**Summary**: V1 supports non-standard training cycles (5-14 days) for shift workers and healthcare professionals, with manual schedule entry or bi-weekly reminders to update availability. V2 adds automatic roster import from external sources.

### Roster Import Features

```python
ROSTER_IMPORT_CONFIG = {
    "description": (
        "Import a work shift roster so the app automatically identifies "
        "available training days without manual entry."
    ),

    "import_methods": {
        "ical_file": {
            "description": "Import .ics calendar file exported from rostering software",
            "parsing": "Identify shift blocks, extract start/end times, classify as work/off",
            "supported_software": [
                "Deputy", "When I Work", "Kronos", "RosterElf",
                "ShiftPlanning", "Humanity", "generic iCal"
            ]
        },
        "pdf_roster": {
            "description": "Upload a PDF roster image/document for OCR parsing",
            "parsing": "Extract shift patterns from tabular PDF layouts",
            "fallback": "If OCR fails, prompt user to manually confirm extracted schedule"
        },
        "manual_pattern": {
            "description": "Enter a repeating shift pattern (e.g., '2 days on, 2 days off, 3 days on, 2 days off')",
            "parsing": "Convert pattern to calendar availability"
        },
        "google_calendar_sync": {
            "description": "Sync with Google Calendar where work shifts are events",
            "parsing": "Filter events by keyword (e.g., 'shift', 'work', 'night duty')"
        }
    },

    "schedule_detection": {
        "classify_days": {
            "available": "Off days, or work days with enough free time for a run",
            "limited": "Early/late shift days where only short runs fit",
            "unavailable": "Night shifts, double shifts, or days with no free time"
        },
        "auto_update": "Re-import roster periodically or when user uploads a new one"
    },

    "integration_with_plan_generator": (
        "Once roster is imported, the plan generator treats it like the "
        "manual schedule input (Section 6.1) — identifying available days, "
        "time slots, and preferred workout placement automatically."
    )
}
```

### Why Deferred

- PDF roster parsing via OCR is unreliable across different roster formats
- iCal import requires handling dozens of rostering software export formats
- Google Calendar sync requires OAuth integration and event classification logic
- V1's manual schedule entry and bi-weekly update reminders cover the core need

### V2 Implementation Notes

- Start with iCal import as the most standardized format
- Manual repeating pattern entry (e.g., "4 on, 4 off") is the simplest and most reliable — consider promoting to V1 if demand is high
- PDF parsing should use LLM-based extraction rather than traditional OCR for better accuracy
- Google Calendar sync could piggyback on the ongoing platform sync infrastructure (V2-1)

---

## Priority and Dependencies

| Feature | Priority | Depends On | User Impact |
|---------|----------|-----------|-------------|
| V2-1: Ongoing Platform Sync | High | Strava/Garmin API partnerships | Reduces daily logging friction |
| V2-2: Wearable Integration | High | V2-1 (shared platform connections) | Better readiness assessment |
| V2-3: Altitude & Terrain | Medium | Elevation API integration | Accurate pacing for mountain/trail runners |
| V2-4: Voice Coaching | Medium | None | Enhanced in-run experience |
| V2-5: Roster Import | Low-Medium | None | Convenience for shift workers (manual entry works in V1) |

---

## Relationship to V1

All V2 features are additive — they enhance existing V1 functionality without changing core behavior. The V1 spec is fully functional without any V2 features. Where applicable, V1 data models already include placeholder fields for V2 data to ensure forward compatibility.

For the complete V1 specification, see `running-training-plan-spec.md`.
