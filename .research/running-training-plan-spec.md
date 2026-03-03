# Running Training Plan Application — Product Specification

## 1. Overview

This specification defines the data models, algorithms, and business logic for an application that generates personalized running training plans for 5K, 10K, Half Marathon, and Full Marathon distances. The app supports three skill levels (Beginner, Intermediate, Advanced), provides week-to-week progressive overload, and includes intelligent schedule flexibility for holidays, missed days, and load redistribution.

This document is designed to be consumed by an LLM or development team to implement the full solution.

---

## 1.1 Product Decisions

The following product-level decisions have been finalized and apply across the entire specification:

### Pace Zone Calculation
User chooses their preferred guidance mode during onboarding or in settings:
- **VDOT mode**: User enters a recent race time. App calculates training paces using Jack Daniels' VDOT formula (easy, tempo, interval, marathon paces). If no race time is available, the app prompts for a timed 1-mile / 1.6 km effort to seed the model.
- **RPE mode**: All workout intensity is described in perceived effort terms (e.g., "conversational pace, RPE 4/10"). No pace numbers shown.
- Users can switch modes at any time. All workout descriptions must render in both formats.

### Long Run Progression Pattern
User selects their preferred build/deload cycle for the long run:
- **Linear** (no pullback weeks)
- **2-up-1-down** (increase 2 weeks, reduce on 3rd)
- **3-up-1-down** (default, recommended)
- **4-up-1-down** (increase 4 weeks, reduce on 5th)

On deload weeks, the long run drops by ~25%. The app should flag if the chosen long run cycle is out of sync with the overall weekly deload frequency.

### Onboarding Flow
Two entry paths:
1. **Experienced runner onboarding**: Short questionnaire (current weekly mileage, recent race times, training days per week). App assigns a skill level; user can adjust.
2. **Quick start (default to beginner)**: User skips onboarding entirely. Plan starts at beginner level. App auto-promotes after 2-3 weeks if logged performance suggests a higher level.

### Cross-Training
Cross-training activities (cycling, swimming, gym) are loggable via an optional toggle. They appear on the calendar but do **not** count toward running volume or affect plan calculations.

### Readiness and Auto-Adjustment
**V1**: Pre-workout subjective check-in on a 1-5 scale (Fresh / Good / Normal / Tired / Exhausted). If the user reports low readiness (4-5), the app suggests downgrading a hard session to easy or shortening the workout.

**V2 (future)**: The data model includes fields for HRV, resting heart rate, and sleep quality to support wearable integration (Garmin, Apple Watch, etc.) when ready.

### Climate Adjustments
**V1**: Heat and humidity adjustments only. Pace targets slow by approximately 1-2 seconds per km per degree Celsius above 15°C, scaling with humidity. Requires a weather API call keyed to user location. The app displays a note (e.g., "Pace adjusted for 32°C heat"). Altitude and terrain adjustments deferred to v2.

### Pause and Resume
Users can explicitly pause their plan via a dedicated button:
- Pausing stops all notifications and the training clock.
- On resume, the user chooses between:
  - **Resume where I left off** (if break was short / user stayed active)
  - **Readjust my plan** (triggers the missed-day recovery algorithm from Section 6.3 based on pause duration)

### Concurrent Plans
One active plan at a time. To start a new plan, users must archive or complete their current one. This is a product design decision (keeps users focused), not a monetization gate. The only paid feature is holiday and vacation handling (Section 14).

---

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

## 9. Notification and Coaching Logic

### 9.1 Adaptive Messaging

```json
{
  "notifications": {
    "workout_reminder": {
      "timing": "morning of workout day OR evening before",
      "content": "Dynamic based on workout type and context"
    },
    "missed_workout": {
      "timing": "end of scheduled workout day if not marked complete",
      "content": "Reassuring message + rescheduling options"
    },
    "weekly_summary": {
      "timing": "end of training week (Sunday evening)",
      "content": "Completed vs planned, next week preview, encouragement"
    },
    "milestone": {
      "triggers": ["new_longest_run", "halfway_through_plan", "peak_week_completed", "taper_start"],
      "content": "Celebration + context for what's next"
    },
    "deload_week_start": {
      "timing": "start of deload week",
      "content": "Explain why reduced volume is beneficial, not a step backward"
    }
  }
}
```

---

## 10. Marketing Data Points

The following data should be stored per user and in aggregate for marketing dashboards and content:

```json
{
  "marketing_metrics": {
    "per_user": {
      "total_distance_trained_km": "number",
      "total_training_days": "number",
      "plan_completion_rate_pct": "number (workouts completed / workouts scheduled)",
      "longest_run_achieved_km": "number",
      "current_streak_days": "number",
      "best_streak_days": "number",
      "weeks_trained": "number",
      "missed_days_recovered": "number (times the app helped reschedule)",
      "holidays_navigated": "number",
      "race_distance": "string",
      "skill_level_at_start": "string",
      "skill_level_current": "string (if re-assessed)",
      "estimated_race_time": "string (if pace data available)"
    },
    "aggregate": {
      "total_users": "number",
      "total_plans_generated": "number",
      "total_km_trained_all_users": "number",
      "avg_plan_completion_rate": "number",
      "most_popular_distance": "string",
      "most_popular_skill_level": "string",
      "avg_missed_days_per_plan": "number",
      "pct_users_who_completed_plan": "number",
      "total_holidays_handled": "number",
      "total_schedule_adjustments": "number",
      "user_retention_rate_weekly": "number"
    },
    "shareable_achievements": {
      "description": "User-facing stats suitable for social sharing and app marketing",
      "examples": [
        "I've run {total_km} km training for my first {race_distance}!",
        "{streak} day training streak and counting!",
        "Week {week_number} of {total_weeks} — {pct_complete}% of my plan done!",
        "{plans_completed} training plans completed on [App Name]"
      ]
    }
  }
}
```

---

## 11. Injury Logging & Plan Adjustment

### Activation Model: Dormant Until Needed

**The entire injury system is OPTIONAL and DORMANT by default.** No injury
features, pain check-ins, risk scores, or rehab exercises appear in the UI
unless the user explicitly reports an injury. This prevents burdening healthy
runners with unnecessary complexity.

```python
INJURY_SYSTEM_ACTIVATION = {
    "default_state": "dormant",
    "activation_trigger": "user_reports_injury",

    "when_dormant": {
        "visible_in_ui": False,          # no injury section shown
        "pain_check_ins": False,         # no prompts
        "risk_score": False,             # not computed
        "rehab_exercises": False,        # not shown
        "injury_history_page": True,     # always accessible in settings/profile
        "report_injury_button": True     # always visible (simple link, not prominent)
    },

    "when_active": {
        "visible_in_ui": True,           # injury banner appears
        "pain_check_ins": True,          # prompts based on severity
        "risk_score": True,              # computed and shown
        "rehab_exercises": True,         # body-part-specific shown
        "race_readiness": True           # shown if race is approaching
    },

    "deactivation": {
        "trigger": "injury_resolved_and_return_protocol_complete",
        "grace_period_days": 7,          # keep visible 7 days after resolved
        "then": "return_to_dormant"
    },

    "proactive_risk_score": {
        "description": (
            "The injury risk score (Section 11.12) runs in the background "
            "even when dormant, but ONLY surfaces a notification if the "
            "score exceeds the 'critical' threshold (80+). This is the "
            "one exception to the dormant rule — a proactive safety net."
        ),
        "threshold_to_surface": 80,
        "notification": (
            "Your training load is very high relative to recent history. "
            "Take it easy today to reduce injury risk."
        )
    }
}
```

### 11.1 Injury Data Model

```typescript
interface InjuryLog {
  id: string;
  user_id: string;
  plan_id: string;
  reported_at: Date;
  body_part: BodyPart;
  severity: InjurySeverity;
  status: "active" | "recovering" | "resolved";
  resolved_at: Date | null;
  notes: string | null;
  plan_adjustments_applied: string[];  // IDs of adjustments made
}

type BodyPart =
  | "knee"
  | "shin"
  | "ankle"
  | "foot_arch"
  | "foot_heel"       // plantar fasciitis, heel pain
  | "achilles"
  | "calf"
  | "hamstring"
  | "quad"
  | "hip"
  | "glute"
  | "lower_back"
  | "it_band"
  | "groin"
  | "toe"
  | "other";

type InjurySeverity =
  | "mild"       // Discomfort but can run with modifications
  | "moderate"   // Pain during or after running, should reduce load
  | "severe";    // Cannot run, need full rest
```

### 11.2 Injury Response Algorithm

```python
def handle_injury_report(plan, injury_log, user_context):
    """
    Adjust the training plan based on reported injury severity.

    Key principles:
    - Never push through pain — reduce first, ask questions later
    - Always recommend medical consultation for moderate/severe
    - Track injury history to detect recurring patterns
    """

    severity = injury_log.severity
    body_part = injury_log.body_part

    if severity == "mild":
        # === MILD: Discomfort, can still run ===
        return {
            "action": "modify",
            "changes": [
                reduce_intensity_all_sessions(plan, days=7, reduction=0.20),
                convert_intervals_to_easy(plan, days=7),
                # Keep long run but reduce by 15%
                reduce_long_run(plan, next_occurrence=True, reduction=0.15)
            ],
            "message": f"Noted: {format_body_part(body_part)} discomfort. "
                      f"Reducing intensity for the next 7 days. "
                      f"All hard sessions converted to easy runs. "
                      f"Please update if symptoms worsen.",
            "follow_up_check_in_days": 3,
            "medical_recommendation": False
        }

    elif severity == "moderate":
        # === MODERATE: Pain during/after running ===
        return {
            "action": "reduce_significantly",
            "changes": [
                # Replace all runs with easy runs at 50% volume for 7 days
                reduce_all_sessions_volume(plan, days=7, reduction=0.50),
                convert_all_to_easy(plan, days=7),
                # Skip the long run this week
                skip_next_workout_type(plan, "long_run"),
                # Add rest days
                insert_extra_rest_days(plan, days=7, extra_rest=2)
            ],
            "message": f"Noted: {format_body_part(body_part)} pain. "
                      f"Significantly reducing your plan for the next week. "
                      f"We recommend seeing a sports medicine professional. "
                      f"Update us when you're feeling better.",
            "follow_up_check_in_days": 2,
            "medical_recommendation": True,
            "suggest_cross_training": get_safe_cross_training(body_part)
        }

    else:  # severe
        # === SEVERE: Cannot run ===
        return {
            "action": "pause_running",
            "changes": [
                pause_all_running(plan, until="user_reports_resolved"),
                # Show cross-training suggestions that avoid the injured area
            ],
            "message": f"Noted: {format_body_part(body_part)} — unable to run. "
                      f"All running is paused until you mark this as resolved. "
                      f"Please see a medical professional. "
                      f"When you're cleared to run, we'll rebuild your plan gradually.",
            "follow_up_check_in_days": 1,
            "medical_recommendation": True,
            "suggest_cross_training": get_safe_cross_training(body_part),
            "return_protocol": "Section 11.3"
        }


def get_safe_cross_training(body_part):
    """
    Suggest cross-training that avoids loading the injured area.
    """
    recommendations = {
        "knee":       ["swimming", "upper body strength", "pool running"],
        "shin":       ["cycling", "swimming", "upper body strength"],
        "ankle":      ["swimming", "upper body strength", "seated cycling"],
        "foot_arch":  ["swimming", "cycling (with caution)", "upper body strength"],
        "foot_heel":  ["swimming", "cycling", "upper body strength"],
        "achilles":   ["swimming", "upper body strength", "pool running (with caution)"],
        "calf":       ["swimming", "upper body strength", "cycling (light)"],
        "hamstring":  ["swimming", "upper body strength", "light cycling"],
        "quad":       ["swimming", "upper body strength"],
        "hip":        ["swimming (with caution)", "upper body strength"],
        "glute":      ["swimming", "upper body strength"],
        "lower_back": ["walking", "swimming (with caution)"],
        "it_band":    ["swimming", "upper body strength", "core work"],
        "groin":      ["swimming", "upper body strength"],
        "toe":        ["cycling", "swimming", "upper body strength"],
    }
    return recommendations.get(body_part, ["swimming", "upper body strength"])
```

### 11.3 Return-to-Running Protocol After Injury

```python
def generate_return_protocol(plan, injury_log, days_off):
    """
    When user marks an injury as resolved, apply a graduated return.

    Protocol based on duration of time off:
    - 1-3 days off:  Resume at 80% volume, all easy, for 3 days. Then normal.
    - 4-7 days off:  Week 1 at 60% (all easy), Week 2 at 80% (add one moderate).
    - 8-14 days off: Week 1 at 50% (easy only), Week 2 at 65%, Week 3 at 80%.
    - 14+ days off:  Apply missed-day algorithm (Section 6.3) with extra caution.

    Additional rules:
    - First run back must be easy and short (≤30 min or ≤5 km)
    - No intervals or tempo for at least 7 days after return
    - Long run not reintroduced until second week back
    - If pain recurs during return protocol, immediately revert to pause
    - Track body part in recurring_injury_tracker (Section 11.4)
    """

    if days_off <= 3:
        return apply_gradual_return(plan, today(), reduction=0.20, rebuild_weeks=1,
                                    easy_only_days=3)
    elif days_off <= 7:
        return apply_gradual_return(plan, today(), reduction=0.40, rebuild_weeks=2,
                                    easy_only_days=7, no_long_run_week=1)
    elif days_off <= 14:
        return apply_gradual_return(plan, today(), reduction=0.50, rebuild_weeks=3,
                                    easy_only_days=10, no_long_run_week=1)
    else:
        # Defer to the missed-day algorithm but add injury-specific caution
        result = handle_missed_day(plan, today(),
                                   user_context._replace(consecutive_missed=days_off))
        result["extra_caution"] = True
        result["no_intervals_days"] = 14
        result["pain_recheck_days"] = [1, 3, 5, 7]
        return result
```

### 11.4 Recurring Injury Detection

```python
def check_recurring_injury(user_id, new_injury):
    """
    Flag if the same body part has been injured multiple times.
    Suggest structural changes to the plan.
    """
    history = get_injury_history(user_id, body_part=new_injury.body_part)

    if len(history) >= 2 and all_within_months(history, months=6):
        return {
            "recurring": True,
            "count": len(history),
            "message": f"This is the {ordinal(len(history))} time you've reported "
                      f"{format_body_part(new_injury.body_part)} issues in the past "
                      f"6 months. We strongly recommend seeing a sports medicine "
                      f"professional or physical therapist. Consider reducing your "
                      f"overall plan intensity.",
            "suggestions": [
                "Reduce peak weekly mileage by 10-15%",
                "Add extra rest days to your weekly schedule",
                "Consider a longer plan duration for your goal race",
                "Add strength training focused on injury prevention"
            ]
        }
    return {"recurring": False}
```

### 11.5 Injury API Endpoints

```
POST   /api/plans/:id/injuries              — Report a new injury
PUT    /api/plans/:id/injuries/:iid         — Update injury (severity change, resolved)
GET    /api/plans/:id/injuries              — List injuries for current plan
GET    /api/users/:id/injury-history        — Full injury history across all plans
POST   /api/plans/:id/injuries/:iid/resolve — Mark injury as resolved, trigger return protocol
```

### 11.6 Pain Tracking & Check-Ins

The app should track pain levels over time so the user (and the algorithm)
can see whether an injury is improving, stable, or worsening. Research from
Brigham and Women's Hospital and the Arthroscopy, Sports Medicine, and
Rehabilitation journal emphasizes grading pain over days/weeks as critical
for injury management decisions.

([PMC — Trail Runner Injury Prevention](https://pmc.ncbi.nlm.nih.gov/articles/PMC8811510/),
[Brigham and Women's Return to Running](https://www.brighamandwomens.org/assets/bwh/patients-and-families/rehabilitation-services/pdfs/le-running-injury-prevention-tips-and-return-to-running-program-bwh.pdf),
[Runners Connect — Return to Running](https://runnersconnect.net/running-injury-return/))

#### 11.6.1 Pain Scale Data Model

```typescript
interface PainCheckIn {
  id: string;
  injury_id: string;
  recorded_at: Date;
  timing: "pre_run" | "during_run" | "post_run" | "morning" | "evening" | "manual";
  pain_level: number;             // 0-10 numerical pain rating scale (NPRS)
  pain_quality: PainQuality | null;
  pain_changed_during_run: "improved" | "same" | "worsened" | null;
  gait_affected: boolean;         // limping, compensating
  notes: string | null;
}

type PainQuality =
  | "dull_ache"       // typical overuse — often manageable
  | "sharp"           // warning sign — stop immediately
  | "burning"         // nerve or tendon irritation
  | "throbbing"       // inflammation
  | "stiff"           // often improves with warm-up
  | "tight"           // muscular — may be manageable
  | "stabbing";       // severe — stop and seek medical help
```

#### 11.6.2 Automated Check-In Prompts

```python
def generate_pain_check_ins(plan, active_injury):
    """
    When an injury is active, the app prompts for pain check-ins
    at strategic times. These are brief — 2 taps max.

    Prompt frequency:
    - Mild injury: pre-run and post-run only
    - Moderate injury: pre-run, during-run (push notification at
      midpoint), post-run, and next-morning
    - Severe injury: daily morning check-in until resolved
    """

    if active_injury.severity == "mild":
        return {
            "check_in_schedule": ["pre_run", "post_run"],
            "prompt": "Quick check: How's your {body_part}? (0-10)",
            "follow_up_if_worsened": True,
            "auto_escalate_threshold": 5  # if pain >= 5, escalate to moderate protocol
        }

    elif active_injury.severity == "moderate":
        return {
            "check_in_schedule": ["pre_run", "mid_run", "post_run", "next_morning"],
            "prompt": "Pain check: {body_part} — rate 0-10 and any change in quality",
            "follow_up_if_worsened": True,
            "auto_escalate_threshold": 7,
            "auto_pause_if": "pain_worsened_during_run OR gait_affected"
        }

    else:  # severe
        return {
            "check_in_schedule": ["morning_daily"],
            "prompt": (
                "Morning check-in: How's your {body_part} today? "
                "Rate 0-10. Any improvement since yesterday?"
            ),
            "suggest_resume_when": "3 consecutive days at pain <= 2",
            "require_medical_clearance": True
        }
```

#### 11.6.3 Pain Trend Analysis

```python
def analyze_pain_trend(injury_id, lookback_days=14):
    """
    Analyze pain check-in data to determine trajectory.
    Used by both the user-facing dashboard and the plan adjustment engine.
    """
    check_ins = get_pain_check_ins(injury_id, last_n_days=lookback_days)

    if len(check_ins) < 3:
        return {"trend": "insufficient_data", "recommendation": "Keep logging pain levels."}

    recent_avg = mean([c.pain_level for c in check_ins[-3:]])
    older_avg = mean([c.pain_level for c in check_ins[:3]])
    overall_avg = mean([c.pain_level for c in check_ins])

    if recent_avg < older_avg - 1.5:
        trend = "improving"
        recommendation = (
            "Your pain is trending down. Continue current modifications "
            "and we'll start reintroducing intensity soon."
        )
    elif recent_avg > older_avg + 1.5:
        trend = "worsening"
        recommendation = (
            "Pain is increasing. We're adding more rest to your plan. "
            "Please consider seeing a sports medicine professional."
        )
    else:
        trend = "stable"
        if overall_avg <= 3:
            recommendation = (
                "Pain is stable and low. You may be ready to start "
                "the return-to-running protocol."
            )
        else:
            recommendation = (
                "Pain is stable but persistent. Current modifications stay "
                "in place. If this continues for another week, consider "
                "seeking professional evaluation."
            )

    # Detect worsening-during-run pattern
    during_run_worsened = [c for c in check_ins
                          if c.pain_changed_during_run == "worsened"]
    if len(during_run_worsened) >= 2:
        recommendation += (
            " Note: Pain has worsened during running on multiple occasions. "
            "This is a sign that current training load exceeds tissue tolerance. "
            "Consider switching to cross-training until pain stops worsening."
        )

    return {
        "trend": trend,
        "recent_average": round(recent_avg, 1),
        "older_average": round(older_avg, 1),
        "recommendation": recommendation,
        "during_run_worsening_count": len(during_run_worsened),
        "check_in_count": len(check_ins)
    }
```

### 11.7 Run/Stop Decision Tree (During-Run Guidance)

Based on the four pain rules from sports medicine research, the app provides
real-time guidance when a runner reports pain during a workout.

```python
DURING_RUN_PAIN_RULES = {
    "description": (
        "Four evidence-based rules for deciding whether to continue, "
        "modify, or stop a run when pain is present. Based on research "
        "published in Arthroscopy, Sports Medicine, and Rehabilitation."
    ),

    "rule_1_stop_immediately": {
        "condition": "Pain increases during the run OR changes from dull/achy to sharp/stabbing",
        "action": "STOP running. Walk home or call for a ride.",
        "plan_effect": "Auto-mark workout as incomplete. Trigger injury check-in.",
        "message": (
            "Pain that increases or sharpens during a run means tissue is being "
            "damaged beyond its recovery capacity. Stop now to prevent this from "
            "becoming a longer-term injury."
        )
    },

    "rule_2_24_hour_check": {
        "condition": "Joint pain lingers or increases 24 hours after a run",
        "action": "Reduce next run volume by 30%. Skip any hard sessions for 3 days.",
        "plan_effect": "Auto-reduce next 3 days of training.",
        "message": (
            "Pain that persists or worsens the next day indicates the volume "
            "was too much. We've dialed back your upcoming sessions."
        )
    },

    "rule_3_preexisting_pain": {
        "condition": "Preexisting pain is present but below 3/10 before the run",
        "action": (
            "OK to run IF: pain does not increase during the run AND "
            "does not persist into the next day. Monitor closely."
        ),
        "plan_effect": "Continue with planned workout but log pain pre and post.",
        "message": (
            "Low-level discomfort (under 3/10) that doesn't increase is "
            "generally safe to run through. We'll check in after."
        )
    },

    "rule_4_gait_compensation": {
        "condition": "Pain causes limping, hip-hiking, or any change in running form",
        "action": "STOP immediately. Compensated running causes secondary injuries.",
        "plan_effect": "Pause running, suggest cross-training.",
        "message": (
            "If pain changes how you run — even slightly — stop. "
            "Compensating creates imbalances that lead to new injuries. "
            "Switch to cross-training until you can run with normal form."
        )
    }
}

def evaluate_mid_run_pain(pain_report):
    """
    Called when user reports pain during a run (via push notification
    check-in or manual entry).
    """
    if pain_report.gait_affected:
        return DURING_RUN_PAIN_RULES["rule_4_gait_compensation"]

    if pain_report.pain_quality in ["sharp", "stabbing"]:
        return DURING_RUN_PAIN_RULES["rule_1_stop_immediately"]

    if pain_report.pain_changed_during_run == "worsened":
        return DURING_RUN_PAIN_RULES["rule_1_stop_immediately"]

    if pain_report.pain_level < 3 and pain_report.pain_changed_during_run in ["same", "improved"]:
        return DURING_RUN_PAIN_RULES["rule_3_preexisting_pain"]

    # Pain present but stable, not sharp, no gait changes
    return {
        "action": "continue_with_caution",
        "message": (
            "Pain is present but stable. You can continue at easy effort. "
            "If it increases at any point, stop and walk."
        ),
        "plan_effect": "Convert remaining workout to easy pace. Log for trend analysis."
    }
```

### 11.8 Body-Part-Specific Plan Modifications

Beyond just reducing volume, the app should make specific modifications
based on WHERE the injury is. Different injuries respond to different
training adjustments.

```python
BODY_PART_PLAN_MODIFICATIONS = {
    "shin": {
        "label": "Shin Splints / Medial Tibial Stress Syndrome",
        "avoid": [
            "Downhill running — increases tibial loading",
            "Hard surfaces (concrete) — switch to treadmill, grass, or trail",
            "Speed work — high-impact forces",
            "Sudden increases in run duration"
        ],
        "modify": [
            "Shorten stride length by 5-10% to reduce impact",
            "Switch to softer surfaces (treadmill, grass, dirt trail)",
            "Replace intervals with cycling or pool running",
            "Add 2 extra minutes to warm-up"
        ],
        "prehab_exercises": [
            {"name": "Calf raises (eccentric focus)", "sets": 3, "reps": 15,
             "notes": "Rise on both feet, lower slowly on one"},
            {"name": "Toe raises / dorsiflexion", "sets": 3, "reps": 20,
             "notes": "Strengthen tibialis anterior"},
            {"name": "Foot intrinsic strengthening (towel scrunches)", "sets": 3, "reps": 15,
             "notes": "Especially important for flat-footed runners"},
            {"name": "Single-leg stance with resistance band", "sets": 3, "duration": "30-45s",
             "notes": "Band around knees, maintain alignment"}
        ],
        "surface_recommendation": "treadmill or grass",
        "typical_recovery_weeks": {"mild": 1, "moderate": 2, "severe": 4}
    },

    "knee": {
        "label": "Runner's Knee / Patellofemoral Pain",
        "avoid": [
            "Downhill running — increases patellofemoral compression",
            "Deep squats or lunges if painful",
            "Long descents on trails",
            "Excessive stair running"
        ],
        "modify": [
            "Reduce overall volume by 20-30%",
            "Avoid cambered roads — run on flat surfaces",
            "Shorten stride slightly to reduce braking forces",
            "Replace hill repeats with flat intervals"
        ],
        "prehab_exercises": [
            {"name": "Single-leg glute bridges", "sets": 3, "reps": 12,
             "notes": "Strengthen glute medius to improve knee tracking"},
            {"name": "Clamshells with band", "sets": 3, "reps": 15,
             "notes": "External rotation strength"},
            {"name": "Wall sits (pain-free range)", "sets": 3, "duration": "30s",
             "notes": "Quad isometric — knee at ~45 degrees"},
            {"name": "Step-downs (eccentric quad)", "sets": 3, "reps": 10,
             "notes": "Slow and controlled on a 6-inch step"}
        ],
        "surface_recommendation": "flat, even surfaces",
        "typical_recovery_weeks": {"mild": 1, "moderate": 3, "severe": 6}
    },

    "it_band": {
        "label": "IT Band Syndrome",
        "avoid": [
            "Downhill running — primary aggravator",
            "Running on banked surfaces or always in one direction on a track",
            "Track running (repetitive turns)",
            "Long tempo runs — sustained pace aggravates friction"
        ],
        "modify": [
            "Reduce long run distance by 20%",
            "Replace tempo runs with easy runs until resolved",
            "Alternate running direction on out-and-back routes",
            "Shorten runs and add more frequency if volume needs maintaining"
        ],
        "prehab_exercises": [
            {"name": "Lateral band walks", "sets": 3, "reps": 15,
             "notes": "Keep tension on the band throughout"},
            {"name": "Single-leg deadlift", "sets": 3, "reps": 10,
             "notes": "Hip stability and posterior chain"},
            {"name": "Side-lying hip abduction", "sets": 3, "reps": 15,
             "notes": "Strengthen glute medius"},
            {"name": "Foam rolling (lateral thigh)", "duration": "2 min per side",
             "notes": "Reduce tension, not a substitute for strengthening"}
        ],
        "surface_recommendation": "flat, non-banked surfaces",
        "typical_recovery_weeks": {"mild": 1, "moderate": 3, "severe": 6}
    },

    "foot_heel": {
        "label": "Plantar Fasciitis",
        "avoid": [
            "Barefoot drills or minimalist shoes",
            "Speed work on hard surfaces",
            "Hill running — stretches plantar fascia under load",
            "Walking barefoot at home (wear supportive sandals)"
        ],
        "modify": [
            "Shorten all runs by 15-20%",
            "Replace morning runs with afternoon/evening (fascia is tightest in AM)",
            "Add 5-minute calf/arch stretch before EVERY run",
            "Replace one run per week with pool running"
        ],
        "prehab_exercises": [
            {"name": "Calf stretches (straight + bent knee)", "sets": 3, "duration": "30s each",
             "notes": "Tight calves are the #1 contributor"},
            {"name": "Towel/marble pickups", "sets": 3, "reps": 15,
             "notes": "Strengthen foot intrinsic muscles"},
            {"name": "Frozen water bottle roll under arch", "duration": "5 min",
             "notes": "Combines massage with ice — do daily"},
            {"name": "Eccentric calf raises off step", "sets": 3, "reps": 15,
             "notes": "Slow lowering to below-step level"}
        ],
        "surface_recommendation": "treadmill or soft trail",
        "typical_recovery_weeks": {"mild": 2, "moderate": 4, "severe": 8}
    },

    "achilles": {
        "label": "Achilles Tendinopathy",
        "avoid": [
            "Hill running (uphill loads the Achilles significantly)",
            "Speed work — high eccentric forces",
            "Sudden increases in run duration",
            "Worn-out shoes with poor heel cushioning"
        ],
        "modify": [
            "Flat routes only until pain-free",
            "Reduce run volume by 30%",
            "Add heel lifts to running shoes if needed",
            "Convert intervals to easy runs"
        ],
        "prehab_exercises": [
            {"name": "Alfredson protocol (eccentric heel drops)", "sets": 3, "reps": 15,
             "notes": "Gold standard for Achilles rehab — straight + bent knee versions"},
            {"name": "Isometric calf holds", "sets": 5, "duration": "45s",
             "notes": "Pain-relieving effect — do before running"},
            {"name": "Single-leg calf raises", "sets": 3, "reps": 12,
             "notes": "Progress to weighted when pain-free"},
            {"name": "Soleus-focused calf stretch (bent knee)", "sets": 3, "duration": "30s",
             "notes": "Often neglected — the soleus drives Achilles load"}
        ],
        "surface_recommendation": "flat treadmill",
        "typical_recovery_weeks": {"mild": 2, "moderate": 6, "severe": 12}
    },

    "hamstring": {
        "label": "Hamstring Strain",
        "avoid": [
            "Sprints and speed work",
            "Aggressive hill running",
            "Overstretching (no ballistic stretching)",
            "Running with long strides"
        ],
        "modify": [
            "Shorten stride by 5-10%",
            "Reduce pace on all runs",
            "Replace intervals with easy runs",
            "Add extra warm-up time (10+ min)"
        ],
        "prehab_exercises": [
            {"name": "Nordic hamstring curls", "sets": 3, "reps": 6,
             "notes": "The gold standard for hamstring injury prevention"},
            {"name": "Single-leg Romanian deadlift", "sets": 3, "reps": 10,
             "notes": "Hip-hinge pattern with hamstring eccentric load"},
            {"name": "Glute bridges (double then single-leg)", "sets": 3, "reps": 12,
             "notes": "Engage glutes to reduce hamstring overloading"},
            {"name": "Hamstring slides (supine)", "sets": 3, "reps": 10,
             "notes": "Eccentric lengthening in a controlled position"}
        ],
        "surface_recommendation": "flat, even surface",
        "typical_recovery_weeks": {"mild": 1, "moderate": 3, "severe": 6}
    },

    "hip": {
        "label": "Hip Pain (Bursitis / Flexor Strain / Labral)",
        "avoid": [
            "Track running (repetitive curves load hip asymmetrically)",
            "Deep lunges or excessive hip flexion",
            "Running with crossover gait"
        ],
        "modify": [
            "Reduce overall mileage by 20%",
            "Flat surfaces only — avoid camber",
            "Add hip-focused dynamic warm-up (5+ min)",
            "Shorten stride — overstriding loads hip flexors"
        ],
        "prehab_exercises": [
            {"name": "Hip flexor stretch (half-kneeling)", "sets": 3, "duration": "30s",
             "notes": "Critical for desk workers who run"},
            {"name": "Side-lying clamshells", "sets": 3, "reps": 15,
             "notes": "Glute med activation"},
            {"name": "Monster walks with band", "sets": 3, "reps": 10,
             "notes": "Forward and backward"},
            {"name": "Pigeon stretch", "sets": 3, "duration": "30s",
             "notes": "Hip external rotation and glute release"}
        ],
        "surface_recommendation": "flat, non-cambered surfaces",
        "typical_recovery_weeks": {"mild": 1, "moderate": 3, "severe": 8}
    },

    "calf": {
        "label": "Calf Strain / Tightness",
        "avoid": [
            "Hill repeats — high eccentric calf load",
            "Speed work",
            "Running in minimalist shoes"
        ],
        "modify": [
            "Reduce pace on all runs for 7 days",
            "Flat routes only",
            "Switch to shoes with slight heel-toe drop",
            "Add walking breaks to longer runs"
        ],
        "prehab_exercises": [
            {"name": "Eccentric calf raises off step", "sets": 3, "reps": 15,
             "notes": "Slow 3-second lowering phase"},
            {"name": "Gastrocnemius stretch (straight leg)", "sets": 3, "duration": "30s",
             "notes": "Hold, don't bounce"},
            {"name": "Soleus stretch (bent knee)", "sets": 3, "duration": "30s",
             "notes": "Often the neglected muscle"},
            {"name": "Foam rolling calves", "duration": "2 min per side",
             "notes": "Roll slowly, pause on tender spots"}
        ],
        "surface_recommendation": "flat treadmill or track",
        "typical_recovery_weeks": {"mild": 1, "moderate": 2, "severe": 4}
    },

    "lower_back": {
        "label": "Lower Back Pain",
        "avoid": [
            "Long runs (prolonged spinal loading)",
            "Running on hard surfaces",
            "Carrying anything while running (hydration vest if heavy)"
        ],
        "modify": [
            "Cap run duration at 30-40 min until resolved",
            "Add core stability work before every run",
            "Replace one run with swimming or aqua jogging",
            "Reduce speed — fast running increases spinal compression"
        ],
        "prehab_exercises": [
            {"name": "Bird-dog", "sets": 3, "reps": 10,
             "notes": "Core anti-extension and stability"},
            {"name": "Dead bug", "sets": 3, "reps": 10,
             "notes": "Deep core activation"},
            {"name": "Cat-cow mobility", "sets": 2, "reps": 10,
             "notes": "Spinal mobility — do before every run"},
            {"name": "Plank (front + side)", "sets": 3, "duration": "30s each",
             "notes": "Core endurance for running posture"}
        ],
        "surface_recommendation": "treadmill (shock absorption)",
        "typical_recovery_weeks": {"mild": 1, "moderate": 3, "severe": 6}
    }
}
```

### 11.9 Chronic vs Acute Injury Handling

The plan generator distinguishes between acute injuries (sudden onset —
twisted ankle, pulled muscle) and chronic/overuse injuries (gradual onset —
plantar fasciitis, IT band, shin splints). The management approach differs.

```python
INJURY_TYPE_CLASSIFICATION = {
    "acute": {
        "description": (
            "Sudden onset during or immediately after activity. "
            "Examples: ankle sprain, muscle pull/tear, fall impact."
        ),
        "identification_clues": [
            "User reports sudden onset during a specific run",
            "Pain level jumps from 0 to 5+ immediately",
            "Specific incident described (rolled ankle, felt pop, etc.)"
        ],
        "initial_protocol": {
            "phase_1_protect": {
                "duration_days": "2-3",
                "action": "Complete rest from running. PEACE protocol.",
                "PEACE": (
                    "Protection, Elevation, Avoid anti-inflammatories (initially), "
                    "Compression, Education. Modern replacement for RICE that "
                    "avoids suppressing the healing inflammation response."
                )
            },
            "phase_2_load": {
                "duration_days": "3-7",
                "action": "Begin LOVE protocol — gentle pain-free movement.",
                "LOVE": (
                    "Load (gradually), Optimism (positive mindset aids healing), "
                    "Vascularisation (pain-free cardio), Exercise (active recovery)."
                )
            },
            "phase_3_return": "Applies Section 11.3 return-to-running protocol"
        },
        "medical_urgency": "Recommend immediate assessment for moderate/severe"
    },

    "chronic_overuse": {
        "description": (
            "Gradual onset over days or weeks. Pain starts mild and worsens "
            "with continued training. Examples: plantar fasciitis, shin splints, "
            "IT band syndrome, Achilles tendinopathy, runner's knee."
        ),
        "identification_clues": [
            "User reports pain that has been building over multiple runs",
            "Pain is worse at start of run, may improve mid-run, returns after",
            "Pain started as 1-2/10 and has gradually increased",
            "Related to specific mileage threshold (e.g., 'hurts after 5 km')"
        ],
        "initial_protocol": {
            "phase_1_deload": {
                "duration": "1-2 weeks",
                "action": (
                    "Reduce volume to the level where symptoms are ≤ 2/10. "
                    "This is the 'symptom threshold' — the app finds it by "
                    "progressively reducing volume until pain stabilizes."
                )
            },
            "phase_2_maintain_and_strengthen": {
                "duration": "2-4 weeks",
                "action": (
                    "Hold at reduced volume. Add body-part-specific rehab "
                    "exercises (Section 11.8). Monitor pain trend (Section 11.6)."
                )
            },
            "phase_3_progressive_reload": {
                "duration": "2-4 weeks",
                "action": (
                    "Increase volume by 5% per week (not 10%) while monitoring "
                    "pain. If pain returns above 3/10, drop back to phase 2."
                )
            }
        },
        "medical_urgency": "Recommend if not improving after 2 weeks of modification"
    }
}

def classify_injury_type(injury_log, recent_check_ins):
    """
    Help classify an injury as acute or chronic based on user input.
    This affects which protocol the plan generator applies.
    """
    # Ask during injury logging
    onset_question = {
        "question": "How did this start?",
        "options": [
            {"label": "Sudden — during a specific run or activity",
             "value": "acute"},
            {"label": "Gradual — built up over several days/weeks",
             "value": "chronic_overuse"},
            {"label": "Not sure",
             "value": "unknown"}
        ]
    }

    # For "unknown", use heuristics
    if injury_log.onset == "unknown":
        # Check if there were recent pain check-ins trending up
        if recent_check_ins and len(recent_check_ins) >= 3:
            if is_gradually_increasing(recent_check_ins):
                return "chronic_overuse"
        # Default to acute protocol (more conservative initially)
        return "acute"

    return injury_log.onset
```

### 11.10 Race-Day Decision with Active Injury

When race day approaches and the user has an active or recently-resolved
injury, the app should provide guidance — not just ignore it.

```python
def evaluate_race_day_readiness(plan, active_injuries, resolved_injuries, race_date):
    """
    Assess whether the user should race, modify race strategy,
    or consider DNS (Did Not Start).

    The app NEVER tells the user they can't race — it provides
    information and lets them decide. But it should be honest.
    """
    days_to_race = (race_date - today()).days
    readiness = {"status": "green", "warnings": [], "suggestions": []}

    # Check active injuries
    for injury in active_injuries:
        if injury.severity == "severe":
            readiness["status"] = "red"
            readiness["warnings"].append(
                f"You have an active severe {format_body_part(injury.body_part)} injury. "
                f"Running {plan.race_distance} with a severe injury risks significant "
                f"damage and extended recovery. Consider deferring to a future race."
            )

        elif injury.severity == "moderate":
            readiness["status"] = "yellow" if readiness["status"] != "red" else "red"
            readiness["warnings"].append(
                f"Your {format_body_part(injury.body_part)} is still moderate. "
                f"If you choose to race, consider:"
            )
            readiness["suggestions"].extend([
                "Start conservatively — 15-30 seconds slower per mile than planned",
                "Use a run/walk strategy to manage load",
                "Set a 'bail-out' point — if pain exceeds 5/10 at any point, walk/stop",
                "Have someone available for pickup if needed"
            ])

        elif injury.severity == "mild":
            if readiness["status"] == "green":
                readiness["status"] = "yellow"
            readiness["warnings"].append(
                f"Mild {format_body_part(injury.body_part)} discomfort noted. "
                f"Monitor during race — if it worsens, follow Rule 1 (stop if pain increases)."
            )

    # Check recently resolved injuries
    for injury in resolved_injuries:
        days_since_resolved = (today() - injury.resolved_at).days
        if days_since_resolved < 14:
            readiness["warnings"].append(
                f"Your {format_body_part(injury.body_part)} was resolved only "
                f"{days_since_resolved} days ago. The tissue may not be fully healed. "
                f"Race with caution and consider a conservative pace."
            )

    # Check if enough training was completed
    if plan.completion_percentage < 0.70:
        readiness["warnings"].append(
            f"You've completed {int(plan.completion_percentage * 100)}% of your plan. "
            f"With significant missed training, consider adjusting your race goal "
            f"from a time target to a 'finish and feel good' approach."
        )

    return readiness
```

### 11.11 Medical Clearance Tracking

For severe injuries, the app should track whether the user has received
medical clearance before allowing the return-to-running protocol.

```python
MEDICAL_CLEARANCE = {
    "required_for": ["severe"],       # only required for severe injuries
    "recommended_for": ["moderate"],  # suggested but not enforced

    "clearance_flow": {
        "step_1": "User marks injury as 'ready to return'",
        "step_2_severe": (
            "App asks: 'Have you been cleared by a medical professional to resume running?'"
        ),
        "step_2_options": [
            {
                "label": "Yes, I've been cleared",
                "action": "proceed_with_return_protocol",
                "follow_up": "Great! We'll start you back gradually."
            },
            {
                "label": "Not yet, but I feel ready",
                "action": "allow_with_warning",
                "follow_up": (
                    "We recommend getting cleared before resuming, especially after "
                    "a severe injury. If you choose to proceed, we'll start very "
                    "conservatively. Stop immediately if pain returns."
                ),
                "extra_caution": True  # even slower return protocol
            },
            {
                "label": "I have a specific restriction",
                "action": "enter_restriction",
                "follow_up": (
                    "Tell us what your provider said (e.g., 'no running over 30 min', "
                    "'no hills for 4 weeks', 'run/walk only'). We'll adjust your plan."
                )
            }
        ]
    },

    "restriction_types": [
        {"type": "time_cap", "example": "No runs over 30 minutes"},
        {"type": "surface_restriction", "example": "Treadmill only for 2 weeks"},
        {"type": "intensity_cap", "example": "Easy runs only, no speedwork"},
        {"type": "frequency_cap", "example": "No more than 3 runs per week"},
        {"type": "distance_cap", "example": "No runs over 5 km"},
        {"type": "terrain_restriction", "example": "No hills"},
        {"type": "run_walk_only", "example": "Run/walk intervals only"},
        {"type": "custom", "example": "Free text from user"}
    ]
}
```

### 11.12 Proactive Injury Risk Scoring

Rather than only reacting to injuries, the app should proactively flag
when a runner is at elevated risk. Research shows ACWR (acute:chronic
workload ratio) above 1.5 is a strong injury predictor.

```python
def calculate_injury_risk_score(user_id, plan):
    """
    Generate a 0-100 injury risk score based on multiple factors.
    Score > 60 triggers a warning; > 80 triggers plan modification.
    """
    factors = {}

    # 1. Acute:Chronic Workload Ratio (ACWR)
    acute_load = get_training_load(user_id, last_n_days=7)
    chronic_load = get_training_load(user_id, last_n_days=28) / 4  # weekly avg
    acwr = acute_load / chronic_load if chronic_load > 0 else 1.0
    if acwr > 1.5:
        factors["acwr"] = {"score": 30, "detail": f"ACWR is {acwr:.2f} — injury danger zone"}
    elif acwr > 1.3:
        factors["acwr"] = {"score": 15, "detail": f"ACWR is {acwr:.2f} — elevated"}
    else:
        factors["acwr"] = {"score": 0, "detail": f"ACWR is {acwr:.2f} — safe zone"}

    # 2. Recent mileage spike
    this_week = get_weekly_mileage(user_id, week=0)
    last_week = get_weekly_mileage(user_id, week=-1)
    if last_week > 0:
        spike = (this_week - last_week) / last_week
        if spike > 0.20:
            factors["mileage_spike"] = {"score": 20, "detail": f"{int(spike*100)}% increase this week"}
        elif spike > 0.10:
            factors["mileage_spike"] = {"score": 10, "detail": f"{int(spike*100)}% increase"}
        else:
            factors["mileage_spike"] = {"score": 0}

    # 3. Injury history
    recent_injuries = get_injury_history(user_id, months=6)
    if len(recent_injuries) >= 3:
        factors["injury_history"] = {"score": 20, "detail": f"{len(recent_injuries)} injuries in 6 months"}
    elif len(recent_injuries) >= 1:
        factors["injury_history"] = {"score": 10}
    else:
        factors["injury_history"] = {"score": 0}

    # 4. Subjective readiness (from check-in if available)
    readiness = get_latest_readiness_check(user_id)
    if readiness and readiness.fatigue_score >= 8:
        factors["fatigue"] = {"score": 15, "detail": "High fatigue reported"}
    elif readiness and readiness.fatigue_score >= 6:
        factors["fatigue"] = {"score": 8}
    else:
        factors["fatigue"] = {"score": 0}

    # 5. Sleep deficit (if available)
    if readiness and readiness.sleep_hours < 6:
        factors["sleep"] = {"score": 10, "detail": f"Only {readiness.sleep_hours}h sleep"}
    else:
        factors["sleep"] = {"score": 0}

    # 6. Missed deload
    weeks_since_deload = get_weeks_since_last_deload(plan)
    expected_deload_freq = plan.config.deload_frequency_weeks
    if weeks_since_deload > expected_deload_freq + 1:
        factors["overdue_deload"] = {"score": 10, "detail": f"{weeks_since_deload} weeks since last deload"}
    else:
        factors["overdue_deload"] = {"score": 0}

    total_score = sum(f["score"] for f in factors.values())
    total_score = min(total_score, 100)

    result = {
        "risk_score": total_score,
        "risk_level": "low" if total_score < 40 else "moderate" if total_score < 60 else "high" if total_score < 80 else "critical",
        "factors": factors
    }

    # Auto-actions for high risk
    if total_score >= 80:
        result["auto_action"] = {
            "action": "reduce_next_2_days",
            "message": (
                "Your injury risk is elevated. We've reduced the next 2 days "
                "to easy effort to help your body recover."
            )
        }
    elif total_score >= 60:
        result["warning"] = (
            "Your training load is high relative to recent history. "
            "Consider taking it easy today or adding an extra rest day."
        )

    return result
```

### 11.13 Expanded Injury API Endpoints

```
POST   /api/plans/:id/injuries                    — Report a new injury
PUT    /api/plans/:id/injuries/:iid               — Update injury (severity, resolved)
GET    /api/plans/:id/injuries                    — List injuries for current plan
GET    /api/users/:id/injury-history              — Full history across all plans
POST   /api/plans/:id/injuries/:iid/resolve       — Mark resolved, trigger return protocol
POST   /api/plans/:id/injuries/:iid/pain-checkin  — Log a pain check-in
GET    /api/plans/:id/injuries/:iid/pain-trend    — Get pain trend analysis
GET    /api/plans/:id/injuries/:iid/exercises      — Get body-part-specific rehab exercises
POST   /api/plans/:id/injuries/:iid/clearance     — Record medical clearance
GET    /api/plans/:id/injuries/:iid/clearance     — Check clearance status
GET    /api/plans/:id/injury-risk                 — Get current injury risk score
GET    /api/users/:id/injury-risk/history         — Risk score over time
GET    /api/plans/:id/race-readiness              — Race-day readiness with active injuries
PUT    /api/plans/:id/injuries/:iid/restriction   — Add medical restriction to injury
```

---

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

---

## 13. Authentication

### 12.1 Passwordless Sign-On

The app uses passwordless authentication via one-time codes (OTP). No passwords are stored or managed.

#### Supported Methods
- **Email OTP**: User enters email address, receives a 6-digit code, enters it in the app.
- **Phone SMS OTP**: User enters phone number, receives a 6-digit code via SMS, enters it in the app.

User chooses their preferred method at first sign-in. Either method can be used on subsequent sign-ins.

#### Auth Flow

```
1. User enters email or phone number
2. Server generates a 6-digit OTP
3. OTP is sent via email (SendGrid, SES, etc.) or SMS (Twilio, SNS, etc.)
4. OTP expires after 10 minutes
5. User enters OTP in app
6. Server validates OTP, issues a JWT access token + refresh token
7. Access token expires after 1 hour; refresh token after 30 days
8. On token refresh, issue a new access/refresh pair (rotation)
9. If refresh token is expired, user must re-authenticate via OTP
```

#### Rate Limiting & Security

```
RULES:
1. Max 5 OTP requests per email/phone per hour
2. Max 5 failed OTP attempts per session — then lock for 15 minutes
3. OTP is single-use — invalidated after first successful or failed validation
4. OTP codes are hashed before storage (never stored in plaintext)
5. All auth endpoints require HTTPS
6. Log all auth events (request, success, failure, lockout) for audit
7. Implement device fingerprinting to flag suspicious sign-in patterns
```

#### Data Model Addition

```typescript
interface AuthSession {
  id: string;
  user_id: string;
  method: "email" | "phone";
  identifier: string;  // email address or phone number
  otp_hash: string;
  otp_expires_at: Date;
  attempts: number;
  locked_until: Date | null;
  created_at: Date;
}

interface RefreshToken {
  id: string;
  user_id: string;
  token_hash: string;
  device_fingerprint: string;
  expires_at: Date;
  created_at: Date;
  revoked: boolean;
}
```

#### API Endpoints

```
POST   /api/auth/request-otp         — Send OTP to email or phone
POST   /api/auth/verify-otp          — Validate OTP, return tokens
POST   /api/auth/refresh             — Refresh access token
POST   /api/auth/logout              — Revoke refresh token
GET    /api/auth/sessions            — List active sessions (for user to manage devices)
DELETE /api/auth/sessions/:id        — Revoke a specific session
```

### 12.2 Account Linking

A single user account can have both an email and phone number linked. Either can be used to sign in. The first method used at registration becomes the primary identifier; the second can be added in settings.

```
RULES:
1. Adding a second auth method requires OTP verification on the new method
2. Users must always have at least one verified auth method
3. Removing the last auth method is blocked
4. If a phone number or email is already linked to another account,
   the linking request is rejected with a clear error
```

---

## 14. Monetization & Plan Purchase Model

### 14.1 Core Principle: Almost Everything is Free

The app is free for the vast majority of features. The goal is to build a
large, engaged user base where runners get genuine value before ever seeing
a paywall. Only ONE feature is gated behind payment.

### 14.2 The Single Paid Feature: Holiday & Vacation Handling

```
FREE (everything else):
- All distances (5K, 10K, half marathon, marathon)
- All skill levels (beginner, intermediate, advanced)
- Full plan generation with periodization
- VDOT and RPE pacing modes
- Workout logging and tracking
- Schedule setup (available days, time budgets, transition times)
- Missed-day recovery algorithm
- Injury logging and plan adjustment (all features)
- Pain tracking, rehab exercises, return-to-run protocols
- Run/walk method support
- Streak tracking and milestone celebrations
- Readiness check-ins (subjective)
- Public profile and plan sharing
- Race time predictor
- Age-adjusted training (masters runners)
- Polarized training validation
- Warm-up and cool-down protocols
- Beginner confidence features
- Experience-level assessment
- Workout swap rules
- Custom long run cycle patterns
- Nutrition and mental preparation guidance
- All 22 workout types
- Volume modes (minimal through elite/doubles)
- Mid-plan schedule changes
- Notifications and coaching messages

PAID:
- Holiday and vacation handling (Section 6.2 and 6.4)
  This includes:
  - Automatic holiday detection and workout rescheduling
  - Preplanned vacation entry with calendar blocking
  - "Redistribute my miles" strategy (pre/post vacation volume balancing)
  - "Ease me back in" strategy (buffer weeks)
  - Cross-training suggestions during vacation
  - Vacation preview (before/after plan comparison)

WHY THIS FEATURE:
- Holidays and vacations are the #1 disruption to training plans
- It's a natural "upgrade moment" — user is planning a trip, realizes
  their plan will be disrupted, and sees the value in smart handling
- It doesn't punish new users or gate core training functionality
- It's complex enough to justify payment (two strategies, safety
  constraints, volume redistribution, preview system)
- Users who are serious enough to plan around vacations are more
  likely to pay
```

### 14.3 Enforcement: One Active Plan at a Time

The app enforces one active plan at a time (Section 1.1). This isn't a paid
gate — it's a product design choice. To start a new plan, users must archive
or complete their current one. This keeps the user focused and prevents plan
confusion.

### 14.4 Purchase Model

```
OPTIONS:

A. One-time unlock:
   - Single purchase unlocks holiday/vacation handling permanently
   - Good for: simplicity, no subscription fatigue
   - User pays once and gets the feature for life

B. Subscription (optional alternative):
   - Monthly or annual subscription
   - Unlocks holiday/vacation handling
   - Leaves room for future premium features (e.g., wearable integration,
     climate adjustments) without re-architecting the paywall
   - Good for: recurring revenue if future features are added

RECOMMENDATION: Start with one-time unlock (A) to minimize friction.
Transition to subscription (B) only if additional premium features
are added later.
```

### 14.5 Free-to-Paid Conversion Points

```python
CONVERSION_TRIGGERS = {
    "natural_upgrade_moments": [
        {
            "trigger": "User adds a holiday to their calendar",
            "message": (
                "Want your plan to automatically adjust around "
                "this holiday? Unlock smart scheduling."
            )
        },
        {
            "trigger": "User enters blocked dates (vacation)",
            "message": (
                "We can redistribute your training around your trip "
                "or ease you back in after. Unlock vacation handling."
            )
        },
        {
            "trigger": "User misses a workout on a known holiday",
            "message": (
                "Missed a workout on a holiday? With smart scheduling, "
                "the app handles this automatically."
            )
        }
    ],
    "never_do": [
        "Never block access to core training plan features",
        "Never nag more than once per trigger event",
        "Never show upgrade prompts during a workout",
        "Never gate injury or safety features behind payment"
    ]
}
```

### 14.6 Data Model

```typescript
interface UserPurchase {
  id: string;
  user_id: string;
  feature: "holiday_vacation_handling";  // only one paid feature for now
  purchase_type: "one_time" | "subscription";
  status: "active" | "expired" | "refunded";
  purchased_at: Date;
  expires_at: Date | null;              // null for one-time purchases
  payment_provider: "stripe" | "apple_iap" | "google_play";
  payment_provider_id: string;
  receipt_id: string;
  amount_cents: number;
  currency: string;
}

// Feature gate check
function hasFeatureAccess(user_id: string, feature: string): boolean {
  // For holiday/vacation handling, check purchase status
  // For everything else, always return true
  if (feature !== "holiday_vacation_handling") return true;
  const purchase = getActivePurchase(user_id, feature);
  return purchase !== null && purchase.status === "active";
}
```

---

## 15. Units & Localization

### 14.1 Unit System

```
RULES:
1. User selects preferred unit system during onboarding: metric (km) or imperial (miles)
2. All internal calculations use metric (km, meters)
3. Display layer converts to user's preferred unit
4. Conversion: 1 mile = 1.60934 km
5. Pace display:
   - Metric: min/km (e.g., "5:30/km")
   - Imperial: min/mile (e.g., "8:51/mi")
6. All API responses include both units; client displays the user's preference
```

### 14.2 Timezone Handling

```
RULES:
1. All dates stored in UTC internally
2. User's timezone captured at registration (from device or manual selection)
3. Workout scheduling and notifications use local time
4. Race date is stored as a local date (no timezone conversion — the race
   happens at a specific place)
5. Weekly boundaries (for mileage totals) follow Monday-Sunday in user's
   local timezone
```

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

## 17. References

1. Mayo Clinic. "5K run: 7-week training schedule for beginners." [mayoclinic.org](https://www.mayoclinic.org/healthy-lifestyle/fitness/in-depth/5k-run/art-20050962)
2. Hal Higdon. "Marathon Training for All Skill Levels." [halhigdon.com](https://www.halhigdon.com/training/marathon-training/)
3. Boston Athletic Association. "Boston Marathon Training." [baa.org](https://www.baa.org/races/boston-marathon/info-for-athletes/boston-marathon-training/)
4. Bosquet, L. et al. (2007). "Effects of tapering on performance: a meta-analysis." *Medicine & Science in Sports & Exercise.* Referenced via [Runners Connect](https://runnersconnect.net/weekly-mileage-progression-10-percent-rule/)
5. Aarhus University (2012). Study of 60 novice runners and weekly training volume increases. Referenced via [Canadian Running Magazine](https://runningmagazine.ca/sections/training/the-10-per-cent-mileage-rule-isnt-what-you-think-study-warns/)
6. BJSM (2025). "How much running is too much? Identifying high-risk running sessions in a 5200-person cohort study." [PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC12421110/)
7. Daniels, J. "Daniels' Running Formula." Referenced via [Luke Humphrey Running](https://lukehumphreyrunning.com/increasing-mileage-the-10-rule/)
8. Scientific Triathlon. "Modern marathon training principles with John Davis." [scientifictriathlon.com](https://scientifictriathlon.com/tts472/)
9. Running Writings. "Building a percentage-based 10K training plan." [runningwritings.com](https://runningwritings.com/2024/10/percentage-based-10k-training.html)
10. Jason Koop / CTS. "What to do if you missed training." [trainright.com](https://trainright.com/what-to-do-if-you-missed-training/)
11. Luke Humphrey Running. "FAQ: Marathon training and missed runs." [lukehumphreyrunning.com](https://lukehumphreyrunning.com/faq-marathon-training-and-missed-runs/)
12. Jeff Galloway. "Missed a Run? Here's How to Get Back on Track." [jeffgalloway.com](https://www.jeffgalloway.com/05/missed-a-run-heres-how-to-get-back-on-track-without-guilt-or-burnout/)
13. Marathon Handbook. "The 10% Rule: Is It A Valid Way To Increase Weekly Mileage?" [marathonhandbook.com](https://marathonhandbook.com/the-10-percent-rule/)
14. Outside Online. "The Myth of the 10 Percent Rule." [outsideonline.com](https://run.outsideonline.com/training/getting-started/myth-of-the-10-percent-rule/)
15. Run to the Finish. "The 10% Rule Is Wrong: How to Actually Build Running Distance." [runtothefinish.com](https://runtothefinish.com/increase-running-mileage/)
16. PMC. "Integrating Deloading into Strength and Physique Sports Training Programmes." [PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC10511399/)
17. Kath-letics. "Tapering for Running." [kath-letics.com](https://www.kath-letics.com/blog/tapering-for-running)
18. MyProCoach. "Free Half Marathon & Marathon Training Plans." [myprocoach.net](https://www.myprocoach.net/free-training-plans/half-marathon/)
19. Healthline. "20-Week Marathon Training Plan: Charts for All Levels." [healthline.com](https://www.healthline.com/health/exercise-fitness/20-week-marathon-training-plan)
20. Knighton Runs. "How to reschedule a workout or long run." [knightonruns.com](https://knightonruns.com/rescheduling-a-workout-or-long-run/)
