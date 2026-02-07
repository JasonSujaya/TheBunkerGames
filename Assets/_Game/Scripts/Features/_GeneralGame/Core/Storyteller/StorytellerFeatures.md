# Storyteller System - Feature Specification

## Overview
The **Storyteller** is the narrative engine of *Entropy's Watch*. It orchestrates the 30-day survival loop, injecting specific events, dilemmas, and flavor text based on the selected "Scenario".

## Core Architecture

### 1. Story Scenario (ScriptableObject)
A `StoryScenarioSO` defines a full campaign or run configuration.
*   **Campaign Name**: e.g., "The First Collapse", "Endless Winter".
*   **Total Days**: Default 30.
*   **Starting Conditions**: Links to `FamilyListSO` (optional override) or specific starting resources.
*   **Event Timeline**: A list mapping `Day Number` -> `Story Event`.

### 2. Story Event (ScriptableObject)
A `StoryEventSO` represents a specific narrative beat that happens on a specific day (or range of days).
*   **Event Type**:
    *   *Fixed*: Always happens on Day X (e.g., "The Door Knock" on Day 3).
    *   *Random*: Pulled from a pool (e.g., "Rat Infestation").
*   **Narrative Content**:
    *   **Title**: "A Knock at the Door".
    *   **Description**: The flavor text A.N.G.E.L. reads or displays.
*   **Choices (Dilemmas)**:
    *   Links to `DilemmaSO` (if using the existing dilemma system).
    *   Or defines simple choices: "Open Door" vs "Ignore".
*   **Consequences**:
    *   Resource changes (-10 Food).
    *   Health/Sanity changes.

### 3. Storyteller Manager (MonoBehaviour)
The active director in the scene.
*   **References**: Holds the current `StoryScenarioSO`.
*   **Cycle Handling**: Listen to `OnDayStart` from `GameManager`.
*   **Logic**:
    1.  Day Starts -> Check Scenario for Day X.
    2.  If Event exists -> Trigger Event UI / A.N.G.E.L. voice.
    3.  If no fixed Event -> Trigger "Quiet Day" or "Random Pool Event".

## Data Structure Proposal

### 1. The Scenario (Campaign Config)
```csharp
public class StoryScenarioSO : ScriptableObject 
{
    public string ScenarioName;
    [TextArea] public string Description;
    public int TotalDays = 30;
    
    // The "Deck" of events. 
    // Key = Day Number, Value = Event to trigger.
    public List<DayEventConfig> Timeline; 
}
```

### 2. The Event (Narrative Unit)
```csharp
public class StoryEventSO : ScriptableObject
{
    [Header("Narrative")]
    public string Title; // "A Knock at the Door"
    [TextArea] public string Description; // The content read by A.N.G.E.L.
    
    [Header("Consequences")]
    // What happens IMMEDIATELY when this event triggers?
    [SerializeReference] public List<StoryEffect> ImmediateEffects; 
    
    [Header("Choices")]
    public List<StoryChoice> Choices; // Options presented to player
}
```

### 3. The Effects (The "Crunch")
We use a polymorphic `StoryEffect` class. This allows both ScriptableObjects and Runtime AI to execute the same logic.

```csharp
[Serializable]
public abstract class StoryEffect 
{
    public abstract void Execute();
}

public class ModifyInventoryEffect : StoryEffect
{
    public string ItemId;
    public int Amount; // Positive to add, negative to remove
    public override void Execute() => InventoryManager.Instance.AddItem(ItemId, Amount);
}

public class ModifyHealthEffect : StoryEffect
{
    public string TargetName; // "Father", "Random", "All"
    public float Amount;      // -10 health
    public override void Execute() => FamilyManager.Instance.ModifyHealth(TargetName, Amount);
}

public class AddCharacterEffect : StoryEffect { ... }
public class KillCharacterEffect : StoryEffect { ... }
```

## AI & Runtime Generation
Since `StoryEffect` is just data, the AI can generate a storyboard in JSON, and we can parse it into runtime objects.

**Example LLM Output:**
```json
{
  "title": "The Wandering Merchant",
  "description": "A figure approaches the airlock...",
  "choices": [
    {
      "text": "Trade Water for Food",
      "effects": [
        { "type": "ModifyInventory", "item": "Water", "amount": -1 },
        { "type": "ModifyInventory", "item": "CannedFood", "amount": 2 }
      ]
    }
  ]
}
```
The `StorytellerManager` will deserialize this JSON into a runtime `StoryEvent` object and execute it seamlessly.

## Workflow
1.  **Designer** creates `StoryScenarioSO` ("Standard Campaign").
2.  **Designer** creates `StoryEventSO` assets ("Day 1 Intro", "Day 3 Trader").
3.  **Designer** drags events into the Scenario's timeline.
4.  **Game Start**: `StorytellerManager` loads the Scenario.
5.  **Day Loop**: `StorytellerManager` looks up `Timeline[CurrentDay]`.

## Future Extensions
*   **Branching**: Events could have "Requirements" (e.g., "Only if Father is Dead").
*   **Dynamic Injection**: A.N.G.E.L. inserts AI-generated events if the timeline is empty.
*   **Theme overrides**: Scenarios could change the UI skin or music.
