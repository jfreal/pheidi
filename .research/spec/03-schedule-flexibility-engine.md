# Running Training Plan App — Schedule Flexibility Engine

> This is the CORE DIFFERENTIATOR of the app. The schedule flexibility engine handles
> holidays, missed days, vacations, time budgets, shower/transition time, shift work,
> volume modes, mid-plan changes, and lunch break/commute runs.
> See `00-index.md` for the full file map.

## 6. Schedule Flexibility Engine

### 6.1 User Schedule Inputs

```json
{
  "user_schedule": {
    "race_date": "2026-06-14",
    "available_days": ["monday", "wednesday", "friday", "saturday"],
    "preferred_long_run_day": "saturday",
    "preferred_rest_days": ["sunday", "tuesday"],
    "holidays": [
      {"date": "2026-05-25", "name": "Memorial Day"},
      {"date": "2026-04-03", "name": "Good Friday"}
    ],
    "blocked_dates": [
      {"start": "2026-04-10", "end": "2026-04-14", "reason": "family vacation"}
    ],
    "max_daily_minutes": 90,
    "preferred_workout_time": "morning"
  }
}
```

### 6.2 Holiday Handling Algorithm

```python
def handle_holiday(plan, holiday_date, user_preferences):
    """
    When a scheduled run falls on a holiday, apply this decision tree:

    1. If it's an EASY or RECOVERY run:
       → Skip it entirely. No rescheduling needed.

    2. If it's a TEMPO or INTERVAL run:
       → Attempt to move to the nearest available day within
         the same week that has no hard workout scheduled.
       → If no available day exists, merge a shortened version
         into the next easy run (e.g., add 10 min of tempo to
         Wednesday's easy run).
       → If merging would exceed max_daily_minutes, skip it.

    3. If it's the LONG RUN:
       → Move to the next available day (even if in the next week).
       → If moved to next week, reduce the following week's long
         run by 10% to avoid back-to-back high-load weekends.
       → Never skip the long run entirely — it is the highest
         priority workout.
    """
    affected_workout = get_workout_on_date(plan, holiday_date)

    if affected_workout is None:
        return plan  # No conflict

    priority = get_workout_priority(affected_workout.type)
    # Priority: long_run > interval > tempo > race_pace > progression > easy > recovery

    if priority <= PRIORITY_EASY:
        return skip_workout(plan, holiday_date)

    elif priority <= PRIORITY_TEMPO:
        alt_day = find_nearest_available_day(
            plan, holiday_date,
            same_week=True,
            exclude_hard_workout_days=True
        )
        if alt_day:
            return move_workout(plan, holiday_date, alt_day)
        else:
            return merge_into_next_easy(plan, affected_workout,
                                        max_minutes=user_preferences.max_daily_minutes)

    else:  # Long run
        alt_day = find_nearest_available_day(
            plan, holiday_date,
            same_week=False,
            prefer_weekend=True
        )
        plan = move_workout(plan, holiday_date, alt_day)
        if is_next_week(alt_day, holiday_date):
            plan = reduce_next_long_run(plan, alt_day, reduction=0.10)
        return plan
```

### 6.3 Missed Day Recovery Algorithm

Based on expert coaching guidelines from Jason Koop (CTS), Jeff Galloway, and Luke Humphrey Running:

```python
def handle_missed_day(plan, missed_date, user_context):
    """
    Decision engine for when a user misses a scheduled workout.

    Key principle: It takes ~10 days of no training before meaningful
    fitness loss occurs. Missing 1-2 days has negligible impact.

    Sources:
    - Jason Koop / CTS: "Play on" after 1-2 missed days
    - Luke Humphrey: Return volume guidelines by days missed
    - Runners Connect: Gradual return protocol
    """

    consecutive_missed = count_consecutive_missed_days(plan, missed_date)
    missed_workout = get_workout_on_date(plan, missed_date)

    # === SCENARIO 1: Single missed day ===
    if consecutive_missed <= 2:
        if missed_workout.type in ["easy_run", "recovery_run"]:
            # Skip entirely, no adjustment needed
            return {
                "action": "skip",
                "message": "No adjustment needed. Resume your plan tomorrow.",
                "plan_changes": None
            }
        elif missed_workout.type == "long_run":
            # Highest priority — try to reschedule within 2 days
            alt_day = find_nearest_available_day(plan, missed_date, window_days=2)
            if alt_day:
                return {
                    "action": "reschedule",
                    "new_date": alt_day,
                    "message": f"Long run moved to {alt_day}. This is your most important workout.",
                    "plan_changes": move_workout(plan, missed_date, alt_day)
                }
            else:
                return {
                    "action": "skip_with_adjustment",
                    "message": "Can't reschedule long run this week. "
                              "Next week's long run will add 10% to compensate.",
                    "plan_changes": increase_next_long_run(plan, missed_date, factor=1.10)
                }
        else:
            # Tempo, intervals, etc. — resume plan, don't double up
            return {
                "action": "skip",
                "message": "Resume your plan as scheduled. Don't try to make this up.",
                "plan_changes": None
            }

    # === SCENARIO 2: Missed 3-7 days ===
    elif consecutive_missed <= 7:
        volume_reduction = 0.10  # Reduce next week by 10%
        return {
            "action": "reduce_and_resume",
            "volume_reduction_pct": volume_reduction,
            "message": f"Welcome back! Reducing this week's volume by {int(volume_reduction*100)}%. "
                      f"You'll be back to normal in 1-2 weeks.",
            "plan_changes": reduce_week_volume(plan, missed_date, volume_reduction),
            "gradual_return_weeks": 1
        }

    # === SCENARIO 3: Missed 1-2 weeks ===
    elif consecutive_missed <= 14:
        volume_reduction = 0.20
        return {
            "action": "reduce_and_resume",
            "volume_reduction_pct": volume_reduction,
            "message": f"Reducing volume by {int(volume_reduction*100)}% this week. "
                      f"Rebuilding over the next 2-3 weeks.",
            "plan_changes": apply_gradual_return(plan, missed_date,
                                                 reduction=volume_reduction,
                                                 rebuild_weeks=3),
            "gradual_return_weeks": 3
        }

    # === SCENARIO 4: Missed 2-4 weeks ===
    elif consecutive_missed <= 28:
        volume_reduction = 0.30
        return {
            "action": "replan",
            "volume_reduction_pct": volume_reduction,
            "message": "Significant time off. Reducing volume by 30% and rebuilding "
                      "over 4-6 weeks. Consider extending your race date if possible.",
            "plan_changes": apply_gradual_return(plan, missed_date,
                                                 reduction=volume_reduction,
                                                 rebuild_weeks=5),
            "suggest_race_date_extension": True,
            "gradual_return_weeks": 5
        }

    # === SCENARIO 5: Missed 4+ weeks ===
    else:
        return {
            "action": "restart",
            "message": "Extended time off detected. We recommend starting a new plan "
                      "or moving your race date. Your fitness base needs rebuilding.",
            "suggest_new_plan": True,
            "suggest_race_date_extension": True
        }
```

**Key source**: Research shows missing 7-13 days results in ~4.25% race time slowdown; missing 28+ days leads to ~8% slowdown ([Runners Connect](https://runnersconnect.net/weekly-mileage-progression-10-percent-rule/), [Luke Humphrey Running](https://lukehumphreyrunning.com/faq-marathon-training-and-missed-runs/)).

### 6.4 Preplanned Vacation Handling

When a user enters a future vacation, they choose one of two strategies:

#### Strategy A: "Redistribute my miles"

The app takes the total volume that would have been scheduled during the vacation
and distributes it across the surrounding weeks, subject to safety constraints.

```python
def handle_vacation_redistribute(plan, vac_start, vac_end, user_context):
    """
    Redistribute missed vacation volume into surrounding weeks.

    Constraints:
    - No single week may exceed 115% of what it was originally scheduled for
    - Single-session spike guard still applies (Section 4.3)
    - User's max_daily_minutes is respected
    - If redistribution would violate constraints, the surplus is dropped
      and user is notified how much volume couldn't be recovered
    """

    vacation_days = get_training_days_in_range(plan, vac_start, vac_end)
    missed_volume_km = sum(w.target_distance_km for w in vacation_days)

    # Define ramp windows: up to 2 weeks before and 2 weeks after
    pre_window_start = vac_start - timedelta(weeks=2)
    post_window_end = vac_end + timedelta(weeks=2)

    pre_weeks = get_weeks_in_range(plan, pre_window_start, vac_start)
    post_weeks = get_weeks_in_range(plan, vac_end, post_window_end)

    # Split redistribution: 40% pre-vacation, 60% post-vacation
    # (front-loading less to avoid fatigue going into the break)
    pre_budget_km = missed_volume_km * 0.40
    post_budget_km = missed_volume_km * 0.60

    redistributed_pre = distribute_volume_across_weeks(
        weeks=pre_weeks,
        extra_km=pre_budget_km,
        max_week_multiplier=1.15,
        max_daily_minutes=user_context.max_daily_minutes,
        spike_guard=True
    )

    redistributed_post = distribute_volume_across_weeks(
        weeks=post_weeks,
        extra_km=post_budget_km,
        max_week_multiplier=1.15,
        max_daily_minutes=user_context.max_daily_minutes,
        spike_guard=True
    )

    unrecovered_km = (pre_budget_km - redistributed_pre) + \
                     (post_budget_km - redistributed_post)

    # Zero out vacation days
    for day in vacation_days:
        plan = skip_workout(plan, day.date)

    # Notify user
    if unrecovered_km > 0:
        plan.add_note(vac_start, vac_end,
            f"Redistributed {round(missed_volume_km - unrecovered_km, 1)} km "
            f"around your vacation. {round(unrecovered_km, 1)} km couldn't be "
            f"safely recovered due to load limits — that's okay, "
            f"consistency matters more than total volume.")

    # Suggest cross-training during vacation
    plan.add_note(vac_start, vac_end,
        "Consider cross-training (swimming, cycling, hotel gym) "
        "to maintain cardiovascular fitness during this break.")

    return plan
```

#### Strategy B: "Just ease me back in"

Simpler approach — no volume redistribution. The app smooths the transition
with a slightly harder pre-vacation week and a gradual post-vacation return.

```python
def handle_vacation_ease_back(plan, vac_start, vac_end, user_context):
    """
    Buffer-week approach: don't try to make up lost volume.
    Focus on a smooth exit and re-entry.
    """

    vacation_days = get_training_days_in_range(plan, vac_start, vac_end)
    num_vacation_days = len(vacation_days)

    # Pre-vacation: move the long run and one quality session
    # to the days just before departure
    long_run = find_workout_type_in_range(vacation_days, "long_run")
    if long_run:
        pre_day = find_available_day_before(plan, vac_start, window_days=3)
        if pre_day:
            plan = move_workout(plan, long_run.date, pre_day)

    quality = find_highest_priority_quality_session(vacation_days)
    if quality:
        pre_day_2 = find_available_day_before(plan, vac_start, window_days=5)
        if pre_day_2:
            plan = move_workout(plan, quality.date, pre_day_2)

    # Zero out remaining vacation days
    for day in vacation_days:
        if day.status == "scheduled":
            plan = skip_workout(plan, day.date)

    # Post-vacation: gradual return based on duration
    if num_vacation_days <= 5:
        plan = reduce_week_volume(plan, vac_end, reduction=0.10)
    elif num_vacation_days <= 10:
        plan = apply_gradual_return(plan, vac_end, reduction=0.15, rebuild_weeks=1)
    else:
        plan = apply_gradual_return(plan, vac_end, reduction=0.20, rebuild_weeks=2)

    plan.add_note(vac_start, vac_end,
        "Consider cross-training during your trip to maintain fitness.")

    return plan
```

#### Vacation Entry UI Flow

```
1. User selects vacation dates on the calendar
2. App shows how many training days are affected and estimated volume loss
3. User chooses:
   a. "Redistribute my miles" — see Strategy A
   b. "Just ease me back in" — see Strategy B
4. App previews the adjusted plan (before/after comparison)
5. User confirms or adjusts
```

### 6.5 Workout Swap Rules

When users want to move workouts between days within the same week:

```
RULES:
1. Never place two hard workouts on consecutive days.
   Hard = interval, tempo, race_pace, long_run

2. Always maintain at least one easy or rest day between hard efforts.

3. The long run should preferably have an easy or rest day before it.

4. If swapping would violate rules 1-3, offer alternatives:
   a. Convert one of the adjacent hard workouts to an easy run
   b. Reduce the volume of one workout by 30%
   c. Suggest an alternative day

5. Weekly volume should remain within ±5% after any swaps.
```

### 6.6 Time-Per-Session Budgets (Day-Level Scheduling)

The current `max_daily_minutes` in Section 6.1 is a single global cap. In
reality, runners have drastically different availability on different days.
The plan generator must support day-level time budgets so it can place the
right workouts on the right days.

#### 6.6.1 Enhanced Schedule Input

```json
{
  "weekly_schedule": {
    "monday": {
      "available": true,
      "total_window_minutes": 60,
      "time_of_day": "morning",
      "transition_time": {
        "pre_run_minutes": 5,
        "post_run_minutes": 15,
        "includes_shower": true,
        "includes_commute_to_location": false,
        "commute_to_location_minutes": 0
      },
      "effective_run_minutes": 40,
      "notes": "before work — need shower after"
    },
    "tuesday": {
      "available": true,
      "total_window_minutes": 45,
      "time_of_day": "lunch",
      "transition_time": {
        "pre_run_minutes": 8,
        "post_run_minutes": 17,
        "includes_shower": true,
        "includes_commute_to_location": false,
        "commute_to_location_minutes": 0
      },
      "effective_run_minutes": 20,
      "notes": "lunch break — need to change + shower + eat"
    },
    "wednesday": {
      "available": true,
      "total_window_minutes": 75,
      "time_of_day": "evening",
      "transition_time": {
        "pre_run_minutes": 5,
        "post_run_minutes": 10,
        "includes_shower": false,
        "includes_commute_to_location": false,
        "commute_to_location_minutes": 0
      },
      "effective_run_minutes": 60,
      "notes": "evening — shower at home on own time"
    },
    "thursday": {
      "available": false,
      "total_window_minutes": 0,
      "time_of_day": null,
      "transition_time": null,
      "effective_run_minutes": 0,
      "notes": "kids' activities"
    },
    "friday": {
      "available": true,
      "total_window_minutes": 60,
      "time_of_day": "morning",
      "transition_time": {
        "pre_run_minutes": 5,
        "post_run_minutes": 15,
        "includes_shower": true,
        "includes_commute_to_location": false,
        "commute_to_location_minutes": 0
      },
      "effective_run_minutes": 40,
      "notes": "before work — need shower after"
    },
    "saturday": {
      "available": true,
      "total_window_minutes": 180,
      "time_of_day": "morning",
      "transition_time": {
        "pre_run_minutes": 5,
        "post_run_minutes": 10,
        "includes_shower": false,
        "includes_commute_to_location": false,
        "commute_to_location_minutes": 0
      },
      "effective_run_minutes": 165,
      "notes": "long run day — no time pressure"
    },
    "sunday": {
      "available": true,
      "total_window_minutes": 75,
      "time_of_day": "morning",
      "transition_time": {
        "pre_run_minutes": 5,
        "post_run_minutes": 10,
        "includes_shower": false,
        "includes_commute_to_location": false,
        "commute_to_location_minutes": 0
      },
      "effective_run_minutes": 60,
      "notes": "easy day"
    }
  },
  "global_defaults": {
    "default_pre_run_minutes": 5,
    "default_post_run_minutes": 10,
    "default_shower_minutes": 10,
    "default_changing_minutes": 5
  },
  "total_available_days": 6,
  "total_effective_run_minutes": 385,
  "user_selected_runs_per_week": 5,
  "preferred_long_run_day": "saturday",
  "preferred_rest_days": ["thursday"],
  "allow_cross_training_on_run_days": true
}
```

#### 6.6.1a Transition Time Budget Calculator

The app distinguishes between the user's TOTAL available window and their
EFFECTIVE running time. This is critical — a user who says "I have an hour
before work" actually has about 40 minutes of running time once you account
for changing, showering, and getting to the door.

```python
TRANSITION_PRESETS = {
    "no_shower_needed": {
        "label": "No shower needed (evening/weekend run, shower at home later)",
        "pre_run_minutes": 5,     # lace up, dynamic warm-up prep
        "post_run_minutes": 5,    # cool-down, stretch, done
        "total_overhead": 10
    },
    "shower_at_home": {
        "label": "Shower at home after (running from home, have time after)",
        "pre_run_minutes": 5,
        "post_run_minutes": 15,   # cool-down + shower + dress
        "total_overhead": 20
    },
    "shower_at_gym_or_work": {
        "label": "Shower at gym/work (need to be presentable after)",
        "pre_run_minutes": 8,     # get to facility, change clothes
        "post_run_minutes": 17,   # cool-down, shower, dress, dry hair
        "total_overhead": 25
    },
    "lunch_break_full": {
        "label": "Lunch break run (change + run + shower + eat)",
        "pre_run_minutes": 8,     # change clothes
        "post_run_minutes": 22,   # shower + dress + quick eat
        "total_overhead": 30
    },
    "gym_with_commute": {
        "label": "Gym/track with travel time",
        "pre_run_minutes": 15,    # drive/bike to location + change
        "post_run_minutes": 20,   # shower + drive back
        "total_overhead": 35
    },
    "custom": {
        "label": "Custom (I'll set my own times)",
        "pre_run_minutes": None,
        "post_run_minutes": None,
        "total_overhead": None
    }
}

def calculate_effective_run_time(total_window_minutes, transition_preset, custom_overrides=None):
    """
    Given a user's total available time window, subtract transition
    overhead to determine how many minutes they can actually run.

    Example:
        User says "I have 60 minutes before work"
        Preset: "shower_at_home" (20 min overhead)
        Effective run time: 40 minutes

    Example:
        User says "I have 45 minutes at lunch"
        Preset: "lunch_break_full" (30 min overhead)
        Effective run time: 15 minutes
        → App warns: "15 minutes of running may not be enough for a
          productive session. Consider extending your lunch window
          or using this day for a walk/stretch instead."
    """
    if custom_overrides:
        overhead = custom_overrides["pre_run_minutes"] + custom_overrides["post_run_minutes"]
    else:
        preset = TRANSITION_PRESETS[transition_preset]
        overhead = preset["total_overhead"]

    effective = total_window_minutes - overhead

    result = {
        "total_window_minutes": total_window_minutes,
        "overhead_minutes": overhead,
        "effective_run_minutes": max(effective, 0),
        "breakdown": {
            "pre_run": custom_overrides["pre_run_minutes"] if custom_overrides
                       else TRANSITION_PRESETS[transition_preset]["pre_run_minutes"],
            "running": max(effective, 0),
            "post_run": custom_overrides["post_run_minutes"] if custom_overrides
                        else TRANSITION_PRESETS[transition_preset]["post_run_minutes"]
        }
    }

    # Warnings for very short effective times
    if effective < 15:
        result["warning"] = (
            f"After accounting for changing and showering, you'd have "
            f"{effective} minutes to run. That's quite short — consider "
            f"using this day for walking, stretching, or a short "
            f"shakeout run instead."
        )
        result["suggest_alternatives"] = ["walking", "yoga", "stretching"]
    elif effective < 25:
        result["info"] = (
            f"You have {effective} minutes of running time after "
            f"transition. That's enough for a focused short run or fartlek."
        )

    return result
```

#### 6.6.1b Onboarding Schedule Setup Flow

During onboarding, the app walks the user through setting up their weekly
schedule. The key is making transition time EASY to set — most users won't
want to manually enter minutes for 7 days.

```python
SCHEDULE_ONBOARDING_FLOW = {
    "step_1_available_days": {
        "question": "Which days can you typically train?",
        "input": "multi-select day picker (Mon-Sun)",
        "default": ["monday", "wednesday", "friday", "saturday"]
    },

    "step_2_time_per_day": {
        "question": "How much total time do you have on each day? (include changing/shower time)",
        "input": "slider or quick-pick per day",
        "quick_picks": [30, 45, 60, 90, 120, "unlimited"],
        "notes": "Show different quick-pick options for weekdays vs weekends"
    },

    "step_3_transition_preset": {
        "question": "What's your typical post-run routine?",
        "input": "single-select from TRANSITION_PRESETS",
        "per_day_override": True,  # user can set different presets per day
        "example": (
            "If you run before work on weekdays (need shower) but "
            "from home on weekends (no rush), you can set different "
            "presets for each."
        )
    },

    "step_4_confirmation": {
        "display": (
            "Show a weekly calendar with:\n"
            "  - Total window per day\n"
            "  - Transition overhead per day\n"
            "  - EFFECTIVE running time per day (highlighted)\n"
            "  - Total weekly effective running time\n"
            "'Your plan will use these running times to build your schedule.'"
        ),
        "allow_adjustment": True
    }
}
```

#### 6.6.1c Warm-Up and Cool-Down Within the Time Budget

The effective running time must ALSO account for warm-up and cool-down
as part of the workout, not as additional overhead. The plan generator
should be transparent about this breakdown.

```python
def build_workout_time_breakdown(workout, effective_run_minutes, skill_level):
    """
    Show the user exactly how their time window breaks down.
    This helps set expectations — a 40-min effective window for a
    tempo run doesn't mean 40 minutes of tempo running.

    Example output for a tempo run in a 60-min total window:
    ┌─────────────────────────────────────────────────┐
    │ Your 60-minute window breakdown:                │
    │                                                 │
    │  5 min  — Get ready (change, lace up)           │
    │ 10 min  — Warm-up (easy jog + dynamic stretches)│
    │ 20 min  — Tempo run at target pace              │
    │  5 min  — Cool-down (easy jog)                  │
    │  5 min  — Static stretching                     │
    │ 15 min  — Shower + change                       │
    │─────────────────────────────────────────────────│
    │ 60 min  — Total                                 │
    └─────────────────────────────────────────────────┘
    """
    warm_up = get_warm_up_duration(workout.type, skill_level)
    cool_down = get_cool_down_duration(workout.type, skill_level)

    core_workout_time = effective_run_minutes - warm_up - cool_down

    if core_workout_time < 10:
        # Not enough time for a meaningful core workout
        return {
            "feasible": False,
            "message": (
                f"After warm-up ({warm_up} min) and cool-down ({cool_down} min), "
                f"only {core_workout_time} min remains for the core workout. "
                f"Consider a simpler workout type for this day."
            ),
            "suggest_alternatives": get_shorter_workout_alternatives(effective_run_minutes)
        }

    return {
        "feasible": True,
        "breakdown": {
            "warm_up_minutes": warm_up,
            "core_workout_minutes": core_workout_time,
            "cool_down_minutes": cool_down,
            "total_running_minutes": effective_run_minutes
        },
        "display_text": format_time_breakdown(workout, warm_up, core_workout_time, cool_down)
    }
```

#### 6.6.2 Workout-to-Time-Slot Placement Algorithm

```python
def place_workouts_in_schedule(week_plan, weekly_schedule, skill_level):
    """
    Place each workout into the appropriate day based on:
    1. Workout duration requirement vs day's max_minutes
    2. Workout type priority (long run placed first, then quality, then easy)
    3. Hard/easy spacing rules (Section 6.5)
    4. User preferences (preferred long run day, rest days)

    Key constraint: A workout is NEVER placed on a day where its
    estimated duration exceeds that day's max_minutes.
    """

    # Step 1: Place the long run on the preferred day
    long_run = get_workout_by_type(week_plan, "long_run")
    if long_run:
        preferred_day = weekly_schedule.preferred_long_run_day
        if weekly_schedule[preferred_day]["max_minutes"] >= long_run.estimated_minutes:
            assign_workout(long_run, preferred_day)
        else:
            # Find the day with the most available time
            best_day = find_day_with_most_time(
                weekly_schedule,
                exclude=weekly_schedule.preferred_rest_days,
                min_minutes=long_run.estimated_minutes
            )
            if best_day:
                assign_workout(long_run, best_day)
            else:
                # Long run doesn't fit any single day — split or cap
                long_run = cap_to_time_budget(long_run, max(
                    d["max_minutes"] for d in weekly_schedule.values() if d["available"]
                ))
                assign_workout(long_run, find_day_with_most_time(weekly_schedule))

    # Step 2: Place quality sessions (tempo, intervals) on high-budget days
    quality_sessions = get_workouts_by_priority(week_plan, ["tempo", "interval", "hill_repeats"])
    available_quality_days = get_days_with_budget(
        weekly_schedule,
        min_minutes=45,  # quality sessions need at least 45 min (warm-up + work + cool-down)
        exclude_assigned=True,
        respect_hard_spacing=True
    )
    for session, day in zip(quality_sessions, available_quality_days):
        if session.estimated_minutes <= weekly_schedule[day]["max_minutes"]:
            assign_workout(session, day)

    # Step 3: Fill remaining days with easy runs, cross-training, rest
    remaining_days = get_unassigned_available_days(weekly_schedule)
    remaining_workouts = get_unassigned_workouts(week_plan)
    for workout, day in zip(remaining_workouts, remaining_days):
        # Adjust workout duration to fit the day's budget
        workout = fit_to_time_budget(workout, weekly_schedule[day]["max_minutes"])
        assign_workout(workout, day)

    return week_plan
```

#### 6.6.3 Time-Capped Workout Adaptation

When a workout can't fit a day's time budget, the plan generator adapts it
rather than skipping it entirely.

```python
def fit_to_time_budget(workout, max_minutes):
    """
    Adapt a workout to fit within a time constraint.
    Preserve the workout's PRIMARY training stimulus.

    Examples:
    - 60-min tempo run in a 30-min slot → 30-min tempo with shortened warm-up/cool-down
    - 90-min easy run in a 45-min slot → 45-min easy run (distance auto-adjusts)
    - 70-min interval session in a 40-min slot → reduce interval count, keep intensity
    """
    if workout.estimated_minutes <= max_minutes:
        return workout  # fits already

    adapted = workout.copy()
    overage = workout.estimated_minutes - max_minutes

    if workout.type in ["easy_run", "recovery_run", "long_run"]:
        # Simply shorten — duration is the variable, not intensity
        adapted.duration_minutes = max_minutes
        adapted.distance_km = estimate_distance(max_minutes, workout.target_pace)
        adapted.notes = f"Shortened to fit your {max_minutes}-min window."

    elif workout.type in ["tempo_run"]:
        # Shorten warm-up and cool-down first, then reduce tempo duration
        adapted.warm_up_minutes = max(5, workout.warm_up_minutes - min(overage // 2, 5))
        adapted.cool_down_minutes = max(5, workout.cool_down_minutes - min(overage // 2, 5))
        remaining_overage = adapted.estimated_minutes - max_minutes
        if remaining_overage > 0:
            adapted.tempo_duration_minutes -= remaining_overage
        adapted.notes = "Compressed tempo — same intensity, shorter duration."

    elif workout.type in ["interval_800m", "interval_1600m", "hill_repeats",
                          "ladder_intervals", "pyramid_intervals"]:
        # Reduce number of repeats, keep intensity and rest intervals
        while adapted.estimated_minutes > max_minutes and adapted.repeats > 2:
            adapted.repeats -= 1
        adapted.notes = f"Reduced to {adapted.repeats} reps to fit your schedule."

    elif workout.type in ["fartlek"]:
        # Shorten total duration, keep interval structure
        adapted.duration_minutes = max_minutes
        adapted.notes = "Shortened fartlek — same variety, less time."

    return adapted
```

### 6.7 Training Volume Modes: Time-Crunched to Unlimited

The app should explicitly support different "volume modes" that shape the
entire plan structure. This isn't just about days per week — it's about the
fundamental training philosophy.

#### 6.7.1 Volume Mode Definitions

```python
TRAINING_VOLUME_MODES = {
    "minimal": {
        "label": "Time-Crunched (3 runs/week)",
        "description": (
            "Based on the Furman FIRST program. Just 3 key runs per week — "
            "a long run, a tempo run, and a speed session. Research shows "
            "participants set personal bests with this approach. Ideal for "
            "busy professionals, parents, or anyone with limited training time."
        ),
        "runs_per_week": 3,
        "hard_sessions_per_week": 2,        # tempo + speed; long run is separate
        "cross_training_days": 2,           # strongly recommended
        "rest_days": 2,
        "min_daily_minutes": 30,
        "philosophy": "intensity_over_volume",
        "weekly_time_commitment_hours": {"min": 3, "max": 5},
        "suitable_for": {
            "5K": ["beginner", "intermediate", "advanced"],
            "10K": ["beginner", "intermediate", "advanced"],
            "half_marathon": ["intermediate", "advanced"],
            "marathon": ["intermediate", "advanced"]
        },
        "notes": (
            "Not recommended for first-time half marathoners or marathoners. "
            "Requires a base of running 3x/week for 4+ weeks."
        ),
        "research": (
            "Furman FIRST program: 21 participants, all finished, 15 set PRs. "
            "3 key workouts per week with cross-training on off-days."
        )
    },

    "moderate": {
        "label": "Standard (4-5 runs/week)",
        "description": (
            "The classic approach used by most training plans. Enough volume "
            "to build a strong aerobic base with room for quality sessions. "
            "Works for all distances and experience levels."
        ),
        "runs_per_week": {"min": 4, "max": 5},
        "hard_sessions_per_week": {"min": 1, "max": 2},
        "cross_training_days": {"min": 0, "max": 2},
        "rest_days": {"min": 1, "max": 2},
        "min_daily_minutes": 30,
        "philosophy": "balanced",
        "weekly_time_commitment_hours": {"min": 4, "max": 8},
        "suitable_for": {
            "5K": ["beginner", "intermediate", "advanced"],
            "10K": ["beginner", "intermediate", "advanced"],
            "half_marathon": ["beginner", "intermediate", "advanced"],
            "marathon": ["beginner", "intermediate", "advanced"]
        },
        "notes": "Default mode. Suitable for all runners."
    },

    "high": {
        "label": "High Volume (6-7 runs/week)",
        "description": (
            "For dedicated runners with significant available time. Higher "
            "weekly mileage through more frequent runs. May include some "
            "short 'shakeout' runs or easy doubles."
        ),
        "runs_per_week": {"min": 6, "max": 7},
        "hard_sessions_per_week": {"min": 2, "max": 3},
        "cross_training_days": {"min": 0, "max": 1},
        "rest_days": {"min": 0, "max": 1},
        "min_daily_minutes": 30,
        "philosophy": "volume_focused",
        "weekly_time_commitment_hours": {"min": 7, "max": 12},
        "suitable_for": {
            "5K": ["intermediate", "advanced"],
            "10K": ["intermediate", "advanced"],
            "half_marathon": ["intermediate", "advanced"],
            "marathon": ["intermediate", "advanced"]
        },
        "prerequisites": "Must be running 5+ days/week for 3+ months",
        "notes": (
            "Not for beginners. Requires demonstrated ability to handle "
            "high-frequency training without injury."
        )
    },

    "elite": {
        "label": "Elite Volume (doubles available)",
        "description": (
            "For experienced runners who want to maximize volume through "
            "two-a-day sessions. Doubles add easy mileage without extending "
            "any single session beyond comfortable range."
        ),
        "runs_per_week": {"min": 7, "max": 10},  # with doubles
        "hard_sessions_per_week": {"min": 2, "max": 3},
        "doubles_per_week": {"min": 1, "max": 3},
        "cross_training_days": 0,
        "rest_days": {"min": 1, "max": 1},       # at least 1 full rest
        "min_daily_minutes": 30,
        "philosophy": "maximum_volume",
        "weekly_time_commitment_hours": {"min": 10, "max": 16},
        "suitable_for": {
            "5K": ["advanced"],
            "10K": ["advanced"],
            "half_marathon": ["advanced"],
            "marathon": ["advanced"]
        },
        "prerequisites": "Must be running 50+ miles/week for 6+ months",
        "notes": (
            "Research shows the primary benefit of doubles is enabling more "
            "total volume — not a unique physiological advantage from splitting. "
            "Never replace the long run with two shorter runs."
        )
    }
}
```

#### 6.7.2 Double Run (Two-a-Day) Engine

For the "elite" volume mode, the app generates double sessions. Research shows
doubles work best when spaced 5-8 hours apart and the second run is always easy.

```python
def generate_double_run(primary_workout, user_schedule, weekly_mileage_target):
    """
    Add a second easy run to a training day when:
    1. User is in "elite" volume mode
    2. The day has enough total available time for both sessions
    3. The user has indicated AM and PM availability
    4. The primary session is NOT a long run (never split long runs)
    5. Adding the double doesn't push weekly volume above target + 5%

    The double is always:
    - Easy pace (Zone 1)
    - 20-40 minutes
    - Placed in the opposite time slot from the primary run
    """

    if primary_workout.type == "long_run":
        return None  # never double on long run day

    double = {
        "type": "easy_run",
        "duration_minutes": 30,  # default; adjust based on volume needs
        "intensity": "zone_1",
        "time_of_day": "pm" if primary_workout.time_of_day in ["morning", "am"] else "am",
        "is_double": True,
        "spacing_hours_min": 5,
        "notes": (
            "Easy double — keep this truly easy. The purpose is volume, "
            "not intensity. If you feel fatigued, skip it."
        )
    }

    # Check volume budget
    current_week_volume = get_current_week_volume(weekly_plan)
    double_volume = estimate_distance(30, user_pace.easy_pace)
    if current_week_volume + double_volume > weekly_mileage_target * 1.05:
        return None  # would exceed weekly budget

    return double

DOUBLE_RUN_RULES = {
    "spacing": "Minimum 5 hours between runs; 7-8 hours is optimal",
    "intensity": "Second run is ALWAYS easy (Zone 1). No exceptions.",
    "nutrition": "Eat a meal between runs. Recovery nutrition is critical.",
    "sleep": "If sleeping less than 7 hours, do NOT add doubles.",
    "long_run_day": "Never schedule a double on long run day.",
    "hard_day": "After a hard session (tempo/intervals), the double should be "
                "20 min max — just a shakeout.",
    "progression": (
        "Start with 1 double per week. Add a second after 3 weeks "
        "if recovery metrics are positive. Maximum 3 per week."
    ),
    "cancel_triggers": [
        "User reports fatigue score > 7/10",
        "Missed a night of sleep (< 5 hours)",
        "Any injury flag active",
        "During deload weeks — no doubles"
    ]
}
```

### 6.8 Runs Per Week as User-Configurable Input

Currently, runs per week is implicitly set by skill level (Section 5G.2).
The app should allow users to OVERRIDE this within safe boundaries based
on their schedule. The skill level sets the default, but the user decides
the final number.

```python
def configure_runs_per_week(skill_level, race_distance, user_request):
    """
    Let the user select how many days per week they want to run.
    Enforce safe minimums and maximums based on skill level and distance.
    """

    SAFE_RANGES = {
        "beginner": {
            "5K": {"min": 3, "max": 5, "default": 3},
            "10K": {"min": 3, "max": 5, "default": 3},
            "half_marathon": {"min": 3, "max": 5, "default": 4},
            "marathon": {"min": 3, "max": 5, "default": 4}
        },
        "intermediate": {
            "5K": {"min": 3, "max": 6, "default": 4},
            "10K": {"min": 3, "max": 6, "default": 4},
            "half_marathon": {"min": 4, "max": 6, "default": 5},
            "marathon": {"min": 4, "max": 6, "default": 5}
        },
        "advanced": {
            "5K": {"min": 4, "max": 7, "default": 5},
            "10K": {"min": 4, "max": 7, "default": 5},
            "half_marathon": {"min": 4, "max": 7, "default": 6},
            "marathon": {"min": 5, "max": 7, "default": 6}
        }
    }

    safe = SAFE_RANGES[skill_level][race_distance]

    if user_request < safe["min"]:
        return {
            "runs_per_week": safe["min"],
            "warning": (
                f"For a {race_distance} at {skill_level} level, we recommend "
                f"at least {safe['min']} runs per week. Running fewer may not "
                f"provide enough training stimulus for this distance."
            ),
            "allow_override": True,
            "override_minimum": 3  # absolute floor for any plan
        }

    if user_request > safe["max"]:
        return {
            "runs_per_week": safe["max"],
            "warning": (
                f"For your experience level, we recommend no more than "
                f"{safe['max']} runs per week. More can increase injury risk "
                f"without proportional benefit at this stage."
            ),
            "allow_override": True,
            "suggest_volume_mode": "high" if user_request >= 6 else None
        }

    return {
        "runs_per_week": user_request,
        "warning": None,
        "allow_override": False
    }
```

### 6.9 Mid-Plan Schedule Changes

Life changes happen mid-training. The app should handle schedule modifications
gracefully without regenerating the entire plan.

```python
def handle_schedule_change(plan, old_schedule, new_schedule, change_effective_date):
    """
    When a user updates their weekly schedule mid-plan:
    1. Compare old and new schedules
    2. Identify affected workouts from change_effective_date forward
    3. Reflow workouts into the new schedule
    4. Preserve weekly volume targets
    5. Show user a before/after preview

    Common scenarios:
    - Lost a training day (new job, new baby, etc.)
    - Gained a training day (summer schedule, retirement, etc.)
    - Changed time-of-day preferences
    - Changed max_minutes for specific days
    """

    changes = diff_schedules(old_schedule, new_schedule)

    # === Lost training days ===
    if changes["days_lost"] > 0:
        new_runs_per_week = count_available_days(new_schedule)
        old_runs_per_week = plan.config.runs_per_week

        if new_runs_per_week < 3:
            return {
                "status": "warning",
                "message": (
                    "You now have fewer than 3 training days per week. "
                    "This may not be enough for your current plan. Consider "
                    "pausing the plan or extending the duration."
                ),
                "options": [
                    "continue_with_fewer_days",
                    "pause_plan",
                    "extend_plan_duration"
                ]
            }

        # Redistribute workouts into fewer days
        remaining_weeks = get_remaining_weeks(plan, change_effective_date)
        for week in remaining_weeks:
            week = reflow_workouts(
                week,
                new_schedule,
                priorities=["long_run", "quality", "easy"],
                respect_hard_spacing=True
            )

            # If a quality session can't fit, convert to easy or merge
            unplaced = get_unplaced_workouts(week)
            for workout in unplaced:
                if workout.type in ["easy_run", "recovery_run"]:
                    week = drop_workout(week, workout)
                elif workout.type in ["tempo_run", "interval"]:
                    # Try to merge into another session as a combo workout
                    week = merge_quality_session(week, workout)

        return {
            "status": "adjusted",
            "message": (
                f"Schedule updated. You now have {new_runs_per_week} training days. "
                f"We've reflowed your remaining workouts to fit."
            ),
            "preview": generate_preview(remaining_weeks[:2])
        }

    # === Gained training days ===
    if changes["days_gained"] > 0:
        return {
            "status": "opportunity",
            "message": (
                f"You have {changes['days_gained']} more training days available. "
                "Would you like to add more easy runs, cross-training, or keep "
                "the extra days as rest?"
            ),
            "options": [
                {"label": "Add easy runs", "effect": "increase_volume"},
                {"label": "Add cross-training", "effect": "add_cross_training"},
                {"label": "Keep as rest", "effect": "no_change"}
            ]
        }

    # === Time budget changes only ===
    if changes["time_changes"]:
        remaining_weeks = get_remaining_weeks(plan, change_effective_date)
        for week in remaining_weeks:
            for day in week.days:
                if day.day_name in changes["time_changes"]:
                    new_max = new_schedule[day.day_name]["max_minutes"]
                    if day.workout and day.workout.estimated_minutes > new_max:
                        day.workout = fit_to_time_budget(day.workout, new_max)

        return {
            "status": "adjusted",
            "message": "Time budgets updated. Some workouts may be shortened to fit."
        }
```

### 6.10 Lunch Break & Commute Runs

Short time windows during the workday are legitimate training opportunities.
The app should have specific workout templates for these scenarios.

```python
SPECIAL_TIME_SLOTS = {
    "lunch_break": {
        "typical_window_minutes": 45,   # 30-60 min
        "effective_run_minutes": 30,    # subtract changing/showering time
        "suitable_workouts": [
            "easy_run",         # 30-min easy is always appropriate
            "strides",          # easy run + 4-6 strides at end
            "tempo_run",        # short tempo: 10 min warm-up, 15 min tempo, 5 min cool-down
            "fartlek"           # 30-min fartlek
        ],
        "not_suitable": [
            "long_run",         # can't fit
            "interval_1600m",   # needs too much recovery time between reps
            "hill_repeats"      # logistics — need a hill
        ],
        "user_config": {
            "has_shower_at_work": True,    # affects available time
            "commute_to_lunch_spot": 0,    # minutes to reach run location
            "changing_time_minutes": 15    # time to change clothes
        },
        "notes": (
            "Lunch runs are excellent for easy/moderate effort. "
            "Keep hard sessions for days with more time to warm up "
            "and cool down properly."
        )
    },

    "commute_run": {
        "description": (
            "Running to or from work as a training session. The app "
            "should count this as a regular run and adjust accordingly."
        ),
        "configuration": {
            "commute_distance_km": None,    # user enters their commute distance
            "direction": "to_work | from_work | both",
            "days_available": [],           # which days user can commute-run
            "gear_logistics": (
                "User needs to plan for clothes/laptop transport. "
                "App can remind about prep the night before."
            )
        },
        "integration": (
            "If commute distance matches a scheduled easy run, replace "
            "the easy run with the commute run. The route is fixed but "
            "the pace/effort can still match the training goal."
        ),
        "weather_sensitivity": True,  # suggest alternatives on bad weather days
        "notes": "Count commute runs as regular training volume."
    },

    "early_morning": {
        "typical_window_minutes": 60,   # before work
        "notes": (
            "Common for busy parents/professionals. The app should "
            "schedule the workout the night before so the user knows "
            "exactly what to do when the alarm goes off."
        ),
        "pre_run_nutrition_reminder": True,
        "darkness_safety_note": True     # seasonal — remind about visibility
    },

    "late_evening": {
        "typical_window_minutes": 60,
        "notes": (
            "Some runners prefer evening sessions. The app should note "
            "that hard sessions close to bedtime may affect sleep quality."
        ),
        "sleep_impact_warning": True,    # warn if hard session after 8 PM
        "suggest_easy_over_hard": True   # prefer easy runs in late evening
    }
}
```

### 6.11 Schedule Flexibility Data Model Additions

```typescript
interface TransitionTime {
  pre_run_minutes: number;             // changing, lacing up, getting to start point
  post_run_minutes: number;            // cool-down transition, shower, changing back
  includes_shower: boolean;
  shower_minutes: number;              // 0 if no shower
  includes_commute_to_location: boolean;
  commute_to_location_minutes: number; // 0 if running from home
  preset: "no_shower_needed" | "shower_at_home" | "shower_at_gym_or_work"
        | "lunch_break_full" | "gym_with_commute" | "custom";
  total_overhead_minutes: number;      // pre_run + post_run (computed)
}

interface DaySchedule {
  day_name: string;                    // "monday" through "sunday"
  available: boolean;
  total_window_minutes: number;        // TOTAL time user has (including shower/changing)
  effective_run_minutes: number;       // total_window - transition overhead = actual running time
  transition_time: TransitionTime | null;  // null if day is unavailable
  time_of_day: "morning" | "lunch" | "afternoon" | "evening" | null;
  is_commute_run: boolean;
  commute_distance_km: number | null;
  notes: string;
}

interface WorkoutTimeBreakdown {
  total_window_minutes: number;        // what user sees as their "time slot"
  pre_run_minutes: number;             // changing, getting ready
  warm_up_minutes: number;             // dynamic warm-up (part of run time)
  core_workout_minutes: number;        // the actual workout
  cool_down_minutes: number;           // easy jog cool-down (part of run time)
  post_run_minutes: number;            // shower, changing back
  feasible: boolean;                   // does core_workout_minutes >= minimum?
}

interface WeeklyScheduleTemplate {
  days: Record<string, DaySchedule>;
  total_available_days: number;
  total_effective_run_minutes: number;  // sum of effective_run_minutes across available days
  user_selected_runs_per_week: number;
  volume_mode: "minimal" | "moderate" | "high" | "elite";
  effective_from: Date;                // supports mid-plan changes
}

interface DoubleRunConfig {
  enabled: boolean;
  max_doubles_per_week: number;
  am_availability: string[];           // days available for AM runs
  pm_availability: string[];           // days available for PM runs
  minimum_spacing_hours: number;       // default 5
  cancel_on_fatigue: boolean;
}

interface ScheduleChange {
  id: string;
  plan_id: string;
  old_schedule: WeeklyScheduleTemplate;
  new_schedule: WeeklyScheduleTemplate;
  effective_date: Date;
  reason: string | null;
  adjustment_type: "lost_days" | "gained_days" | "time_change" | "preference_change";
  user_approved: boolean;
}
```

### 6.12 Schedule Flexibility API Endpoints

```
GET    /api/users/:id/schedule                  — Get current weekly schedule template
PUT    /api/users/:id/schedule                  — Update weekly schedule (triggers mid-plan reflow)
GET    /api/plans/:id/schedule-preview          — Preview plan with new schedule before confirming
POST   /api/plans/:id/schedule-change           — Apply a schedule change mid-plan
GET    /api/plans/:id/volume-mode               — Get current volume mode
PUT    /api/plans/:id/volume-mode               — Change volume mode (triggers plan adjustment)
GET    /api/plans/:id/doubles-config            — Get double run configuration
PUT    /api/plans/:id/doubles-config            — Update double run settings
POST   /api/plans/:id/weeks/:wnum/double/:day   — Add a double run to a specific day
DELETE /api/plans/:id/weeks/:wnum/double/:day   — Remove a double run
GET    /api/plans/:id/time-slots                — Get time slot recommendations per day
```

---
