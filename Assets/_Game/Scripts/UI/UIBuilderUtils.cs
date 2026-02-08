using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace TheBunkerGames
{
    /// <summary>
    /// Static utility class for code-generating UI elements.
    /// Shared by ThemeSelectUI, FamilySelectUI, and InventoryDisplayUI auto-setup methods.
    /// </summary>
    public static class UIBuilderUtils
    {
        // -------------------------------------------------------------------------
        // Canvas & Infrastructure
        // -------------------------------------------------------------------------

        /// <summary>
        /// Creates a UI root as a child of the given parent.
        /// If the parent is already inside a Canvas, creates a stretched RectTransform container.
        /// If the parent is NOT inside a Canvas, creates a new Canvas (ScreenSpace-Overlay, 1920x1080).
        /// </summary>
        public static GameObject CreateCanvasRoot(Transform parent, string canvasName, int sortOrder = 0)
        {
            GameObject rootObj = new GameObject(canvasName);
            rootObj.transform.SetParent(parent, false);

            // Check if we're already inside a Canvas
            Canvas parentCanvas = parent.GetComponentInParent<Canvas>();

            if (parentCanvas != null)
            {
                // Already inside a Canvas — just create a stretched RectTransform container
                RectTransform rect = rootObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                // No parent Canvas — create a standalone Canvas
                Canvas canvas = rootObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = sortOrder;

                CanvasScaler scaler = rootObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                rootObj.AddComponent<GraphicRaycaster>();
            }

            return rootObj;
        }

        /// <summary>
        /// Ensures an EventSystem exists in the scene. Creates one if missing.
        /// </summary>
        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }
        }

        // -------------------------------------------------------------------------
        // Panels & Layout
        // -------------------------------------------------------------------------

        /// <summary>
        /// Creates a full-screen panel with background color.
        /// </summary>
        public static GameObject CreatePanel(Transform parent, string panelName, Color bgColor)
        {
            GameObject panel = new GameObject(panelName);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = panel.AddComponent<Image>();
            bg.color = bgColor;

            return panel;
        }

        /// <summary>
        /// Creates a title text anchored at the top of its parent.
        /// </summary>
        public static GameObject CreateTitle(Transform parent, string titleText)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);

            RectTransform rect = titleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.88f);
            rect.anchorMax = new Vector2(0.9f, 0.96f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = titleObj.AddComponent<Text>();
            text.text = titleText;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 36;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontStyle = FontStyle.Bold;

            return titleObj;
        }

        /// <summary>
        /// Creates a ScrollRect with Viewport, Content (VerticalLayoutGroup + ContentSizeFitter).
        /// Returns the Content GameObject (where child items should be added).
        /// </summary>
        public static GameObject CreateScrollArea(Transform parent)
        {
            // ScrollRect container
            GameObject scrollObj = new GameObject("ScrollArea");
            scrollObj.transform.SetParent(parent, false);

            RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.1f, 0.12f);
            scrollRect.anchorMax = new Vector2(0.9f, 0.86f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0, 0, 0, 0.3f);

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);

            RectTransform viewRect = viewport.AddComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = Vector2.zero;
            viewRect.offsetMax = Vector2.zero;

            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewRect;
            scroll.content = contentRect;

            return content;
        }

        // -------------------------------------------------------------------------
        // Buttons
        // -------------------------------------------------------------------------

        /// <summary>
        /// Creates a simple action button anchored at the bottom of its parent.
        /// </summary>
        public static GameObject CreateButton(Transform parent, string label, string buttonName = null)
        {
            if (string.IsNullOrEmpty(buttonName)) buttonName = "Button_" + label;

            GameObject btnObj = new GameObject(buttonName);
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.35f, 0.05f);
            rect.anchorMax = new Vector2(0.65f, 0.1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.55f, 0.25f, 1f);

            Button btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.7f, 0.3f, 1f);
            colors.pressedColor = new Color(0.2f, 0.4f, 0.2f, 1f);
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            btn.colors = colors;
            btn.interactable = false;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            return btnObj;
        }

        /// <summary>
        /// Creates a selection button with a label and sublabel, suitable for list items.
        /// Includes a LayoutElement for VerticalLayoutGroup compatibility.
        /// </summary>
        public static GameObject CreateSelectionButton(Transform parent, string label, string sublabel, UnityAction onClick)
        {
            GameObject btnObj = new GameObject("SelectBtn_" + label);
            btnObj.transform.SetParent(parent, false);

            LayoutElement layoutElem = btnObj.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = 80;
            layoutElem.minHeight = 80;

            Image bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);

            Button btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.5f, 1f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.25f, 1f);
            colors.selectedColor = new Color(0.25f, 0.5f, 0.25f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            // Title text
            GameObject titleObj = new GameObject("Label");
            titleObj.transform.SetParent(btnObj.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.5f);
            titleRect.anchorMax = new Vector2(0.95f, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            Text titleText = titleObj.AddComponent<Text>();
            titleText.text = label;
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 22;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = Color.white;
            titleText.fontStyle = FontStyle.Bold;

            // Sub text
            if (!string.IsNullOrEmpty(sublabel))
            {
                GameObject subObj = new GameObject("SubLabel");
                subObj.transform.SetParent(btnObj.transform, false);

                RectTransform subRect = subObj.AddComponent<RectTransform>();
                subRect.anchorMin = new Vector2(0.05f, 0.05f);
                subRect.anchorMax = new Vector2(0.95f, 0.5f);
                subRect.offsetMin = Vector2.zero;
                subRect.offsetMax = Vector2.zero;

                Text subText = subObj.AddComponent<Text>();
                subText.text = sublabel;
                subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                subText.fontSize = 16;
                subText.alignment = TextAnchor.MiddleLeft;
                subText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            }

            return btnObj;
        }

        // -------------------------------------------------------------------------
        // Inventory Rows
        // -------------------------------------------------------------------------

        /// <summary>
        /// Creates a single inventory row with item name and quantity.
        /// </summary>
        public static GameObject CreateInventoryRow(Transform parent, string itemName, string quantity)
        {
            GameObject row = new GameObject("InvRow_" + itemName);
            row.transform.SetParent(parent, false);

            LayoutElement layoutElem = row.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = 40;
            layoutElem.minHeight = 40;

            Image bg = row.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.18f, 0.22f, 0.8f);

            // Item name
            GameObject nameObj = new GameObject("ItemName");
            nameObj.transform.SetParent(row.transform, false);

            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.05f, 0);
            nameRect.anchorMax = new Vector2(0.75f, 1);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            Text nameText = nameObj.AddComponent<Text>();
            nameText.text = itemName;
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 18;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.color = Color.white;

            // Quantity
            if (!string.IsNullOrEmpty(quantity))
            {
                GameObject qtyObj = new GameObject("Quantity");
                qtyObj.transform.SetParent(row.transform, false);

                RectTransform qtyRect = qtyObj.AddComponent<RectTransform>();
                qtyRect.anchorMin = new Vector2(0.75f, 0);
                qtyRect.anchorMax = new Vector2(0.95f, 1);
                qtyRect.offsetMin = Vector2.zero;
                qtyRect.offsetMax = Vector2.zero;

                Text qtyText = qtyObj.AddComponent<Text>();
                qtyText.text = quantity;
                qtyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                qtyText.fontSize = 18;
                qtyText.alignment = TextAnchor.MiddleRight;
                qtyText.color = new Color(0.9f, 0.8f, 0.4f, 1f);
            }

            return row;
        }

        // -------------------------------------------------------------------------
        // Utility
        // -------------------------------------------------------------------------

        /// <summary>
        /// Finds a Button component on a named child of the given panel.
        /// </summary>
        public static Button FindButton(GameObject panel, string buttonName)
        {
            Transform btnTransform = panel.transform.Find(buttonName);
            if (btnTransform != null) return btnTransform.GetComponent<Button>();
            return null;
        }

        /// <summary>
        /// Sets the interactable state of a named button child.
        /// </summary>
        public static void SetButtonInteractable(GameObject panel, string buttonName, bool interactable)
        {
            Button btn = FindButton(panel, buttonName);
            if (btn != null) btn.interactable = interactable;
        }

        /// <summary>
        /// Finds the Content transform inside ScrollArea/Viewport/Content.
        /// </summary>
        public static Transform FindScrollContent(GameObject panel)
        {
            return panel.transform.Find("ScrollArea/Viewport/Content");
        }

        /// <summary>
        /// Destroys all children of a transform.
        /// </summary>
        public static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.Destroy(parent.GetChild(i).gameObject);
        }

        /// <summary>
        /// Destroys all children of a transform immediately (editor-safe).
        /// </summary>
        public static void ClearChildrenImmediate(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }
}
