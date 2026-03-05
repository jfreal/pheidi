# Running Training Plan App — Calendar View & UI

> The calendar is the heart of the app. This module covers all 4 calendar views,
> color coding, smart features, external calendar sync, home screen widgets,
> print/PDF export, cross-training scheduling, calendar sharing, and marketing data.
> See `00-index.md` for the full file map.

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

## 9B. Calendar View — The Core UI

The calendar is the heart of the app. It's where runners interact with
their plan daily. The calendar must be beautiful, informative, and adapt
to every schedule configuration (standard weeks, non-standard cycles,
shift patterns). It must show life happening alongside training — holidays,
vacations, reduced weeks, injuries — and make it clear the plan bends
around life, not the other way around.

### 9B.1 Calendar Views

```python
CALENDAR_VIEWS = {
    "daily": {
        "description": (
            "Single-day detail view. Shows the full workout for today "
            "with time breakdown, warm-up/cool-down, and any notes."
        ),
        "displays": [
            "Workout type and description",
            "Full time breakdown (Section 6.6.1c): transition + warm-up + core + cool-down",
            "Target pace / effort / HR zone",
            "Weather-adjusted pace if applicable (Section 12.3)",
            "Injury check-in prompt if active injury",
            "Start workout button",
            "Quick actions: skip, swap day, mark complete manually"
        ],
        "swipe_gesture": "Swipe left/right to navigate between days"
    },

    "weekly": {
        "description": (
            "7-day view (or N-day cycle view for non-standard schedules). "
            "The primary planning view. At a glance, the runner sees "
            "their entire week of training."
        ),
        "displays": {
            "each_day_card": [
                "Day name and date",
                "Workout type (color-coded by category)",
                "Planned distance or duration",
                "Time of day (morning/lunch/evening icon)",
                "Status: upcoming / completed / skipped / rescheduled",
                "Transition time indicator (if shower needed)"
            ],
            "week_summary_bar": [
                "Total planned mileage vs completed mileage",
                "Runs completed / runs planned",
                "Training phase label (Base / Build / Peak / Taper / Recovery)",
                "Week number in plan (e.g., 'Week 8 of 16')",
                "Intensity distribution mini-chart (easy/hard ratio)"
            ],
            "weekly_note": (
                "Space for the plan generator to display a weekly message: "
                "'This is your peak mileage week — you're at your strongest.' "
                "or 'Deload week — trust the recovery.'"
            )
        },
        "interactions": [
            "Tap any day to open daily view",
            "Long-press to drag-and-drop swap workouts between days",
            "Swipe left/right to navigate between weeks/cycles",
            "Pull down to see week-over-week comparison"
        ],
        "non_standard_cycles": (
            "For non-7-day cycles (Section 12.7a), the weekly view shows "
            "the full cycle length (e.g., 9 or 10 days). The header says "
            "'Cycle 5' instead of 'Week 5'. Everything else works the same."
        )
    },

    "monthly": {
        "description": (
            "Full month calendar view. Shows the big picture — training "
            "phases, upcoming races, holidays, vacations, and overall "
            "progression at a glance."
        ),
        "displays": {
            "each_day_cell": [
                "Small color dot indicating workout type",
                "Completion status (filled dot = done, empty = upcoming, X = skipped)",
                "Holiday/vacation/blocked date indicator",
                "Injury flag if active on that day"
            ],
            "month_overlays": [
                "Training phase bands (color bars spanning the phase duration)",
                "Race day marker (prominent, unmissable)",
                "Vacation blocks (shaded date ranges)",
                "Holiday markers",
                "Deload weeks (subtle background highlight)",
                "Current day indicator"
            ],
            "month_summary": [
                "Total mileage for the month",
                "Completion rate (runs completed / runs planned)",
                "Longest run of the month",
                "Phase progression (e.g., 'Build → Peak transition this month')"
            ]
        },
        "interactions": [
            "Tap any day to jump to daily view",
            "Tap a phase band to see phase description",
            "Swipe left/right to navigate months",
            "Pinch to zoom between monthly and weekly view"
        ]
    },

    "plan_overview": {
        "description": (
            "Full plan timeline view — from start date to race day. "
            "Shows the entire journey in one scrollable view."
        ),
        "displays": [
            "Timeline of all phases with dates (Base → Build → Peak → Taper → Race → Recovery)",
            "Weekly mileage progression chart (line chart overlaid on timeline)",
            "Key milestones: longest run, peak week, taper start, race day",
            "Holidays and vacations marked",
            "Current position indicator ('You are here')",
            "Projected race time (if enough data)"
        ],
        "interactions": [
            "Tap any week to jump to weekly view",
            "Scroll horizontally through the full plan",
            "Toggle between mileage chart and intensity chart"
        ]
    }
}
```

### 9B.2 Color Coding System

```python
WORKOUT_COLOR_CODING = {
    "description": (
        "Consistent color coding across all calendar views so runners "
        "can instantly identify workout types at a glance."
    ),

    "categories": {
        "easy_aerobic": {
            "color": "green",
            "workouts": ["easy_run", "recovery_run", "long_run"],
            "meaning": "Low intensity — the foundation of your training"
        },
        "quality_hard": {
            "color": "orange",
            "workouts": ["tempo_run", "interval_800m", "interval_1600m",
                         "hill_repeats", "fartlek", "ladder_intervals",
                         "pyramid_intervals", "sprints"],
            "meaning": "Hard effort — these build speed and strength"
        },
        "cross_training": {
            "color": "blue",
            "workouts": ["cycling", "swimming", "elliptical", "rowing",
                         "aqua_jogging", "strength_training"],
            "meaning": "Non-running fitness — complementary training"
        },
        "recovery_flexibility": {
            "color": "purple",
            "workouts": ["yoga", "pilates", "walking", "stretching"],
            "meaning": "Active recovery — restoring your body"
        },
        "rest": {
            "color": "gray",
            "workouts": ["rest_day"],
            "meaning": "Full rest — your body adapts on rest days"
        },
        "race": {
            "color": "gold",
            "workouts": ["race_day", "tune_up_race"],
            "meaning": "Race day — everything has led to this"
        }
    },

    "status_indicators": {
        "completed": "Filled color dot or checkmark",
        "upcoming": "Outlined dot (empty)",
        "skipped": "X mark or strikethrough",
        "rescheduled": "Arrow icon indicating moved",
        "in_progress": "Pulsing dot animation",
        "modified_by_injury": "Color dot with small caution indicator"
    },

    "special_overlays": {
        "holiday": "Small flag icon on the date",
        "vacation": "Shaded background spanning date range",
        "deload_week": "Subtle lighter background for the entire week",
        "peak_week": "Subtle border highlight — 'this is your biggest week'",
        "taper_lock": "Lock icon on taper/recovery weeks (Section 3)",
        "injury_active": "Small caution badge on affected days",
        "weather_alert": "Cloud/sun icon when pace is weather-adjusted"
    }
}
```

### 9B.3 Calendar Interactions & Smart Features

```python
CALENDAR_SMART_FEATURES = {
    "drag_and_drop_swap": {
        "description": (
            "In weekly view, user can long-press a workout and drag it "
            "to another day. The app applies swap rules (Section 6.5) "
            "in real-time: valid drop targets highlight green, invalid "
            "ones highlight red with a brief explanation of why."
        ),
        "validation": "Hard/easy spacing rules enforced visually",
        "undo": "Swaps can be undone within 10 seconds"
    },

    "today_widget": {
        "description": (
            "A persistent 'today' card that shows at the top of the app. "
            "At a glance: what's today's workout, how long, and when."
        ),
        "contents": [
            "Workout type and summary (e.g., 'Easy Run — 5 km, ~30 min')",
            "Time of day from schedule",
            "Weather snapshot if outdoor run",
            "Quick start button",
            "Quick skip button"
        ]
    },

    "upcoming_week_preview": {
        "description": (
            "At the end of each week, show a preview of next week's plan. "
            "Let the user review and adjust before the week starts."
        ),
        "timing": "Shown on the last day of the current week/cycle",
        "actions": [
            "Approve the week as-is",
            "Swap workouts between days",
            "Flag a reduced availability day",
            "Add a holiday or blocked date"
        ]
    },

    "phase_transition_banners": {
        "description": (
            "When the plan transitions between phases (e.g., Base → Build), "
            "show a banner in the calendar explaining what's changing and why."
        ),
        "example_messages": {
            "base_to_build": (
                "Starting your Build phase this week. You'll notice more "
                "quality sessions — your body is ready for them after building "
                "that aerobic base."
            ),
            "build_to_peak": (
                "Peak phase begins. These are your hardest weeks — but also "
                "the ones that make race day feel achievable."
            ),
            "peak_to_taper": (
                "Taper time! Volume drops but fitness doesn't. You might "
                "feel antsy — that's normal. Trust the science."
            ),
            "taper_to_race": (
                "Race week. You've done the work. Focus on rest, hydration, "
                "and logistics. The hay is in the barn."
            )
        }
    },

    "life_events_on_calendar": {
        "description": (
            "The calendar should show non-training events alongside workouts "
            "so the runner sees their training in the context of their life."
        ),
        "event_types": [
            "Holidays (auto-detected + user-added)",
            "Vacations (user-entered date ranges)",
            "Reduced availability weeks",
            "Night shifts (for shift workers)",
            "Tune-up races",
            "Goal race",
            "Injury active period (start → resolved)",
            "Recovery phase post-race"
        ],
        "import_support": (
            "Optional: import events from Google Calendar / Apple Calendar / "
            "Outlook to auto-identify potential scheduling conflicts."
        )
    },

    "plan_adjustment_history": {
        "description": (
            "Visual timeline of every adjustment the plan has made — so the "
            "user can see how the plan adapted around their life."
        ),
        "logged_events": [
            "Workout rescheduled due to holiday",
            "Volume reduced for vacation",
            "Intensity reduced for injury",
            "Plan extended / compressed",
            "Schedule changed mid-plan",
            "Deload week inserted",
            "Workout swapped by user"
        ],
        "display": "Subtle annotation dots on the monthly view, expandable on tap"
    }
}
```

### 9B.4 Calendar Data Model

```typescript
interface CalendarDay {
  date: Date;
  day_of_week: string;
  cycle_day_number: number;           // 1-N for non-standard cycles
  week_number: number;                // plan week (or cycle number)
  training_phase: TrainingPhase;

  // Workout
  workout: ScheduledWorkout | null;
  workout_status: "upcoming" | "completed" | "skipped" | "rescheduled" | "rest";
  workout_color: string;              // from color coding system

  // Life events
  is_holiday: boolean;
  holiday_name: string | null;
  is_vacation: boolean;
  is_reduced_availability: boolean;
  is_night_shift: boolean;
  is_race_day: boolean;
  is_tune_up_race: boolean;

  // Injury
  injury_active: boolean;
  injury_modification_applied: boolean;

  // Weather (populated day-of)
  weather_forecast: WeatherForecast | null;
  pace_adjusted_for_weather: boolean;

  // User modifications
  user_swapped: boolean;
  swapped_from: Date | null;
  user_notes: string | null;
}

interface CalendarWeek {
  week_number: number;
  cycle_length: number;               // 7 for standard, N for non-standard
  start_date: Date;
  end_date: Date;
  training_phase: TrainingPhase;
  is_deload: boolean;
  is_peak: boolean;
  is_taper: boolean;
  is_race_week: boolean;

  // Aggregates
  planned_mileage_km: number;
  completed_mileage_km: number;
  planned_runs: number;
  completed_runs: number;
  intensity_distribution: IntensityDistribution;
  weekly_note: string | null;

  days: CalendarDay[];
}

interface CalendarMonth {
  year: number;
  month: number;
  weeks: CalendarWeek[];              // weeks that overlap this month

  // Aggregates
  total_planned_mileage_km: number;
  total_completed_mileage_km: number;
  completion_rate: number;
  longest_run_km: number;
  phases_this_month: TrainingPhase[];
  holidays: Holiday[];
  vacations: VacationBlock[];
}

interface PlanTimeline {
  plan_id: string;
  start_date: Date;
  race_date: Date;
  end_date: Date;                     // includes recovery phase
  total_weeks: number;
  current_week: number;
  phases: {
    phase: TrainingPhase;
    start_week: number;
    end_week: number;
    start_date: Date;
    end_date: Date;
  }[];
  milestones: {
    type: string;
    week: number;
    date: Date;
    label: string;
  }[];
  adjustment_history: CalendarAdjustment[];
}

interface CalendarAdjustment {
  id: string;
  date: Date;
  type: "reschedule" | "volume_change" | "injury_modification"
      | "vacation_adjustment" | "schedule_change" | "deload_insert"
      | "user_swap";
  description: string;
  before_snapshot: any;               // what the plan looked like before
  after_snapshot: any;                // what it looks like after
}
```

### 9B.5 External Calendar Sync

```python
EXTERNAL_CALENDAR_SYNC = {
    "description": (
        "Runners live in Google Calendar, Apple Calendar, or Outlook. "
        "The training plan must exist where they already look at their "
        "schedule — not trapped in a separate app. This is critical for "
        "the core promise: training that fits around your life."
    ),

    "export_modes": {
        "subscription_feed": {
            "description": (
                "A live .ics subscription URL that external calendars poll "
                "periodically. Workouts appear in the user's main calendar "
                "and auto-update when the plan changes (reschedules, injury "
                "modifications, vacation adjustments)."
            ),
            "url_format": "webcal://api.app.com/plans/{plan_id}/calendar.ics?token={auth_token}",
            "update_frequency": (
                "Calendar apps poll the .ics feed on their own schedule "
                "(usually every 1-24 hours). Changes made in-app propagate "
                "on the next poll. We include a Cache-Control header "
                "suggesting a 1-hour refresh."
            ),
            "supported_calendars": [
                "Google Calendar (Add by URL → paste webcal link)",
                "Apple Calendar (Settings → Accounts → Add Subscribed Calendar)",
                "Outlook (Add calendar → From internet)",
                "Any app supporting iCalendar (.ics) subscription"
            ]
        },
        "one_time_download": {
            "description": (
                "Download a .ics file of the entire plan for manual import. "
                "Static snapshot — does not update when the plan changes. "
                "Useful for runners who want a backup or use a calendar app "
                "that doesn't support subscription feeds."
            ),
            "file": "{plan_name}_{distance}_{start_date}.ics"
        }
    },

    "calendar_event_format": {
        "description": (
            "Each workout appears as a time-blocked event (NOT an all-day event). "
            "This is essential — runners complained that TrainerRoad exports "
            "workouts as all-day events, which are useless for time management."
        ),
        "event_fields": {
            "summary": "Easy Run — 5 km (~30 min)",
            "start_time": "Pulled from user's day schedule (Section 6.6)",
            "end_time": "start_time + total_window_minutes (includes transition time)",
            "location": "Optional: 'Treadmill' or 'Outdoor' based on surface preference",
            "description": (
                "Workout details:\n"
                "- Warm-up: 5 min walk/easy jog\n"
                "- Core: 5 km at easy pace (6:00-6:30/km)\n"
                "- Cool-down: 5 min walk\n"
                "- Total window: 55 min (includes 15 min shower/change)\n"
                "\n"
                "Training Phase: Build (Week 8 of 16)\n"
                "Color: Green (easy/aerobic)"
            ),
            "categories": "Running, Training",
            "status": "TENTATIVE for upcoming, CONFIRMED for today",
            "alarm": "Reminder 30 min before (configurable)"
        },
        "special_events": {
            "rest_day": "Optional: show as all-day event 'Rest Day — Recovery' or exclude entirely (user choice)",
            "race_day": "All-day event with race name, location, and start time as a timed sub-event",
            "holiday": "All-day event showing holiday name and any workout modifications",
            "vacation": "All-day events spanning vacation dates with 'No training' or reduced plan"
        }
    },

    "two_way_sync": {
        "description": (
            "If a user reschedules a workout in their external calendar "
            "(e.g., drags 'Tempo Run' from Tuesday to Wednesday in Google "
            "Calendar), the change syncs back to the app. This is Runna's "
            "standout feature and a major differentiator."
        ),
        "implementation": {
            "google_calendar": (
                "Use Google Calendar API (CalDAV or REST) with push notifications "
                "to detect when a synced event is moved. Map the event UID back "
                "to the workout and trigger the swap rules (Section 6.5)."
            ),
            "apple_calendar": (
                "Use EventKit framework on iOS to monitor changes to events "
                "the app created. Detect date changes and trigger swaps."
            ),
            "outlook": (
                "Use Microsoft Graph API with change notifications "
                "to detect event modifications."
            )
        },
        "conflict_handling": {
            "valid_swap": (
                "If the moved workout passes swap rules (Section 6.5), "
                "accept the change silently and update the plan."
            ),
            "invalid_swap": (
                "If the swap violates hard/easy spacing rules, send a "
                "push notification: 'Moving your tempo run to Wednesday "
                "puts two hard sessions back-to-back. Want to proceed or "
                "revert?'"
            ),
            "cross_week_move": (
                "Moving a workout to a different week triggers a plan "
                "regeneration for both weeks. Notify user of any cascading "
                "changes."
            )
        },
        "user_setting": "Two-way sync is opt-in. Default is one-way (export only)."
    },

    "calendar_import_for_conflict_detection": {
        "description": (
            "Import the user's existing calendar (read-only) to detect "
            "scheduling conflicts. If a user has a dentist appointment at "
            "6 PM and their run is scheduled for 5:30 PM, flag it."
        ),
        "method": (
            "User grants read-only access to their Google/Apple/Outlook "
            "calendar. The app scans for time conflicts with planned workouts."
        ),
        "conflict_handling": [
            "Highlight conflicting days in the calendar view with a warning icon",
            "Suggest alternative time slots on the same day",
            "Offer to move the workout to a different day if no time slots fit",
            "Never auto-move workouts — always ask first"
        ],
        "privacy": (
            "The app reads event times and titles only (for conflict labels). "
            "Event details, attendees, and content are never stored. "
            "User can revoke access at any time."
        )
    }
}
```

### 9B.6 Home Screen & Lock Screen Widget

```python
HOME_SCREEN_WIDGET = {
    "description": (
        "A home screen widget that shows today's workout at a glance — "
        "without opening the app. Runners check their phone constantly; "
        "the widget keeps training visible and top-of-mind."
    ),

    "ios_widget": {
        "sizes": {
            "small": {
                "displays": [
                    "Workout type icon (color-coded)",
                    "Distance or duration",
                    "Scheduled time"
                ]
            },
            "medium": {
                "displays": [
                    "Today's workout: type, distance, pace target",
                    "Scheduled time with transition time",
                    "Weather snapshot (temp + conditions icon)",
                    "Tap to open daily view"
                ]
            },
            "large": {
                "displays": [
                    "Today's workout (full detail)",
                    "Tomorrow's workout preview",
                    "Week progress bar (runs completed / total)",
                    "Training phase label"
                ]
            }
        },
        "lock_screen_widget": {
            "displays": [
                "Workout type icon + distance",
                "Time of day"
            ],
            "tappable": "Opens directly to today's workout"
        },
        "live_activity": {
            "description": (
                "During a run, show a Live Activity on the lock screen and "
                "Dynamic Island with elapsed time, distance, and current pace."
            )
        }
    },

    "android_widget": {
        "sizes": {
            "small_2x1": "Workout type + distance + time",
            "medium_3x2": "Full today card with weather and quick start",
            "large_4x3": "Today + tomorrow + week progress"
        },
        "glance": {
            "description": "Android Glance-based widget for Wear OS integration"
        }
    },

    "rest_day_display": {
        "rest": "Widget shows 'Rest Day' with a calming color and encouraging message",
        "examples": [
            "'Rest Day — Your muscles are adapting right now.'",
            "'Rest Day — Back at it tomorrow with a 5 km easy run.'"
        ]
    },

    "update_frequency": "Widget refreshes when the app detects a plan change, at midnight, and when weather data updates"
}
```

### 9B.7 Print & PDF Export

```python
PRINT_AND_PDF_EXPORT = {
    "description": (
        "Many runners — especially those less tech-savvy, or those who "
        "prefer a physical reference — print their plan and tape it to "
        "the fridge, put it on a desk, or carry it in a running bag. "
        "The app must produce a clean, printable version of the plan."
    ),

    "export_options": {
        "full_plan_overview": {
            "format": "Multi-page PDF",
            "contents": [
                "Cover page: race distance, target date, skill level, plan duration",
                "Phase summary: dates and goals for each phase",
                "Week-by-week grid: each week on one row, days as columns",
                "Each cell shows: workout type, distance, target pace/effort",
                "Color coding preserved (print-friendly palette)",
                "Holidays, vacations, and deload weeks marked",
                "Race day highlighted prominently",
                "Space for handwritten notes beside each day"
            ],
            "layout": "Landscape orientation, one week per row, fits on letter/A4"
        },
        "single_week": {
            "format": "Single-page PDF",
            "contents": [
                "Week number, dates, training phase",
                "Each day with full workout detail",
                "Pace targets and effort levels",
                "Weekly mileage total",
                "Notes from the plan generator"
            ],
            "use_case": "Print the current week and pin it up"
        },
        "monthly_view": {
            "format": "Single-page PDF",
            "contents": [
                "Calendar grid for the month",
                "Workout type and distance in each day cell",
                "Phase bands and race day markers",
                "Monthly mileage summary"
            ],
            "layout": "Portrait orientation, standard month calendar grid"
        }
    },

    "print_friendly_design": {
        "colors": "Use print-safe colors (no neon, high contrast for B&W printers)",
        "fonts": "Clean sans-serif, minimum 10pt for readability",
        "margins": "Standard print margins (1 inch / 2.5 cm)",
        "no_app_chrome": "Remove all navigation, buttons, interactive elements",
        "logo": "Small app logo in footer only"
    },

    "sharing_via_pdf": (
        "The PDF export doubles as a sharing mechanism — users can email "
        "or text the PDF to a running buddy, coach, or partner so they "
        "can see the plan without needing an app account."
    )
}
```

### 9B.8 Cross-Training & Strength on the Calendar

```python
CROSS_TRAINING_CALENDAR = {
    "description": (
        "Cross-training (cycling, swimming, gym) and strength training "
        "don't count toward running volume but still need to live on the "
        "calendar so runners see their complete week. Many runners also "
        "struggle with when to schedule strength relative to hard run days."
    ),

    "adding_cross_training": {
        "methods": [
            "Tap a day → 'Add activity' → select from cross-training types",
            "Recurring schedule: 'I do yoga every Wednesday' → auto-populate",
            "Quick-add from a library of common activities"
        ],
        "activity_types": [
            "Strength training (gym)",
            "Cycling",
            "Swimming",
            "Yoga / Pilates",
            "Walking",
            "Elliptical / rowing",
            "Other (user-defined label)"
        ]
    },

    "display_on_calendar": {
        "appearance": (
            "Cross-training activities appear as secondary cards below the "
            "primary running workout for that day. They use the blue "
            "(cross-training) or purple (recovery/flexibility) color from "
            "the color coding system (Section 9B.2)."
        ),
        "multiple_per_day": (
            "A day can have one running workout + one or more cross-training "
            "activities. Example: Morning easy run + evening strength session."
        ),
        "rest_day_activities": (
            "On rest days, cross-training like yoga or walking can appear "
            "without violating the rest day. These are clearly labeled as "
            "'active recovery' not 'workouts'."
        )
    },

    "smart_placement_suggestions": {
        "description": (
            "The app suggests optimal days for strength training based on "
            "the running plan. Research shows strength should ideally follow "
            "hard run days (not precede them) and avoid the day before long runs."
        ),
        "rules": [
            "Suggest strength on hard run days (after the run) or easy run days",
            "Avoid strength the day before a long run or race-pace workout",
            "Avoid strength on rest days (true rest)",
            "If user does strength 2x/week, space them 3+ days apart",
            "Lower-body strength: same day as hard run (combined stress + recovery)",
            "Upper-body/core: flexible placement"
        ],
        "user_override": "Suggestions only — user can place activities wherever they want"
    },

    "time_budgeting": (
        "Cross-training activities respect the same transition time budget "
        "system (Section 6.6.1a) — if the user budgets 60 minutes for a "
        "lunch workout with 15 min shower, and they choose to swim instead "
        "of run, the time budget still applies."
    ),

    "plan_impact": (
        "Cross-training does NOT affect running plan calculations. "
        "It does not count toward weekly mileage, ACWR, or intensity "
        "distribution. It appears on the calendar purely for the user's "
        "scheduling visibility. This is a deliberate design decision — "
        "the app is a running plan tool, not a general fitness planner."
    )
}
```

### 9B.9 Calendar Sharing

```python
CALENDAR_SHARING = {
    "description": (
        "Beyond the public profile (Section 11D), users may want to share "
        "specific views of their calendar with a coach, partner, running "
        "buddy, or accountability partner without giving full profile access."
    ),

    "share_options": {
        "share_current_week": {
            "method": "Generate a shareable link or image of the current week view",
            "includes": [
                "Workout types and distances for each day",
                "Completion status (done/upcoming/skipped)",
                "Training phase and week number",
                "Weekly mileage progress"
            ],
            "excludes": [
                "Pace targets (optional — user can toggle on)",
                "Injury details",
                "Personal notes"
            ],
            "format": "Web link (no login needed) or downloadable image"
        },
        "share_plan_overview": {
            "method": "Shareable link to a read-only plan timeline",
            "includes": [
                "Full plan phases and dates",
                "Weekly mileage chart",
                "Race day and key milestones",
                "Overall completion percentage"
            ],
            "format": "Web link with a clean read-only view"
        },
        "share_to_coach": {
            "method": (
                "Generate a read-only link that a coach can bookmark. "
                "Updates in real-time as the runner completes workouts."
            ),
            "includes": "Full calendar with completion data and feedback",
            "expiry": "Link expires after 30 days unless refreshed"
        }
    },

    "social_sharing": {
        "description": (
            "Share a week's accomplishments to social media — a clean "
            "graphic showing the week's completed workouts, total mileage, "
            "and training phase."
        ),
        "platforms": ["Instagram Stories", "Twitter/X", "Facebook", "iMessage/WhatsApp"],
        "format": "Auto-generated image card with app branding",
        "privacy": "Only shows what the user opts into — no pace or injury data by default"
    }
}
```

### 9B.10 Calendar API Endpoints

```
GET    /api/plans/:id/calendar/day/:date         — Get single day detail
GET    /api/plans/:id/calendar/week/:week_num     — Get week view
GET    /api/plans/:id/calendar/month/:year/:month — Get month view
GET    /api/plans/:id/calendar/timeline           — Get full plan timeline
GET    /api/plans/:id/calendar/today              — Get today's card
GET    /api/plans/:id/calendar/upcoming           — Get next 7 days preview
POST   /api/plans/:id/calendar/swap               — Swap two workouts (drag-and-drop)
POST   /api/plans/:id/calendar/event              — Add a life event (holiday, vacation, etc.)
DELETE /api/plans/:id/calendar/event/:eid         — Remove a life event
GET    /api/plans/:id/calendar/adjustments        — Get adjustment history

# External calendar sync
GET    /api/plans/:id/calendar/export/ical        — Get .ics subscription feed URL
GET    /api/plans/:id/calendar/export/ical/download — Download static .ics file
POST   /api/plans/:id/calendar/sync/connect       — Connect external calendar for two-way sync
DELETE /api/plans/:id/calendar/sync/disconnect     — Disconnect external calendar sync
GET    /api/plans/:id/calendar/sync/status         — Check sync status and last sync time
POST   /api/plans/:id/calendar/import              — Import external calendar events (conflict detection)
GET    /api/plans/:id/calendar/conflicts           — Get detected scheduling conflicts

# Cross-training
POST   /api/plans/:id/calendar/cross-training      — Add a cross-training activity
PUT    /api/plans/:id/calendar/cross-training/:aid  — Update a cross-training activity
DELETE /api/plans/:id/calendar/cross-training/:aid  — Remove a cross-training activity
GET    /api/plans/:id/calendar/cross-training/suggest — Get smart placement suggestions

# Print & export
GET    /api/plans/:id/calendar/export/pdf           — Generate printable PDF (params: scope=full|week|month)
GET    /api/plans/:id/calendar/export/pdf/week/:num — Generate single-week PDF

# Sharing
POST   /api/plans/:id/calendar/share               — Generate a shareable link (params: scope, privacy)
DELETE /api/plans/:id/calendar/share/:link_id       — Revoke a shareable link
GET    /api/plans/:id/calendar/share/image/:scope   — Generate a social sharing image
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
