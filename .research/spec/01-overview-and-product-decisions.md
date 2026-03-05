# Running Training Plan App — Overview & Product Decisions

> This module defines the product foundation. All decisions here apply across every other module.
> See `00-index.md` for the full file map and cross-module dependencies.

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

