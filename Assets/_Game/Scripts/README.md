# Project Architecture

The `Assets/_Game/Scripts` directory follows a **Feature-Based Architecture** to improve modularity and maintainability.

## 📂 Features
Each subfolder in `Features` represents a self-contained game system. To keep these systems clean, we enforce a strict internal structure:

### Standard Feature Structure
*   **Data/**: Contains `ScriptableObjects`, data classes (POCOS), and configuration files. (**Pure Data**)
*   **UI/**: Contains scripts that manage visual elements, panels, and view logic. (**View**)
*   **Root**: Contains the `Manager` or `Controller` scripts that handle the logic and connect Data to UI. (**Logic**)

### Current Features
*   **AI**:
    *   `Infrastructure/`: Generic LLM integration (OpenRouter/Mistral service).
    *   `Angel/`: Gameplay-specific AI logic (A.N.G.E.L.'s personality and game interaction).
*   **Character**: Family survivorship, stats, and character management.
*   **Inventory**: Items, crafting, and storage.
*   **Quests**: Task tracking system.
*   **Dilemmas**: Choice/consequence system.
*   **Exploration**: Scavenging logic.
*   **Status**: Daily report and status check logic.
*   **NightCycle**: End-of-day processing.

## 📂 Core
Contains global systems and shared utilities that are used across multiple features.

*   `GameManager`: Main entry point and state machine.
*   `SaveLoad`: Persistence system.
*   `UI`: Shared UI systems (feel, effects, common widgets).
*   `Enums.cs`: Global game states and types.
