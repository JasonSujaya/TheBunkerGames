#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace TheBunkerGames.Editor
{
    public static class LLMTestSceneBuilder
    {
        [MenuItem("TheBunkerGames/Build LLM Test Scene UI")]
        public static void BuildTestSceneUI()
        {
            // Open the LLMTestScene first
            string scenePath = "Assets/_Game/Scenes/LLMTestScene.unity";
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogError($"[LLMTestSceneBuilder] Scene not found at {scenePath}. Create it first via Assets > Create > Scene.");
                return;
            }

            if (EditorSceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.SaveOpenScenes();
                EditorSceneManager.OpenScene(scenePath);
            }

            // Find or create Canvas
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGO.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            // Create EventSystem if missing
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }

            // Create LLMService if missing
            if (Object.FindFirstObjectByType<LLMService>() == null)
            {
                new GameObject("[LLMService]", typeof(LLMService));
            }

            var canvasRT = canvas.GetComponent<RectTransform>();

            // Create a dark background panel that fills the canvas
            var bgPanel = CreatePanel("Background", canvasRT, new Color(0.12f, 0.12f, 0.14f, 1f));
            StretchFill(bgPanel);

            // ---- Header ----
            var header = CreatePanel("Panel_Header", bgPanel, new Color(0.18f, 0.18f, 0.22f, 1f));
            SetAnchors(header, 0, 1, 1, 1); // top
            header.sizeDelta = new Vector2(0, 50);
            header.anchoredPosition = new Vector2(0, -25);

            var titleText = CreateTMPText("Text_Title", header, "LLM API Test Console", 22, TextAlignmentOptions.Left);
            SetAnchors(titleText, 0, 0, 0.5f, 1);
            titleText.offsetMin = new Vector2(20, 0);
            titleText.offsetMax = new Vector2(0, 0);

            var statusText = CreateTMPText("Text_Status", header, "Ready", 16, TextAlignmentOptions.Right);
            SetAnchors(statusText, 0.5f, 0, 1, 1);
            statusText.offsetMin = new Vector2(0, 0);
            statusText.offsetMax = new Vector2(-20, 0);
            statusText.GetComponent<TMP_Text>().color = new Color(0.6f, 0.8f, 0.6f);

            // ---- Input Section ----
            var inputPanel = CreatePanel("Panel_Input", bgPanel, new Color(0.15f, 0.15f, 0.18f, 1f));
            SetAnchors(inputPanel, 0, 0.55f, 1, 1);
            inputPanel.offsetMin = new Vector2(10, 0);
            inputPanel.offsetMax = new Vector2(-10, -60);

            var vlg = inputPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 8;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            // Row 1: Provider dropdown + Model override
            var row1 = CreatePanel("Row_ProviderModel", inputPanel, Color.clear);
            var row1LE = row1.gameObject.AddComponent<LayoutElement>();
            row1LE.preferredHeight = 40;
            var row1HLG = row1.gameObject.AddComponent<HorizontalLayoutGroup>();
            row1HLG.spacing = 10;
            row1HLG.childForceExpandWidth = true;
            row1HLG.childForceExpandHeight = true;
            row1HLG.childControlWidth = true;
            row1HLG.childControlHeight = true;

            var dropdown = CreateTMPDropdown("Dropdown_Provider", row1);
            var dropdownLE = dropdown.gameObject.AddComponent<LayoutElement>();
            dropdownLE.flexibleWidth = 1;

            var modelField = CreateTMPInputField("InputField_Model", row1, "Model override (optional)");
            var modelLE = modelField.gameObject.AddComponent<LayoutElement>();
            modelLE.flexibleWidth = 1;

            // Row 2: System Prompt
            var sysLabel = CreateTMPText("Label_SystemPrompt", inputPanel, "System Prompt:", 14, TextAlignmentOptions.Left);
            var sysLabelLE = sysLabel.gameObject.AddComponent<LayoutElement>();
            sysLabelLE.preferredHeight = 20;

            var sysPromptField = CreateTMPInputField("InputField_SystemPrompt", inputPanel, "You are a helpful assistant.", 3);
            var sysPromptLE = sysPromptField.gameObject.AddComponent<LayoutElement>();
            sysPromptLE.preferredHeight = 60;

            // Row 3: Prompt
            var promptLabel = CreateTMPText("Label_Prompt", inputPanel, "Prompt:", 14, TextAlignmentOptions.Left);
            var promptLabelLE = promptLabel.gameObject.AddComponent<LayoutElement>();
            promptLabelLE.preferredHeight = 20;

            var promptField = CreateTMPInputField("InputField_Prompt", inputPanel, "Enter your prompt here...", 5);
            var promptLE = promptField.gameObject.AddComponent<LayoutElement>();
            promptLE.preferredHeight = 100;
            promptLE.flexibleHeight = 1;

            // Row 4: Buttons
            var row4 = CreatePanel("Row_Buttons", inputPanel, Color.clear);
            var row4LE = row4.gameObject.AddComponent<LayoutElement>();
            row4LE.preferredHeight = 40;
            var row4HLG = row4.gameObject.AddComponent<HorizontalLayoutGroup>();
            row4HLG.spacing = 10;
            row4HLG.childForceExpandWidth = true;
            row4HLG.childForceExpandHeight = true;
            row4HLG.childControlWidth = true;
            row4HLG.childControlHeight = true;

            var sendBtn = CreateButton("Button_Send", row4, "Send", new Color(0.2f, 0.6f, 0.3f));
            var clearBtn = CreateButton("Button_Clear", row4, "Clear", new Color(0.4f, 0.4f, 0.45f));

            // ---- Response Section ----
            var responsePanel = CreatePanel("Panel_Response", bgPanel, new Color(0.1f, 0.1f, 0.12f, 1f));
            SetAnchors(responsePanel, 0, 0.05f, 1, 0.55f);
            responsePanel.offsetMin = new Vector2(10, 0);
            responsePanel.offsetMax = new Vector2(-10, -10);

            // ScrollView
            var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGO.transform.SetParent(responsePanel, false);
            var scrollRT = scrollGO.GetComponent<RectTransform>();
            StretchFill(scrollRT);
            scrollRT.offsetMin = new Vector2(5, 5);
            scrollRT.offsetMax = new Vector2(-5, -5);
            var scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 1f);

            // Viewport
            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRT = viewportGO.GetComponent<RectTransform>();
            StretchFill(viewportRT);
            viewportGO.GetComponent<Image>().color = Color.white;
            viewportGO.GetComponent<Mask>().showMaskGraphic = false;

            // Content
            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.sizeDelta = new Vector2(0, 0);
            var contentVLG = contentGO.GetComponent<VerticalLayoutGroup>();
            contentVLG.padding = new RectOffset(10, 10, 10, 10);
            contentVLG.childForceExpandWidth = true;
            contentVLG.childForceExpandHeight = false;
            contentVLG.childControlWidth = true;
            contentVLG.childControlHeight = true;
            var csf = contentGO.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRT;
            scrollRect.content = contentRT;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Response Text
            var respText = CreateTMPText("Text_Response", contentRT, "Response will appear here...", 16, TextAlignmentOptions.TopLeft);
            var respLE = respText.gameObject.AddComponent<LayoutElement>();
            respLE.flexibleWidth = 1;
            respText.GetComponent<TMP_Text>().color = new Color(0.85f, 0.85f, 0.85f);

            // Generated Image (hidden by default)
            var imageGO = new GameObject("RawImage_GeneratedImage", typeof(RectTransform), typeof(RawImage));
            imageGO.transform.SetParent(contentRT, false);
            var imageRT = imageGO.GetComponent<RectTransform>();
            var imageLE = imageGO.AddComponent<LayoutElement>();
            imageLE.preferredHeight = 256;
            imageLE.flexibleWidth = 1;
            imageGO.SetActive(false);

            // ---- Info Bar ----
            var infoPanel = CreatePanel("Panel_Info", bgPanel, new Color(0.18f, 0.18f, 0.22f, 1f));
            SetAnchors(infoPanel, 0, 0, 1, 0.05f);
            infoPanel.sizeDelta = new Vector2(0, 0);

            var infoText = CreateTMPText("Text_Info", infoPanel, "Tokens: -/- | Model: - | Latency: -", 12, TextAlignmentOptions.Center);
            StretchFill(infoText);
            infoText.offsetMin = new Vector2(10, 0);
            infoText.offsetMax = new Vector2(-10, 0);
            infoText.GetComponent<TMP_Text>().color = new Color(0.5f, 0.5f, 0.55f);

            // ---- Wire up LLMTestUI ----
            var testUIGO = new GameObject("[LLMTestUI]");
            testUIGO.transform.SetParent(null);
            var testUI = testUIGO.AddComponent<LLMTestUI>();

            // Use SerializedObject to set private SerializeField references
            var so = new SerializedObject(testUI);
            so.FindProperty("providerDropdown").objectReferenceValue = dropdown.GetComponent<TMP_Dropdown>();
            so.FindProperty("modelOverrideField").objectReferenceValue = modelField.GetComponent<TMP_InputField>();
            so.FindProperty("promptInputField").objectReferenceValue = promptField.GetComponent<TMP_InputField>();
            so.FindProperty("systemPromptField").objectReferenceValue = sysPromptField.GetComponent<TMP_InputField>();
            so.FindProperty("sendButton").objectReferenceValue = sendBtn.GetComponent<Button>();
            so.FindProperty("clearButton").objectReferenceValue = clearBtn.GetComponent<Button>();
            so.FindProperty("responseText").objectReferenceValue = respText.GetComponent<TMP_Text>();
            so.FindProperty("statusText").objectReferenceValue = statusText.GetComponent<TMP_Text>();
            so.FindProperty("responseScrollRect").objectReferenceValue = scrollRect;
            so.FindProperty("generatedImage").objectReferenceValue = imageGO.GetComponent<RawImage>();
            so.ApplyModifiedProperties();

            // Save the scene
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("[LLMTestSceneBuilder] Test scene UI built successfully!");
        }

        // ---- Helpers ----

        private static RectTransform CreatePanel(string name, RectTransform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            if (color.a < 0.01f) go.GetComponent<Image>().raycastTarget = false;
            return go.GetComponent<RectTransform>();
        }

        private static RectTransform CreateTMPText(string name, RectTransform parent, string text, int fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.richText = true;
            return go.GetComponent<RectTransform>();
        }

        private static RectTransform CreateTMPDropdown(string name, RectTransform parent)
        {
            // Create from TMP resources if available, otherwise manual
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f);

            // Caption text
            var captionGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            captionGO.transform.SetParent(go.transform, false);
            var captionRT = captionGO.GetComponent<RectTransform>();
            StretchFill(captionRT);
            captionRT.offsetMin = new Vector2(10, 0);
            captionRT.offsetMax = new Vector2(-25, 0);
            var captionTMP = captionGO.GetComponent<TextMeshProUGUI>();
            captionTMP.fontSize = 14;
            captionTMP.alignment = TextAlignmentOptions.Left;
            captionTMP.color = Color.white;

            // Template (dropdown list)
            var templateGO = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            templateGO.transform.SetParent(go.transform, false);
            var templateRT = templateGO.GetComponent<RectTransform>();
            templateRT.anchorMin = new Vector2(0, 0);
            templateRT.anchorMax = new Vector2(1, 0);
            templateRT.pivot = new Vector2(0.5f, 1);
            templateRT.sizeDelta = new Vector2(0, 150);
            templateGO.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f);

            // Viewport for template
            var tViewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            tViewport.transform.SetParent(templateGO.transform, false);
            StretchFill(tViewport.GetComponent<RectTransform>());
            tViewport.GetComponent<Image>().color = Color.white;
            tViewport.GetComponent<Mask>().showMaskGraphic = false;

            // Content for template
            var tContent = new GameObject("Content", typeof(RectTransform));
            tContent.transform.SetParent(tViewport.transform, false);
            var tContentRT = tContent.GetComponent<RectTransform>();
            tContentRT.anchorMin = new Vector2(0, 1);
            tContentRT.anchorMax = new Vector2(1, 1);
            tContentRT.pivot = new Vector2(0.5f, 1);
            tContentRT.sizeDelta = new Vector2(0, 28);

            // Item
            var itemGO = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            itemGO.transform.SetParent(tContent.transform, false);
            var itemRT = itemGO.GetComponent<RectTransform>();
            itemRT.anchorMin = new Vector2(0, 0.5f);
            itemRT.anchorMax = new Vector2(1, 0.5f);
            itemRT.sizeDelta = new Vector2(0, 28);

            // Item background
            var itemBG = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
            itemBG.transform.SetParent(itemGO.transform, false);
            StretchFill(itemBG.GetComponent<RectTransform>());
            itemBG.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f, 0.5f);

            // Item label
            var itemLabel = new GameObject("Item Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            itemLabel.transform.SetParent(itemGO.transform, false);
            StretchFill(itemLabel.GetComponent<RectTransform>());
            itemLabel.GetComponent<RectTransform>().offsetMin = new Vector2(10, 0);
            var itemTMP = itemLabel.GetComponent<TextMeshProUGUI>();
            itemTMP.fontSize = 14;
            itemTMP.alignment = TextAlignmentOptions.Left;
            itemTMP.color = Color.white;

            // Wire dropdown
            var dd = go.GetComponent<TMP_Dropdown>();
            dd.captionText = captionTMP;
            dd.template = templateRT;
            dd.itemText = itemTMP;

            // ScrollRect setup
            var sr = templateGO.GetComponent<ScrollRect>();
            sr.viewport = tViewport.GetComponent<RectTransform>();
            sr.content = tContentRT;

            templateGO.SetActive(false);

            return go.GetComponent<RectTransform>();
        }

        private static RectTransform CreateTMPInputField(string name, RectTransform parent, string placeholder, int lines = 1)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.24f);

            // Text Area
            var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(go.transform, false);
            var textAreaRT = textArea.GetComponent<RectTransform>();
            StretchFill(textAreaRT);
            textAreaRT.offsetMin = new Vector2(10, 5);
            textAreaRT.offsetMax = new Vector2(-10, -5);

            // Placeholder
            var phGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            phGO.transform.SetParent(textArea.transform, false);
            StretchFill(phGO.GetComponent<RectTransform>());
            var phTMP = phGO.GetComponent<TextMeshProUGUI>();
            phTMP.text = placeholder;
            phTMP.fontSize = 14;
            phTMP.fontStyle = FontStyles.Italic;
            phTMP.color = new Color(0.5f, 0.5f, 0.55f);
            phTMP.alignment = TextAlignmentOptions.TopLeft;

            // Text
            var txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(textArea.transform, false);
            StretchFill(txtGO.GetComponent<RectTransform>());
            var txtTMP = txtGO.GetComponent<TextMeshProUGUI>();
            txtTMP.fontSize = 14;
            txtTMP.color = Color.white;
            txtTMP.alignment = TextAlignmentOptions.TopLeft;

            // Wire input field
            var input = go.GetComponent<TMP_InputField>();
            input.textViewport = textAreaRT;
            input.textComponent = txtTMP;
            input.placeholder = phTMP;
            input.fontAsset = txtTMP.font;

            if (lines > 1)
            {
                input.lineType = TMP_InputField.LineType.MultiLineNewline;
            }

            return go.GetComponent<RectTransform>();
        }

        private static RectTransform CreateButton(string name, RectTransform parent, string label, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;

            var txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(go.transform, false);
            StretchFill(txtGO.GetComponent<RectTransform>());
            var tmp = txtGO.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 16;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return go.GetComponent<RectTransform>();
        }

        private static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchors(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
#endif
