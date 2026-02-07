# Entropy's Watch - Game Flow & Loop Design

> [!NOTE]
> **Design Critique & Goal**
> The previous draft implied a real-time clock (08:00, 10:00), which suggests players must react to time.
> **Correction**: This is a **Turn-Based Management Game**. The "Day" is a container for a set of **Action Phases**.
> The player should feel pressure from *resources*, not a clock.
>
> **Goal**: Create a general, repetitive loop where "Day 1" teaches the mechanics, and "Day N" tests efficiency.

---

## 1. Game Start (The Wake Up)
**Narrative State**: The Bunker is powered on.
*   **Initialization**:
    *   Load save / Generate new run.
    *   **Day 1 Start**: No resources decayed yet.
    *   **Day N Start**: `SurvivalManager` processes decay (Hunger/Thirst).

## 2. The Core Loop (The Day)

The day is structured into **5 Critical Phases**. You proceed linearly through them.

### Phase A: status_review (The Assessment)
*   **"Wake Up"**: The current state of the family is revealed.
*   **Player Actions available**:
    *   [x] View Biometrics (Check Health/Hunger/Sanity).
    *   [x] Check Inventory (Count Rations/Water/Fuel).
    *   [ ] *Cannot feed yet (Distribution happens at night).*
*   **Goal**: Identify the crisis. (e.g., "Father is starving", "Filter is broken").

### Phase B: angel_operations (The Request)
*   **Context**: You interact with the Bunker AI (A.N.G.E.L.).
*   **Player Actions available**:
    *   [x] **Request Ration**: Ask for food contextually.
    *   [x] **Request Info**: Ask about the outside world.
*   **System**: A.N.G.E.L.'s *Processing Integrity* determines if she helps or hallucinates.

### Phase C: expedition_planning (The Scavenge)
*   **Context**: You need more than A.N.G.E.L. gives.
*   **Player Actions available**:
    *   [x] **Select Explorer**: Choose a family member (e.g., Mother).
    *   [x] **Select Location**: Choose a destination (e.g., Pharmacy).
    *   [x] **Equip**: Give them a weapon or mask (if available).
    *   [ ] **Skip**: Stay inside to save energy/health.

### Phase D: anomaly_resolution (The Dilemma)
*   **Context**: Something goes wrong. A narrative event triggers.
*   **Player Actions available**:
    *   [x] **Make a Choice**: Pick 1 of 2 options (Usually a moral trade-off).
    *   *Example*: "Fix the vent manually (Injury Risk)" vs "Burn fuel (Resource Cost)".

### Phase E: night_protocol (Distribution & End)
*   **Context**: The day is done. Finalize survival.
*   **Player Actions available**:
    *   [x] **Distribute Food**: Feed hungry members.
    *   [x] **Distribute Water**: Cure thirst.
    *   [x] **Use Medkits**: Heal injuries.
    *   [x] **Sleep**: Commit the day.
*   **System Outcome**:
    *   A.N.G.E.L. generates a visual/text log of the day (The Dream).
    *   Unfed members take damage (calculated for next morning).
    *   **Day Counter +1**.

## 3. General Progression
*   **Day 1-3**: Tutorial / Stabilization. Learning to talk to A.N.G.E.L.
*   **Day 4-10**: Scarcity hits. First sacrifices needed.
*   **Day 20+**: Mental breakdown (Sanity mechanics become dominant).

## 4. Technical Implementation Plan
To support this "General" flow, we ensure:
1.  **Managers are State Machines**: `switch(CurrentPhase)` determines which UI is active.
2.  **Flexible Logic**: We can add new "Activities" to phases (e.g., adding a "Crafting" phase later) easily.
