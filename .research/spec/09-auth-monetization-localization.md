# Running Training Plan App — Authentication, Monetization & Localization

> This module covers passwordless authentication (OTP), the monetization model
> (almost everything free, only holiday/vacation handling is paid), and
> units/localization settings.
> See `00-index.md` for the full file map.

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

