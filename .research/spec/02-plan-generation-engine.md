# Running Training Plan App — Plan Generation Engine

> This module covers training plan parameters, periodization phases, progression algorithms,
> workout types, warm-up/cool-down, polarized training, age-adjusted training, experience-level
> differentiation, race nutrition, mental preparation, and the plan generation algorithm.
> See `00-index.md` for the full file map.

## 2. Training Plan Parameters

### 2.1 Distance Configurations

Each race distance has a set of baseline parameters that vary by skill level.
Users can accept the **default duration** (research-backed average) or select
any custom duration within the allowed range. The plan generator adapts
progression rates, phase allocation, and deload frequency accordingly.

### Duration Flexibility

```json
{
  "duration_ranges": {
    "5K": {
      "minimum_weeks": 4,
      "default_beginner_weeks": 10,
      "default_intermediate_weeks": 8,
      "default_advanced_weeks": 6,
      "maximum_weeks": 24,
      "source": "Mayo Clinic 7-week plan; C25K 6-12 week range; Nike 4-8 week range"
    },
    "10K": {
      "minimum_weeks": 6,
      "default_beginner_weeks": 12,
      "default_intermediate_weeks": 8,
      "default_advanced_weeks": 8,
      "maximum_weeks": 24,
      "source": "Hal Higdon Novice 8-week; Outside Online 12-week; PureGym 8-10 week"
    },
    "half_marathon": {
      "minimum_weeks": 8,
      "default_beginner_weeks": 14,
      "default_intermediate_weeks": 12,
      "default_advanced_weeks": 10,
      "maximum_weeks": 36,
      "source": "Hal Higdon Novice 12-week; Nike 6-14 week; MOTTIV 10-week"
    },
    "marathon": {
      "minimum_weeks": 12,
      "default_beginner_weeks": 20,
      "default_intermediate_weeks": 16,
      "default_advanced_weeks": 16,
      "maximum_weeks": 52,
      "source": "Hal Higdon 18-week; BAA 20-week; Outside Online 24-week couch-to-marathon"
    }
  }
}
```

#### How Custom Durations Affect the Plan

```python
def adapt_plan_to_duration(config, user_selected_weeks):
    """
    When a user selects a custom duration, the plan generator adjusts:

    1. SHORTER than default → steeper progression, fewer deload weeks,
       higher starting mileage required. App warns if the user's
       current fitness may not support the compressed timeline.

    2. LONGER than default → gentler progression, more deload weeks,
       longer base phase, lower starting mileage acceptable.
       Great for injury-prone runners or those building from scratch.

    3. Phase allocation always follows the same percentages (Section 3)
       but applied to the new total duration.

    4. Taper length is FIXED (not scaled) — it's always the evidence-based
       duration for the distance regardless of plan length.
    """

    default_weeks = config.default_duration_weeks
    ratio = user_selected_weeks / default_weeks

    if ratio < 0.75:
        # Significantly compressed plan
        # Validate that user's current fitness can handle it
        min_starting_mileage = config.peak_weekly_mileage_km * 0.60
        if user_current_mileage < min_starting_mileage:
            return {
                "warning": True,
                "message": (
                    f"A {user_selected_weeks}-week plan for {config.distance} "
                    f"requires a starting mileage of at least "
                    f"{min_starting_mileage} km/week. Your current level is "
                    f"{user_current_mileage} km/week. Consider a longer plan "
                    f"or building your base first."
                ),
                "suggest_duration": default_weeks,
                "allow_override": True  # user can proceed at own risk
            }

        adapted = config.copy()
        adapted.duration_weeks = user_selected_weeks
        adapted.starting_weekly_mileage_km = max(
            config.starting_weekly_mileage_km,
            config.peak_weekly_mileage_km * 0.55
        )
        # Fewer deloads in compressed plans but never zero
        adapted.deload_frequency_weeks = max(
            config.deload_frequency_weeks,
            user_selected_weeks // 3  # at least 1 deload in the plan
        )
        return adapted

    elif ratio > 1.25:
        # Extended plan — more gradual, more recovery
        adapted = config.copy()
        adapted.duration_weeks = user_selected_weeks
        adapted.starting_weekly_mileage_km = max(
            config.starting_weekly_mileage_km * 0.60,
            8  # absolute minimum starting point (km)
        )
        # More deloads in extended plans
        adapted.deload_frequency_weeks = min(
            config.deload_frequency_weeks,
            3  # deload every 3 weeks for very long plans
        )
        # Extended base phase — up to 40% of total for long plans
        adapted.base_phase_pct = min(0.40, 0.25 + (ratio - 1.0) * 0.10)
        return adapted

    else:
        # Close to default — minor adjustments only
        adapted = config.copy()
        adapted.duration_weeks = user_selected_weeks
        return adapted
```

#### Duration Selection UI

```
USER FLOW:
1. User selects race distance
2. App shows default duration with explanation:
   "Most [beginner] runners train for a [half marathon] over
    [14 weeks]. This is based on plans from Hal Higdon, Nike,
    and published coaching research."
3. User can accept default OR tap "Choose my own duration"
4. Slider or number picker: [minimum] ←——→ [maximum] weeks
5. If user selects outside recommended range:
   - Below default: "This is a compressed plan. You'll need to
     already be running [X] km/week to start safely."
   - Above default: "A longer plan means gentler progression and
     more recovery time. Great for building a strong base."
6. If user selects below minimum: blocked with explanation.
7. If user selects above maximum: allowed with note that the plan
   will include an extended base-building phase.
```

### Distance Configurations (defaults shown, all adjustable via duration selection)

```json
{
  "distances": {
    "5K": {
      "beginner": {
        "duration_weeks": 10,
        "runs_per_week": 3,
        "peak_weekly_mileage_km": 24,
        "starting_weekly_mileage_km": 10,
        "longest_run_peak_km": 8,
        "taper_weeks": 1,
        "deload_frequency_weeks": 4,
        "run_walk_intervals": true
      },
      "intermediate": {
        "duration_weeks": 8,
        "runs_per_week": 4,
        "peak_weekly_mileage_km": 40,
        "starting_weekly_mileage_km": 24,
        "longest_run_peak_km": 10,
        "taper_weeks": 1,
        "deload_frequency_weeks": 4,
        "run_walk_intervals": false
      },
      "advanced": {
        "duration_weeks": 6,
        "runs_per_week": 5,
        "peak_weekly_mileage_km": 56,
        "starting_weekly_mileage_km": 40,
        "longest_run_peak_km": 13,
        "taper_weeks": 1,
        "deload_frequency_weeks": 4,
        "run_walk_intervals": false
      }
    },
    "10K": {
      "beginner": {
        "duration_weeks": 12,
        "runs_per_week": 3,
        "peak_weekly_mileage_km": 32,
        "starting_weekly_mileage_km": 14,
        "longest_run_peak_km": 12,
        "taper_weeks": 1,
        "deload_frequency_weeks": 4,
        "run_walk_intervals": true
      },
      "intermediate": {
        "duration_weeks": 8,
        "runs_per_week": 4,
        "peak_weekly_mileage_km": 48,
        "starting_weekly_mileage_km": 28,
        "longest_run_peak_km": 14,
        "taper_weeks": 1,
        "deload_frequency_weeks": 4,
        "run_walk_intervals": false
      },
      "advanced": {
        "duration_weeks": 8,
        "runs_per_week": 5,
        "peak_weekly_mileage_km": 64,
        "starting_weekly_mileage_km": 45,
        "longest_run_peak_km": 16,
        "taper_weeks": 1,
        "deload_frequency_weeks": 4,
        "run_walk_intervals": false
      }
    },
    "half_marathon": {
      "beginner": {
        "duration_weeks": 14,
        "runs_per_week": 3,
        "peak_weekly_mileage_km": 40,
        "starting_weekly_mileage_km": 16,
        "longest_run_peak_km": 18,
        "taper_weeks": 2,
        "deload_frequency_weeks": 4,
        "run_walk_intervals": true
      },
      "intermediate": {
        "duration_weeks": 12,
        "runs_per_week": 4,
        "peak_weekly_mileage_km": 56,
        "starting_weekly_mileage_km": 32,
        "longest_run_peak_km": 19,
        "taper_weeks": 2,
        "deload_frequency_weeks": 4,
        "run_walk_intervals": false
      },
      "advanced": {
        "duration_weeks": 10,
        "runs_per_week": 5,
        "peak_weekly_mileage_km": 72,
        "starting_weekly_mileage_km": 48,
        "longest_run_peak_km": 21,
        "taper_weeks": 2,
        "deload_frequency_weeks": 4,
        "run_walk_intervals": false
      }
    },
    "marathon": {
      "beginner": {
        "duration_weeks": 20,
        "runs_per_week": 4,
        "peak_weekly_mileage_km": 56,
        "starting_weekly_mileage_km": 24,
        "longest_run_peak_km": 32,
        "taper_weeks": 3,
        "deload_frequency_weeks": 4,
        "run_walk_intervals": true
      },
      "intermediate": {
        "duration_weeks": 16,
        "runs_per_week": 5,
        "peak_weekly_mileage_km": 72,
        "starting_weekly_mileage_km": 40,
        "longest_run_peak_km": 34,
        "taper_weeks": 3,
        "deload_frequency_weeks": 4,
        "run_walk_intervals": false
      },
      "advanced": {
        "duration_weeks": 16,
        "runs_per_week": 6,
        "peak_weekly_mileage_km": 96,
        "starting_weekly_mileage_km": 56,
        "longest_run_peak_km": 35,
        "taper_weeks": 3,
        "deload_frequency_weeks": 4,
        "run_walk_intervals": false
      }
    }
  }
}
```

### 2.2 Sources for These Parameters

These values are synthesized from the following evidence-based sources:

- Mayo Clinic 7-week 5K plan for beginners ([Mayo Clinic](https://www.mayoclinic.org/healthy-lifestyle/fitness/in-depth/5k-run/art-20050962))
- Hal Higdon's Novice through Advanced plans for all distances ([Hal Higdon](https://www.halhigdon.com/training/marathon-training/))
- Boston Athletic Association Level 1–3 marathon training guidelines ([BAA](https://www.baa.org/races/boston-marathon/info-for-athletes/boston-marathon-training/))
- Running Writings percentage-based 10K training methodology ([Running Writings](https://runningwritings.com/2024/10/percentage-based-10k-training.html))
- Scientific Triathlon's interview with coach John Davis on periodized marathon training ([Scientific Triathlon](https://scientifictriathlon.com/tts472/))
- Healthline 20-week marathon plans ([Healthline](https://www.healthline.com/health/exercise-fitness/20-week-marathon-training-plan))
- MyProCoach half marathon and marathon plans ([MyProCoach](https://www.myprocoach.net/free-training-plans/half-marathon/))

---

## 3. Training Phases (Periodization Model)

Every plan follows a four-phase periodization structure. The proportion of total weeks allocated to each phase scales by plan duration.

```
PHASE ALLOCATION (as percentage of total plan weeks):

Base Phase:      25% of total weeks (rounded up)
Build Phase:     45% of total weeks
Peak Phase:      ~10-15% of total weeks (1-2 weeks)
Taper Phase:     defined per distance/level (see Section 2.1)
```

### 3.1 Phase Definitions

#### Base Phase
- Purpose: Build aerobic foundation, adapt musculoskeletal system
- Intensity: 80% easy effort, 20% moderate
- Workout types: Easy runs, run/walk intervals (beginners), short long runs
- Weekly mileage: Start at `starting_weekly_mileage_km`, increase gradually

#### Build Phase
- Purpose: Develop race-specific fitness, increase mileage toward peak
- Intensity: 70% easy, 20% tempo/threshold, 10% intervals
- Workout types: Tempo runs, interval sessions, progressive long runs
- Weekly mileage: Progressive increase toward `peak_weekly_mileage_km`

#### Peak Phase (1–2 weeks)
- Purpose: Highest training load, race-specific sharpening
- Intensity: 65% easy, 20% race-pace, 15% VO2max
- Workout types: Race-pace long runs, tune-up races, hard intervals
- Weekly mileage: At or near `peak_weekly_mileage_km`

#### Taper Phase
- Purpose: Reduce fatigue while maintaining fitness for race day
- Volume reduction: 40–60% of peak volume, reduced progressively
- Intensity: Maintained (short race-pace efforts preserved)
- Frequency: Maintained (same number of run days)
- Source: Bosquet et al. (2007) meta-analysis found 41–60% volume reduction over 2 weeks optimal for endurance athletes ([Runners Connect](https://runnersconnect.net/weekly-mileage-progression-10-percent-rule/))

### 3.2 Taper Lock

The taper phase is locked and cannot be modified by the user. This protects
against the common mistake of panicking during taper and adding volume.

```python
TAPER_LOCK_RULES = {
    "modifications_blocked": [
        "add_workout",
        "increase_distance",
        "increase_intensity",
        "add_interval_session",
        "swap_easy_for_hard",
        "reschedule_to_add_volume"
    ],

    "modifications_allowed": [
        "skip_workout",           # Life happens, skipping is fine
        "reduce_distance",        # Feeling tired, shorter is okay
        "swap_hard_for_easy",     # Downgrading intensity is always safe
        "move_workout_same_week", # Rearranging days within the week
        "log_cross_training",     # Non-running activity is fine
        "report_injury"           # Safety always overrides lock
    ],

    "user_messaging": {
        "on_blocked_action": (
            "Your taper is locked to protect your race performance. "
            "Research shows that runners who stick to their taper "
            "finish ~2.6% faster than those who add extra training. "
            "Trust the process — you've done the work."
        ),
        "taper_start_notification": (
            "Taper starts this week! Your volume will drop but your "
            "fitness won't. You may feel restless — that's normal. "
            "Your body is absorbing all the training you've done."
        ),
        "race_week_notification": (
            "Race week! A short shakeout run (10-15 min easy) the day "
            "before is optional. Focus on sleep, hydration, and nutrition. "
            "Your plan has prepared you. Time to execute."
        )
    }
}
```

#### Taper Week-by-Week Volume Schedule

```
TAPER VOLUME BY DISTANCE (as % of peak weekly mileage):

5K / 10K (1-week taper):
  Race week:     50-60% of peak, maintain 1 short tempo or interval

Half Marathon (2-week taper):
  Taper week 1:  60-70% of peak, maintain 1 quality session (shortened)
  Race week:     40-50% of peak, 1 short shakeout + easy runs only

Marathon (3-week taper):
  Taper week 1:  75-80% of peak, maintain 1 tempo (shortened)
  Taper week 2:  55-65% of peak, 1 short interval session
  Race week:     30-40% of peak, shakeout run only, easy effort

ALL DISTANCES:
  - Long run in taper: cap at 60% of peak long run distance
  - No long run in race week
  - Last hard session: no later than 10 days before race
  - Final run before race: easy 10-15 min shakeout, 1-2 days before
  - Race day eve: rest or optional very short shakeout
```

#### Race Week Countdown (built into taper)

```json
{
  "race_week_countdown": {
    "day_minus_7": {
      "training": "Normal taper run",
      "tip": "Review the course map, note elevation and aid station locations."
    },
    "day_minus_6": {
      "training": "Easy run or rest",
      "tip": "Lay out your race kit — everything you'll wear should be tested in training."
    },
    "day_minus_5": {
      "training": "Short tempo or strides (last quality session for marathon)",
      "tip": "Finalize your race-day nutrition plan. Nothing new on race day."
    },
    "day_minus_4": {
      "training": "Easy run",
      "tip": "This is the most important sleep night — prioritize rest tonight."
    },
    "day_minus_3": {
      "training": "Easy run or rest",
      "tip": "Start carb-loading if racing half marathon or longer (70% of calories from carbs)."
    },
    "day_minus_2": {
      "training": "Optional shakeout (10-15 min easy) or rest",
      "tip": "Reduce fiber intake to avoid race-day GI issues. Stay hydrated."
    },
    "day_minus_1": {
      "training": "Rest or very easy 10 min shakeout",
      "tip": "Charge your watch. Check the weather forecast. Set two alarms. Bag is packed."
    },
    "race_day": {
      "training": "RACE DAY",
      "tip": "5-10 min easy jog warm-up + dynamic stretches. Start conservative — negative split wins races."
    }
  }
}
```

#### Post-Race Recovery Phase

After race day, the plan automatically appends a recovery phase. The user
does NOT need to create a new plan — recovery is part of the plan they purchased.

```python
def generate_post_race_recovery(plan, race_distance, skill_level):
    """
    Append a recovery phase after race day.

    Recovery duration by distance:
    - 5K:             1 week
    - 10K:            1 week
    - Half Marathon:  2 weeks
    - Marathon:       3 weeks

    Principle: 1 easy day per mile raced (Galloway guideline).
    Marathon = 26 days before resuming quality work.
    """

    recovery_weeks = {
        "5K": 1,
        "10K": 1,
        "half_marathon": 2,
        "marathon": 3
    }

    num_weeks = recovery_weeks[race_distance]
    recovery_plan = []

    # Week 1 (all distances): complete rest or very easy activity
    week_1 = {
        "week_label": "Recovery Week 1",
        "days": [
            {"day": 1, "activity": "Rest. Celebrate. You earned it."},
            {"day": 2, "activity": "Rest or gentle walk (20-30 min)"},
            {"day": 3, "activity": "Rest or gentle walk"},
            {"day": 4, "activity": "Optional very easy jog (15-20 min) IF pain-free"},
            {"day": 5, "activity": "Rest or cross-training (swimming, yoga)"},
            {"day": 6, "activity": "Easy jog (20-30 min) if feeling good"},
            {"day": 7, "activity": "Rest"}
        ],
        "notes": [
            "Soreness is normal for 3-5 days. Sharp or localized pain is not — report it.",
            "Eat well. Sleep well. Your body is repairing.",
            "Do NOT run through pain. Walking is fine."
        ]
    }
    recovery_plan.append(week_1)

    if num_weeks >= 2:
        # Week 2: easy running only, 40-50% of pre-taper volume
        week_2 = {
            "week_label": "Recovery Week 2",
            "runs_per_week": min(plan.config.runs_per_week, 3),
            "volume_pct_of_peak": 0.40,
            "intensity": "all_easy",
            "longest_run_km": plan.config.longest_run_peak_km * 0.30,
            "notes": [
                "All runs at easy/conversational pace.",
                "If anything hurts, take another rest day.",
                "No intervals, tempo, or race-pace work."
            ]
        }
        recovery_plan.append(week_2)

    if num_weeks >= 3:
        # Week 3 (marathon only): gradual reintroduction
        week_3 = {
            "week_label": "Recovery Week 3",
            "runs_per_week": min(plan.config.runs_per_week, 4),
            "volume_pct_of_peak": 0.55,
            "intensity": "mostly_easy_one_moderate",
            "longest_run_km": plan.config.longest_run_peak_km * 0.40,
            "notes": [
                "One run can include strides or a short progression finish.",
                "Long run capped at 40% of peak long run distance.",
                "Listen to your body — this is still recovery."
            ]
        }
        recovery_plan.append(week_3)

    # Final message
    recovery_plan.append({
        "completion_message": (
            "Recovery complete! You're ready to start your next training cycle. "
            "What's your next goal?"
        ),
        "next_action_prompt": "suggest_new_plan"
    })

    return recovery_plan
```

#### Recovery Phase Rules

```
RULES:
1. Recovery phase is auto-generated — user does NOT opt in or out.
2. Recovery phase is part of the plan (not a separate purchase).
3. User CAN skip recovery workouts (no guilt messaging).
4. User CANNOT add hard workouts during recovery (same lock logic as taper).
5. Injury reporting is still active during recovery.
6. Recovery completion triggers a "What's next?" prompt to drive
   repeat engagement / next plan purchase.
7. Recovery phase workouts appear on the calendar as a distinct
   visual style (e.g., lighter color) so users know it's different.
```

---

## 4. Week-to-Week Progression Algorithm

### 4.1 Mileage Progression Model

The app uses an **adaptive stepped progression** model rather than the traditional 10% rule, which research shows is an oversimplification with limited scientific backing ([Marathon Handbook](https://marathonhandbook.com/the-10-percent-rule/), [Outside Online](https://run.outsideonline.com/training/getting-started/myth-of-the-10-percent-rule/)).

```python
def calculate_weekly_mileage(plan_config):
    """
    Generate week-by-week mileage targets.

    Uses a '3 up, 1 down' pattern with adaptive increase rates:
    - Low volume (<30 km/week): up to 20% increase per build week
    - Medium volume (30-50 km/week): 10-15% increase per build week
    - High volume (>50 km/week): 5-10% increase per build week

    Every 4th week is a deload week (15-30% volume reduction).
    """

    total_weeks = plan_config.duration_weeks
    start_mileage = plan_config.starting_weekly_mileage_km
    peak_mileage = plan_config.peak_weekly_mileage_km
    taper_weeks = plan_config.taper_weeks
    deload_freq = plan_config.deload_frequency_weeks  # typically 4

    training_weeks = total_weeks - taper_weeks
    weekly_targets = []
    current_mileage = start_mileage

    for week in range(1, training_weeks + 1):
        # Deload week: every Nth week
        if week % deload_freq == 0:
            deload_mileage = current_mileage * 0.75  # 25% reduction
            weekly_targets.append({
                "week": week,
                "mileage_km": round(deload_mileage, 1),
                "type": "deload"
            })
        else:
            # Adaptive increase rate based on current volume
            if current_mileage < 30:
                increase_rate = 0.15  # up to 20%, use 15% as safe default
            elif current_mileage < 50:
                increase_rate = 0.10
            else:
                increase_rate = 0.07

            current_mileage = min(
                current_mileage * (1 + increase_rate),
                peak_mileage
            )
            weekly_targets.append({
                "week": week,
                "mileage_km": round(current_mileage, 1),
                "type": "build"
            })

    # Taper weeks
    taper_start_mileage = weekly_targets[-1]["mileage_km"]
    for t in range(1, taper_weeks + 1):
        reduction = 0.5 + (0.1 * t)  # progressive reduction
        reduction = min(reduction, 0.7)
        weekly_targets.append({
            "week": training_weeks + t,
            "mileage_km": round(taper_start_mileage * (1 - reduction), 1),
            "type": "taper"
        })

    return weekly_targets
```

### 4.2 Evidence Base for Progression

- **2012 Aarhus University Study**: 47 uninjured novice runners averaged 22.1% weekly volume increases — more than double the 10% rule — without injury. Injured runners had increases over 30%. ([Canadian Running Magazine](https://runningmagazine.ca/sections/training/the-10-per-cent-mileage-rule-isnt-what-you-think-study-warns/))
- **2025 BJSM Study (n=5,200)**: Found that single-session spikes (>10% above longest run in prior 30 days) are a better injury predictor than week-to-week mileage changes. ([PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC12421110/))
- **Jack Daniels' Equilibrium Method**: Increase mileage by 20-30%, then hold for 3-4 weeks before the next increase. Supported by bone remodeling research showing bones are weaker for ~1 month after new stress. ([Luke Humphrey Running](https://lukehumphreyrunning.com/increasing-mileage-the-10-rule/))
- **3 Up, 1 Down Pattern**: Widely used by elite runners and coaches. Build mileage for 3 weeks, reduce on week 4. ([Run to the Finish](https://runtothefinish.com/increase-running-mileage/))

### 4.3 Single-Session Spike Guard

```python
def validate_session_distance(proposed_distance_km, run_history_30_days):
    """
    Prevent single-session spikes that predict injury.
    Based on 2025 BJSM cohort study of 5,200 runners.

    Rule: No single run should exceed 110% of the longest run
    in the previous 30 days.
    """
    max_recent = max(run.distance_km for run in run_history_30_days)
    limit = max_recent * 1.10

    if proposed_distance_km > limit:
        return {
            "allowed": False,
            "max_allowed_km": round(limit, 1),
            "recommendation": f"Cap this run at {round(limit, 1)} km. "
                            f"Your longest run in the past 30 days was {max_recent} km."
        }
    return {"allowed": True}
```

---

## 5. Workout Types

### 5.1 Workout Type Catalog

All workout types are organized into three categories: **Running**, **Speed & Power**,
and **Cross-Training & Recovery**. Users can schedule any of these into their plan.
The plan generator uses running types automatically; speed/power and cross-training
types can be added by the user or suggested by the app.

#### Category A: Running Workouts

```json
{
  "running_workouts": {
    "easy_run": {
      "description": "Conversational pace, RPE 3-5/10",
      "intensity_zone": "zone_2",
      "pace_reference_vdot": "Easy pace from VDOT tables",
      "pace_reference_rpe": "Can hold a conversation comfortably",
      "purpose": "Aerobic base, recovery, volume accumulation",
      "typical_duration_minutes": [20, 60],
      "category": "running",
      "counts_as_run": true,
      "hard_workout": false
    },
    "long_run": {
      "description": "Extended duration at easy to moderate effort",
      "intensity_zone": "zone_2",
      "pace_reference_vdot": "Easy to marathon pace",
      "pace_reference_rpe": "Comfortable but purposeful, RPE 4-6/10",
      "purpose": "Endurance, fat adaptation, mental toughness",
      "typical_duration_minutes": [45, 180],
      "category": "running",
      "counts_as_run": true,
      "hard_workout": true,
      "notes": "Highest priority workout in any plan. Never skip if possible."
    },
    "tempo_run": {
      "description": "Sustained effort at lactate threshold pace",
      "intensity_zone": "zone_3_4",
      "pace_reference_vdot": "Threshold pace from VDOT tables (~half marathon pace)",
      "pace_reference_rpe": "Comfortably hard — can speak in short phrases, RPE 7/10",
      "purpose": "Raise lactate threshold, sustain faster paces longer",
      "typical_duration_minutes": [20, 45],
      "category": "running",
      "counts_as_run": true,
      "hard_workout": true
    },
    "interval_run": {
      "description": "Repeated hard efforts with recovery jogs",
      "intensity_zone": "zone_4_5",
      "pace_reference_vdot": "Interval pace from VDOT tables (~5K pace or faster)",
      "pace_reference_rpe": "Hard effort, breathing heavy, RPE 8-9/10",
      "purpose": "VO2max development, speed, running economy",
      "typical_duration_minutes": [25, 50],
      "structure": "Warm-up + N x (hard effort + recovery jog) + Cool-down",
      "example_sessions": [
        "6 x 800m at 5K pace, 400m jog recovery",
        "4 x 1200m at 10K pace, 600m jog recovery",
        "10 x 400m at mile pace, 400m jog recovery"
      ],
      "category": "running",
      "counts_as_run": true,
      "hard_workout": true
    },
    "progression_run": {
      "description": "Start easy, finish at tempo or faster",
      "intensity_zone": "zone_2_to_4",
      "pace_reference_vdot": "Easy pace → tempo pace over the course of the run",
      "pace_reference_rpe": "Start at RPE 3, finish at RPE 7",
      "purpose": "Teach negative splitting, build fatigue resistance",
      "typical_duration_minutes": [30, 60],
      "category": "running",
      "counts_as_run": true,
      "hard_workout": true,
      "notes": "Great bridge workout between easy runs and tempo runs."
    },
    "recovery_run": {
      "description": "Very easy effort, shorter duration",
      "intensity_zone": "zone_1",
      "pace_reference_vdot": "Slower than easy pace",
      "pace_reference_rpe": "Effortless, RPE 2-3/10, could easily talk",
      "purpose": "Active recovery, blood flow without stress",
      "typical_duration_minutes": [15, 30],
      "category": "running",
      "counts_as_run": true,
      "hard_workout": false
    },
    "race_pace_run": {
      "description": "Segments at goal race pace within a longer run",
      "intensity_zone": "zone_3",
      "pace_reference_vdot": "Goal race pace from VDOT tables",
      "pace_reference_rpe": "Race effort — focused and controlled, RPE 6-7/10",
      "purpose": "Race specificity, pacing practice, confidence building",
      "typical_duration_minutes": [30, 90],
      "structure": "Easy warm-up + race-pace segments + easy cool-down",
      "category": "running",
      "counts_as_run": true,
      "hard_workout": true
    },
    "run_walk": {
      "description": "Alternating running and walking intervals",
      "intensity_zone": "zone_1_2",
      "pace_reference_vdot": "N/A — effort-based",
      "pace_reference_rpe": "Comfortable during run segments, RPE 3-4/10",
      "purpose": "Build endurance for beginners, reduce injury risk",
      "typical_duration_minutes": [20, 60],
      "structure": "Run X min / Walk Y min, repeated",
      "progression": "Gradually increase run intervals, decrease walk intervals",
      "category": "running",
      "counts_as_run": true,
      "hard_workout": false,
      "notes": "Beginner-only workout type. Phased out as fitness improves."
    },
    "fartlek": {
      "description": "Unstructured speed play — vary pace by feel throughout the run",
      "intensity_zone": "zone_2_to_5",
      "pace_reference_vdot": "Varies — easy to sprint",
      "pace_reference_rpe": "Alternating RPE 3-9/10 by feel",
      "purpose": "Build speed and endurance with less mental fatigue than structured intervals",
      "typical_duration_minutes": [25, 50],
      "structure": "Continuous run with random surges by time, feel, or landmarks",
      "example_sessions": [
        "30 min run with 6 x 1 min hard / 2 min easy (by feel)",
        "40 min run — pick up pace between every other lamppost",
        "35 min run with surges on every uphill"
      ],
      "category": "running",
      "counts_as_run": true,
      "hard_workout": true,
      "notes": "Swedish for 'speed play.' More fun than intervals, great intro to speed work."
    }
  }
}
```

#### Category B: Speed & Power Workouts

```json
{
  "speed_power_workouts": {
    "hill_repeats": {
      "description": "Sprint up a hill, jog or walk back down, repeat",
      "intensity_zone": "zone_4_5",
      "pace_reference_vdot": "N/A — effort-based (hard uphill effort)",
      "pace_reference_rpe": "Hard uphill, RPE 8-9/10 on the climb",
      "purpose": "Build leg strength, power, and running economy. 'Strength training in disguise'",
      "typical_duration_minutes": [20, 40],
      "structure": "Warm-up jog + N x (hill sprint + jog-down recovery) + Cool-down",
      "example_sessions": [
        "8 x 30-sec hill sprint, walk-down recovery",
        "6 x 90-sec hill climb at 10K effort, jog-down recovery",
        "10 x 20-sec short steep hill sprint, walk-down recovery"
      ],
      "hill_guidance": {
        "short_hills": "20-30 sec, steep grade (6-10%), builds power",
        "medium_hills": "60-90 sec, moderate grade (4-6%), builds strength-endurance",
        "long_hills": "2-4 min, gentle grade (3-5%), builds aerobic power"
      },
      "category": "speed_power",
      "counts_as_run": true,
      "hard_workout": true
    },
    "strides": {
      "description": "Short accelerations at near-max speed, with full recovery",
      "intensity_zone": "zone_5",
      "pace_reference_vdot": "N/A — near-maximal effort",
      "pace_reference_rpe": "Fast and smooth, RPE 8-9/10 but short enough to feel easy",
      "purpose": "Improve turnover/cadence, neuromuscular speed, running form",
      "typical_duration_minutes": [5, 10],
      "structure": "4-8 x 80-100m acceleration, walk-back recovery between each",
      "category": "speed_power",
      "counts_as_run": false,
      "hard_workout": false,
      "notes": "Usually added to the END of an easy run. Low fatigue cost, high speed benefit. Can be done 2-3x per week."
    },
    "sprints": {
      "description": "All-out short efforts to build raw speed and fast-twitch power",
      "intensity_zone": "zone_5",
      "pace_reference_vdot": "N/A — maximum effort",
      "pace_reference_rpe": "All-out, RPE 10/10",
      "purpose": "Recruit fast-twitch muscle fibers, improve finishing kick, neuromuscular coordination",
      "typical_duration_minutes": [15, 25],
      "structure": "Warm-up + 6-10 x 50-150m all-out sprints, full recovery (2-3 min) between",
      "category": "speed_power",
      "counts_as_run": true,
      "hard_workout": true,
      "notes": "Use sparingly — 1x per week max. Best in build and peak phases."
    },
    "ladder_pyramid": {
      "description": "Intervals that increase then decrease in length",
      "intensity_zone": "zone_4_5",
      "pace_reference_vdot": "Interval to threshold pace, adjusting for interval length",
      "pace_reference_rpe": "Hard, RPE 7-9/10, scaling with interval length",
      "purpose": "Build speed across multiple effort durations, mentally engaging",
      "typical_duration_minutes": [30, 50],
      "structure": "Warm-up + ascending/descending intervals + Cool-down",
      "example_sessions": [
        "Ladder: 200-400-600-800-1000m with equal jog recovery",
        "Pyramid: 200-400-600-800-600-400-200m with 200m jog recovery",
        "Cutdown: 1600-1200-800-400m getting faster each rep"
      ],
      "category": "speed_power",
      "counts_as_run": true,
      "hard_workout": true,
      "notes": "Mentally easier than repeating the same distance. Good variety workout."
    }
  }
}
```

#### Category C: Cross-Training & Recovery

```json
{
  "cross_training_workouts": {
    "strength_training": {
      "description": "Resistance training focused on running-specific muscles",
      "intensity": "moderate to hard",
      "purpose": "Injury prevention, running economy, muscle balance, power",
      "typical_duration_minutes": [30, 60],
      "recommended_frequency": "2x per week (expert consensus)",
      "focus_areas": {
        "lower_body": ["squats", "deadlifts", "lunges", "calf raises", "step-ups",
                       "single-leg squats", "glute bridges", "hip thrusts"],
        "core": ["planks", "dead bugs", "bird dogs", "pallof press",
                 "russian twists", "leg raises"],
        "upper_body": ["push-ups", "rows", "overhead press",
                       "pull-ups (optional)"]
      },
      "phase_guidance": {
        "base": "Higher reps (12-15), moderate weight — build foundation",
        "build": "Moderate reps (8-12), increasing weight — build strength",
        "peak": "Lower reps (5-8), heavier weight — maintain strength, reduce volume",
        "taper": "Light weight, low volume — maintain without fatigue"
      },
      "category": "cross_training",
      "counts_as_run": false,
      "hard_workout": true,
      "scheduling_rules": "Never on the same day as a hard run. Ideally after an easy run day or on a non-run day."
    },
    "cycling": {
      "description": "Road, stationary, or spin bike workout",
      "intensity": "easy to moderate (matches the run it replaces)",
      "purpose": "Aerobic fitness with zero impact, quad/hamstring/glute development",
      "typical_duration_minutes": [30, 90],
      "category": "cross_training",
      "counts_as_run": false,
      "hard_workout": false,
      "notes": "Best cardio substitute for running. Same leg muscles, high VO2max benefit."
    },
    "swimming": {
      "description": "Pool laps or open water swimming",
      "intensity": "easy to moderate",
      "purpose": "Full-body aerobic workout, zero impact, breath control, upper body strength",
      "typical_duration_minutes": [20, 60],
      "category": "cross_training",
      "counts_as_run": false,
      "hard_workout": false
    },
    "aqua_jogging": {
      "description": "Running motion in deep water with a flotation belt",
      "intensity": "easy to moderate",
      "purpose": "Mimic running neuromuscular patterns without impact. Top rehab tool for injured runners",
      "typical_duration_minutes": [20, 45],
      "category": "cross_training",
      "counts_as_run": false,
      "hard_workout": false,
      "notes": "Can substitute for easy runs during injury. Maintains running-specific fitness."
    },
    "yoga": {
      "description": "Flexibility, mobility, and mindfulness practice",
      "intensity": "low",
      "purpose": "Mobility, flexibility, breathing, mental recovery, injury prevention",
      "typical_duration_minutes": [20, 60],
      "category": "cross_training",
      "counts_as_run": false,
      "hard_workout": false,
      "caution": "Avoid heavy yoga during speed-focused phases — excess flexibility can reduce muscle tension and power output."
    },
    "pilates": {
      "description": "Low-impact core and stabilizer muscle training",
      "intensity": "low to moderate",
      "purpose": "Core strength, posture, stabilizer muscles, running form improvement",
      "typical_duration_minutes": [20, 45],
      "category": "cross_training",
      "counts_as_run": false,
      "hard_workout": false
    },
    "elliptical": {
      "description": "Elliptical trainer workout",
      "intensity": "easy to moderate",
      "purpose": "Running-adjacent cardio with reduced impact. Good substitute when mildly injured",
      "typical_duration_minutes": [20, 60],
      "category": "cross_training",
      "counts_as_run": false,
      "hard_workout": false
    },
    "rowing": {
      "description": "Rowing machine or on-water rowing",
      "intensity": "moderate",
      "purpose": "Full-body cardio, posterior chain strength, core engagement",
      "typical_duration_minutes": [20, 45],
      "category": "cross_training",
      "counts_as_run": false,
      "hard_workout": false
    },
    "walking": {
      "description": "Brisk or easy walking",
      "intensity": "very low",
      "purpose": "Active recovery, mental break, movement on rest days",
      "typical_duration_minutes": [20, 60],
      "category": "cross_training",
      "counts_as_run": false,
      "hard_workout": false,
      "notes": "Underrated. Great for beginners transitioning from run/walk, or as active recovery."
    },
    "rest": {
      "description": "Complete rest — no structured activity",
      "intensity": "none",
      "purpose": "Full recovery, musculoskeletal repair, mental reset",
      "typical_duration_minutes": [0, 0],
      "category": "recovery",
      "counts_as_run": false,
      "hard_workout": false,
      "notes": "At least 1 full rest day per week for all levels. Not optional."
    }
  }
}
```

### 5.2 Updated WorkoutType Enum

```typescript
type WorkoutType =
  // Running
  | "easy_run"
  | "long_run"
  | "tempo_run"
  | "interval_run"
  | "progression_run"
  | "recovery_run"
  | "race_pace_run"
  | "run_walk"
  | "fartlek"
  // Speed & Power
  | "hill_repeats"
  | "strides"
  | "sprints"
  | "ladder_pyramid"
  // Cross-Training & Recovery
  | "strength_training"
  | "cycling"
  | "swimming"
  | "aqua_jogging"
  | "yoga"
  | "pilates"
  | "elliptical"
  | "rowing"
  | "walking"
  | "rest";
```

### 5.3 Scheduling Rules for New Workout Types

```
HARD/EASY SEQUENCING (updated with all workout types):

Hard workouts (never back-to-back):
  interval_run, tempo_run, progression_run, race_pace_run,
  fartlek, hill_repeats, sprints, ladder_pyramid, strength_training

Easy/recovery workouts (can be on consecutive days):
  easy_run, recovery_run, run_walk, strides (appended to easy run),
  cycling, swimming, aqua_jogging, yoga, pilates, elliptical,
  rowing, walking, rest

STRIDES SPECIAL RULE:
  Strides are appended to the end of easy runs, not scheduled
  as standalone workouts. They add ~5-10 minutes to the session.
  Can be added 2-3x per week during build and peak phases.

STRENGTH TRAINING PLACEMENT:
  - Never on the same day as a hard run
  - Best placed after an easy run (same day, PM session) or on a
    non-run day
  - Reduce strength volume during taper (light weight, low reps)
  - Skip strength during recovery phase (post-race)

CROSS-TRAINING DAYS:
  - Users can designate specific days as cross-training days
  - Cross-training replaces an easy run or fills a non-run day
  - Never replaces the long run or a quality session
  - Cross-training volume does NOT count toward running mileage
    (per Section 1.1 product decisions)

HILL REPEATS PLACEMENT:
  - Counts as a hard workout (replaces intervals or tempo for the week)
  - Best in base and build phases for strength development
  - Reduce or remove in peak/taper (shift to race-specific work)

FARTLEK PLACEMENT:
  - Great transitional workout: use in base phase to introduce speed
    before formal intervals begin in build phase
  - Can replace intervals for beginners who aren't ready for track work
  - Counts as a hard workout
```

### 5.4 Weekly Workout Distribution by Phase (Updated)

```
BASE PHASE:
  Beginner:     2 easy/run-walk + 1 long run (+ strides 1x)
  Intermediate: 1 easy + 1 fartlek or progression + 1 long run + 1 strength
  Advanced:     1 easy + 1 hills or fartlek + 1 tempo + 1 long run + 1 strength

BUILD PHASE:
  Beginner:     1 easy + 1 fartlek or tempo + 1 long run (+ strides 1-2x)
  Intermediate: 1 easy + 1 tempo + 1 intervals + 1 long run + 1 strength
  Advanced:     1 easy + 1 tempo + 1 intervals or ladder + 1 race-pace + 1 long run + 1 strength + 1 recovery

PEAK PHASE:
  Beginner:     1 easy + 1 tempo + 1 long run (race-pace segments) (+ strides 2x)
  Intermediate: 1 easy + 1 intervals + 1 tempo + 1 long run (race-pace segments) + 1 strength (light)
  Advanced:     1 recovery + 1 intervals or sprints + 1 tempo + 1 race-pace + 1 long run + 1 strength (light)

TAPER PHASE (all levels):
  Reduce volume of each session by 40-60%
  Maintain 1 short tempo or interval session (shortened)
  Reduce long run to 50-60% of peak distance
  Maintain run frequency
  Strength: light weight, maintenance only
  Strides: continue 2x/week (low fatigue, keeps legs sharp)
  Cross-training: optional, easy effort only

RECOVERY PHASE (post-race):
  Week 1: Rest, walking, swimming, yoga only — no running (see Section 3.2)
  Week 2+: Easy runs + optional cross-training
  No strength training until week 2 of recovery
  No hard workouts until recovery phase is complete
```

### 5.5 User-Added Workouts

Users can add any workout type to any day, subject to scheduling rules:

```python
def validate_user_added_workout(plan, date, workout_type):
    """
    When a user manually adds a workout to their plan:
    1. Check hard/easy sequencing rules
    2. Warn if it would create back-to-back hard days
    3. Warn if weekly volume would exceed 115% of target
    4. Block additions during taper (Section 3.2 taper lock)
    5. Allow but flag if it replaces a planned workout
    """

    existing = get_workout_on_date(plan, date)
    prev_day = get_workout_on_date(plan, date - timedelta(days=1))
    next_day = get_workout_on_date(plan, date + timedelta(days=1))

    is_hard = WORKOUT_CATALOG[workout_type]["hard_workout"]
    in_taper = get_phase_for_date(plan, date) in ["taper", "recovery"]

    # Block during taper/recovery
    if in_taper and is_hard:
        return {
            "allowed": False,
            "reason": "Taper and recovery phases are locked. "
                     "You can add easy or cross-training workouts only."
        }

    # Check back-to-back hard days
    if is_hard:
        if (prev_day and WORKOUT_CATALOG[prev_day.type]["hard_workout"]) or \
           (next_day and WORKOUT_CATALOG[next_day.type]["hard_workout"]):
            return {
                "allowed": True,
                "warning": "This puts two hard workouts on consecutive days. "
                          "Consider moving one to allow recovery between them."
            }

    # Check weekly volume
    week_volume = get_week_volume(plan, date)
    added_volume = estimate_workout_volume(workout_type)
    if (week_volume + added_volume) > plan.target_weekly_mileage * 1.15:
        return {
            "allowed": True,
            "warning": f"This would bring your week to {int(((week_volume + added_volume) / plan.target_weekly_mileage) * 100)}% "
                      f"of target. That's above the recommended range."
        }

    return {"allowed": True}
```

---

## 5B. Warm-Up & Cool-Down Protocols

Every workout in the plan should include warm-up and cool-down guidance.
Research shows dynamic warm-ups reduce injury risk and improve running economy,
while static stretching before running can reduce muscle power output.

([PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC12034053/),
[PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC8391672/),
[Yale Medicine](https://www.yalemedicine.org/news/how-to-stretch-before-run))

```json
{
  "warm_up": {
    "pre_easy_run": {
      "duration_minutes": 5,
      "protocol": "2-3 min brisk walk, then 3-4 dynamic stretches (30 sec each)",
      "exercises": ["leg swings (front/back)", "leg swings (side to side)",
                    "walking lunges", "high knees"]
    },
    "pre_hard_workout": {
      "duration_minutes": 10,
      "protocol": "5 min easy jog + 5-6 dynamic stretches + 2-4 strides",
      "exercises": ["easy jog", "leg swings (front/back)", "leg swings (side to side)",
                    "walking lunges", "A-skips", "butt kicks", "high knees",
                    "2-4 x 80m strides building to workout pace"]
    },
    "pre_long_run": {
      "duration_minutes": 5,
      "protocol": "Start the run at very easy pace for first 5-10 min (self-warming)",
      "exercises": ["Optional: 2-3 dynamic stretches before starting"]
    }
  },
  "cool_down": {
    "post_easy_run": {
      "duration_minutes": 3,
      "protocol": "2-3 static stretches held 20-30 sec each",
      "exercises": ["calf stretch", "quad stretch", "hamstring stretch"]
    },
    "post_hard_workout": {
      "duration_minutes": 10,
      "protocol": "5 min easy jog + 5-6 static stretches held 20-30 sec each",
      "exercises": ["easy jog to bring heart rate down",
                    "calf stretch (straight + bent knee)",
                    "quad stretch", "hamstring stretch",
                    "hip flexor stretch", "IT band stretch",
                    "glute stretch"]
    },
    "post_long_run": {
      "duration_minutes": 10,
      "protocol": "Walk 3-5 min, then 5-6 static stretches",
      "exercises": ["walk until breathing normalizes",
                    "calf stretch", "quad stretch", "hamstring stretch",
                    "hip flexor stretch", "pigeon stretch (glutes/piriformis)"]
    }
  }
}
```

---

## 5C. Polarized Training Integration (80/20 Rule)

### 5C.1 Overview

The plan generator enforces the polarized training distribution validated
by Dr. Stephen Seiler's research and confirmed by multiple meta-analyses:
approximately 80% of training volume at easy effort and 20% at hard effort,
with minimal time in the "gray zone" of moderate intensity.

**Why this matters**: Most recreational runners unknowingly spend the majority
of their training in the gray zone — too hard for recovery, too easy for
speed adaptation. This is the #1 training mistake and the primary cause of
stalled progress and overtraining.

Sources:
- [Fast Talk Labs — Complete Guide to Polarized Training](https://www.fasttalklabs.com/pathways/polarized-training/)
- [Marathon Handbook — Polarized Training Guide](https://marathonhandbook.com/polarized-training/)
- [PMC — 2024 Meta-Analysis on Polarized vs. Other TID](https://pmc.ncbi.nlm.nih.gov/articles/PMC11329428/)
- [PMC — 16 Weeks Pyramidal vs. Polarized in Runners](https://pmc.ncbi.nlm.nih.gov/articles/PMC9299127/)
- [Outside Online — The Truth About the Grey Zone](https://run.outsideonline.com/training/the-truth-about-running-in-the-grey-zone/)
- [Frontiers in Physiology — 2025 TID Theory Review](https://www.frontiersin.org/journals/physiology/articles/10.3389/fphys.2025.1657892/full)

### 5C.2 Zone Model

The app uses a **simplified 3-zone model** for polarized training validation,
mapped to both heart rate and RPE for accessibility.

```json
{
  "polarized_zones": {
    "zone_1_easy": {
      "label": "Easy / Low Intensity",
      "hr_pct_of_max": [0, 75],
      "hr_pct_of_hrr": [0, 70],
      "rpe": [1, 4],
      "rpe_description": "Conversational. Could talk in full sentences.",
      "pace_reference": "Easy pace and slower from VDOT tables",
      "workout_types": ["easy_run", "long_run", "recovery_run", "run_walk",
                         "warm_up", "cool_down"],
      "target_time_pct": "75-90% of total weekly running time",
      "color": "green"
    },
    "zone_2_gray": {
      "label": "Moderate / Gray Zone (minimize)",
      "hr_pct_of_max": [76, 85],
      "hr_pct_of_hrr": [71, 85],
      "rpe": [5, 6],
      "rpe_description": "Uncomfortably moderate. Can speak in short phrases only.",
      "pace_reference": "Between easy pace and threshold pace",
      "workout_types": ["progression_run (middle segment)", "race_pace_run (marathon pace)"],
      "target_time_pct": "0-10% of total weekly running time",
      "color": "yellow",
      "warning": "The gray zone feels productive but yields poor returns. Most recreational runners spend 50%+ here without realizing it."
    },
    "zone_3_hard": {
      "label": "Hard / High Intensity",
      "hr_pct_of_max": [86, 100],
      "hr_pct_of_hrr": [86, 100],
      "rpe": [7, 10],
      "rpe_description": "Hard to very hard. Speaking is difficult or impossible.",
      "pace_reference": "Threshold pace and faster from VDOT tables",
      "workout_types": ["interval_run", "tempo_run", "fartlek", "hill_repeats",
                         "sprints", "ladder_pyramid", "strides"],
      "target_time_pct": "10-25% of total weekly running time",
      "color": "red"
    }
  },

  "zone_mapping_to_5_zone": {
    "description": "For users familiar with 5-zone models (Garmin, Apple Watch, etc.)",
    "5_zone_1_recovery": "→ Polarized Zone 1 (Easy)",
    "5_zone_2_aerobic": "→ Polarized Zone 1 (Easy)",
    "5_zone_3_tempo": "→ Polarized Zone 2 (Gray — minimize!)",
    "5_zone_4_threshold": "→ Polarized Zone 3 (Hard)",
    "5_zone_5_vo2max": "→ Polarized Zone 3 (Hard)"
  }
}
```

### 5C.3 Intensity Distribution Targets by Phase

Research shows the optimal intensity distribution shifts across training phases.
A 2022 PMC study found that a "pyramidal → polarized" periodization pattern
was more effective than other patterns in improving 5K time trial performance.

([PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC9299127/),
[TrainingPeaks](https://www.trainingpeaks.com/coach-blog/polarized-pyramidal-training-which-is-better/),
[Laura Norris Running](https://lauranorrisrunning.com/training-intensity-distribution-for-runners/))

```python
PHASE_INTENSITY_TARGETS = {
    "base": {
        "zone_1_easy_pct": [85, 95],     # 85-95% easy
        "zone_2_gray_pct": [0, 5],       # 0-5% moderate (almost none)
        "zone_3_hard_pct": [5, 15],      # 5-15% hard (strides, light fartlek)
        "distribution_model": "highly_polarized",
        "rationale": (
            "Base phase is about aerobic development. Almost all running "
            "should be easy. Only introduce light speed work (strides, "
            "short fartleks) to maintain neuromuscular coordination."
        ),
        "max_hard_sessions_per_week": {
            "beginner": 1,
            "intermediate": 1,
            "advanced": 2
        }
    },

    "build": {
        "zone_1_easy_pct": [75, 85],     # 75-85% easy
        "zone_2_gray_pct": [0, 5],       # 0-5% moderate
        "zone_3_hard_pct": [15, 25],     # 15-25% hard
        "distribution_model": "polarized",
        "rationale": (
            "Build phase introduces structured quality sessions "
            "(intervals, tempo, hill repeats). The 80/20 rule is "
            "most critical here — easy days MUST stay easy to absorb "
            "the hard work."
        ),
        "max_hard_sessions_per_week": {
            "beginner": 1,
            "intermediate": 2,
            "advanced": 3
        }
    },

    "peak": {
        "zone_1_easy_pct": [70, 80],     # 70-80% easy
        "zone_2_gray_pct": [0, 5],       # 0-5% moderate
        "zone_3_hard_pct": [20, 30],     # 20-30% hard
        "distribution_model": "polarized",
        "rationale": (
            "Peak phase has the highest intensity proportion. "
            "Hard sessions are race-specific. Easy runs are "
            "shorter but still truly easy. This is where fitness "
            "peaks — but only if recovery is protected."
        ),
        "max_hard_sessions_per_week": {
            "beginner": 2,
            "intermediate": 2,
            "advanced": 3
        }
    },

    "taper": {
        "zone_1_easy_pct": [80, 90],     # 80-90% easy
        "zone_2_gray_pct": [0, 5],       # 0-5% moderate
        "zone_3_hard_pct": [10, 20],     # 10-20% hard (short, sharp)
        "distribution_model": "polarized_reduced_volume",
        "rationale": (
            "Taper maintains intensity but reduces volume. "
            "Short strides and abbreviated tempo/interval sessions "
            "keep the legs sharp without adding fatigue."
        ),
        "max_hard_sessions_per_week": {
            "beginner": 1,
            "intermediate": 1,
            "advanced": 2
        }
    },

    "marathon_specific_note": (
        "For marathon and half marathon plans, Zone 2 (gray zone) gets "
        "slightly more allowance in the build and peak phases because "
        "marathon race pace often sits in or near Zone 2. Long runs "
        "with race-pace segments are a necessary gray-zone exposure "
        "for these distances. Target: max 10% in Zone 2 during build/peak."
    )
}
```

### 5C.4 Two-Layer Validation: Session-Based + Time-in-Zone

Research shows the most accurate picture comes from checking intensity
distribution at both the **session level** (how many hard vs easy sessions)
and the **time-in-zone level** (actual minutes in each zone, accounting
for warm-ups, cool-downs, and recovery between intervals).

([Marco Altini Substack](https://marcoaltini.substack.com/p/training-intensity-distribution),
[High North Performance](https://www.highnorth.co.uk/articles/polarised-training-cycling))

```python
def validate_polarized_distribution(plan_week, method="both"):
    """
    Validate intensity distribution using two methods.

    METHOD 1: Session-based (recommended for users without HR data)
    Count sessions by primary intensity classification.
    Target: ~80% easy sessions, ~20% hard sessions.

    METHOD 2: Time-in-zone (for users with HR/pace data)
    Sum actual minutes in each zone across all workouts.
    Target: 80-90% Zone 1, 0-5% Zone 2, 10-20% Zone 3.

    Using both methods gives the most accurate picture.
    """

    phase = plan_week.phase
    targets = PHASE_INTENSITY_TARGETS[phase]

    # === METHOD 1: Session-based ===
    total_run_sessions = [w for w in plan_week.workouts
                          if WORKOUT_CATALOG[w.type]["counts_as_run"]]
    if not total_run_sessions:
        return {"valid": True}

    easy_sessions = [w for w in total_run_sessions
                     if not WORKOUT_CATALOG[w.type]["hard_workout"]]
    hard_sessions = [w for w in total_run_sessions
                     if WORKOUT_CATALOG[w.type]["hard_workout"]]

    total = len(total_run_sessions)
    easy_session_pct = len(easy_sessions) / total
    hard_session_pct = len(hard_sessions) / total

    session_result = {
        "method": "session_based",
        "easy_sessions": len(easy_sessions),
        "hard_sessions": len(hard_sessions),
        "easy_pct": round(easy_session_pct * 100),
        "hard_pct": round(hard_session_pct * 100)
    }

    # Check against phase targets
    max_hard = targets["max_hard_sessions_per_week"][plan_week.skill_level]
    if len(hard_sessions) > max_hard:
        session_result["warning"] = (
            f"You have {len(hard_sessions)} hard sessions this week. "
            f"For the {phase} phase at {plan_week.skill_level} level, "
            f"we recommend a maximum of {max_hard}. Consider converting "
            f"one to an easy run."
        )

    # === METHOD 2: Time-in-zone (if HR/pace data available) ===
    time_in_zone_result = None
    if has_logged_data(plan_week):
        z1_minutes = sum_minutes_in_zone(plan_week, "zone_1_easy")
        z2_minutes = sum_minutes_in_zone(plan_week, "zone_2_gray")
        z3_minutes = sum_minutes_in_zone(plan_week, "zone_3_hard")
        total_minutes = z1_minutes + z2_minutes + z3_minutes

        if total_minutes > 0:
            z1_pct = z1_minutes / total_minutes
            z2_pct = z2_minutes / total_minutes
            z3_pct = z3_minutes / total_minutes

            time_in_zone_result = {
                "method": "time_in_zone",
                "zone_1_pct": round(z1_pct * 100),
                "zone_2_pct": round(z2_pct * 100),
                "zone_3_pct": round(z3_pct * 100),
                "zone_1_minutes": round(z1_minutes),
                "zone_2_minutes": round(z2_minutes),
                "zone_3_minutes": round(z3_minutes)
            }

            # Gray zone warning
            if z2_pct > 0.10:
                time_in_zone_result["gray_zone_warning"] = (
                    f"{round(z2_pct*100)}% of your running time was in the "
                    f"gray zone this week ({round(z2_minutes)} min). "
                    f"This zone feels productive but yields poor returns. "
                    f"Try slowing your easy runs down — they should feel "
                    f"genuinely easy, not 'comfortably moderate.'"
                )

            # Too little hard work in build/peak
            min_hard_pct = targets["zone_3_hard_pct"][0] / 100
            if z3_pct < min_hard_pct and phase in ["build", "peak"]:
                time_in_zone_result["info"] = (
                    f"Only {round(z3_pct*100)}% hard this week. During "
                    f"{phase} phase, aim for {targets['zone_3_hard_pct'][0]}-"
                    f"{targets['zone_3_hard_pct'][1]}% to drive adaptation."
                )

    return {
        "valid": True,
        "session_analysis": session_result,
        "time_in_zone_analysis": time_in_zone_result
    }
```

### 5C.5 Gray Zone Detection & User Coaching

The #1 training mistake for recreational runners. The app should actively
detect and coach users out of the gray zone.

```python
GRAY_ZONE_DETECTION = {
    "what_it_is": (
        "The gray zone is the moderate intensity range (Zone 2 in the "
        "3-zone model, Zone 3 in the 5-zone model) where effort feels "
        "'comfortably hard.' It's too fast for aerobic base building "
        "and too slow for VO2max or threshold adaptation."
    ),

    "why_runners_fall_into_it": [
        "Easy runs 'feel too slow' so they push slightly harder",
        "Coffee, stress, or excitement before a run unintentionally raises effort",
        "Running with faster friends pulls pace up",
        "Ego — slow running doesn't feel like 'real training'",
        "Lack of objective metrics (no HR monitor, no pace targets)"
    ],

    "detection_methods": {
        "pace_based": {
            "trigger": "Easy run logged at pace faster than VDOT easy pace by >15 sec/km",
            "message": (
                "Your easy run today was faster than your prescribed easy pace. "
                "Easy runs should feel genuinely effortless — try slowing down "
                "next time. The aerobic benefits are the same at a slower pace, "
                "but recovery is much better."
            )
        },
        "hr_based": {
            "trigger": "Easy run with avg HR above 76% of max HR",
            "message": (
                "Your heart rate during today's easy run averaged {avg_hr} bpm "
                "({hr_pct}% of max). For easy runs, aim to stay below 75% of max. "
                "Slow down, take walk breaks if needed, and let your heart rate "
                "guide the effort."
            )
        },
        "rpe_based": {
            "trigger": "User reports RPE 5-6 on an easy run day",
            "message": (
                "You rated today's easy run as RPE {rpe}. Easy runs should "
                "feel like a 3-4 — genuinely comfortable, like you could run "
                "for hours. If easy runs feel harder than that, it's okay to "
                "slow down or take walk breaks."
            )
        },
        "pattern_based": {
            "trigger": "3+ easy runs in the past 2 weeks flagged as too fast",
            "message": (
                "We've noticed your easy runs have been trending faster than "
                "your easy pace target. This is the #1 training mistake runners "
                "make — it feels productive but actually slows your progress. "
                "The best runners in the world run their easy days VERY easy. "
                "Try a 'talk test' on your next run: if you can't hold a "
                "conversation, you're going too fast."
            ),
            "action": "show_educational_content"
        }
    },

    "educational_content": {
        "title": "Why Slow Running Makes You Faster",
        "key_points": [
            "80% of elite runners' training is at easy effort (Seiler, 2010)",
            "Gray zone training creates fatigue without triggering adaptation",
            "Easy running builds mitochondria, capillaries, and fat oxidation",
            "Your hard days can only be truly hard if your easy days are truly easy",
            "A study of Ironman athletes found r=0.94 correlation between gray zone time and slower race times"
        ],
        "display": "Show once, then reference in nudges. Don't lecture repeatedly."
    }
}
```

### 5C.6 Intensity Distribution Dashboard

Show users their actual intensity distribution vs. target, updated weekly.

```json
{
  "intensity_dashboard": {
    "display_location": "Weekly summary + plan overview page",

    "visualization": {
      "type": "stacked_bar_chart",
      "bars": [
        {"label": "This Week", "segments": ["zone_1_pct", "zone_2_pct", "zone_3_pct"]},
        {"label": "Target", "segments": ["target_z1", "target_z2", "target_z3"]},
        {"label": "Plan Average", "segments": ["avg_z1", "avg_z2", "avg_z3"]}
      ],
      "colors": {
        "zone_1": "green",
        "zone_2": "yellow",
        "zone_3": "red"
      }
    },

    "data_sources": {
      "with_hr_data": "Time-in-zone from heart rate (most accurate)",
      "with_pace_data": "Pace-to-zone mapping from VDOT tables",
      "with_rpe_only": "Session-based classification from workout type",
      "fallback": "Planned distribution based on workout types scheduled"
    },

    "weekly_summary_message_templates": {
      "on_target": (
          "Your intensity split this week: {z1}% easy / {z3}% hard. "
          "Right in the zone. Keep your easy days easy and your hard days hard."
      ),
      "too_much_gray": (
          "Your intensity split: {z1}% easy / {z2}% gray zone / {z3}% hard. "
          "Try to reduce that {z2}% in the middle — slow down your easy runs "
          "and make your hard sessions count."
      ),
      "too_much_hard": (
          "Your intensity split: {z1}% easy / {z3}% hard. That's more hard "
          "training than recommended for {phase} phase. Your body needs easy "
          "days to absorb the hard work. Consider backing off next week."
      ),
      "all_easy": (
          "Your intensity split: {z1}% easy / {z3}% hard. This week was "
          "almost all easy — that's fine during {phase} phase. "
          "{conditional: If build/peak, add: 'Next week, make sure to hit your quality session.'}"
      )
    },

    "public_profile_display": {
      "show_on_profile": false,
      "reason": "Intensity distribution is personal training data — too granular for public sharing"
    }
  }
}
```

### 5C.7 Polarized Training by Race Distance

The 80/20 rule applies differently depending on the target race distance,
because race intensity itself falls in different zones.

```python
DISTANCE_SPECIFIC_POLARIZATION = {
    "5K": {
        "race_intensity_zone": "zone_3_hard",
        "notes": (
            "5K race pace is high intensity (Zone 3). Training should "
            "develop VO2max through intervals at or faster than race pace. "
            "The 80/20 split applies cleanly — 80% easy base, 20% intervals."
        ),
        "recommended_hard_types": ["interval_run", "fartlek", "hill_repeats",
                                    "ladder_pyramid", "strides"],
        "gray_zone_tolerance": 0.05  # max 5% in gray zone
    },

    "10K": {
        "race_intensity_zone": "zone_3_hard (lower end)",
        "notes": (
            "10K pace sits at the lower end of Zone 3. Training blends "
            "VO2max intervals with tempo runs near threshold. Similar "
            "80/20 split with slightly more tempo work than 5K plans."
        ),
        "recommended_hard_types": ["interval_run", "tempo_run", "fartlek",
                                    "hill_repeats", "ladder_pyramid"],
        "gray_zone_tolerance": 0.05
    },

    "half_marathon": {
        "race_intensity_zone": "zone_2_gray to zone_3_hard boundary",
        "notes": (
            "Half marathon race pace sits near the threshold — the "
            "boundary between Zone 2 and Zone 3. This means some "
            "race-specific training legitimately falls in the gray zone. "
            "Allow slightly more gray zone time (up to 10%) for "
            "half marathon plans to accommodate race-pace long runs."
        ),
        "recommended_hard_types": ["tempo_run", "race_pace_run", "interval_run",
                                    "progression_run"],
        "gray_zone_tolerance": 0.10  # slightly more for HM plans
    },

    "marathon": {
        "race_intensity_zone": "zone_2_gray (for most runners)",
        "notes": (
            "Marathon race pace typically falls in Zone 2 (gray zone) "
            "for most recreational runners. This creates a unique challenge: "
            "the race itself is run at an intensity we normally minimize in "
            "training. Solution: use marathon-pace long runs as controlled "
            "gray-zone exposure (up to 10-15% of weekly time), but keep "
            "all other running in Zone 1 or Zone 3. Easy runs must be "
            "VERY easy to compensate."
        ),
        "recommended_hard_types": ["tempo_run", "interval_run", "race_pace_run",
                                    "progression_run"],
        "gray_zone_tolerance": 0.15  # highest allowance for marathon plans
    }
}
```

### 5C.8 API Endpoints

```
GET    /api/plans/:id/intensity-distribution          — Get current week's distribution
GET    /api/plans/:id/intensity-distribution/history   — Distribution trend over plan
GET    /api/plans/:id/gray-zone-alerts                — Get recent gray zone warnings
PUT    /api/users/:id/zones                           — Set custom zone boundaries (HR/pace)
```

### 5C.9 Data Model Additions

```typescript
interface IntensityDistribution {
  plan_id: string;
  week_number: number;
  method: "session_based" | "time_in_zone" | "both";

  // Session-based
  easy_sessions: number;
  hard_sessions: number;

  // Time-in-zone (if HR/pace data available)
  zone_1_minutes: number | null;
  zone_2_minutes: number | null;
  zone_3_minutes: number | null;
  zone_1_pct: number | null;
  zone_2_pct: number | null;
  zone_3_pct: number | null;

  // Phase targets
  phase: string;
  target_zone_1_pct: [number, number];  // range
  target_zone_3_pct: [number, number];

  // Flags
  gray_zone_warning: boolean;
  too_much_hard_warning: boolean;
}

interface UserZoneSettings {
  user_id: string;
  method: "hr_based" | "pace_based" | "rpe_only";

  // HR-based zones
  max_hr: number | null;
  resting_hr: number | null;
  zone_1_ceiling_hr: number | null;   // auto-calculated or manual
  zone_3_floor_hr: number | null;

  // Pace-based zones (from VDOT)
  easy_pace_min_per_km: number | null;
  threshold_pace_min_per_km: number | null;

  // Source
  zones_source: "auto_from_vdot" | "auto_from_hr" | "manual" | "default";
}
```

---

## 5D. Age-Adjusted Training (Masters Runners)

Runners over 40 experience physiological changes that require plan modifications.
The app should adjust automatically based on the user's age.

### 5D.1 Overview & Research

Research shows VO2max declines ~0.5-1% per year from age 35, accelerating after
60. However, masters runners who maintain training intensity show remarkably
small declines — Pollock et al. reported a non-significant 1.7% VO2max decline
over 10 years in masters athletes who stayed competitive and maintained
intensity, versus 12.6% decline in those who reduced intensity.

The key insight: training modifications — not reduced ambition — are the answer.
Masters runners have documented ADVANTAGES in pacing discipline, fat oxidation
efficiency, and mental toughness.

Approximately 50% of VO2max decline is attributable to loss of muscle mass and
increased fat mass, with the other ~50% related to declining oxygen delivery.
This means strength training and body composition management are as important
as running itself for masters athletes.

Running economy (the other major performance determinant) does NOT decline with
age in well-trained masters athletes. This is a significant advantage.

([Runners Connect](https://runnersconnect.net/master-running/),
[Run to the Finish](https://runtothefinish.com/how-to-run-in-old-age/),
[TrainingPeaks](https://www.trainingpeaks.com/blog/training-for-masters-runners-part-2-block-periodization/),
[PMC — VO2max and Training Volume](https://pmc.ncbi.nlm.nih.gov/articles/PMC9517884/),
[PMC — Age-Associated Recovery](https://pmc.ncbi.nlm.nih.gov/articles/PMC10854791/),
[PubMed — VO2max Decline](https://pubmed.ncbi.nlm.nih.gov/2361923/))

### 5D.2 Recovery & Physiological Changes by Age

Research consistently shows slower muscle protein synthesis, reduced collagen
turnover in tendons, and longer inflammatory responses with age. These directly
affect how the plan generator spaces workouts.

```python
RECOVERY_PHYSIOLOGY = {
    "muscle_repair": {
        "under_40": {
            "full_recovery_hours_after_hard": 24-36,
            "muscle_protein_synthesis_window": "24h — standard response",
            "tendon_adaptation_rate": "normal"
        },
        "40_49": {
            "full_recovery_hours_after_hard": 36-48,
            "muscle_protein_synthesis_window": "24-36h — slightly delayed",
            "tendon_adaptation_rate": "slowing — collagen turnover begins to decrease"
        },
        "50_59": {
            "full_recovery_hours_after_hard": 48-72,
            "muscle_protein_synthesis_window": "36-48h — notably delayed",
            "tendon_adaptation_rate": "slow — non-enzymatic crosslinks increasing"
        },
        "60_plus": {
            "full_recovery_hours_after_hard": 72,
            "muscle_protein_synthesis_window": "48h+ — significantly delayed",
            "tendon_adaptation_rate": "very slow — prioritize injury-free consistency"
        }
    },

    "hormonal_factors": {
        "description": (
            "Declines in growth hormone, testosterone, and estrogen coupled "
            "with increases in cortisol and inflammatory markers reduce the "
            "margin for error. Poor sleep, high stress, or inadequate nutrition "
            "have a bigger impact on recovery after 40 than before."
        )
    },

    "the_50_year_threshold": {
        "description": (
            "Research found no notable decline in training volume, cardiovascular "
            "fitness, or performance up to age 50 in well-trained runners. Beyond "
            "50, all variables begin declining more rapidly. The plan generator "
            "applies its most significant modifications at this boundary."
        )
    }
}
```

### 5D.3 Age-Based Plan Adjustments

```python
AGE_ADJUSTMENTS = {
    "adjustments_by_age": {
        "under_40": {
            "recovery_days_between_hard": 1,      # standard: 1 easy day between hard
            "deload_frequency_weeks": 4,           # every 4th week
            "strength_emphasis": "normal",         # 1-2x/week recommended
            "max_hard_sessions_per_week": 3,
            "warm_up_extension_minutes": 0,        # standard warm-up protocol
            "protein_g_per_kg": 1.2,               # standard athletic recommendation
            "plan_duration_modifier": 1.0,          # no extension
            "training_cycle_days": 7,              # standard weekly cycle
            "notes": "Standard plan, no modifications"
        },
        "40_49": {
            "recovery_days_between_hard": 1,       # still 1, but monitor closely
            "deload_frequency_weeks": 3,           # every 3rd week
            "strength_emphasis": "increased",      # 2x/week mandatory, focus glutes/hamstrings/calves
            "max_hard_sessions_per_week": 3,
            "warm_up_extension_minutes": 5,        # add 5 min to pre-hard warm-up
            "protein_g_per_kg": 1.4,               # increased for slower protein synthesis
            "plan_duration_modifier": 1.0,          # suggest default or longer
            "training_cycle_days": 7,              # standard weekly cycle
            "notes": "Increase strength training emphasis. Monitor recovery more closely."
        },
        "50_59": {
            "recovery_days_between_hard": 2,       # 2 easy days between hard sessions
            "deload_frequency_weeks": 3,           # every 3rd week
            "strength_emphasis": "high",           # 2x/week, add power exercises
            "max_hard_sessions_per_week": 2,
            "warm_up_extension_minutes": 8,        # substantially longer warm-up
            "protein_g_per_kg": 1.6,               # aging muscles need ~40% more amino acids
            "plan_duration_modifier": 1.15,         # suggest 15% longer plans
            "training_cycle_days": 7,              # 7-day but with hard/easy/easy pattern
            "notes": (
                "Hard/easy/easy pattern. Add explosive exercises (box jumps, bounding) "
                "to counteract fast-twitch fiber loss. Injury-free training time is the "
                "#1 objective — recovery from injury is much slower after 50."
            )
        },
        "60_plus": {
            "recovery_days_between_hard": 2,       # 2 easy days minimum, 3 if needed
            "deload_frequency_weeks": 2,           # every 2nd week option available
            "strength_emphasis": "high",
            "max_hard_sessions_per_week": 2,
            "warm_up_extension_minutes": 10,       # extended warm-up critical
            "protein_g_per_kg": 1.7,               # maximum recommended range
            "plan_duration_modifier": 1.25,         # suggest 25% longer plans
            "training_cycle_days": 9,              # 9-10 day cycle instead of 7-day weeks
            "extended_plan_recommendation": True,
            "notes": (
                "Consider 9-10 day training cycles instead of 7-day weeks. "
                "Prioritize consistency over intensity. An elite runner who "
                "stopped for 16 years ran 2:30 marathon at age 59 — the body "
                "retains and rebuilds adaptations at any age."
            )
        }
    },

    "universal_masters_rules": [
        "Easy runs must be TRULY easy — gray zone training is more harmful for masters",
        "Warm-up duration increases with age (see warm_up_extension_minutes)",
        "Keep speed work in the plan — it counteracts fast-twitch fiber loss",
        "Sleep and recovery nutrition become more important, not less",
        "Strength training is non-negotiable for runners over 50",
        "Tendon adaptation takes months/years — never spike intensity suddenly",
        "Omega-3s, vitamin D, creatine are evidence-backed supplements for masters",
        "Active recovery (walking, light cycling) improves nutrient delivery to tendons"
    ]
}
```

### 5D.4 Masters-Specific Plan Duration Recommendations

When a masters runner creates a plan, the app should suggest extended durations
based on age group. Users can always override, but the defaults should reflect
the reality that slower recovery means more gradual build-up is safer.

```python
def suggest_masters_duration(base_default_weeks, age_group):
    """
    Apply age-based duration modifier to the default plan length.
    The modifier ensures masters runners get enough time to build
    safely with their slower recovery timelines.
    """
    modifier = AGE_ADJUSTMENTS["adjustments_by_age"][age_group]["plan_duration_modifier"]
    suggested = round(base_default_weeks * modifier)

    return {
        "suggested_weeks": suggested,
        "reason": (
            f"Based on your age group, we recommend {suggested} weeks "
            f"to allow for additional recovery between hard sessions. "
            f"You can choose any duration from the standard range."
        ),
        "allow_override": True,
        "minimum_for_age_group": base_default_weeks  # never shorter than standard default
    }
```

### 5D.5 Non-Standard Training Cycles (9-10 Day Cycles)

For runners 60+, the app should support training cycles that break from the
traditional 7-day week. Research suggests older athletes benefit from cycles
that space workouts based on recovery needs rather than calendar weeks.

```python
def generate_non_standard_cycle(age_group, training_cycle_days):
    """
    For 60+ runners, generate 9-10 day training cycles.
    Each cycle contains the same workout types as a standard week
    but spread over more days for adequate recovery.

    Example 9-day cycle:
    Day 1: Easy run
    Day 2: Strength training
    Day 3: Easy run
    Day 4: Rest
    Day 5: Hard session (tempo/intervals)
    Day 6: Easy run
    Day 7: Rest or active recovery
    Day 8: Long run
    Day 9: Rest

    The plan generator maps these cycles to calendar dates.
    The calendar view shows workouts by date (not by "Week 1 Day 3")
    so the non-standard cycle is transparent to the user.

    Weekly mileage targets are recalculated as:
        cycle_mileage = weekly_target * (training_cycle_days / 7)
    """

    cycle_template = {
        9: {
            "hard_sessions": 1,
            "easy_runs": 3,
            "long_run": 1,
            "strength": 1,
            "rest_days": 3,
            "pattern": "E-S-E-R-H-E-R-L-R"
        },
        10: {
            "hard_sessions": 1,
            "easy_runs": 3,
            "long_run": 1,
            "strength": 1,
            "rest_days": 4,
            "pattern": "E-S-R-E-R-H-E-R-L-R"
        }
    }

    return cycle_template.get(training_cycle_days, cycle_template[9])
```

### 5D.6 Returning Masters Runners (Coming Back from Break)

Research shows that VO2max begins declining almost linearly within days of
stopping training, with up to 20% loss after 12 weeks of inactivity. However,
resuming training can quickly restore some or all of the lost fitness.
The app should handle masters runners returning from a break differently
than younger runners.

```python
def assess_masters_return(user_profile, weeks_inactive):
    """
    Masters runners lose fitness faster during breaks but CAN recover it.
    The plan should be more conservative on return based on both age
    and duration of inactivity.
    """
    age_group = user_profile.age_group
    base_ramp_weeks = {
        "under_40": max(2, weeks_inactive // 3),
        "40_49": max(3, weeks_inactive // 2),
        "50_59": max(4, int(weeks_inactive * 0.6)),
        "60_plus": max(4, int(weeks_inactive * 0.75))
    }

    return {
        "ramp_back_weeks": base_ramp_weeks[age_group],
        "starting_volume_percent": {
            "under_40": 0.60,   # start at 60% of pre-break volume
            "40_49": 0.50,      # start at 50%
            "50_59": 0.40,      # start at 40%
            "60_plus": 0.35     # start at 35% — tendons need the most caution
        }[age_group],
        "max_intensity_first_2_weeks": "easy_only",  # no hard sessions
        "strength_training_first": True,  # resume strength before adding intensity
        "message": (
            "Welcome back! We've adjusted your plan to account for your "
            f"time away. You'll spend the first {base_ramp_weeks[age_group]} weeks "
            "rebuilding your base before reintroducing harder workouts."
        )
    }
```

### 5D.7 Masters Nutritional Guidance

```python
MASTERS_NUTRITION = {
    "protein": {
        "description": (
            "Aging muscles need up to 40% more amino acids to match the "
            "recovery response of younger athletes. Spread protein intake "
            "across 4 meals rather than 3 for better utilization."
        ),
        "recommendation_by_age": {
            "under_40": "1.2 g/kg/day",
            "40_49": "1.4 g/kg/day",
            "50_59": "1.6 g/kg/day",
            "60_plus": "1.7 g/kg/day"
        },
        "timing": "Within 30 min post-workout, especially after hard sessions"
    },
    "supplements_with_evidence": [
        {
            "name": "Omega-3 fatty acids",
            "benefit": "Reduce inflammation, improve cardiac efficiency",
            "evidence_level": "strong"
        },
        {
            "name": "Creatine",
            "benefit": "Aid muscle mass retention and training intensity",
            "evidence_level": "strong for older adults"
        },
        {
            "name": "Vitamin D",
            "benefit": "Immune health, muscle function, bone density",
            "evidence_level": "strong"
        },
        {
            "name": "Collagen peptides",
            "benefit": "Support tendon and connective tissue health",
            "evidence_level": "moderate — emerging research"
        }
    ],
    "display_mode": "educational_tips",
    "disclaimer": "Consult a healthcare provider before starting supplements"
}
```

### 5D.8 Age-Adjusted Data Model

```typescript
// Add to UserProfile interface:
interface UserProfile {
  // ... existing fields ...
  date_of_birth: Date | null;   // optional, used for age adjustments
  age_group: "under_40" | "40_49" | "50_59" | "60_plus" | null;
  // If date_of_birth not provided, user can self-select age group
  // If neither provided, no age adjustments applied
}

interface MastersAdjustments {
  age_group: string;
  recovery_days_between_hard: number;
  deload_frequency_weeks: number;
  strength_emphasis: "normal" | "increased" | "high";
  max_hard_sessions_per_week: number;
  warm_up_extension_minutes: number;
  protein_g_per_kg: number;
  plan_duration_modifier: number;
  training_cycle_days: number;          // 7 for standard, 9-10 for 60+
  returning_from_break: boolean;
  ramp_back_weeks: number | null;       // if returning
}

interface NutritionalGuidance {
  protein_target: string;
  supplement_suggestions: SupplementSuggestion[];
  timing_advice: string;
  display_mode: "educational_tips";     // not prescriptive
}
```

### 5D.9 API Endpoints

```
GET    /api/users/:id/age-adjustments        — Get computed adjustments for user's age
GET    /api/plans/:id/masters-config          — Get masters-specific plan configuration
POST   /api/users/:id/return-assessment       — Assess returning runner's starting point
GET    /api/plans/:id/training-cycle          — Get current cycle structure (7 or 9-10 day)
GET    /api/users/:id/nutrition-guidance      — Get age-appropriate nutritional tips
```

---

## 5G. Experience-Level Plan Differentiation

A first-time half marathoner should receive a fundamentally different plan than
an experienced runner training for the same distance. The difference is not just
duration — it affects workout types, progression rate, goal framing, and injury
risk management. The plan generator must treat skill level as a primary input
that shapes every aspect of the plan, not just volume.

### 5G.1 Overview & Research

Research consistently shows that beginners benefit from longer, more gradual
plans. Jack Daniels identifies 24 weeks as ideal for all levels; Jeff Galloway's
gradual approach achieves 98%+ marathon finish rates. Meanwhile, Brad Hudson's
experienced runners can prepare in as few as 12 weeks with an existing aerobic
base.

Key findings:
- Beginners who follow structured, gradual programs have dramatically higher
  completion rates than those using compressed plans.
- A three-year study found runners training consistently 23+ miles/week for the
  half marathon had significantly fewer injuries.
- Doherty et al. (systematic review of 127 cohorts) showed increases in weekly
  distance, frequency, and longest run all correlated with faster finish times —
  but only when the build-up was gradual.
- Injury risk is highest in first-year athletes and when ACWR exceeds 1.5
  (acute workload spike relative to chronic fitness).

([Hal Higdon — Half Marathon Plans](https://www.halhigdon.com/training/half-marathon-training/),
[Outside Online — First Half Marathon](https://run.outsideonline.com/training/getting-started/half-marathon-training-plan/),
[campus.coach — Marathon Duration](https://www.campus.coach/en/blog/duration-of-marathon-race-training-plan),
[campus.coach — 1 Year Prep](https://www.campus.coach/en/blog/marathon-preparation-for-1-year-the-pros-and-cons),
[PMC — 92 Marathon Plans](https://pmc.ncbi.nlm.nih.gov/articles/PMC11065819/),
[PMC — ACWR and Injury](https://pmc.ncbi.nlm.nih.gov/articles/PMC12487117/),
[Runners Connect — Injury Prevention](https://runnersconnect.net/injury-prevention/),
[Jeff Galloway — Half Marathon](https://www.jeffgalloway.com/training/half-marathon-training/))

### 5G.2 How Plans Differ by Experience Level

```python
EXPERIENCE_LEVEL_PLAN_DESIGN = {
    "beginner": {
        "description": (
            "First-time runner at this distance, or runner with less than "
            "6 months of consistent training. Goal: FINISH the distance "
            "safely and enjoyably."
        ),

        # PREREQUISITES
        "minimum_prerequisites": {
            "5K": "Can walk 30 minutes continuously",
            "10K": "Can run 2-3 miles continuously",
            "half_marathon": "Can run 3-5 miles continuously, running 3x/week",
            "marathon": "Has completed a half marathon OR runs 15+ miles/week for 3+ months"
        },

        # PLAN STRUCTURE
        "runs_per_week": {"min": 3, "max": 4},
        "hard_sessions_per_week": 0,      # NO speedwork in first 4-6 weeks
        "hard_sessions_introduced_week": {
            "5K": 3,          # introduce strides at week 3
            "10K": 4,         # introduce tempo at week 4
            "half_marathon": 5,  # introduce tempo at week 5
            "marathon": 6     # introduce tempo at week 6
        },
        "max_hard_sessions_per_week": 1,   # never more than 1 quality session

        # PROGRESSION
        "weekly_mileage_increase_percent": {"max": 8},    # conservative, not 10%
        "long_run_increase_percent": {"max": 10},
        "long_run_cap_hours": 2.5,         # cap at 2.5 hours, not distance
        "run_walk_option": True,           # offer run/walk intervals
        "run_walk_default": {
            "5K": False,      # most beginners can run a 5K continuously
            "10K": True,      # offer as option
            "half_marathon": True,  # strongly recommend for first-timers
            "marathon": True        # strongly recommend
        },

        # GOAL FRAMING
        "primary_goal": "finish",
        "show_pace_targets": False,        # no pace pressure
        "show_time_estimates": True,       # "you're on track to finish"
        "completion_messaging": (
            "Every run you complete is building your endurance. "
            "There's no wrong pace — if you finished, you nailed it."
        ),

        # INJURY PREVENTION
        "max_single_session_spike": 1.08,  # max 8% above recent peak (conservative)
        "mandatory_rest_days": 2,          # at least 2 rest days per week
        "strength_training": "optional_but_recommended",
        "cross_training_emphasis": "high"  # encourage non-impact days
    },

    "intermediate": {
        "description": (
            "Has completed this distance before OR has 6-12 months of "
            "consistent running. Goal: IMPROVE — run faster, stronger, "
            "or with better strategy."
        ),

        # PREREQUISITES
        "minimum_prerequisites": {
            "5K": "Runs 3x/week, 10+ miles/week",
            "10K": "Runs 3-4x/week, 15+ miles/week",
            "half_marathon": "Runs 4x/week, 20+ miles/week, has run 10K+",
            "marathon": "Runs 4-5x/week, 25+ miles/week, has run half marathon"
        },

        # PLAN STRUCTURE
        "runs_per_week": {"min": 4, "max": 5},
        "hard_sessions_per_week": 1,       # 1 quality session from week 1
        "hard_sessions_introduced_week": {
            "5K": 1, "10K": 1, "half_marathon": 2, "marathon": 2
        },
        "max_hard_sessions_per_week": 2,   # up to 2 quality sessions

        # PROGRESSION
        "weekly_mileage_increase_percent": {"max": 10},   # standard 10% rule
        "long_run_increase_percent": {"max": 12},
        "long_run_cap_hours": 3.0,
        "run_walk_option": True,
        "run_walk_default": {
            "5K": False, "10K": False, "half_marathon": False, "marathon": False
        },

        # GOAL FRAMING
        "primary_goal": "improve",
        "show_pace_targets": True,
        "show_time_estimates": True,
        "completion_messaging": (
            "You know you can finish. Now let's see how strong "
            "you can run it. Trust the process."
        ),

        # INJURY PREVENTION
        "max_single_session_spike": 1.10,  # standard 10% spike guard
        "mandatory_rest_days": 1,          # at least 1 rest day per week
        "strength_training": "recommended",
        "cross_training_emphasis": "moderate"
    },

    "advanced": {
        "description": (
            "Experienced runner with 12+ months at this distance, multiple "
            "races completed, running 5+ days/week. Goal: OPTIMIZE — PR, "
            "qualify for events, or master race execution."
        ),

        # PREREQUISITES
        "minimum_prerequisites": {
            "5K": "Runs 5x/week, 25+ miles/week, has raced 5K",
            "10K": "Runs 5x/week, 30+ miles/week, has raced 10K",
            "half_marathon": "Runs 5-6x/week, 35+ miles/week, has completed half",
            "marathon": "Runs 5-7x/week, 40+ miles/week, has completed marathon"
        },

        # PLAN STRUCTURE
        "runs_per_week": {"min": 5, "max": 7},
        "hard_sessions_per_week": 2,       # 2 quality sessions from week 1
        "hard_sessions_introduced_week": {
            "5K": 1, "10K": 1, "half_marathon": 1, "marathon": 1
        },
        "max_hard_sessions_per_week": 3,   # up to 3 in peak phase

        # PROGRESSION
        "weekly_mileage_increase_percent": {"max": 12},
        "long_run_increase_percent": {"max": 15},
        "long_run_cap_hours": 3.5,
        "run_walk_option": False,          # not applicable
        "run_walk_default": {
            "5K": False, "10K": False, "half_marathon": False, "marathon": False
        },

        # GOAL FRAMING
        "primary_goal": "optimize",
        "show_pace_targets": True,
        "show_time_estimates": True,
        "completion_messaging": (
            "Fine-tuning your fitness. Every workout has a purpose — "
            "trust the training and execute on race day."
        ),

        # INJURY PREVENTION
        "max_single_session_spike": 1.10,
        "mandatory_rest_days": 1,
        "strength_training": "integrated",  # part of the plan
        "cross_training_emphasis": "low"     # running-focused
    }
}
```

### 5G.3 Extended Plans for Beginners — Why Longer is Better

Research confirms that beginners benefit significantly from longer plan
durations. The plan generator should actively recommend extended plans for
beginners and explain the benefits.

```python
def recommend_plan_duration(skill_level, race_distance, user_current_mileage):
    """
    For beginners, recommend the longer end of the duration range.
    For advanced, recommend the shorter end.
    The user can always override, but the recommendation should be clear.
    """
    config = DURATION_RANGES[race_distance]
    default_weeks = {
        "beginner": config["default_beginner_weeks"],
        "intermediate": config["default_intermediate_weeks"],
        "advanced": config["default_advanced_weeks"]
    }[skill_level]

    # For beginners with LOW current mileage, recommend even longer
    if skill_level == "beginner":
        distance_min_mileage = {
            "5K": 5,    # km/week
            "10K": 10,
            "half_marathon": 20,
            "marathon": 30
        }
        if user_current_mileage < distance_min_mileage[race_distance] * 0.5:
            # User is well below baseline — recommend extended plan
            extended = min(default_weeks + 6, config["maximum_weeks"])
            return {
                "recommended_weeks": extended,
                "reason": (
                    f"Since you're currently running {user_current_mileage} km/week, "
                    f"we recommend {extended} weeks to build your base gradually. "
                    f"This gives your tendons, muscles, and joints time to adapt "
                    f"and dramatically reduces injury risk."
                ),
                "allow_shorter": True,
                "minimum_safe": default_weeks
            }

    return {
        "recommended_weeks": default_weeks,
        "reason": f"Based on your experience level and the {race_distance} distance.",
        "allow_shorter": True,
        "minimum_safe": config["minimum_weeks"]
    }
```

### 5G.4 Workout Type Availability by Experience Level

Not all 22 workout types (Section 5) are appropriate for all skill levels.
The plan generator should unlock workout types progressively.

```python
WORKOUT_AVAILABILITY = {
    "beginner": {
        "always_available": [
            "easy_run", "long_run", "rest_day", "walking",
            "cross_training_cycling", "cross_training_swimming",
            "cross_training_elliptical", "yoga", "pilates"
        ],
        "unlocked_after_base_phase": [
            "tempo_run", "strides", "fartlek"
        ],
        "never_in_plan": [
            "interval_800m", "interval_1600m", "hill_repeats",
            "sprints", "ladder_intervals", "pyramid_intervals"
        ],
        "notes": (
            "Beginners should build consistency and aerobic base before "
            "any speedwork. Strides are the first intensity introduction — "
            "short, controlled accelerations that teach good form without "
            "the injury risk of full intervals."
        )
    },

    "intermediate": {
        "always_available": [
            "easy_run", "long_run", "tempo_run", "strides",
            "fartlek", "rest_day", "walking",
            "cross_training_cycling", "cross_training_swimming",
            "cross_training_elliptical", "yoga", "pilates",
            "strength_training"
        ],
        "unlocked_after_base_phase": [
            "interval_800m", "interval_1600m", "hill_repeats"
        ],
        "never_in_plan": [
            "sprints"  # reserved for advanced
        ],
        "notes": (
            "Intermediate runners can handle structured speedwork but should "
            "still be conservative with volume of hard efforts. Hill repeats "
            "are excellent — they build strength with lower injury risk than "
            "flat intervals."
        )
    },

    "advanced": {
        "always_available": [
            # All 22 workout types available from the start
            "easy_run", "long_run", "tempo_run", "strides",
            "fartlek", "interval_800m", "interval_1600m",
            "hill_repeats", "sprints", "ladder_intervals",
            "pyramid_intervals", "strength_training",
            "cross_training_cycling", "cross_training_swimming",
            "cross_training_elliptical", "cross_training_rowing",
            "aqua_jogging", "yoga", "pilates",
            "rest_day", "walking"
        ],
        "unlocked_after_base_phase": [],
        "never_in_plan": [],
        "notes": (
            "Advanced runners have full access to all workout types. "
            "The plan generator selects based on phase, distance, and "
            "the runner's specific goals (speed vs endurance vs race execution)."
        )
    }
}
```

### 5G.5 Run/Walk Method for True Beginners

For runners who can't yet run continuously for 30 minutes, the app should
support run/walk intervals as a first-class training method, not a fallback.

```python
RUN_WALK_CONFIG = {
    "description": (
        "Jeff Galloway's run/walk method has helped hundreds of thousands "
        "of runners finish half marathons and marathons with 98%+ completion "
        "rates. It reduces injury risk, manages fatigue, and builds confidence."
    ),

    "starter_intervals": {
        "phase_1_weeks_1_3": {
            "run_minutes": 1,
            "walk_minutes": 2,
            "total_workout_minutes": 30,
            "label": "Getting Started"
        },
        "phase_2_weeks_4_6": {
            "run_minutes": 2,
            "walk_minutes": 1,
            "total_workout_minutes": 35,
            "label": "Building Confidence"
        },
        "phase_3_weeks_7_9": {
            "run_minutes": 4,
            "walk_minutes": 1,
            "total_workout_minutes": 40,
            "label": "Finding Your Rhythm"
        },
        "phase_4_weeks_10_plus": {
            "run_minutes": 8,
            "walk_minutes": 1,
            "total_workout_minutes": 45,
            "label": "Run Strong"
        }
    },

    "race_day_strategy": {
        "description": (
            "Run/walk is a legitimate race strategy, not a compromise. "
            "Galloway data shows run/walkers finish with similar or FASTER "
            "times than continuous runners at the same fitness level because "
            "they maintain pace consistency and avoid the late-race fade."
        ),
        "recommended_race_intervals": {
            "half_marathon": {"run": 4, "walk": 1, "unit": "minutes"},
            "marathon": {"run": 3, "walk": 1, "unit": "minutes"}
        }
    },

    "transition_to_continuous": {
        "description": (
            "If the user wants to transition to continuous running, "
            "the app progressively extends run intervals and shortens walks. "
            "This is OPTIONAL — many experienced runners use run/walk "
            "for all distances."
        ),
        "never_pressure": True,
        "messaging": (
            "Run/walk is a strategy, not a stepping stone. "
            "Use it as long as it serves you."
        )
    }
}
```

### 5G.6 Beginner Confidence Building

First-time runners at any distance face psychological barriers alongside
physical ones. The plan should actively build confidence through design.

```python
BEGINNER_CONFIDENCE_FEATURES = {
    "milestone_celebrations": [
        {"trigger": "first_run_completed", "message": "You did it! Your first run in the books."},
        {"trigger": "first_week_completed", "message": "Week 1 done. You're officially in training."},
        {"trigger": "longest_run_ever", "message": "That's the farthest you've ever run. New personal record!"},
        {"trigger": "halfway_through_plan", "message": "Halfway there. Look how far you've come."},
        {"trigger": "first_double_digit_miles_week", "message": "Double-digit mileage week. You're a runner."},
        {"trigger": "race_distance_75_percent", "message": "You just ran 75% of race distance in training. Race day is going to feel great."}
    ],

    "beginner_specific_tips": {
        "frequency": "one_per_week",  # don't overwhelm
        "topics": [
            "It's normal to feel slow at first — pace comes with consistency",
            "Walk breaks are a strategy, not a failure",
            "Soreness after 1-2 days is normal; sharp pain is not — here's the difference",
            "You don't need expensive gear to start — any comfortable shoes work",
            "Side stitches happen to everyone — slow down and breathe deeply",
            "Your easy pace should feel like you could hold a conversation",
            "Rest days are when your body actually gets stronger"
        ]
    },

    "social_proof": {
        "show_aggregate_stats": True,
        "examples": [
            "85% of beginners on this plan completed their race",
            "The average first-time half marathoner started exactly where you are",
            "Runners who follow the plan for 4+ weeks are 3x more likely to finish"
        ]
    }
}
```

### 5G.7 Experience Level Assessment

The onboarding flow should intelligently assess skill level rather than
relying solely on self-reporting (runners tend to overestimate).

```python
def assess_skill_level(user_responses):
    """
    Combine self-assessment with objective indicators.
    If there's a mismatch, default to the MORE CONSERVATIVE level
    and explain why.
    """
    indicators = {
        "self_reported_level": user_responses.get("level"),  # beginner/intermediate/advanced
        "current_weekly_mileage": user_responses.get("weekly_km"),
        "runs_per_week": user_responses.get("runs_per_week"),
        "months_running_consistently": user_responses.get("months_consistent"),
        "has_completed_target_distance": user_responses.get("completed_distance"),
        "previous_race_times": user_responses.get("race_times", []),
        "current_longest_run_km": user_responses.get("longest_run_km")
    }

    # Score each indicator
    objective_score = 0
    max_score = 0

    if indicators["current_weekly_mileage"] is not None:
        max_score += 1
        if indicators["current_weekly_mileage"] >= 40: objective_score += 1
        elif indicators["current_weekly_mileage"] >= 20: objective_score += 0.5

    if indicators["runs_per_week"] is not None:
        max_score += 1
        if indicators["runs_per_week"] >= 5: objective_score += 1
        elif indicators["runs_per_week"] >= 3: objective_score += 0.5

    if indicators["months_running_consistently"] is not None:
        max_score += 1
        if indicators["months_running_consistently"] >= 12: objective_score += 1
        elif indicators["months_running_consistently"] >= 6: objective_score += 0.5

    if indicators["has_completed_target_distance"]:
        max_score += 1
        objective_score += 1

    # Determine objective level
    if max_score > 0:
        ratio = objective_score / max_score
        if ratio >= 0.75:
            objective_level = "advanced"
        elif ratio >= 0.4:
            objective_level = "intermediate"
        else:
            objective_level = "beginner"
    else:
        objective_level = indicators["self_reported_level"] or "beginner"

    # If self-reported level is HIGHER than objective assessment, use objective
    level_order = {"beginner": 0, "intermediate": 1, "advanced": 2}
    self_level = indicators["self_reported_level"] or objective_level

    if level_order.get(self_level, 0) > level_order.get(objective_level, 0):
        return {
            "assessed_level": objective_level,
            "self_reported": self_level,
            "mismatch": True,
            "message": (
                f"Based on your current training ({indicators['current_weekly_mileage']} km/week, "
                f"{indicators['runs_per_week']}x/week), we'd recommend starting with an "
                f"{objective_level} plan. This gives you room to build safely. "
                f"You can always upgrade mid-plan if it feels too easy."
            ),
            "allow_override": True  # user can insist on their self-assessment
        }

    return {
        "assessed_level": objective_level,
        "self_reported": self_level,
        "mismatch": False,
        "message": None,
        "allow_override": False
    }
```

### 5G.8 Data Model Additions

```typescript
interface ExperienceLevelConfig {
  skill_level: "beginner" | "intermediate" | "advanced";
  assessed_level: string;             // objective assessment result
  self_reported_level: string;        // what user said
  level_mismatch: boolean;
  user_overrode_assessment: boolean;

  // Plan structure
  runs_per_week: { min: number; max: number };
  hard_sessions_per_week: number;
  max_hard_sessions_per_week: number;
  hard_sessions_introduced_week: number;

  // Progression limits
  weekly_mileage_increase_percent_max: number;
  long_run_increase_percent_max: number;
  max_single_session_spike: number;

  // Run/walk
  run_walk_enabled: boolean;
  run_walk_intervals: RunWalkInterval | null;

  // Goal framing
  primary_goal: "finish" | "improve" | "optimize";
  show_pace_targets: boolean;

  // Available workouts
  available_workout_types: string[];
  locked_workout_types: string[];     // unlocked after base phase
  excluded_workout_types: string[];   // never used for this level
}

interface RunWalkInterval {
  run_minutes: number;
  walk_minutes: number;
  progression_phase: string;
  race_day_strategy: { run: number; walk: number } | null;
}

interface SkillLevelAssessment {
  self_reported: string;
  objective_indicators: {
    weekly_mileage_km: number | null;
    runs_per_week: number | null;
    months_consistent: number | null;
    completed_target_distance: boolean;
    longest_run_km: number | null;
  };
  assessed_level: string;
  mismatch: boolean;
  user_overrode: boolean;
}
```

### 5G.9 API Endpoints

```
GET    /api/plans/:id/experience-config       — Get experience-level plan configuration
POST   /api/users/:id/assess-skill-level      — Run skill level assessment
PUT    /api/users/:id/skill-level-override     — Override assessed skill level
GET    /api/plans/:id/available-workouts       — Get workouts available for this level/phase
GET    /api/plans/:id/run-walk-config          — Get current run/walk interval settings
PUT    /api/plans/:id/run-walk-config          — Update run/walk preferences
GET    /api/plans/:id/confidence-milestones    — Get upcoming milestone triggers
```

---

## 5E. Race Day Nutrition Guidance

For half marathon and marathon distances, the app should include nutrition
guidance as part of the taper and race week experience. Fueling strategy
is a trainable skill — it should be practiced during long runs.

([Korey Stringer Institute](https://koreystringer.institute.uconn.edu/2024/06/03/the-first-time-marathoners-guide-to-fuel-and-hydration-for-your-marathon-training/),
[PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC11911277/),
[Skratch Labs](https://www.skratchlabs.com/blogs/science-products/fueling-your-half-marathon-or-marathon))

```json
{
  "nutrition_guidance": {
    "applies_to": ["half_marathon", "marathon"],
    "display": "Integrated into long run instructions and race week countdown",

    "during_training_long_runs": {
      "trigger": "Long runs over 75 minutes",
      "message": "Practice your race-day fueling on this long run. Take a gel or sports drink at the 45-min mark, then every 30-45 minutes after.",
      "goal": "Train your GI tract to tolerate fuel during effort. Never try anything new on race day."
    },

    "race_week_nutrition": {
      "day_minus_3": "Begin carb-loading if racing half marathon or longer. Aim for 8-10 g/kg/day of carbohydrates. Reduce fiber intake.",
      "day_minus_2": "Continue carb-loading (70% of calories from carbs). Stay hydrated — pale yellow urine is the target.",
      "day_minus_1": "Familiar, carb-rich meals. No new foods. Hydrate normally — don't overdo it.",
      "race_morning": "Eat 2-3 hours before start. Light, carb-rich meal (toast, banana, oatmeal). Sip water, don't chug."
    },

    "during_race_fueling": {
      "5K": "No fueling needed. Water if hot.",
      "10K": "Water at aid stations if available. No gels needed for most runners.",
      "half_marathon": "25-30g carbs every 30-45 min after the 45-min mark. Water at every other aid station.",
      "marathon": "30-60g carbs per hour (gels, chews, or sports drink). Start at mile 4-5. Water at every aid station. Add electrolytes if hot."
    },

    "hydration_guideline": {
      "pre_race": "5-7 mL/kg body weight, 4 hours before start",
      "during_race": "Drink to thirst — 400-800 mL per hour depending on sweat rate and heat",
      "warning": "Do NOT overdrink. Hyponatremia (low sodium from excess water) is dangerous."
    },

    "caffeine": {
      "recommendation": "3-6 mg/kg body weight, 30-60 min before start (if practiced in training)",
      "note": "Evidence-based performance supplement. Only use if tested during training."
    }
  }
}
```

---

## 5F. Mental Preparation

Race anxiety can reduce performance by 10-15% (Raglin & Hanin). The app
should include mental preparation as part of the taper and race week experience.

([PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC11607677/),
[Runners Connect](https://runnersconnect.net/pre-race-anxiety-runners-guide/),
[Precision Hydration](https://www.precisionhydration.com/performance-advice/performance/how-to-mentally-prepare-for-an-endurance-event-race/))

```json
{
  "mental_preparation": {
    "applies_to": "all_distances",
    "display": "Integrated into taper phase and race week countdown",

    "visualization_prompt": {
      "trigger": "Daily during taper phase",
      "duration_minutes": 5,
      "message": "Spend 5 minutes visualizing your race. Picture the start line, your pace settling in, passing mile markers, pushing through the hard middle miles, and crossing the finish line. Use all five senses.",
      "research": "Mental rehearsal activates the same neural pathways as physical training (Martin, Moritz & Hall)."
    },

    "anxiety_reframe": {
      "trigger": "Race week (day_minus_3 through race_day)",
      "messages": [
        "Pre-race nerves are normal — they mean you care. Research shows anxiety and excitement trigger the same physiological response. Tell yourself 'I'm excited' instead of 'I'm nervous.'",
        "Review your training log. You've put in the work. This race is a celebration of that effort.",
        "Set a process goal for race day: 'I will run my first mile at [pace] and fuel at every planned point.' Process goals outperform time goals under pressure."
      ]
    },

    "race_day_mantras": {
      "trigger": "Race morning notification",
      "options": [
        "Strong and steady.",
        "One mile at a time.",
        "I trained for this.",
        "Smooth is fast."
      ],
      "note": "User can set a custom mantra during taper."
    },

    "mid_race_mental_tools": {
      "description": "Included in race-day pacing guidance",
      "tools": [
        "Break the race into thirds: settle in, sustain, push.",
        "If struggling, focus on the next aid station, not the finish.",
        "Count steps in sets of 100 to stay present.",
        "If behind pace at halfway, adopt a revised target rather than giving up. The app can show an adjusted prediction."
        ]
    }
  }
}
```

---

---

## 8. Plan Generation Algorithm

```python
def generate_plan(user_profile, race_distance, race_date):
    """
    Master algorithm for generating a complete training plan.
    """

    # 1. Determine skill level from user profile
    skill_level = assess_skill_level(user_profile)

    # 2. Get base configuration
    config = DISTANCE_CONFIGS[race_distance][skill_level]

    # 3. Calculate start date
    start_date = race_date - timedelta(weeks=config.duration_weeks)

    # 4. Validate start date isn't in the past
    if start_date < today():
        # Offer shortened plan or suggest later race
        available_weeks = (race_date - today()).days // 7
        config = adjust_config_for_shorter_duration(config, available_weeks)

    # 5. Generate week-by-week mileage targets
    weekly_mileage = calculate_weekly_mileage(config)

    # 6. Assign phases to weeks
    weeks = assign_phases(weekly_mileage, config)

    # 7. Populate each week with specific workouts
    for week in weeks:
        week.workouts = generate_weekly_workouts(
            week=week,
            config=config,
            user_days=user_profile.available_days,
            long_run_day=user_profile.preferred_long_run_day,
            max_daily_minutes=user_profile.max_daily_minutes
        )

    # 8. Apply schedule exceptions (holidays, blocked dates)
    for exception in user_profile.schedule_exceptions:
        weeks = apply_schedule_exception(weeks, exception)

    # 9. Validate single-session spike guard across entire plan
    weeks = validate_all_sessions(weeks)

    # 10. Return complete plan
    return TrainingPlan(
        user_id=user_profile.id,
        race_distance=race_distance,
        skill_level=skill_level,
        race_date=race_date,
        start_date=start_date,
        duration_weeks=config.duration_weeks,
        weeks=weeks
    )
```

### 8.1 Skill Level Assessment

```python
def assess_skill_level(user_profile):
    """
    Determine beginner/intermediate/advanced based on user data.
    """
    score = 0

    # Running experience
    if user_profile.running_experience_months >= 24:
        score += 3
    elif user_profile.running_experience_months >= 6:
        score += 2
    else:
        score += 1

    # Current weekly mileage
    if user_profile.current_weekly_mileage_km >= 40:
        score += 3
    elif user_profile.current_weekly_mileage_km >= 20:
        score += 2
    else:
        score += 1

    # Race history
    if len(user_profile.recent_race_times) >= 3:
        score += 3
    elif len(user_profile.recent_race_times) >= 1:
        score += 2
    else:
        score += 1

    if score >= 8:
        return "advanced"
    elif score >= 5:
        return "intermediate"
    else:
        return "beginner"
```

---
