---
trigger: always_on
---

# Unity Coding Standards & Architecture Guide

## 1. Core Principles
*   **Simple English**: Use clear, full words. Avoid abbreviations.
    *   *Bad*: `spwnMgr`, `init`, `btn`
    *   *Good*: `SpawnManager`, `Initialize`, `Button`
*   **Readability First**: Code is read more than it is written.
*   **Separation of Concerns**: Don't put everything in one script.

## 2. Directory Structure
Maintain a clean project by separating your custom code from third-party assets.

```text
Assets/
├── _Game/                  <-- ALL your custom code goes here
│   ├── Scripts/
│   │   ├── Managers/       <-- Global systems (GameManager, AudioManager)
│   │   ├── Controllers/    <-- Logic & decisions (PlayerController, EnemyController)
│   │   ├── Actions/        <-- "Doers" (JumpAction, ShootAction, SpawnAction)
│   │   ├── UI/             <-- View logic only
│   │   ├── Data/           <-- ScriptableObjects, Enums, Constants
│   │   └── Utils/          <-- Helper classes
│   ├── Prefabs/
│   └── Scenes/
├── Plugins/                <-- Third party plugins
└── Resources/
```

## 3. Architecture Pattern
We use a **Manager-Controller-Action** pattern to keep code modular and testable.

### The Hierarchy
1.  **Managers**: The "Bosses". They hold references to everything and handle global state.
    *   *Example*: `EnemyManager` tracks all active enemies.
2.  **Controllers**: The "Brains". They decide *what* needs to happen and *when*.
    *   *Example*: `PlayerController` reads input and decides if the player should jump.
3.  **Actions**: The "Workers". They do *one specific thing* very well.
    *   *Example*: `JumpAction` only handles the physics of jumping. It doesn't care about input.

### Flow Example
1.  **InputController** detects "Spacebar" press.
2.  **InputController** calls `PlayerController.OnJumpPressed()`.
3.  **PlayerController** checks `if (isGrounded)`.
4.  **PlayerController** calls `JumpAction.Execute()`.
5.  **JumpAction** applies `Rigidbody.AddForce()`.

## 4. Coding Template (The "Golden Standard")

Use this template for new scripts. It includes:
*   Safe Odin Inspector usage (works for everyone).
*   Debug buttons for easy testing.
*   Clear regions.

```csharp
using UnityEngine;
// Wrap Odin namespaces in #if to prevent errors for developers without Odin
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    public class StandardComponent : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Settings")]
        #endif
        [SerializeField] private float movementSpeed = 5.0f;
        
        #if ODIN_INSPECTOR
        [InfoBox("This is helpful for designers to understand what this toggle does.")]
        #endif
        [SerializeField] private bool enableDebugLogs = true;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        // Public properties for other classes to read
        public bool IsMoving { get; private set; }

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            Initialize();
        }

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public void Initialize()
        {
            // Setup code here
        }

        public void PerformAction()
        {
            if (enableDebugLogs) Debug.Log("Action Performed!");
            IsMoving = true;
        }

        // -------------------------------------------------------------------------
        // Debug / Editor Buttons
        // -------------------------------------------------------------------------
        // These buttons allow you to test functions directly from the Inspector
        
        #if ODIN_INSPECTOR
        [Button("Test Action", ButtonSizes.Medium)]
        [GUIColor(0, 1, 0)] // Green button
        private void Debug_TestPerformAction()
        {
            if (Application.isPlaying)
            {
                PerformAction();
            }
            else
            {
                Debug.LogWarning("Please enter Play Mode to test this action.");
            }
        }
        #endif
    }
}
```

## 5. Specific Rules

### For Odin Inspector
Always wrap Odin-specific attributes in `#if ODIN_INSPECTOR` **if** they cause compilation errors or if the project might be shared with users who don't have Odin.
*   **However**, simpler attributes like `[SerializeField]` work everywhere.
*   Use `[Button]` widely for debugging. Use `#if ODIN_INSPECTOR` around the button method itself if the attribute is only available with Odin.

### For Managers
Managers should often be Singletons or referenced by a central Service Locator, but keep it simple for now.
```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(this); 
            return; 
        }
        Instance = this;
    }
}
```

### For UI
UI scripts should NEVER run game logic. They should only:
1.  Update the text/images to match the data.
2.  Tell the Controller that a button was clicked.
```csharp
public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;

    // Called by Event or Manager
    public void UpdateScore(int newScore)
    {
        scoreText.text = $"Score: {newScore}";
    }
}
```

## 6. Auto-Setup Tool
To ensure every project starts with the correct structure AND code base, we use the `ProjectSetupTool`.

### Features
1.  **Setup Folders**: Creates the standard `_Game/Scripts/...` hierarchy.
2.  **Create Template Scripts**: Generates `GameManager`, `PlayerController`, and `GameSettings` (SO) using our coding standards.
3.  **Create Example Assets**: 
    *   Creates a `Player` prefab with a capsule mesh.
    *   Creates a `GlobalSettings` scriptable object asset.

### How to use
1.  Go to `TheBunkerGames` -> `Project Setup Tool`.
2.  Click **Create Folder Structure**.
3.  Click **Create Template Scripts** -> *Wait for Unity to Compile*.
4.  Click **Create Example Assets** (Only works after compilation!).

### The Script
Location: `Assets/Editor/ProjectSetupTool.cs`
(See project for full source)


MOST IMPORTANT make sure to commit to git for the files getting created.... Just commit relevant parts...