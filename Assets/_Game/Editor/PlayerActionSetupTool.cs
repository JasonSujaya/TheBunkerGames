using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.Collections.Generic;

namespace TheBunkerGames.Editor
{
    /// <summary>
    /// Editor tool that creates and populates all ScriptableObject assets
    /// AND spawns scene GameObjects for the Player Action System.
    /// Menu: TheBunkerGames > Player Actions > ...
    /// </summary>
    public static class PlayerActionSetupTool
    {
        private const string SOFolder = "Assets/_Game/ScriptableObjects/PlayerActions";
        private const string ResourcesFolder = "Assets/_Game/Resources";

        // =====================================================================
        // Menu Items
        // =====================================================================

        [MenuItem("TheBunkerGames/Player Actions/Full Setup (Assets + Scene)", priority = 50)]
        public static void FullSetup()
        {
            EnsureFolders();
            CreateChallengePool();
            CreateStoryLog();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SpawnSceneObjects();

            Debug.Log("[PlayerActionSetup] FULL SETUP COMPLETE! Assets created + scene objects spawned + references wired.");
        }

        [MenuItem("TheBunkerGames/Player Actions/Create All Assets", priority = 100)]
        public static void CreateAllAssets()
        {
            EnsureFolders();
            CreateChallengePool();
            CreateStoryLog();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlayerActionSetup] All assets created and populated!");
        }

        [MenuItem("TheBunkerGames/Player Actions/Create Challenge Pool Only", priority = 200)]
        public static void CreateChallengePoolOnly()
        {
            EnsureFolders();
            CreateChallengePool();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("TheBunkerGames/Player Actions/Create Story Log Only", priority = 201)]
        public static void CreateStoryLogOnly()
        {
            EnsureFolders();
            CreateStoryLog();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // =====================================================================
        // Challenge Pool
        // =====================================================================
        private static void CreateChallengePool()
        {
            string path = $"{SOFolder}/PlayerActionChallengePool.asset";

            var asset = ScriptableObject.CreateInstance<PlayerActionChallengePoolSO>();

            // --- EXPLORATION CHALLENGES (15) ---
            var exploration = new List<PlayerActionChallenge>();

            exploration.Add(MakeChallenge(PlayerActionCategory.Exploration,
                "Locked Door",
                "You've found a heavy locked door in the lower bunker level. It might lead to a supply cache. How do you get it open?"));

            exploration.Add(MakeChallenge(PlayerActionCategory.Exploration,
                "Collapsed Tunnel",
                "A section of the maintenance tunnel has caved in, blocking access to the water pump. How do you clear the debris?"));

            exploration.Add(MakeChallenge(PlayerActionCategory.Exploration,
                "Flooded Room",
                "The storage room is ankle-deep in murky water from a burst pipe. Important supplies are on the shelves. How do you retrieve them safely?"));

            exploration.Add(MakeChallenge(PlayerActionCategory.Exploration,
                "Jammed Air Vent",
                "The main ventilation shaft is clogged with dust and debris. Air quality is dropping. How do you fix the airflow?"));

            exploration.Add(MakeChallenge(PlayerActionCategory.Exploration,
                "Broken Generator",
                "The backup generator sputtered and died. Without it, you lose lighting and the water pump. How do you get it running again?"));

            exploration.Add(MakeChallenge(PlayerActionCategory.Exploration,
                "Contaminated Water Tank",
                "The main water tank has a strange discoloration. It might be contaminated. How do you handle the water situation?"));

            exploration.Add(MakeChallenge(PlayerActionCategory.Exploration,
                "Strange Noises Above",
                "You hear scratching and thumping sounds from the ceiling. Something is above the bunker. How do you investigate?"));

            exploration.Add(MakeChallenge(PlayerActionCategory.Exploration,
                "Rusted Hatch",
                "The emergency exit hatch is rusted shut. You might need it as an escape route. How do you try to free it?"));

            exploration.Add(MakeChallenge(PlayerActionCategory.Exploration,
                "Flickering Lights",
                "The electrical wiring is sparking in the main corridor. It's a fire hazard and could short out the whole system. How do you deal with it?"));

            exploration.Add(MakeChallenge(PlayerActionCategory.Exploration,
                "Rat Infestation",
                "Rats have gotten into the food storage area. They're chewing through packaging and contaminating supplies. How do you handle the pest problem?"));

            exploration.Add(MakeChallenge(PlayerActionCategory.Exploration,
                "Mysterious Radio Signal",
                "The old radio picked up a faint transmission. It could be survivors, military, or a trap. How do you respond?"));

            exploration.Add(MakeChallenge(PlayerActionCategory.Exploration,
                "Cracked Wall",
                "A large crack has appeared in the bunker wall. You can feel cold air seeping through. Is the structural integrity at risk? What do you do?"));

            exploration.Add(MakeChallenge(PlayerActionCategory.Exploration,
                "Medicine Cabinet",
                "You found a sealed medicine cabinet in an unused room, but it has a combination lock. How do you access it?"));

            exploration.Add(MakeChallenge(PlayerActionCategory.Exploration,
                "Blocked Drain",
                "The bunker's waste drain is completely blocked. Sewage is starting to back up. How do you unclog it before it becomes a health crisis?"));

            exploration.Add(MakeChallenge(PlayerActionCategory.Exploration,
                "Solar Panel Access",
                "There's a solar panel array on the surface that could supplement power, but going outside is dangerous. How do you attempt to connect it?"));

            SetPrivateField(asset, "explorationChallenges", exploration);

            // --- DILEMMA CHALLENGES (10) ---
            var dilemmas = new List<PlayerActionChallenge>();

            dilemmas.Add(MakeChallenge(PlayerActionCategory.Dilemma,
                "Water Rationing",
                "Water supplies are running critically low. You can ration equally (everyone suffers a little) or prioritize the children (adults go thirsty). What do you decide?"));

            dilemmas.Add(MakeChallenge(PlayerActionCategory.Dilemma,
                "Stranger at the Door",
                "Someone is banging on the bunker door begging for help. They claim to be injured. Letting them in risks your family's safety. Ignoring them means they might die. What do you do?"));

            dilemmas.Add(MakeChallenge(PlayerActionCategory.Dilemma,
                "Stolen Supplies",
                "You discover that food has been disappearing. Circumstantial evidence points to a family member sneaking extra rations at night. How do you handle this?"));

            dilemmas.Add(MakeChallenge(PlayerActionCategory.Dilemma,
                "Power Rationing",
                "The generator fuel is almost gone. You can keep the lights on (morale), run the water pump (hydration), or power the radio (information). You can only pick one. Which do you choose?"));

            dilemmas.Add(MakeChallenge(PlayerActionCategory.Dilemma,
                "Sick Outsider",
                "A wounded stranger managed to get inside. They're clearly infected with something. Using your limited medicine on them means less for your family. What do you do?"));

            dilemmas.Add(MakeChallenge(PlayerActionCategory.Dilemma,
                "The Last Antibiotics",
                "Two family members are getting sick. You only have enough antibiotics for one. Who gets the medicine?"));

            dilemmas.Add(MakeChallenge(PlayerActionCategory.Dilemma,
                "Risky Trade",
                "A passing trader offers a large amount of food in exchange for your only weapon. Without the weapon you're defenseless, but without food you'll starve. What's your call?"));

            dilemmas.Add(MakeChallenge(PlayerActionCategory.Dilemma,
                "Evacuation Rumor",
                "A radio broadcast claims evacuation helicopters are coming to a location 2 days' walk away. It could be real, or it could be a raider trap. Do you stay in the bunker or risk the journey?"));

            dilemmas.Add(MakeChallenge(PlayerActionCategory.Dilemma,
                "The Confession",
                "A family member confesses they've been secretly communicating with outsiders via radio, potentially revealing your location. They say they were trying to find help. How do you respond?"));

            dilemmas.Add(MakeChallenge(PlayerActionCategory.Dilemma,
                "Contaminated Food",
                "Half of your remaining food supply may have been exposed to contamination. Eating it risks sickness. Throwing it away means going hungry. What's the plan?"));

            SetPrivateField(asset, "dilemmaChallenges", dilemmas);

            // --- FAMILY REQUEST CHALLENGES (10) ---
            var family = new List<PlayerActionChallenge>();

            family.Add(MakeFamilyChallenge(
                "High Fever",
                "{target} has developed a high fever and is shivering uncontrollably. They need care and possibly medicine. How do you help them?"));

            family.Add(MakeFamilyChallenge(
                "Nightmares",
                "{target} hasn't been sleeping. They keep waking up screaming from nightmares about the outside world. Their sanity is slipping. How do you comfort them?"));

            family.Add(MakeFamilyChallenge(
                "Refusing to Eat",
                "{target} has stopped eating. They say they'd rather the others have their share. They're getting weaker by the day. How do you convince them to eat?"));

            family.Add(MakeFamilyChallenge(
                "Panic Attack",
                "{target} is having a severe panic attack. They're hyperventilating and saying the walls are closing in. How do you calm them down?"));

            family.Add(MakeFamilyChallenge(
                "Infected Wound",
                "{target} has a wound that's turning red and swollen. It looks infected. Without treatment it could get much worse. What do you do?"));

            family.Add(MakeFamilyChallenge(
                "Homesick and Hopeless",
                "{target} has completely lost hope. They keep talking about how pointless it is to keep trying. Their despair is affecting everyone. How do you lift their spirits?"));

            family.Add(MakeFamilyChallenge(
                "Family Conflict",
                "{target} got into a heated argument with another family member. Tensions are high and they're refusing to speak to each other. How do you mediate?"));

            family.Add(MakeFamilyChallenge(
                "Dehydration",
                "{target} is showing signs of severe dehydration: dry lips, dizziness, confusion. They need water urgently, but supplies are limited. How do you handle this?"));

            family.Add(MakeFamilyChallenge(
                "Broken Bone",
                "{target} fell and may have broken their arm. They're in significant pain. You have limited medical supplies. How do you treat the injury?"));

            family.Add(MakeFamilyChallenge(
                "Wants to Leave",
                "{target} wants to leave the bunker alone to search for help. It's dangerous outside, but they're determined. How do you respond to their plan?"));

            SetPrivateField(asset, "familyRequestChallenges", family);

            // Save
            var existing = AssetDatabase.LoadAssetAtPath<PlayerActionChallengePoolSO>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
                Object.DestroyImmediate(asset);
                Debug.Log($"[PlayerActionSetup] Updated: {path} (Exploration:{exploration.Count} | Dilemma:{dilemmas.Count} | Family:{family.Count})");
            }
            else
            {
                AssetDatabase.CreateAsset(asset, path);
                Debug.Log($"[PlayerActionSetup] Created: {path} (Exploration:{exploration.Count} | Dilemma:{dilemmas.Count} | Family:{family.Count})");
            }
        }

        // =====================================================================
        // Story Log
        // =====================================================================
        private static void CreateStoryLog()
        {
            string path = $"{SOFolder}/StoryLog.asset";

            var existing = AssetDatabase.LoadAssetAtPath<StoryLogSO>(path);
            if (existing != null)
            {
                Debug.Log($"[PlayerActionSetup] StoryLog already exists: {path}");
                return;
            }

            var asset = ScriptableObject.CreateInstance<StoryLogSO>();
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[PlayerActionSetup] Created: {path}");
        }

        // =====================================================================
        // Scene Object Spawner
        // =====================================================================

        [MenuItem("TheBunkerGames/Player Actions/Spawn Scene Objects Only", priority = 300)]
        public static void SpawnSceneObjects()
        {
            // Load SO assets
            var challengePool = AssetDatabase.LoadAssetAtPath<PlayerActionChallengePoolSO>(
                $"{SOFolder}/PlayerActionChallengePool.asset");
            var storyLog = AssetDatabase.LoadAssetAtPath<StoryLogSO>(
                $"{SOFolder}/StoryLog.asset");

            if (challengePool == null)
                Debug.LogWarning("[PlayerActionSetup] ChallengePool asset not found! Run 'Create All Assets' first.");
            if (storyLog == null)
                Debug.LogWarning("[PlayerActionSetup] StoryLog asset not found! Run 'Create All Assets' first.");

            // ---------------------------------------------------------------
            // Root container
            // ---------------------------------------------------------------
            var root = new GameObject("--- PLAYER ACTIONS ---");
            Undo.RegisterCreatedObjectUndo(root, "Create Player Action System");

            // ---------------------------------------------------------------
            // [PlayerActionManager]
            // ---------------------------------------------------------------
            var managerObj = new GameObject("[PlayerActionManager]");
            managerObj.transform.SetParent(root.transform);
            var manager = managerObj.AddComponent<PlayerActionManager>();

            // Wire SO references via SerializedObject
            var managerSO = new SerializedObject(manager);
            if (challengePool != null)
                managerSO.FindProperty("challengePool").objectReferenceValue = challengePool;
            if (storyLog != null)
                managerSO.FindProperty("storyLog").objectReferenceValue = storyLog;
            managerSO.ApplyModifiedPropertiesWithoutUndo();

            // ---------------------------------------------------------------
            // [PlayerActionLLMBridge]
            // ---------------------------------------------------------------
            var bridgeObj = new GameObject("[PlayerActionLLMBridge]");
            bridgeObj.transform.SetParent(root.transform);
            bridgeObj.AddComponent<PlayerActionLLMBridge>();

            // ---------------------------------------------------------------
            // [PlayerActionUI] Canvas
            // ---------------------------------------------------------------
            var canvasObj = new GameObject("[PlayerActionUI]");
            canvasObj.transform.SetParent(root.transform);

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            var actionUI = canvasObj.AddComponent<PlayerActionUI>();

            // ---------------------------------------------------------------
            // Root Panel (holds all content, can be toggled)
            // ---------------------------------------------------------------
            var rootPanel = CreateUIElement("RootPanel", canvasObj.transform);
            var rootRect = rootPanel.GetComponent<RectTransform>();
            StretchFull(rootRect);
            var rootBg = rootPanel.AddComponent<Image>();
            rootBg.color = new Color(0.05f, 0.05f, 0.1f, 0.9f);

            // Header
            var headerObj = CreateUIElement("Header", rootPanel.transform);
            var headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.92f);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;

            var headerText = CreateTMPText("HeaderText", headerObj.transform, "DAILY ACTIONS", 28, TextAlignmentOptions.Center);
            StretchFull(headerText.GetComponent<RectTransform>());

            // Day label
            var dayLabel = CreateTMPText("DayLabel", headerObj.transform, "Day 1", 20, TextAlignmentOptions.TopRight);
            var dayRect = dayLabel.GetComponent<RectTransform>();
            dayRect.anchorMin = new Vector2(0.8f, 0);
            dayRect.anchorMax = new Vector2(1, 1);
            dayRect.offsetMin = Vector2.zero;
            dayRect.offsetMax = new Vector2(-10, 0);

            // ---------------------------------------------------------------
            // Content area with panels
            // ---------------------------------------------------------------
            var contentArea = CreateUIElement("ContentArea", rootPanel.transform);
            var contentRect = contentArea.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.02f, 0.12f);
            contentRect.anchorMax = new Vector2(0.98f, 0.9f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            // Horizontal layout for the 3 panels side by side
            var layout = contentArea.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 15;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(10, 10, 10, 10);

            // Create 3 category panels
            var explorationPanel = CreateCategoryPanel("ExplorationPanel", contentArea.transform, PlayerActionCategory.Exploration);
            var dilemmaPanel = CreateCategoryPanel("DilemmaPanel", contentArea.transform, PlayerActionCategory.Dilemma);
            var familyPanel = CreateCategoryPanel("FamilyPanel", contentArea.transform, PlayerActionCategory.FamilyRequest);

            // ---------------------------------------------------------------
            // Completion Panel (shown when all done)
            // ---------------------------------------------------------------
            var completionObj = CreateUIElement("CompletionPanel", rootPanel.transform);
            var completionRect = completionObj.GetComponent<RectTransform>();
            completionRect.anchorMin = new Vector2(0.15f, 0.15f);
            completionRect.anchorMax = new Vector2(0.85f, 0.85f);
            completionRect.offsetMin = Vector2.zero;
            completionRect.offsetMax = Vector2.zero;
            var completionBg = completionObj.AddComponent<Image>();
            completionBg.color = new Color(0.1f, 0.15f, 0.1f, 0.95f);
            completionObj.SetActive(false);

            var completionText = CreateTMPText("CompletionText", completionObj.transform, "All actions complete!", 18, TextAlignmentOptions.TopLeft);
            var compTextRect = completionText.GetComponent<RectTransform>();
            compTextRect.anchorMin = new Vector2(0.05f, 0.2f);
            compTextRect.anchorMax = new Vector2(0.95f, 0.95f);
            compTextRect.offsetMin = Vector2.zero;
            compTextRect.offsetMax = Vector2.zero;

            var continueBtn = CreateButton("ContinueButton", completionObj.transform, "Continue");
            var contBtnRect = continueBtn.GetComponent<RectTransform>();
            contBtnRect.anchorMin = new Vector2(0.3f, 0.03f);
            contBtnRect.anchorMax = new Vector2(0.7f, 0.15f);
            contBtnRect.offsetMin = Vector2.zero;
            contBtnRect.offsetMax = Vector2.zero;

            // ---------------------------------------------------------------
            // Wire PlayerActionUI references
            // ---------------------------------------------------------------
            var uiSO = new SerializedObject(actionUI);
            uiSO.FindProperty("explorationPanel").objectReferenceValue = explorationPanel.GetComponent<PlayerActionCategoryPanel>();
            uiSO.FindProperty("dilemmaPanel").objectReferenceValue = dilemmaPanel.GetComponent<PlayerActionCategoryPanel>();
            uiSO.FindProperty("familyRequestPanel").objectReferenceValue = familyPanel.GetComponent<PlayerActionCategoryPanel>();
            uiSO.FindProperty("rootPanel").objectReferenceValue = rootPanel;
            uiSO.FindProperty("headerText").objectReferenceValue = headerText;
            uiSO.FindProperty("dayLabel").objectReferenceValue = dayLabel;
            uiSO.FindProperty("completionPanel").objectReferenceValue = completionObj;
            uiSO.FindProperty("completionText").objectReferenceValue = completionText;
            uiSO.FindProperty("continueButton").objectReferenceValue = continueBtn.GetComponent<Button>();
            uiSO.ApplyModifiedPropertiesWithoutUndo();

            // ---------------------------------------------------------------
            // Done
            // ---------------------------------------------------------------
            Selection.activeGameObject = root;
            Debug.Log("[PlayerActionSetup] Scene objects spawned!");
            Debug.Log("  --- PLAYER ACTIONS ---");
            Debug.Log("    [PlayerActionManager] (with ChallengePool + StoryLog wired)");
            Debug.Log("    [PlayerActionLLMBridge]");
            Debug.Log("    [PlayerActionUI] Canvas with 3 category panels + completion panel");
        }

        // =====================================================================
        // Category Panel Builder
        // =====================================================================
        private static GameObject CreateCategoryPanel(string name, Transform parent, PlayerActionCategory category)
        {
            var panelObj = CreateUIElement(name, parent);
            var panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.12f, 0.12f, 0.18f, 1f);

            // Add layout
            var vLayout = panelObj.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 8;
            vLayout.padding = new RectOffset(10, 10, 10, 10);
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.childControlHeight = false;
            vLayout.childControlWidth = true;

            // Category label
            var catLabel = CreateTMPText("CategoryLabel", panelObj.transform, category.ToString().ToUpper(), 22, TextAlignmentOptions.Center);
            catLabel.color = new Color(1f, 0.85f, 0.3f);
            SetPreferredHeight(catLabel.gameObject, 35);

            // Challenge title
            var titleText = CreateTMPText("ChallengeTitle", panelObj.transform, "Challenge Title", 18, TextAlignmentOptions.Center);
            titleText.fontStyle = FontStyles.Bold;
            SetPreferredHeight(titleText.gameObject, 30);

            // Challenge description
            var descText = CreateTMPText("ChallengeDescription", panelObj.transform, "Challenge description goes here...", 14, TextAlignmentOptions.TopLeft);
            SetPreferredHeight(descText.gameObject, 80);

            // Input field
            var inputObj = new GameObject("PlayerInput");
            inputObj.transform.SetParent(panelObj.transform, false);
            var inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(0, 80);

            var inputBg = inputObj.AddComponent<Image>();
            inputBg.color = new Color(0.2f, 0.2f, 0.25f, 1f);

            var inputField = inputObj.AddComponent<TMP_InputField>();
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;

            // Input text area and placeholder
            var textArea = CreateUIElement("Text Area", inputObj.transform);
            StretchFull(textArea.GetComponent<RectTransform>(), 5);

            var inputText = CreateTMPText("Text", textArea.transform, "", 14, TextAlignmentOptions.TopLeft);
            StretchFull(inputText.GetComponent<RectTransform>());

            var placeholder = CreateTMPText("Placeholder", textArea.transform, "Type your response...", 14, TextAlignmentOptions.TopLeft);
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.color = new Color(1, 1, 1, 0.3f);
            StretchFull(placeholder.GetComponent<RectTransform>());

            inputField.textViewport = textArea.GetComponent<RectTransform>();
            inputField.textComponent = inputText;
            inputField.placeholder = placeholder;

            var inputLayout = inputObj.AddComponent<LayoutElement>();
            inputLayout.preferredHeight = 80;

            // Item toggle container (empty, populated at runtime)
            var itemContainer = CreateUIElement("ItemToggleContainer", panelObj.transform);
            SetPreferredHeight(itemContainer, 40);
            var itemLayout = itemContainer.AddComponent<HorizontalLayoutGroup>();
            itemLayout.spacing = 5;
            itemLayout.childForceExpandWidth = false;
            itemLayout.childForceExpandHeight = true;

            // Submit button
            var submitBtn = CreateButton("SubmitButton", panelObj.transform, "Submit");
            var btnLayout = submitBtn.AddComponent<LayoutElement>();
            btnLayout.preferredHeight = 40;

            // Status text
            var statusText = CreateTMPText("StatusText", panelObj.transform, "", 12, TextAlignmentOptions.Center);
            statusText.color = new Color(0.7f, 0.7f, 0.7f);
            SetPreferredHeight(statusText.gameObject, 20);

            // Loading indicator (simple text, hidden by default)
            var loadingObj = new GameObject("LoadingIndicator");
            loadingObj.transform.SetParent(panelObj.transform, false);
            loadingObj.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 20);
            var loadText = CreateTMPText("LoadingText", loadingObj.transform, "Processing...", 14, TextAlignmentOptions.Center);
            loadText.color = new Color(1f, 0.9f, 0.3f);
            StretchFull(loadText.GetComponent<RectTransform>());
            var loadLayout = loadingObj.AddComponent<LayoutElement>();
            loadLayout.preferredHeight = 20;
            loadingObj.SetActive(false);

            // Result panel (hidden by default)
            var resultPanel = CreateUIElement("ResultPanel", panelObj.transform);
            var resultBg = resultPanel.AddComponent<Image>();
            resultBg.color = new Color(0.08f, 0.15f, 0.08f, 1f);
            var resultLayout = resultPanel.AddComponent<VerticalLayoutGroup>();
            resultLayout.spacing = 4;
            resultLayout.padding = new RectOffset(8, 8, 8, 8);
            resultLayout.childForceExpandWidth = true;
            resultLayout.childForceExpandHeight = false;
            resultLayout.childControlHeight = false;
            resultLayout.childControlWidth = true;
            var resultLE = resultPanel.AddComponent<LayoutElement>();
            resultLE.preferredHeight = 120;

            var resultTitle = CreateTMPText("ResultTitle", resultPanel.transform, "Result Title", 16, TextAlignmentOptions.Center);
            resultTitle.fontStyle = FontStyles.Bold;
            resultTitle.color = new Color(0.4f, 1f, 0.4f);
            SetPreferredHeight(resultTitle.gameObject, 25);

            var resultDesc = CreateTMPText("ResultDescription", resultPanel.transform, "", 13, TextAlignmentOptions.TopLeft);
            SetPreferredHeight(resultDesc.gameObject, 50);

            var resultEffects = CreateTMPText("ResultEffects", resultPanel.transform, "", 11, TextAlignmentOptions.TopLeft);
            resultEffects.color = new Color(1f, 0.7f, 0.3f);
            SetPreferredHeight(resultEffects.gameObject, 35);

            resultPanel.SetActive(false);

            // ---------------------------------------------------------------
            // Wire PlayerActionCategoryPanel references
            // ---------------------------------------------------------------
            var panelComp = panelObj.AddComponent<PlayerActionCategoryPanel>();
            var panelSO = new SerializedObject(panelComp);
            panelSO.FindProperty("category").enumValueIndex = (int)category;
            panelSO.FindProperty("categoryLabel").objectReferenceValue = catLabel;
            panelSO.FindProperty("challengeTitleText").objectReferenceValue = titleText;
            panelSO.FindProperty("challengeDescriptionText").objectReferenceValue = descText;
            panelSO.FindProperty("playerInputField").objectReferenceValue = inputField;
            panelSO.FindProperty("submitButton").objectReferenceValue = submitBtn.GetComponent<Button>();
            panelSO.FindProperty("submitButtonText").objectReferenceValue = submitBtn.GetComponentInChildren<TMP_Text>();
            panelSO.FindProperty("itemToggleContainer").objectReferenceValue = itemContainer.transform;
            panelSO.FindProperty("resultPanel").objectReferenceValue = resultPanel;
            panelSO.FindProperty("resultTitleText").objectReferenceValue = resultTitle;
            panelSO.FindProperty("resultDescriptionText").objectReferenceValue = resultDesc;
            panelSO.FindProperty("resultEffectsText").objectReferenceValue = resultEffects;
            panelSO.FindProperty("loadingIndicator").objectReferenceValue = loadingObj;
            panelSO.FindProperty("statusText").objectReferenceValue = statusText;
            panelSO.ApplyModifiedPropertiesWithoutUndo();

            return panelObj;
        }

        // =====================================================================
        // UI Helpers
        // =====================================================================
        private static GameObject CreateUIElement(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private static TMP_Text CreateTMPText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            return tmp;
        }

        private static GameObject CreateButton(string name, Transform parent, string label)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            btnObj.AddComponent<RectTransform>();

            var btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(0.2f, 0.5f, 0.2f, 1f);

            var btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.65f, 0.3f, 1f);
            colors.pressedColor = new Color(0.15f, 0.4f, 0.15f, 1f);
            btn.colors = colors;

            var btnText = CreateTMPText("Text", btnObj.transform, label, 16, TextAlignmentOptions.Center);
            StretchFull(btnText.GetComponent<RectTransform>());

            return btnObj;
        }

        private static void StretchFull(RectTransform rect, float padding = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private static void SetPreferredHeight(GameObject obj, float height)
        {
            var le = obj.GetComponent<LayoutElement>();
            if (le == null) le = obj.AddComponent<LayoutElement>();
            le.preferredHeight = height;
        }

        // =====================================================================
        // Helpers
        // =====================================================================
        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets/_Game", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder(SOFolder))
                AssetDatabase.CreateFolder("Assets/_Game/ScriptableObjects", "PlayerActions");
        }

        private static PlayerActionChallenge MakeChallenge(PlayerActionCategory category, string title, string description)
        {
            var challenge = new PlayerActionChallenge();
            SetPrivateField(challenge, "category", category);
            SetPrivateField(challenge, "title", title);
            SetPrivateField(challenge, "description", description);
            SetPrivateField(challenge, "targetCharacter", "");
            return challenge;
        }

        private static PlayerActionChallenge MakeFamilyChallenge(string title, string description)
        {
            var challenge = new PlayerActionChallenge();
            SetPrivateField(challenge, "category", PlayerActionCategory.FamilyRequest);
            SetPrivateField(challenge, "title", title);
            SetPrivateField(challenge, "description", description);
            SetPrivateField(challenge, "targetCharacter", "");
            return challenge;
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(obj, value);
            else
                Debug.LogWarning($"[PlayerActionSetup] Field '{fieldName}' not found on {obj.GetType().Name}");
        }
    }
}
