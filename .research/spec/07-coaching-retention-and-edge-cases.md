# Running Training Plan App — Coaching, Retention & Edge Cases

> This module covers reduced availability weeks, race time prediction, public profiles,
> retention/motivation engine, weather-adjusted pacing, positive-only grading, pre-plan
> base building, in-run audio cues, non-standard week lengths, history import,
> and multi-race season planning.
> See `00-index.md` for the full file map.

## 11B. Reduced Availability Weeks

### 11B.1 Overview

Users may have weeks where they can still train, but with significantly less
time available (busy work weeks, family events, social commitments). Rather
than skipping or pausing, the app compresses the week's workouts into shorter
sessions while preserving the most important training stimulus.

### 11B.2 User Input

```json
{
  "reduced_week": {
    "week_start": "2026-05-04",
    "max_daily_minutes": 30,    // normally 60-90
    "available_days": ["tuesday", "thursday", "saturday"],  // normally 4-5 days
    "reason": "busy_week"       // optional, for analytics
  }
}
```

### 11B.3 Compression Algorithm

```python
def compress_week(plan, week, reduced_availability):
    """
    When a user flags a week as time-limited, compress workouts
    to fit the available time and days while preserving training value.

    Priority order for what to keep:
    1. Long run (shortened to fit max_daily_minutes)
    2. One quality session (tempo or intervals, shortened)
    3. Easy runs to fill remaining days

    What gets cut:
    - Recovery runs (lowest priority)
    - Extra easy runs beyond what fits in available days
    - Volume of each session (duration capped)
    """

    available_days = reduced_availability.available_days
    max_minutes = reduced_availability.max_daily_minutes
    original_workouts = week.workouts

    compressed = []

    # 1. Keep the long run, but cap its duration
    long_run = find_workout_type(original_workouts, "long_run")
    if long_run and len(available_days) > 0:
        shortened_long = shorten_workout(long_run, max_minutes)
        shortened_long.date = assign_to_day(available_days, prefer="last")
        compressed.append(shortened_long)
        available_days = remove_day(available_days, shortened_long.date)

    # 2. Keep one quality session (tempo > interval > race_pace)
    quality = find_highest_priority_quality_session(original_workouts)
    if quality and len(available_days) > 0:
        shortened_quality = shorten_workout(quality, max_minutes)
        # Shorten intervals: reduce reps, keep intensity
        if quality.type == "interval_run":
            shortened_quality.structure = reduce_interval_reps(
                quality.structure, max_minutes
            )
        shortened_quality.date = assign_to_day(available_days, prefer="middle")
        compressed.append(shortened_quality)
        available_days = remove_day(available_days, shortened_quality.date)

    # 3. Fill remaining days with easy runs
    for day in available_days:
        easy = create_easy_run(duration_minutes=max_minutes, date=day)
        compressed.append(easy)

    # 4. Calculate actual weekly volume and note the difference
    original_volume = sum(w.target_distance_km for w in original_workouts)
    compressed_volume = sum(w.target_distance_km for w in compressed)
    volume_pct = round((compressed_volume / original_volume) * 100)

    week.workouts = compressed
    week.target_mileage_km = compressed_volume
    week.notes.append(
        f"Compressed week: {volume_pct}% of normal volume. "
        f"Maintained long run and one quality session."
    )

    # 5. Adjust next week if needed
    # If compressed week was < 60% of normal volume, next week
    # should not jump back to full volume — cap at 90% to smooth transition
    if volume_pct < 60:
        next_week = get_next_week(plan, week)
        if next_week:
            next_week.target_mileage_km *= 0.90
            next_week.notes.append(
                "Slightly reduced to smooth transition after compressed week."
            )

    return week
```

### 11B.4 API Endpoint

```
POST   /api/plans/:id/weeks/:wnum/reduce   — Flag a week as reduced availability
PUT    /api/plans/:id/weeks/:wnum/reduce    — Update reduced week parameters
DELETE /api/plans/:id/weeks/:wnum/reduce    — Revert to normal week
```

---

## 11C. Race Time Predictor

### 11C.1 Overview

The app passively estimates the user's projected race finish time based on
training data they've already logged. The predictor never asks users to enter
additional data — it works with whatever is available.

### 11C.2 Data Sources (priority order)

```python
def estimate_race_time(user_profile, plan):
    """
    Estimate projected race finish time using available data.
    Uses whichever data source is available, in priority order.

    The predictor is passive — it uses data the user has already
    logged through normal plan usage. No extra input required.
    """

    # SOURCE 1: Recent race result (most accurate)
    # If user has a race time for ANY distance, use VDOT equivalence
    # tables to predict the target distance.
    recent_race = get_most_recent_race_time(user_profile)
    if recent_race and is_within_months(recent_race.date, months=6):
        vdot = calculate_vdot(recent_race.distance, recent_race.time_seconds)
        predicted = vdot_predict(vdot, plan.race_distance)
        return {
            "method": "vdot_from_race",
            "predicted_time": predicted,
            "confidence": "high",
            "basis": f"Based on your {recent_race.distance} time of "
                    f"{format_time(recent_race.time_seconds)}"
        }

    # SOURCE 2: Logged workout paces (good accuracy)
    # Use average pace from recent easy runs and tempo runs to
    # estimate fitness level via Daniels' VDOT tables.
    recent_paces = get_logged_paces(plan, workout_types=["easy_run", "tempo_run"],
                                     last_n_weeks=4)
    if len(recent_paces) >= 5:
        estimated_vdot = estimate_vdot_from_training_paces(recent_paces)
        predicted = vdot_predict(estimated_vdot, plan.race_distance)
        return {
            "method": "vdot_from_training_paces",
            "predicted_time": predicted,
            "confidence": "medium",
            "basis": f"Based on your average training paces over the past 4 weeks"
        }

    # SOURCE 3: Completion rate + volume (rough estimate)
    # If no pace data, use plan adherence and mileage to give a range.
    if plan.weeks_completed >= 4:
        completion_rate = plan.actual_mileage / plan.target_mileage
        predicted_range = estimate_from_plan_adherence(
            plan.race_distance,
            plan.skill_level,
            completion_rate
        )
        return {
            "method": "plan_adherence",
            "predicted_time_range": predicted_range,  # e.g., "2:05 - 2:20"
            "confidence": "low",
            "basis": f"Based on {int(completion_rate*100)}% plan completion "
                    f"at {plan.skill_level} level"
        }

    # SOURCE 4: Not enough data yet
    return {
        "method": "insufficient_data",
        "predicted_time": None,
        "confidence": None,
        "basis": "Keep logging workouts — we'll have an estimate for you "
                "after a few more weeks of training."
    }
```

### 11C.3 Display Rules

```
RULES:
1. Show the prediction ONLY when confidence is medium or high.
   Low-confidence predictions show as a range, not a single number.

2. Never show a prediction before the user has completed at least 4 weeks
   of training (insufficient data leads to misleading numbers).

3. Update the prediction weekly (after the weekly summary calculates).
   Don't update mid-week — fluctuations would cause anxiety.

4. Show prediction as a trend over time:
   "Week 6: 2:12  →  Week 8: 2:08  →  Week 10: 2:05"
   This shows improvement and motivates continued training.

5. If the prediction worsens week-over-week, don't alarm the user:
   "Your predicted time shifted slightly. This is normal — deload weeks,
   weather, and fatigue all affect pace. The trend matters more than
   any single week."

6. Never claim the prediction is guaranteed. Always frame as:
   "Based on your training, you're on track for approximately..."

7. If user is in RPE mode (no pace data), prediction relies on
   SOURCE 3 (plan adherence) only. Show range, not exact time.
```

### 11C.4 VDOT Equivalence Reference

```json
{
  "vdot_sample_equivalences": {
    "description": "Sample VDOT values showing equivalent performances across distances",
    "note": "Full VDOT tables from Daniels' Running Formula should be implemented",
    "examples": [
      {"vdot": 30, "5K": "27:39", "10K": "57:26", "half": "2:07:16", "marathon": "4:29:31"},
      {"vdot": 35, "5K": "24:03", "10K": "49:54", "half": "1:50:24", "marathon": "3:52:07"},
      {"vdot": 40, "5K": "21:14", "10K": "44:06", "half": "1:37:48", "marathon": "3:24:39"},
      {"vdot": 45, "5K": "18:58", "10K": "39:22", "half": "1:27:36", "marathon": "3:03:02"},
      {"vdot": 50, "5K": "17:05", "10K": "35:26", "half": "1:19:07", "marathon": "2:45:36"},
      {"vdot": 55, "5K": "15:30", "10K": "32:11", "half": "1:11:56", "marathon": "2:31:14"},
      {"vdot": 60, "5K": "14:10", "10K": "29:25", "half": "1:05:47", "marathon": "2:19:07"}
    ]
  }
}
```

### 11C.5 API Endpoints

```
GET    /api/plans/:id/prediction          — Get current race time prediction
GET    /api/plans/:id/prediction/history   — Get prediction trend over weeks
```

---

## 11D. Public Profile & Plan Sharing

### 11D.1 Overview

Users can optionally share their training progress via a public URL. This
drives organic marketing (shared on social media, running forums, etc.)
and creates accountability.

### 11D.2 Public Profile Configuration

```typescript
interface PublicProfile {
  user_id: string;
  enabled: boolean;                    // default: false (opt-in)
  slug: string;                        // e.g., "john-runs-boston" → app.com/u/john-runs-boston
  display_name: string;                // what visitors see (can differ from account name)
  show_avatar: boolean;

  // Granular visibility controls — user picks what's public
  visibility: {
    race_goal: boolean;                // e.g., "Training for Boston Marathon — June 14, 2026"
    plan_progress: boolean;            // "Week 12 of 18 — 67% complete"
    completed_workouts: boolean;       // list of completed runs with dates
    distances: boolean;                // show km/miles per workout
    paces: boolean;                    // show pace data (some users want privacy here)
    weekly_mileage_chart: boolean;     // visual chart of weekly volume over time
    streak: boolean;                   // current training streak
    total_distance: boolean;           // cumulative km trained
    predicted_race_time: boolean;      // show the race time estimate
    injury_status: boolean;            // "Currently healthy" or "Recovering" (no details)
  };

  // Things that are NEVER shown publicly
  private_fields: [
    "email",
    "phone",
    "exact_location",
    "routes_or_gps_data",
    "injury_body_part_details",
    "readiness_check_in_scores",
    "payment_or_subscription_info"
  ];
}
```

### 11D.3 Public Profile Page Content

```
PUBLIC PROFILE URL: https://[app-domain]/u/{slug}

PAGE DISPLAYS (based on user's visibility settings):

HEADER:
  - Display name
  - Avatar (if enabled)
  - Race goal: "Training for [distance] — [race date]"
  - Plan progress bar: "Week 12 of 18"

STATS SECTION (based on toggles):
  - Total distance trained: "342 km"
  - Current streak: "14 days"
  - Plan completion rate: "89%"
  - Predicted finish time: "1:48:30" (if enabled and available)

ACTIVITY FEED (if completed_workouts enabled):
  - Reverse chronological list of completed workouts
  - Each entry shows: date, workout type, distance, pace (if enabled)
  - Example: "Mar 1 — Long Run — 18 km — 5:42/km"
  - Skipped workouts are NOT shown (no public shaming)

WEEKLY MILEAGE CHART (if enabled):
  - Simple bar chart showing weekly volume over the plan duration
  - Deload weeks visually distinct
  - Current week highlighted

FOOTER:
  - "Powered by [App Name]" with link to app
  - "Start your own training plan →" CTA button
```

### 11D.4 Sharing Features

```json
{
  "sharing_options": {
    "profile_link": {
      "description": "Copy link to public profile",
      "format": "https://[app-domain]/u/{slug}"
    },
    "achievement_card": {
      "description": "Auto-generated image card for social media sharing",
      "triggers": [
        "plan_started",
        "weekly_milestone",
        "new_longest_run",
        "plan_50_percent",
        "peak_week_completed",
        "taper_started",
        "race_day",
        "plan_completed"
      ],
      "format": "Open Graph image (1200x630px) with stats overlay",
      "includes": "App branding + CTA link",
      "example_text": "Week 12 of 18 done! 342 km trained for my first half marathon."
    },
    "weekly_summary_share": {
      "description": "Share weekly training summary as image or link",
      "includes": "Workouts completed, total distance, plan progress"
    }
  }
}
```

### 11D.5 Privacy & Safety Rules

```
RULES:
1. Public profiles are OPT-IN only. Default is private.
2. Users can disable their public profile at any time — page returns 404 instantly.
3. No GPS data, routes, or location information is ever shown publicly.
4. Injury details (body part, severity) are never shown — only
   "Currently healthy" or "Recovery mode" status if the user enables it.
5. Readiness check-in scores are never public.
6. Slug must be unique. Users can change it (old slug returns 404 after change).
7. Public profiles are indexable by search engines only if user opts in.
8. Rate-limit public profile page views to prevent scraping.
9. No comments or interaction on public profiles (keeps it simple,
   avoids moderation burden).
10. Achievement cards include app branding and a "Start your plan" CTA
    for organic acquisition.
```

### 11D.6 Data Model & API

```typescript
interface PublicProfileView {
  slug: string;
  display_name: string;
  avatar_url: string | null;
  race_goal: string | null;
  plan_progress: {
    current_week: number;
    total_weeks: number;
    completion_pct: number;
  } | null;
  stats: {
    total_distance_km: number;
    current_streak_days: number;
    predicted_race_time: string;   // formatted time string
  } | null;
  recent_workouts: PublicWorkoutEntry[] | null;
  weekly_mileage_chart_data: WeeklyMileagePoint[] | null;
}

interface PublicWorkoutEntry {
  date: Date;
  type: string;        // "Long Run", "Easy Run", "Tempo", etc.
  distance_km: number | null;
  pace_min_per_km: number | null;
}

interface WeeklyMileagePoint {
  week_number: number;
  mileage_km: number;
  week_type: "build" | "deload" | "taper" | "recovery";
}
```

```
API ENDPOINTS:

GET    /api/public/u/:slug                — Public profile page data (no auth required)
PUT    /api/users/:id/public-profile      — Update public profile settings
POST   /api/users/:id/public-profile/slug — Claim/change slug
DELETE /api/users/:id/public-profile      — Disable public profile
GET    /api/plans/:id/share-card/:type    — Generate achievement card image
```

---

## 11E. Retention & Motivation Engine

### 11E.1 The 90% Completion Target

The app redefines "plan completion" as hitting **90% of scheduled mileage**,
not 100% of workouts. Anything above 90% is framed as a bonus.

**Why 90%**: A University College Dublin study of nearly 300,000 marathoners
found that over 50% miss at least one full week of training and still finish
their race. The coaching consensus is that ~80% adherence produces successful
outcomes. Setting 90% as the target is ambitious but achievable — and
eliminates the guilt spiral that causes dropout.

([Runners Connect](https://runnersconnect.net/training-flexibility/),
[Outside Online](https://www.outsideonline.com/health/training-performance/missed-marathon-training-study-2023/))

```python
COMPLETION_MODEL = {
    "target_pct": 0.90,   # 90% of planned mileage = "plan complete"
    "bonus_zone": 0.90,   # anything above 90% is celebrated as bonus

    "weekly_scoring": {
        "green":  0.90,    # >= 90% of weekly mileage target
        "yellow": 0.75,    # 75-89% — solid week, on track
        "orange": 0.50,    # 50-74% — reduced week, still counts
        "red":    0.50     # < 50% — rough week, supportive messaging
    },

    "messaging": {
        "green": "Nailed it. {pct}% of your target this week.",
        "yellow": "Strong week — {pct}% is right in the zone.",
        "orange": "Every km counts. {pct}% keeps you moving forward.",
        "red": "Tough week — that's okay. {pct_overall}% overall and climbing."
    },

    "plan_completion_display": {
        # Instead of "You completed 42 of 48 workouts"
        # show: "You hit 93% of your training mileage — target achieved!"
        "metric": "percentage of total planned mileage completed",
        "threshold_for_celebration": 0.90,
        "above_target_label": "Bonus miles!",
        "achievement_unlocked_at": [0.50, 0.75, 0.90, 0.95, 1.00]
    }
}
```

### 11E.2 Streak System

Two streak types: **daily** (did you run today?) and **weekly** (did you hit your weekly target?).
Weekly streaks are the primary motivator. Daily streaks are secondary and more forgiving.

```python
STREAK_CONFIG = {
    "daily_streak": {
        "description": "Consecutive scheduled training days completed",
        "counts_as_completed": [
            "workout marked complete",
            "cross-training logged (if toggle enabled)",
            "shortened workout completed (any distance counts)"
        ],
        "grace_days_per_week": 1,
        # One missed scheduled day per week does NOT break the daily streak.
        # This prevents the all-or-nothing spiral.
        # Grace day resets every Monday.
        "streak_break_message": (
            "Your daily streak paused at {streak_days} days. "
            "That's {streak_days} days of showing up — impressive. "
            "Start your next streak today."
        ),
        "streak_milestones": [7, 14, 21, 30, 45, 60, 90, 120, 180],
        "display": "secondary"  # shown but not prominently
    },

    "weekly_streak": {
        "description": "Consecutive weeks hitting >= 90% of weekly mileage target",
        "threshold_pct": 0.90,  # matches the 90% completion target
        "deload_weeks": "auto_pass",
        # Deload weeks automatically count as streak-maintaining because
        # the reduced target IS the target. Users shouldn't feel penalized
        # for following the plan's built-in recovery.
        "taper_weeks": "auto_pass",
        # Same logic: taper is part of the plan, not a failure.
        "recovery_weeks": "auto_pass",
        # Post-race recovery weeks also auto-pass.
        "streak_break_message": (
            "Your weekly streak paused at {streak_weeks} weeks. "
            "You've been consistent for {streak_weeks} weeks straight — "
            "most runners never get close to that. Pick it back up this week."
        ),
        "streak_milestones": [4, 8, 12, 16, 20, 24],
        "display": "primary"  # prominently shown on dashboard and public profile
    }
}
```

### 11E.3 Milestone System (Replaces Badges)

Milestones are tied to real training achievements, not arbitrary gamification.
They evolve as the user progresses, avoiding the overjustification effect
where external rewards erode intrinsic motivation.

([PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC10998180/),
[Plotline](https://www.plotline.so/blog/streaks-for-gamification-in-mobile-apps))

```python
MILESTONES = {
    "distance_milestones": {
        "description": "Cumulative distance achievements",
        "thresholds_km": [50, 100, 250, 500, 750, 1000, 1500, 2000],
        "framing": "contextual",
        # Instead of "You ran 100 km!" show:
        # "You've run 100 km — that's further than London to Paris."
        "contextual_comparisons": {
            50: "further than the English Channel is wide",
            100: "the length of a flight from NYC to Philadelphia",
            250: "about the distance from LA to Las Vegas",
            500: "further than the length of Ireland",
            750: "the distance from San Francisco to Portland",
            1000: "nearly the length of Japan's main island",
            1500: "further than London to Barcelona",
            2000: "the distance from New York to Denver"
        }
    },

    "consistency_milestones": {
        "description": "Based on sustained effort over time",
        "types": [
            {"name": "First Week Done", "trigger": "1 week completed, >= 90%"},
            {"name": "Month Strong", "trigger": "4 consecutive green/yellow weeks"},
            {"name": "Base Builder", "trigger": "Base phase completed"},
            {"name": "Peak Performer", "trigger": "Peak week completed"},
            {"name": "Taper Trust", "trigger": "Taper completed without adding volume"},
            {"name": "Race Ready", "trigger": "Plan completed at >= 90%"},
            {"name": "Comeback Kid", "trigger": "Resumed after 7+ missed days and completed the plan"},
            {"name": "Weather Warrior", "trigger": "Completed a run on a day above 30°C or below -5°C"},
            {"name": "Early Bird", "trigger": "10 workouts completed before 7 AM"},
            {"name": "Double Down", "trigger": "Completed 2 plans on the platform"}
        ]
    },

    "personal_bests": {
        "description": "Track and celebrate PRs across the platform",
        "tracked": [
            "longest_single_run_km",
            "highest_weekly_mileage_km",
            "fastest_logged_pace_per_km",
            "longest_daily_streak",
            "longest_weekly_streak",
            "most_plans_completed"
        ],
        "messaging": "New personal best! Your longest run is now {distance} km — "
                    "that's {diff} km further than your previous best."
    }
}
```

### 11E.4 Context-Aware Nudges

Generic reminders don't work. Nudges must be personalized to the specific
workout, time of day, and user behavior patterns.

([PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC8076996/))

```python
NUDGE_ENGINE = {
    "workout_reminder": {
        "timing": "user's typical workout time minus 1 hour, OR morning of workout day",
        "personalization": True,
        "templates": {
            "easy_run": (
                "Easy {distance} km today — about {est_minutes} minutes "
                "at your usual pace. A good one for {time_of_day}."
            ),
            "long_run": (
                "Long run day: {distance} km on the schedule. "
                "Remember to hydrate beforehand and start easy."
            ),
            "tempo_run": (
                "Tempo day: {distance} km with {tempo_segment} km "
                "at threshold effort. This one builds race fitness."
            ),
            "interval_run": (
                "Intervals today: {structure}. "
                "These are short and sharp — you'll be done in {est_minutes} min."
            )
        }
    },

    "late_day_fallback": {
        "trigger": "Scheduled workout day, user hasn't logged by 4 PM local time",
        "message": (
            "Still have your {workout_type} on the calendar today. "
            "Short on time? Even a {fallback_minutes}-minute easy jog counts. "
            "Something always beats nothing."
        ),
        "fallback_workout": {
            "type": "easy_run",
            "duration_minutes": 15,
            "counts_toward_streak": True,
            "counts_toward_mileage": True
        }
    },

    "post_missed_day": {
        "trigger": "Day after a missed workout",
        "message": (
            "Yesterday's rest is today's fuel. "
            "You're at {overall_pct}% of your plan — right on track."
        ),
        "rule": "Never mention the specific missed workout. Forward-looking only."
    },

    "deload_week_anxiety": {
        "trigger": "First day of a deload week",
        "message": (
            "Deload week: your body absorbs fitness during rest, not during runs. "
            "Runners who follow their deload come back stronger. Trust the plan."
        )
    },

    "weekly_win": {
        "trigger": "End of a green or yellow week",
        "message": (
            "{pct}% of your mileage target this week. "
            "That's {consecutive_good_weeks} strong weeks in a row."
        )
    },

    "prediction_improvement": {
        "trigger": "Race time prediction improved week over week",
        "message": (
            "Your projected {race_distance} time improved to {predicted_time}. "
            "The training is working."
        )
    }
}
```

### 11E.5 "Just Show Up" Fallback Workouts

When the full workout feels overwhelming, offer a minimal version that still
counts. This prevents the all-or-nothing mindset that is the #1 driver of
plan abandonment.

```python
FALLBACK_WORKOUTS = {
    "description": (
        "Minimal workouts offered when user seems unlikely to complete "
        "the full session. Always framed positively."
    ),

    "trigger_conditions": [
        "User opens app on workout day but hasn't started by late afternoon",
        "User reports readiness as 4 (Tired) or 5 (Exhausted)",
        "User manually requests a shorter option"
    ],

    "options_by_original_type": {
        "easy_run": {
            "fallback": "15 min easy jog",
            "distance_km": None,  # time-based, no distance pressure
            "message": "Just 15 minutes. Lace up, jog easy, come home."
        },
        "long_run": {
            "fallback": "30 min easy run (50% of planned long run)",
            "distance_km": None,
            "message": "Half the long run is still a great workout. "
                      "30 minutes at easy effort."
        },
        "tempo_run": {
            "fallback": "20 min easy run with 5 min tempo finish",
            "distance_km": None,
            "message": "Easy jog with a tempo push at the end. "
                      "Short and effective."
        },
        "interval_run": {
            "fallback": "15 min easy run with 4 x 30-sec strides",
            "distance_km": None,
            "message": "Easy jog with a few strides to keep the legs sharp."
        }
    },

    "counting_rules": {
        "counts_toward_daily_streak": True,
        "counts_toward_weekly_mileage": True,  # actual distance logged
        "counts_toward_workout_completion": True,
        "marked_as": "completed_modified"  # distinct from full completion
                                           # for analytics but NOT shown to user
                                           # as lesser
    }
}
```

### 11E.6 Positive Framing Rules

```
MESSAGING RULES (enforced across all notifications and UI):

1. NEVER say "You missed..." — say "You completed X of Y this week."

2. NEVER show a red X on missed days — show nothing. Only show
   green checks on completed days. Absence of a check is neutral,
   not negative.

3. Frame partial completion as success:
   - "4 of 5 runs done — 80% is a strong week"
   NOT "You missed 1 workout this week"

4. Streak breaks are "paused," not "broken" or "lost":
   - "Your streak paused at 21 days" NOT "You lost your 21-day streak"

5. Weekly summaries lead with what was accomplished:
   - "32 km this week across 4 runs" NOT "You were 6 km short of target"

6. Deload and taper weeks are celebrated, not apologized for:
   - "Recovery week complete — your body is absorbing the training"
   NOT "Reduced volume this week"

7. NEVER compare users to other users. Only compare to their own
   past performance:
   - "Your longest run is now 18 km — up from 14 km last month"
   NOT "You're in the top 30% of runners this week"

8. When the user is below 50% for a week, don't pile on:
   - "Tough week — everyone has them. Your overall plan is at {pct}%
     and you've got {weeks_remaining} weeks to go."

9. The race time predictor should frame dips as normal:
   - "Your projected time shifted slightly. Weather, fatigue, and
     deload weeks all affect pace. The trend matters more than
     any single week."

10. End every weekly summary with a forward-looking statement:
    - "Next week: {next_week_summary}. You've got this."
```

### 11E.7 Early Confidence Building (Beginners)

```python
BEGINNER_ONBOARDING_BOOST = {
    "description": (
        "Beginner plans front-load easy wins in the first 2 weeks. "
        "Self-efficacy is the #1 predictor of plan completion — "
        "early success builds the belief that they can do this."
    ),

    "week_1_rules": {
        "target_achievability": "deliberately easy — 80% of estimated capacity",
        "celebration_frequency": "after every single completed workout",
        "messages": [
            "First run done! You're officially in training.",
            "Two down. You're building a habit.",
            "Week 1 complete — you're a runner now."
        ]
    },

    "week_2_rules": {
        "target_achievability": "still easy — 85% of estimated capacity",
        "introduce": "first gentle progression (e.g., 2 extra minutes)",
        "messages": [
            "You ran further than last week. The plan is working.",
            "Week 2 down — most people who make it this far finish their plan."
        ]
    },

    "first_month_milestone": {
        "trigger": "4 weeks completed at >= 75% mileage",
        "message": (
            "One month of training complete. Research shows that runners who "
            "make it through the first month are far more likely to reach "
            "the start line. You're on your way."
        ),
        "achievement_card": True  # auto-generate shareable image
    }
}
```

### 11E.8 API Endpoints

```
GET    /api/plans/:id/streaks                — Get current daily and weekly streaks
GET    /api/plans/:id/milestones             — Get earned milestones
GET    /api/users/:id/milestones             — Get all milestones across plans
GET    /api/plans/:id/completion             — Get 90% completion status and breakdown
POST   /api/plans/:id/workouts/:wid/fallback — Accept fallback workout for today
```

### 11E.9 Data Model Additions

```typescript
interface UserStreaks {
  daily_streak: {
    current: number;
    best: number;
    grace_days_used_this_week: number;
  };
  weekly_streak: {
    current: number;
    best: number;
    // Deload, taper, and recovery weeks auto-pass
  };
}

interface PlanCompletion {
  plan_id: string;
  target_mileage_km: number;          // total planned
  actual_mileage_km: number;          // total logged
  completion_pct: number;             // actual / target
  target_met: boolean;                // >= 0.90
  bonus_km: number;                   // anything above 90% target
  weeks_green: number;                // >= 90% weekly target
  weeks_yellow: number;               // 75-89%
  weeks_orange: number;               // 50-74%
  weeks_red: number;                  // < 50%
}

interface Milestone {
  id: string;
  user_id: string;
  type: "distance" | "consistency" | "personal_best";
  name: string;
  earned_at: Date;
  plan_id: string | null;
  shareable: boolean;
  share_card_url: string | null;
}

interface NudgeLog {
  id: string;
  user_id: string;
  nudge_type: string;
  sent_at: Date;
  workout_id: string | null;
  result: "completed" | "fallback_accepted" | "ignored" | null;
}
```

---

## 12. Edge Cases and Validation Rules

### 12.1 Plan Validation

```
VALIDATION RULES:

1. Plan duration must be within the allowed range for the distance
   (see Section 2.1 Duration Flexibility):
   - 5K:             4 – 24 weeks (default: 6-10 by level)
   - 10K:            6 – 24 weeks (default: 8-12 by level)
   - Half Marathon:  8 – 36 weeks (default: 10-14 by level)
   - Marathon:       12 – 52 weeks (default: 16-20 by level)

2. Starting mileage must not exceed 80% of peak mileage
   (ensures room for meaningful progression).

3. For compressed plans (below default duration), starting mileage
   must be >= 55% of peak mileage. If user's current mileage
   doesn't meet this, show a warning and suggest a longer plan
   (but allow override).

4. No single run should exceed:
   - 5K plan:   16 km
   - 10K plan:  19 km
   - Half plan: 24 km
   - Marathon:  37 km

5. At least 1 rest day per week for all levels.

6. Taper length is FIXED per distance regardless of plan duration.
   Taper must end on race week (never skip taper).

7. If user's current weekly mileage is significantly below
   the plan's starting mileage, prepend a "pre-plan" base
   building block of 2-4 weeks (or suggest a longer plan duration).

8. If race date would result in a plan starting in the past,
   offer:
   a. A compressed plan (if >= minimum safe duration remains)
   b. Suggest a later race date

9. For extended plans (above default duration):
   - Base phase extends to up to 40% of total weeks
   - Deload frequency may increase to every 3 weeks
   - Starting mileage may drop to as low as 8 km/week
   - Extra weeks are NOT wasted — they allow gentler progression
     and more recovery, which reduces injury risk

10. Plans over 36 weeks should include a progress reassessment
    at the halfway mark. If user has outpaced the plan, offer
    to compress the remaining weeks or add quality sessions
```

### 12.2 Injury/Illness Protocol (Legacy — see Section 11 for full system)

```
IF user reports pain or injury:
  → Immediately reduce plan to easy runs only
  → Suggest rest days
  → Recommend consulting a medical professional
  → Provide "return to running" protocol:
    - Day 1-3 back: 50% of pre-illness volume, all easy
    - Day 4-7: 70% volume, add one moderate effort
    - Week 2 back: 85% volume, resume quality sessions
    - Week 3: Full plan resumption
  → If >2 weeks off, apply Section 6.3 Scenario 3+
```

### 12.3 Weather-Adjusted Pace Targets

Section 1.1 establishes that heat/humidity adjustments exist in v1.
This subsection defines the algorithm. Research shows performance degrades
significantly above 15°C and especially above 60% humidity.

```python
def adjust_pace_for_weather(target_pace_sec_per_km, temperature_c, humidity_pct, user_location):
    """
    Adjust prescribed workout paces based on current weather conditions.
    Uses a weather API call keyed to user location at workout start time.

    Research basis:
    - ~1-2 sec/km slower per 1°C above 15°C (Ely et al., 2007)
    - Heat + humidity compound: heat index is more relevant than temp alone
    - Below 15°C: no adjustment (cold has minimal performance impact for
      distances up to marathon, aside from comfort)
    - Wind chill considered for perceived effort but NOT pace adjustment

    CRITICAL: Weather adjustments are NEVER used to penalize or "grade"
    a completed workout. They are ONLY used to adjust TARGETS before
    the workout starts and to contextualize results after.
    """

    if temperature_c <= 15:
        adjustment_sec = 0
    elif temperature_c <= 25:
        # Moderate heat: ~1.5 sec/km per degree above 15
        base_adjustment = (temperature_c - 15) * 1.5
        # Humidity multiplier: above 60%, compound effect
        humidity_factor = 1.0 + max(0, (humidity_pct - 60) / 100)
        adjustment_sec = base_adjustment * humidity_factor
    elif temperature_c <= 35:
        # High heat: ~2.5 sec/km per degree above 25, plus humidity
        base_adjustment = (10 * 1.5) + ((temperature_c - 25) * 2.5)
        humidity_factor = 1.0 + max(0, (humidity_pct - 50) / 80)
        adjustment_sec = base_adjustment * humidity_factor
    else:
        # Extreme heat: recommend indoor/treadmill or skip
        return {
            "adjusted_pace": None,
            "warning": (
                f"It's {temperature_c}°C with {humidity_pct}% humidity. "
                "We recommend running indoors or rescheduling. "
                "Heat illness risk is very high."
            ),
            "suggest_treadmill": True,
            "suggest_reschedule": True
        }

    adjusted = round(target_pace_sec_per_km + adjustment_sec)

    return {
        "original_pace_sec_per_km": target_pace_sec_per_km,
        "adjusted_pace_sec_per_km": adjusted,
        "adjustment_sec": round(adjustment_sec),
        "conditions": {
            "temperature_c": temperature_c,
            "humidity_pct": humidity_pct
        },
        "display_note": (
            f"Pace adjusted +{round(adjustment_sec)}s/km for "
            f"{temperature_c}°C and {humidity_pct}% humidity. "
            f"Same effort, slower pace — that's normal."
        )
    }
```

### 12.4 Workout Completion Grading (Positive-Only)

A major complaint about existing apps (Runna, Nike Run Club, Garmin) is
that they "grade" or "score" workouts in a way that makes runners feel
like failures when they run slower or shorter than prescribed. This app
takes a fundamentally different approach.

```python
WORKOUT_GRADING_PHILOSOPHY = {
    "core_principle": (
        "The app NEVER tells a user they failed a workout. Every completed "
        "workout is a positive. The grading system exists to inform the "
        "plan generator, NOT to judge the runner."
    ),

    "grading_rules": {
        "completed_as_planned": {
            "internal_score": 1.0,
            "user_message": "Nailed it! Right on target.",
            "emoji": None  # no emojis per app rules
        },
        "completed_slower_than_planned": {
            "internal_score": 0.85,
            "user_message": (
                "Run complete! Pace was a bit off target — that happens. "
                "Weather, sleep, stress, and dozens of other factors affect pace. "
                "What matters is you got out there."
            ),
            "check_weather": True,  # if weather was a factor, say so
            "weather_message": (
                "Pace was slower than target, but it was {temp}°C today. "
                "Your effort was likely right on track."
            )
        },
        "completed_shorter_than_planned": {
            "internal_score": 0.70,
            "user_message": (
                "You ran {actual_km} of {planned_km} km today. That still counts! "
                "Any run is better than no run."
            ),
            "plan_effect": "No automatic adjustment. Note for weekly review."
        },
        "completed_harder_than_planned": {
            "internal_score": 0.90,
            "user_message": (
                "You went harder than planned today. That enthusiasm is great, "
                "but make sure tomorrow is easy to balance the load."
            ),
            "plan_effect": "Flag for gray zone check (Section 5C). May suggest extra recovery."
        },
        "skipped": {
            "internal_score": 0.0,
            "user_message": None,  # don't rub it in
            "plan_effect": "Apply missed day algorithm (Section 6.3)"
        }
    },

    "what_we_never_do": [
        "Never show a letter grade (A, B, C, D, F)",
        "Never show a percentage score to the user (e.g., '72% completion')",
        "Never compare the user to other runners",
        "Never use red/failure colors for slower-than-planned runs",
        "Never penalize weather-affected runs in any visible metric",
        "Never say 'you should have...' or 'you failed to...'",
        "Never auto-decrease plan difficulty based on one bad workout"
    ],

    "internal_use_only": (
        "The plan generator uses completion data to adjust future weeks. "
        "If a user consistently runs 15%+ slower than prescribed paces, "
        "the plan generator silently adjusts future pace targets downward. "
        "If they consistently exceed targets, it adjusts upward. "
        "This happens automatically with no negative messaging."
    )
}
```

### 12.5 Pre-Plan Base Building for Absolute Beginners

The C25K program has a 72.7% dropout rate, largely because progression is
too aggressive. Week 5 is the critical failure point, jumping from 5-min
runs to a 20-min sustained run. The app should offer a gentler "pre-plan"
phase for runners who aren't ready for the main training plan.

([None to Run — C25K Alternative](https://www.nonetorun.com/blog/couch-to-5k-running-plan-alternative),
[Mel Magazine — C25K Reddit Discussion](https://melmagazine.com/en-us/story/couch-to-5k-c25k-reddit-training-plan-best-app))

```python
PRE_PLAN_BASE_BUILDING = {
    "description": (
        "For runners who cannot yet meet the minimum prerequisites for "
        "a beginner plan (Section 5G.2). This phase brings them from "
        "'I can walk 20 minutes' to 'I can run 15-20 minutes continuously.' "
        "It's a bridge TO the training plan, not part of it."
    ),

    "entry_criteria": {
        "5K": "Cannot yet run 1 mile (1.6 km) continuously",
        "10K": "Cannot yet run 2 miles (3.2 km) continuously",
        "half_marathon": "Cannot yet run 3 miles (4.8 km) continuously",
        "marathon": "Has not completed any race distance"
    },

    "phase_structure": {
        "duration_weeks": "4-8 (auto-adjusts based on progress)",
        "sessions_per_week": 3,
        "session_duration_minutes": "20-30",
        "method": "run/walk intervals with gradual progression",

        "progression": {
            "weeks_1_2": {
                "pattern": "Walk 3 min → Run 30 sec → repeat",
                "total_run_time": "5 min",
                "total_session": "20 min",
                "notes": "If 30 sec of running feels hard, that's OK. We start here."
            },
            "weeks_3_4": {
                "pattern": "Walk 2 min → Run 1 min → repeat",
                "total_run_time": "10 min",
                "total_session": "25 min",
                "notes": "You're doubling your run time. That's a big deal."
            },
            "weeks_5_6": {
                "pattern": "Walk 1.5 min → Run 2 min → repeat",
                "total_run_time": "14 min",
                "total_session": "25 min",
                "notes": "Running more than walking now."
            },
            "weeks_7_8": {
                "pattern": "Walk 1 min → Run 3 min → repeat",
                "total_run_time": "18 min",
                "total_session": "28 min",
                "notes": "Almost ready for your training plan!"
            }
        },

        "the_week_5_problem": {
            "description": (
                "C25K infamously jumps from 5-min runs to 20-min runs in Week 5. "
                "This app NEVER makes that jump. Progression between any two weeks "
                "is capped at 50% increase in continuous run duration. "
                "If 2 min → 3 min is a 50% jump, that's the maximum."
            ),
            "max_run_duration_increase_pct": 50,
            "max_run_duration_increase_minutes": 2  # absolute cap
        }
    },

    "graduation_to_main_plan": {
        "criteria": "Can run 15-20 minutes continuously without stopping",
        "test_workout": {
            "description": "Run at easy pace for 15 minutes. If completed, graduate.",
            "pass": "Start beginner training plan for chosen distance",
            "fail": "Continue pre-plan phase for 1-2 more weeks"
        },
        "celebration_message": (
            "You just ran 15 minutes straight! You're ready for your "
            "{race_distance} training plan. Let's go!"
        )
    },

    "dropout_prevention": [
        "Every session starts with a warm-up walk — never cold-start running",
        "Run intervals are at CONVERSATIONAL pace — if you can't talk, slow down",
        "Walking is part of the plan, not a failure",
        "Repeat any week that felt too hard — there's no penalty for that",
        "Show progress chart: total run minutes per week trending upward"
    ]
}
```

### 12.6 In-Run Notifications & Audio Cues

Runners want real-time guidance during workouts — not just a plan on paper.
The app should support push notifications and audio cues during active sessions.

```python
IN_RUN_NOTIFICATIONS = {
    "description": (
        "During an active workout, the app provides timely notifications "
        "via audio cues, vibration (watch), or push notification (phone). "
        "User configures which channels they want."
    ),

    "notification_types": {
        "interval_alerts": {
            "trigger": "Start/end of each interval, recovery period",
            "content": "3-2-1 beep for interval start; double beep for recovery",
            "applicable_workouts": ["interval_800m", "interval_1600m", "fartlek",
                                    "hill_repeats", "ladder_intervals", "pyramid_intervals"],
            "user_configurable": True
        },

        "pace_alerts": {
            "trigger": "Current pace deviates >10% from target for 30+ seconds",
            "content": {
                "too_fast": "You're running faster than planned. Ease back to save energy.",
                "too_slow": "Pace is a bit off — that's OK. Stay comfortable."
            },
            "applicable_workouts": ["tempo_run", "easy_run", "long_run"],
            "frequency_cap": "Max once per km",  # don't nag
            "user_configurable": True,
            "can_disable": True  # some runners find pace alerts stressful
        },

        "distance_milestones": {
            "trigger": "Every 1 km (or 1 mile in imperial)",
            "content": "{distance} done. Pace: {current_pace}. Keep it up.",
            "user_configurable": True,
            "custom_intervals": True  # user can set every 0.5 km, 2 km, etc.
        },

        "halfway_turnaround": {
            "trigger": "50% of planned distance or duration reached",
            "content": "Halfway point! Time to turn around if doing an out-and-back.",
            "applicable_workouts": ["easy_run", "long_run", "tempo_run"],
            "user_configurable": True
        },

        "hydration_reminders": {
            "trigger": "Every 20-30 minutes during runs > 45 min",
            "content": "Hydration check — take a sip if you have water.",
            "applicable_workouts": ["long_run"],
            "user_configurable": True
        },

        "run_walk_interval_alerts": {
            "trigger": "Start/end of run and walk phases",
            "content": "Run! / Walk.",
            "applicable_workouts": ["run_walk sessions"],
            "audio_distinction": "Different tones for run vs walk",
            "user_configurable": True
        },

        "warm_up_complete": {
            "trigger": "Warm-up duration elapsed",
            "content": "Warm-up done. Time for the main workout.",
            "applicable_workouts": ["tempo_run", "interval sessions"]
        },

        "cool_down_start": {
            "trigger": "Main workout complete",
            "content": "Great work! Easy jog cool-down for {cool_down_minutes} minutes.",
            "applicable_workouts": ["all with warm-up/cool-down"]
        },

        "pain_check_mid_run": {
            "trigger": "Midpoint of run when injury is active",
            "content": "Quick check: How's your {body_part}? (tap to rate 0-10)",
            "applicable_when": "active_injury",
            "links_to": "Section 11.7 run/stop decision tree"
        }
    },

    "user_preferences": {
        "audio_enabled": True,
        "vibration_enabled": True,
        "voice_coaching": False,        # v2 feature: spoken cues like "Great pace!"
        "notification_volume": "medium",
        "do_not_disturb_mode": True,    # suppress non-run notifications during workout
        "music_integration": (
            "Audio cues play over music without pausing it. "
            "Compatible with Spotify, Apple Music, etc."
        )
    }
}
```

### 12.7 Week Start Day Configuration

Different runners think of their training week differently. The app should
let users choose which day their week starts — this affects weekly mileage
display, streak tracking, and deload scheduling.

```python
WEEK_START_CONFIG = {
    "options": [
        "monday", "tuesday", "wednesday", "thursday",
        "friday", "saturday", "sunday"
    ],
    "default": "monday",

    "effects": [
        "Weekly mileage totals start/end on the chosen day",
        "Weekly streak tracking uses this boundary",
        "Deload weeks align to this cycle",
        "The 'week view' in the calendar starts on this day",
        "Weekly summary notifications send at end of this week boundary"
    ],

    "why_it_matters": (
        "A runner whose long run is Sunday might prefer Monday-Sunday weeks "
        "so their long run caps the week. A nurse who works Sat-Mon might "
        "prefer Tuesday as their week start. A runner whose long run is "
        "Saturday might prefer Saturday-Friday. This affects how they "
        "perceive their weekly mileage and progress."
    ),

    "data_model_addition": {
        "user_profile_field": "week_start_day",
        "type": "string",  # any day of the week
        "default": "monday"
    }
}
```

### 12.7a Non-Standard Week Lengths (Shift Workers, Healthcare, etc.)

Not everyone works Monday-Friday. Healthcare workers, firefighters, pilots,
retail staff, and many other professionals have rotating, staggered, or
compressed schedules. The app must support training cycles of ANY length,
not just 7-day weeks. This builds on Section 5D.5 (non-standard cycles
for 60+ runners) but applies it universally.

```python
NON_STANDARD_WEEK_CONFIG = {
    "description": (
        "For runners whose schedules don't fit a traditional 7-day week. "
        "A nurse on a 3-on-4-off / 4-on-3-off rotating schedule, a "
        "firefighter on 48-on-96-off, or a retail worker with inconsistent "
        "days off needs a plan built around THEIR cycle, not a calendar week."
    ),

    "supported_cycle_lengths": {
        "minimum_days": 5,
        "maximum_days": 14,
        "default": 7,
        "common_examples": [
            {"days": 5, "label": "5-day cycle", "use_case": "Compressed work week (4x10 schedule)"},
            {"days": 6, "label": "6-day cycle", "use_case": "6-on-1-off shift pattern"},
            {"days": 7, "label": "Standard week", "use_case": "Traditional Mon-Sun schedule"},
            {"days": 8, "label": "8-day cycle", "use_case": "Rotating shift (2 days, 2 nights, 4 off)"},
            {"days": 9, "label": "9-day cycle", "use_case": "3-on-3-off pattern"},
            {"days": 10, "label": "10-day cycle", "use_case": "Extended rotation (firefighters, EMS)"},
            {"days": 14, "label": "2-week cycle", "use_case": "Biweekly rotation schedules"}
        ]
    },

    "setup_options": {
        "option_a_fixed_cycle": {
            "label": "My schedule repeats every N days",
            "input": "User enters cycle length and marks available/unavailable days",
            "example": {
                "cycle_length": 8,
                "pattern": [
                    {"day": 1, "available": False, "note": "12-hour shift"},
                    {"day": 2, "available": False, "note": "12-hour shift"},
                    {"day": 3, "available": True,  "max_minutes": 45},
                    {"day": 4, "available": True,  "max_minutes": 90},
                    {"day": 5, "available": False, "note": "12-hour shift"},
                    {"day": 6, "available": False, "note": "12-hour shift"},
                    {"day": 7, "available": True,  "max_minutes": 120},
                    {"day": 8, "available": True,  "max_minutes": 60}
                ]
            }
        },

        "option_b_irregular_schedule": {
            "label": "My schedule changes week to week",
            "input": (
                "User enters their actual schedule 2-4 weeks at a time. "
                "The plan generator builds workouts around the specific "
                "days they mark as available."
            ),
            "how_it_works": (
                "Instead of repeating a fixed pattern, the user opens the "
                "calendar every 2-4 weeks and marks which days they're "
                "available and for how long. The plan generator fills in "
                "workouts around their availability."
            ),
            "reminder": "App reminds user to update their schedule every 2 weeks"
        },

        "option_c_roster_import": {
            "label": "Import my work roster (v2)",
            "description": (
                "Future feature: import a shift roster (iCal, PDF, or manual entry) "
                "and the app automatically identifies available training days."
            ),
            "status": "deferred_to_v2"
        }
    },

    "plan_generation_for_non_standard_cycles": {
        "description": (
            "The plan generator adapts all its logic to work with non-7-day cycles."
        ),
        "adaptations": [
            {
                "feature": "Weekly mileage targets",
                "adaptation": (
                    "Recalculated as: cycle_mileage = weekly_target * (cycle_days / 7). "
                    "A 10-day cycle with a 40 km/week target becomes 57 km per cycle."
                )
            },
            {
                "feature": "Deload frequency",
                "adaptation": (
                    "Expressed in cycles, not weeks. 'Deload every 3rd cycle' means "
                    "every 3rd cycle regardless of cycle length."
                )
            },
            {
                "feature": "Hard session spacing",
                "adaptation": (
                    "Still requires N easy/rest days between hard sessions, but "
                    "the algorithm schedules within the cycle, not within a week."
                )
            },
            {
                "feature": "Long run placement",
                "adaptation": (
                    "Placed on the day with the most available time in each cycle, "
                    "with an easy/rest day before and after."
                )
            },
            {
                "feature": "Streak tracking",
                "adaptation": (
                    "Weekly streak becomes 'cycle streak' — completing the target "
                    "number of runs per cycle, not per calendar week."
                )
            },
            {
                "feature": "Calendar display",
                "adaptation": (
                    "The calendar always shows dates (not 'Week 3 Day 2'). "
                    "Non-standard cycles are invisible in the UI — the user just "
                    "sees their workouts on specific dates."
                )
            },
            {
                "feature": "Polarized training validation",
                "adaptation": (
                    "Intensity distribution checked per cycle instead of per week. "
                    "The 80/20 rule still applies but over the cycle length."
                )
            }
        ]
    },

    "healthcare_worker_specific": {
        "notes": (
            "Healthcare workers (nurses, doctors, EMTs) face unique challenges: "
            "12-hour shifts that leave zero training time, night shifts that "
            "disrupt sleep and recovery, and rotating schedules that change "
            "every few weeks. The app should:"
        ),
        "features": [
            "Accept that some cycle-days have ZERO availability (12-hour shifts)",
            "Never schedule hard sessions after a night shift",
            "Account for sleep disruption in readiness scoring",
            "Allow marking days as 'night shift' which auto-reduces next-day intensity",
            "Support 2-3 runs per cycle if that's all that's available — quality over quantity"
        ],

        "night_shift_handling": {
            "user_marks": "Which days are night shifts",
            "plan_effect": (
                "Day after a night shift is auto-marked as rest or very easy only. "
                "No hard sessions within 24 hours of a night shift ending. "
                "This is similar to jet lag — circadian disruption affects performance "
                "and injury risk."
            )
        }
    }
}
```
```

### 12.8 Running History Import (Strava/Garmin/Apple Health)

Rather than relying solely on self-reported fitness during onboarding,
the app should offer to import running history from existing platforms.
This gives the plan generator accurate data about the user's actual
current fitness, paces, and training patterns.

```python
RUNNING_HISTORY_IMPORT = {
    "supported_platforms": [
        {
            "platform": "strava",
            "import_method": "OAuth2 API",
            "data_extracted": [
                "run activities (last 6 months)",
                "weekly mileage averages",
                "pace data by workout type",
                "longest recent run",
                "training frequency (runs per week)"
            ]
        },
        {
            "platform": "garmin_connect",
            "import_method": "OAuth2 API or .FIT file upload",
            "data_extracted": [
                "run activities with HR data",
                "VO2max estimate",
                "training status/load",
                "race predictions",
                "weekly mileage"
            ]
        },
        {
            "platform": "apple_health",
            "import_method": "HealthKit API (iOS only)",
            "data_extracted": [
                "workout records",
                "distance and pace",
                "heart rate zones",
                "cardio fitness (VO2max estimate)"
            ]
        },
        {
            "platform": "manual_entry",
            "import_method": "User enters key metrics",
            "data_fields": [
                "average_weekly_mileage_km",
                "runs_per_week",
                "longest_recent_run_km",
                "recent_race_times (optional)",
                "typical_easy_pace"
            ]
        }
    ],

    "how_imported_data_is_used": {
        "skill_level_assessment": (
            "Cross-reference imported data with the skill level assessment "
            "(Section 5G.7). Imported data takes priority over self-reporting "
            "because it's objective."
        ),
        "starting_mileage": (
            "Set plan starting mileage based on actual recent average, "
            "not a default. If user ran 30 km/week last month, the plan "
            "starts near 30 km/week, not at a generic beginner baseline."
        ),
        "pace_calibration": (
            "If VDOT mode selected, use recent race times or training paces "
            "to calibrate zones. Much more accurate than self-reported pace."
        ),
        "injury_risk_baseline": (
            "Import training load history to establish the 'chronic' baseline "
            "for ACWR calculation (Section 11.12) from day one, instead of "
            "waiting 4 weeks to build the baseline."
        )
    },

    "privacy": {
        "what_we_import": "Only running/workout data. No social, no photos, no routes.",
        "data_retention": "Imported data stored locally. User can delete at any time.",
        "ongoing_sync": (
            "Optional: keep syncing new workouts from Strava/Garmin so the "
            "plan generator always has current data. User controls this."
        )
    },

    "when_no_import": (
        "If user declines import, fall back to the questionnaire-based "
        "onboarding (Section 5G.7). The plan will still work — it just "
        "takes 2-3 weeks longer to calibrate accurately."
    )
}
```

### 12.9 Multi-Race Season Planning

The app enforces one active plan at a time, but many runners race multiple
times per season (e.g., a 10K tune-up race 4 weeks before a half marathon,
or a spring half marathon followed by a fall marathon). The app should
support this without requiring the user to start from scratch each time.

```python
MULTI_RACE_SUPPORT = {
    "description": (
        "While only one plan is active at a time, the app should make "
        "transitioning between plans seamless. A runner finishing a 10K "
        "plan who wants to start a half marathon plan shouldn't lose "
        "their fitness data or have to re-onboard."
    ),

    "tune_up_races": {
        "description": (
            "Races that happen DURING a training plan — not the goal race. "
            "Common examples: a 5K during half marathon training, a 10K "
            "during marathon training. These don't require a new plan."
        ),
        "handling": {
            "schedule_as_workout": (
                "The tune-up race replaces a planned hard session in that "
                "week. It does NOT add volume — it replaces."
            ),
            "taper": "No taper for tune-up races (maybe 1 easy day before)",
            "recovery": "1-2 easy days after, then resume plan",
            "pace_data": (
                "Race result feeds into the race time predictor (Section 11C) "
                "and can recalibrate VDOT paces."
            )
        },
        "ui_flow": (
            "User adds a tune-up race to their calendar. The app asks: "
            "'Is this your goal race or a tune-up?' If tune-up, it slots "
            "into the existing plan with minimal disruption."
        )
    },

    "plan_transitions": {
        "description": (
            "When a user finishes one plan (including recovery phase) and "
            "wants to start another for a different distance or date."
        ),
        "carry_forward": [
            "Current fitness level (weekly mileage, paces, VDOT)",
            "Injury history",
            "Schedule preferences",
            "Streak data (continues across plans)",
            "Pain tracking (if active)"
        ],
        "transition_options": {
            "immediate_next_plan": {
                "condition": "Recovery phase from previous plan is complete",
                "action": (
                    "Start new plan with starting mileage = 80% of previous "
                    "plan's peak mileage (fitness is maintained)."
                )
            },
            "off_season_gap": {
                "condition": "More than 2 weeks between plans",
                "action": (
                    "Auto-assess current fitness based on recent activity. "
                    "If user kept running, credit that. If they took time off, "
                    "apply the returning runner protocol."
                )
            },
            "stepping_up_distance": {
                "condition": "New plan is a longer distance (e.g., 10K → half)",
                "action": (
                    "Start the new plan's base phase at a level appropriate "
                    "for the user's demonstrated fitness from the completed plan. "
                    "Skip early base building if the runner is already above "
                    "the new plan's starting requirements."
                ),
                "message": (
                    "Great job finishing your 10K plan! Your fitness carries "
                    "over — we're starting your half marathon plan at a level "
                    "that matches where you are, not from scratch."
                )
            }
        }
    },

    "race_calendar": {
        "description": (
            "Users can add multiple future races to a calendar. The app "
            "helps them decide which is the goal race and which are tune-ups. "
            "Only one plan is generated at a time, but the calendar shows "
            "the full season view."
        ),
        "fields": {
            "race_name": "string",
            "race_date": "Date",
            "race_distance": "5K | 10K | half_marathon | marathon",
            "race_type": "goal | tune_up | fun_run",
            "registered": "boolean",
            "notes": "string"
        }
    }
}
```

---
