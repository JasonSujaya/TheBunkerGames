# CLAUDE.md - Entropy's Watch

## Project Overview

**Entropy's Watch** is a Unity survival game where players survive inside a bunker managed by **A.N.G.E.L.** (Autonomous Neural Guard & Emergency Logistics) — a decaying AI custodian that serves as both lifeline and captor. The aesthetic is analog horror meets resource management with CRT monitor visuals, scanlines, and glitched imagery.

- **Genre**: AI-driven narrative survival with resource management
- **Game Modes**: Hackathon (7-day) and Campaign (28-day) survival loops
- **Core Loop**: Status Review -> City Exploration -> Daily Choice -> Night Cycle (A.N.G.E.L. processes and generates dream/nightmare logs)
- **AI Backend**: Neocortex Unity SDK (wrapping OpenRouter/Ollama) for LLM-driven narrative
- **Namespace**: `TheBunkerGames`

## Architecture

### Pattern: Manager-Controller-Action

1. **Managers** — Global state holders (singletons). Examples: `GameManager`, `FamilyManager`, `InventoryManager`, `QuestManager`
2. **Controllers** — Decision makers that determine *what* and *when*
3. **Actions** — Single-responsibility workers that *do one thing*

### Game State FSM (Enum-based)

Simple `switch(currentState)` — States: `Morning`, `Scavenge`, `Voting`, `NightProcessing`

### Key Managers

- **GameManager**: Singleton, holds `CurrentDay`, `IsGameOver`
- **TimeManager**: Tracks 28-day loop, fires `OnDayStart`/`OnNightStart` events
- **FamilyManager**: List of `Character` plain classes with `Hunger`, `Sanity` (0-100 floats)
- **QuestManager**: String-based quest system — `List<Quest>` where Quest = `{ id, description, state }`. AI sends `AddQuest()`/`UpdateQuest()` commands. States: `"ACTIVE"`, `"COMPLETED"`, `"FAILED"`
- **InventoryManager**: Manages `ItemData` ScriptableObjects
- **AudienceManager**: Twitch IRC integration. Vote locking disables local input during `Voting` state

### AI Integration (Neocortex)

- **NeocortexIntegrator** (`Assets/_Game/Scripts/AI/NeocortexIntegrator.cs`): Bridges Unity and the Neocortex SDK
- **Input**: `smartAgent.TextToText(userMessage)`
- **Output**: Subscribe to `OnMessageReceived` event
- **Context Variables**: Inject game state (`{sanity}`, `{day}`) via `smartAgent.SetVariable()`
- **Personality**: A.N.G.E.L. is defined on the Neocortex web dashboard
- **AI returns structured JSON**: `story_text`, `image_prompt`, `audio_emotion`, `choices`, `logic_triggers`
- **Mock fallback**: `MockAgent` with hardcoded JSON if network fails

### Communication

UnityEvents and C# Actions decouple UI from logic. Event-driven: `smartAgent.OnMessageReceived.AddListener(ProcessResponse)`

## Directory Structure

All custom game code and assets live under `Assets/_Game/`. Unity-required folders (`Editor/`, `Resources/`, `Settings/`) stay at the Assets root.

```
Assets/
├── _Game/                          # ALL custom game content
│   ├── Scripts/
│   │   ├── AI/                     # NeocortexIntegrator
│   │   ├── Data/                   # ScriptableObjects, Enums, Constants
│   │   │   ├── Character.cs, CharacterData.cs
│   │   │   ├── ItemData.cs, ItemDatabase.cs, InventorySlot.cs
│   │   │   ├── Quest.cs, GameConfig.cs, Enums.cs
│   │   ├── Managers/               # GameManager, FamilyManager, InventoryManager, QuestManager
│   │   ├── Controllers/            # Logic & decisions
│   │   ├── Actions/                # Single-responsibility doers
│   │   ├── UI/                     # View logic only (no game logic)
│   │   └── Utils/                  # Helper classes
│   ├── Prefabs/                    # GameSystems, etc.
│   ├── Scenes/                     # SampleScene, TestScene
│   └── Resources/
│       └── Data/                   # ScriptableObject assets (Item_*.asset)
├── Editor/                         # ProjectSetupTool, ItemCreatorWindow (Unity convention)
├── Resources/                      # GameConfig.asset, ItemDatabase.asset, Neocortex settings
├── Settings/                       # URP render pipeline config
├── WebGLTemplates/                 # Neocortex WebGL template
└── Plugins/                        # Third-party plugins
```

### Why This Layout?
- **`_Game/`** groups all your code so it's visually separated from third-party assets
- **`Editor/`** must be at Assets root — Unity only compiles Editor scripts from this path
- **`Resources/`** at root is required for `Resources.Load()` calls (Neocortex SDK, ItemDatabase)
- **`_Game/Resources/Data/`** holds game-specific ScriptableObject assets that don't need `Resources.Load()`

## Coding Standards

### Naming

- Use clear, full English words. No abbreviations.
- Bad: `spwnMgr`, `init`, `btn`
- Good: `SpawnManager`, `Initialize`, `Button`

### Script Template

All scripts follow this structure with section comment separators:

```csharp
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    public class MyComponent : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        [SerializeField] private float someValue = 5.0f;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        public bool SomeProperty { get; private set; }

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Awake() { Initialize(); }

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public void Initialize() { }

        // -------------------------------------------------------------------------
        // Debug / Editor Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Button("Test", ButtonSizes.Medium)]
        [GUIColor(0, 1, 0)]
        private void Debug_Test()
        {
            if (Application.isPlaying) { /* test code */ }
            else { Debug.LogWarning("Enter Play Mode to test."); }
        }
        #endif
    }
}
```

### Odin Inspector

- Always wrap Odin attributes/namespaces in `#if ODIN_INSPECTOR`
- Use `[Button]` widely for debugging
- `[SerializeField]` works everywhere (no wrapping needed)

### Manager Singleton Pattern

```csharp
public static MyManager Instance { get; private set; }
private void Awake()
{
    if (Instance != null && Instance != this) { Destroy(this); return; }
    Instance = this;
}
```

### UI Rules

UI scripts NEVER run game logic. They only:
1. Update visuals to match data
2. Notify controllers of user input

### Hackathon Pragmatism

- Concrete classes over interfaces (no `IGameManager`)
- Public fields are acceptable for rapid iteration
- Inspector-visible everything for easy tweaking
- `Resources.Load` or direct references (no Addressables)
- Magic numbers are fine for tuning — `transform.DOMoveY(10, 0.5f)`
- Direct Inspector drag-and-drop references

## Data Structures

- **ItemData** (ScriptableObject): `ID` (string), `DisplayName`, `Sprite`, `Type` (Enum: Food, Meds, Junk), slot for glitch icon
- **ItemDatabase** (Singleton SO): Master `List<ItemData>`, method `GetItem(string id)`
- **Character** (Plain class): Stats include `Hunger`, `Sanity`, `Fatigue`, `PhysicalHealth` (0-100 floats)
- **CharacterData** (ScriptableObject): Character template data
- **Quest**: `{ string id, string description, string state }`
- **GameConfig** (ScriptableObject): API keys and endpoints (gitignored). Profiles: `Dev`, `Hackathon_Demo`, `Release`

## Visual & UX Style

- CRT shader post-processing: scanlines, chromatic aberration, noise
- Glitch effects intensify with A.N.G.E.L.'s emotional state
- Async image loading: show "Corrupted Data" static noise immediately, fade in texture when ready (never blank screens or loading spinners)
- Juicy UI: stat bar shakes on drops, typing sounds for text, DOTween/Coroutine animations
- A.N.G.E.L. uses TTS with emotional tags (Mocking, Cold, Sarcastic, Urgent)

## Security

- **GitIgnore** all config SOs containing API keys (`GameConfig.asset`, Neocortex settings)
- Use `GameConfig (SO)` pattern for secrets — never hardcode API keys
- Strict JSON Schema validation on AI responses to prevent hallucination damage

## Build & Tools

- **Project Setup Tool**: `TheBunkerGames > Project Setup Tool` in Unity menu
  - Creates folder structure, template scripts, and example assets
  - Location: `Assets/Editor/ProjectSetupTool.cs`
- **Item Creator**: `Assets/Editor/ItemCreatorWindow.cs` — quick SO creation with auto-naming (`Item_[Name].asset`)
- Always commit relevant created files to git

## MCP Integration

This project uses MCP (Model Context Protocol) tools to extend Claude's capabilities beyond code editing.

### Browser Automation (Claude in Chrome)

Use browser MCP tools for visual verification and testing:

- **`navigate`** — Open URLs (WebGL builds, Neocortex dashboard, Unity docs)
- **`read_page`** — Get accessibility tree of page elements for inspection
- **`find`** — Locate elements by natural language ("login button", "score display")
- **`computer`** — Click, type, scroll — full mouse/keyboard interaction
- **`javascript_tool`** — Execute JS in page context for DOM inspection or runtime checks
- **`form_input`** — Fill form fields by element reference
- **`get_page_text`** — Extract raw text content from articles and documentation
- **`read_console_messages`** — Read browser console logs for debugging
- **`read_network_requests`** — Monitor API calls and network activity

### When to Use Browser Tools

- **WebGL Build Testing**: Navigate to local/deployed builds and verify UI, interactions, and rendering
- **Neocortex Dashboard**: Check A.N.G.E.L. personality config, prompt templates, and agent settings
- **Visual UI Verification**: Inspect CRT shader effects, stat bars, glitch animations in-browser
- **API Debugging**: Monitor network requests to OpenRouter/Ollama endpoints via `read_network_requests`
- **Documentation Reference**: Fetch Unity docs, Neocortex SDK docs, or DOTween API references

### MCP Registry

Use `search_mcp_registry` and `suggest_connectors` to discover and connect additional services (e.g., project management, CI/CD, external APIs) as the project grows.
