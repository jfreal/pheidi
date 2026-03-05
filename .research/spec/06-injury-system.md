# Running Training Plan App — Injury Logging & Plan Adjustment

> The injury system is DORMANT by default — completely invisible until a user reports
> an injury. This module covers pain tracking, run/stop decision trees, body-part-specific
> modifications, chronic vs acute handling, race-day decisions, medical clearance,
> and proactive injury risk scoring.
> See `00-index.md` for the full file map.

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
