using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Per-character action menu placed on each Character body slot (Character1, Character2, etc.).
    /// Click the character → their action panel opens (Eat, Drink, Heal).
    /// Click an action → see matching consumable items from inventory.
    /// Click an item → character consumes it and gains stats.
    /// Only one panel can be open at a time across all characters.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class CharacterActionUI : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Static — only one panel open at a time
        // -------------------------------------------------------------------------
        private static CharacterActionUI _activePanel;

        /// <summary>Fired when any character consumes an item.</summary>
        public static event Action<CharacterData, ItemData, CharacterAction> OnItemConsumed;

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Character Binding")]
        [InfoBox("Index into FamilyManager.FamilyMembers (0 = first character, 1 = second, etc.)")]
        #endif
        [SerializeField] private int characterIndex = 0;
        [SerializeField] private int canvasSortOrder = 100;
        [SerializeField] private bool enableDebugLogs = false;

        // -------------------------------------------------------------------------
        // Visual Assets
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Visual Assets")]
        #endif
        [SerializeField] private TMP_FontAsset titleFont;
        [SerializeField] private TMP_FontAsset bodyFont;
        [SerializeField] private Sprite panelBackgroundSprite;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private Sprite itemRowSprite;

        #if ODIN_INSPECTOR
        [Title("Icons")]
        #endif
        [SerializeField] private Sprite iconEat;
        [SerializeField] private Sprite iconDrink;
        [SerializeField] private Sprite iconHeal;

        // -------------------------------------------------------------------------
        // Style
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Style")]
        #endif
        [SerializeField] private Color panelBgColor = new Color(0.15f, 0.13f, 0.10f, 0.95f);
        [SerializeField] private Color headerBgColor = new Color(0.20f, 0.17f, 0.13f, 1f);
        [SerializeField] private Color actionBtnColor = new Color(0.25f, 0.22f, 0.18f, 1f);
        [SerializeField] private Color actionBtnDisabledColor = new Color(0.20f, 0.18f, 0.16f, 0.5f);
        [SerializeField] private Color itemRowColor = new Color(0.22f, 0.20f, 0.17f, 0.9f);
        [SerializeField] private Color textColor = new Color(0.88f, 0.84f, 0.78f, 1f);
        [SerializeField] private Color textDisabledColor = new Color(0.50f, 0.48f, 0.44f, 1f);
        [SerializeField] private Color quantityColor = new Color(0.9f, 0.8f, 0.4f, 1f);

        // -------------------------------------------------------------------------
        // Generated References
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Generated References")]
        [ReadOnly]
        #endif
        [SerializeField] private GameObject canvasRoot;
        [SerializeField] private GameObject panel;

        // -------------------------------------------------------------------------
        // Runtime State
        // -------------------------------------------------------------------------
        private CharacterData _boundCharacter;
        private CharacterAction? _selectedAction;

        // Cached UI references
        private TextMeshProUGUI _headerText;
        private Transform _actionButtonContainer;
        private Transform _itemListContainer;
        private GameObject _itemListPanel;
        private TextMeshProUGUI _itemListHeader;

        // Action button cache
        private readonly Dictionary<CharacterAction, Button> _actionButtons = new Dictionary<CharacterAction, Button>();
        private readonly Dictionary<CharacterAction, TextMeshProUGUI> _actionLabels = new Dictionary<CharacterAction, TextMeshProUGUI>();
        private readonly Dictionary<CharacterAction, Image> _actionBgs = new Dictionary<CharacterAction, Image>();

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public int CharacterIndex => characterIndex;
        public CharacterData BoundCharacter => _boundCharacter;
        public bool IsVisible => canvasRoot != null && canvasRoot.activeSelf;

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Start()
        {
            SetupClickHandler();
        }

        // -------------------------------------------------------------------------
        // Click Handler — makes this character's Image clickable
        // -------------------------------------------------------------------------
        private void SetupClickHandler()
        {
            Button btn = GetComponent<Button>();
            if (btn == null)
            {
                btn = gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
            }
            btn.onClick.AddListener(OnCharacterClicked);
        }

        private void OnCharacterClicked()
        {
            // Resolve the character from FamilyManager at click time
            _boundCharacter = ResolveCharacter();

            if (_boundCharacter == null)
            {
                if (enableDebugLogs) Debug.LogWarning($"[CharacterActionUI] No character at index {characterIndex}.");
                return;
            }

            if (!_boundCharacter.IsAlive)
            {
                if (enableDebugLogs) Debug.Log($"[CharacterActionUI] {_boundCharacter.Name} is dead.");
                return;
            }

            // Close any other open panel first
            if (_activePanel != null && _activePanel != this)
                _activePanel.Hide();

            Show();
        }

        // -------------------------------------------------------------------------
        // Character Resolution
        // -------------------------------------------------------------------------
        private CharacterData ResolveCharacter()
        {
            if (FamilyManager.Instance == null || CharacterManager.Instance == null) return null;

            var family = FamilyManager.Instance.FamilyMembers;
            if (characterIndex < 0 || characterIndex >= family.Count) return null;

            return family[characterIndex];
        }

        // -------------------------------------------------------------------------
        // Auto Setup
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Auto Setup")]
        [Button("Auto Setup", ButtonSizes.Large)]
        [GUIColor(0.2f, 0.8f, 0.2f)]
        #endif
        [ContextMenu("Auto Setup")]
        public void AutoSetup()
        {
            if (canvasRoot != null)
            {
                DestroyImmediate(canvasRoot);
                canvasRoot = null;
            }

            // The panel canvas is NOT a child of this character — it's a top-level overlay
            // so it renders full-screen above everything
            canvasRoot = new GameObject($"CharacterActionCanvas_{gameObject.name}");

            Canvas canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = canvasSortOrder;

            CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasRoot.AddComponent<GraphicRaycaster>();
            UIBuilderUtils.EnsureEventSystem();

            BuildPanel();

            canvasRoot.SetActive(false);

            if (enableDebugLogs) Debug.Log($"[CharacterActionUI] Auto Setup complete for {gameObject.name}.");

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }

        // -------------------------------------------------------------------------
        // Panel Construction
        // -------------------------------------------------------------------------
        private void BuildPanel()
        {
            // Full-screen dim overlay (click to close)
            GameObject dimOverlay = new GameObject("DimOverlay");
            dimOverlay.transform.SetParent(canvasRoot.transform, false);
            RectTransform dimRect = dimOverlay.AddComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            Image dimImg = dimOverlay.AddComponent<Image>();
            dimImg.color = new Color(0, 0, 0, 0.4f);
            Button dimBtn = dimOverlay.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Hide);

            // Main panel (centered card)
            panel = new GameObject("ActionPanel");
            panel.transform.SetParent(canvasRoot.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.25f, 0.15f);
            panelRect.anchorMax = new Vector2(0.75f, 0.85f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelBg = panel.AddComponent<Image>();
            if (panelBackgroundSprite != null)
            {
                panelBg.sprite = panelBackgroundSprite;
                panelBg.type = Image.Type.Sliced;
            }
            panelBg.color = panelBgColor;

            BuildHeader();
            BuildActionButtons();
            BuildItemList();
            BuildCloseButton();
        }

        private void BuildHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(panel.transform, false);
            RectTransform rect = header.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.88f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image headerBg = header.AddComponent<Image>();
            headerBg.color = headerBgColor;

            GameObject textObj = new GameObject("HeaderText");
            textObj.transform.SetParent(header.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0f);
            textRect.anchorMax = new Vector2(0.85f, 1f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _headerText = textObj.AddComponent<TextMeshProUGUI>();
            _headerText.text = "CHARACTER ACTIONS";
            if (titleFont != null) _headerText.font = titleFont;
            _headerText.fontSize = 28;
            _headerText.alignment = TextAlignmentOptions.MidlineLeft;
            _headerText.color = textColor;
        }

        private void BuildActionButtons()
        {
            GameObject container = new GameObject("ActionButtons");
            container.transform.SetParent(panel.transform, false);
            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.03f, 0.55f);
            rect.anchorMax = new Vector2(0.97f, 0.86f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            _actionButtonContainer = container.transform;

            CreateActionButton(CharacterAction.Eat, "EAT", iconEat);
            CreateActionButton(CharacterAction.Drink, "DRINK", iconDrink);
            CreateActionButton(CharacterAction.Heal, "HEAL", iconHeal);
        }

        private void CreateActionButton(CharacterAction action, string label, Sprite icon)
        {
            GameObject btnObj = new GameObject($"Btn_{action}");
            btnObj.transform.SetParent(_actionButtonContainer, false);

            LayoutElement layoutElem = btnObj.AddComponent<LayoutElement>();
            layoutElem.flexibleWidth = 1;

            Image bg = btnObj.AddComponent<Image>();
            if (buttonSprite != null)
            {
                bg.sprite = buttonSprite;
                bg.type = Image.Type.Sliced;
            }
            bg.color = actionBtnColor;

            Button btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 0.9f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.7f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            btn.colors = colors;

            CharacterAction capturedAction = action;
            btn.onClick.AddListener(() => OnActionClicked(capturedAction));

            // Icon (if provided)
            if (icon != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(btnObj.transform, false);
                RectTransform iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.25f, 0.50f);
                iconRect.anchorMax = new Vector2(0.75f, 0.90f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;

                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = icon;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            // Label
            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0.05f);
            textRect.anchorMax = new Vector2(0.95f, icon != null ? 0.48f : 0.95f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
            textComp.text = label;
            if (bodyFont != null) textComp.font = bodyFont;
            textComp.fontSize = 20;
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.color = textColor;
            textComp.raycastTarget = false;

            _actionButtons[action] = btn;
            _actionLabels[action] = textComp;
            _actionBgs[action] = bg;
        }

        private void BuildItemList()
        {
            _itemListPanel = new GameObject("ItemListPanel");
            _itemListPanel.transform.SetParent(panel.transform, false);
            RectTransform rect = _itemListPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.03f, 0.08f);
            rect.anchorMax = new Vector2(0.97f, 0.53f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = _itemListPanel.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.10f, 0.08f, 0.8f);

            // Sub-header
            GameObject subHeader = new GameObject("ItemListHeader");
            subHeader.transform.SetParent(_itemListPanel.transform, false);
            RectTransform subRect = subHeader.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0.88f);
            subRect.anchorMax = new Vector2(1f, 1f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;

            _itemListHeader = subHeader.AddComponent<TextMeshProUGUI>();
            _itemListHeader.text = "Select item:";
            if (bodyFont != null) _itemListHeader.font = bodyFont;
            _itemListHeader.fontSize = 20;
            _itemListHeader.alignment = TextAlignmentOptions.Center;
            _itemListHeader.color = textColor;

            // Scroll area for items
            GameObject scrollObj = new GameObject("ScrollArea");
            scrollObj.transform.SetParent(_itemListPanel.transform, false);
            RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 0.86f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;

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

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewRect;
            scroll.content = contentRect;

            _itemListContainer = content.transform;

            _itemListPanel.SetActive(false);
        }

        private void BuildCloseButton()
        {
            GameObject btnObj = new GameObject("CloseButton");
            btnObj.transform.SetParent(panel.transform, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.90f, 0.90f);
            rect.anchorMax = new Vector2(0.98f, 0.98f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.6f, 0.15f, 0.15f, 0.9f);

            Button closeBtn = btnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Hide);

            GameObject textObj = new GameObject("X");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI xText = textObj.AddComponent<TextMeshProUGUI>();
            xText.text = "X";
            if (titleFont != null) xText.font = titleFont;
            xText.fontSize = 22;
            xText.alignment = TextAlignmentOptions.Center;
            xText.color = textColor;
            xText.raycastTarget = false;
        }

        // -------------------------------------------------------------------------
        // Show / Hide
        // -------------------------------------------------------------------------
        public void Show()
        {
            if (_boundCharacter == null || canvasRoot == null) return;

            _selectedAction = null;

            CacheReferences();

            if (_headerText != null)
                _headerText.text = $"{_boundCharacter.Name.ToUpper()} — ACTIONS";

            RefreshActionButtons();

            if (_itemListPanel != null) _itemListPanel.SetActive(false);

            canvasRoot.SetActive(true);
            _activePanel = this;

            if (enableDebugLogs) Debug.Log($"[CharacterActionUI] Shown for {_boundCharacter.Name} (index {characterIndex})");
        }

        public void Hide()
        {
            if (canvasRoot != null) canvasRoot.SetActive(false);

            if (_activePanel == this) _activePanel = null;
            _selectedAction = null;

            if (enableDebugLogs) Debug.Log($"[CharacterActionUI] Hidden ({gameObject.name}).");
        }

        // -------------------------------------------------------------------------
        // Action Logic
        // -------------------------------------------------------------------------
        private void OnActionClicked(CharacterAction action)
        {
            _selectedAction = action;
            ShowItemsForAction(action);
        }

        private void ShowItemsForAction(CharacterAction action)
        {
            if (_itemListContainer == null) return;

            UIBuilderUtils.ClearChildren(_itemListContainer);

            List<MatchingItem> matchingItems = GetMatchingItems(action);

            if (_itemListHeader != null)
            {
                string actionLabel = action switch
                {
                    CharacterAction.Eat => "FOOD",
                    CharacterAction.Drink => "WATER",
                    CharacterAction.Heal => "MEDICINE",
                    _ => "ITEMS"
                };
                _itemListHeader.text = $"Select {actionLabel} to use:";
            }

            if (matchingItems.Count == 0)
            {
                CreateEmptyRow("No items available");
            }
            else
            {
                foreach (var match in matchingItems)
                {
                    CreateItemRow(match.ItemData, match.Slot);
                }
            }

            if (_itemListPanel != null) _itemListPanel.SetActive(true);
        }

        private void CreateEmptyRow(string message)
        {
            GameObject row = new GameObject("EmptyRow");
            row.transform.SetParent(_itemListContainer, false);

            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 50;

            TextMeshProUGUI txt = row.AddComponent<TextMeshProUGUI>();
            txt.text = message;
            if (bodyFont != null) txt.font = bodyFont;
            txt.fontSize = 18;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = textDisabledColor;
        }

        private void CreateItemRow(ItemData itemData, InventorySlotData slot)
        {
            GameObject row = new GameObject($"Item_{itemData.ItemName}");
            row.transform.SetParent(_itemListContainer, false);

            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            le.minHeight = 60;

            Image bg = row.AddComponent<Image>();
            if (itemRowSprite != null)
            {
                bg.sprite = itemRowSprite;
                bg.type = Image.Type.Sliced;
            }
            bg.color = itemRowColor;

            Button btn = row.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.7f, 1f);
            btn.colors = colors;

            ItemData capturedItem = itemData;
            btn.onClick.AddListener(() => OnItemSelected(capturedItem));

            // Icon
            if (itemData.Icon != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(row.transform, false);
                RectTransform iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.02f, 0.10f);
                iconRect.anchorMax = new Vector2(0.12f, 0.90f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;

                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = itemData.Icon;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
            }

            // Item name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(row.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.14f, 0.50f);
            nameRect.anchorMax = new Vector2(0.75f, 0.95f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            TextMeshProUGUI nameTxt = nameObj.AddComponent<TextMeshProUGUI>();
            nameTxt.text = itemData.ItemName;
            if (bodyFont != null) nameTxt.font = bodyFont;
            nameTxt.fontSize = 18;
            nameTxt.alignment = TextAlignmentOptions.MidlineLeft;
            nameTxt.color = textColor;
            nameTxt.raycastTarget = false;

            // Effect description
            GameObject effectObj = new GameObject("Effect");
            effectObj.transform.SetParent(row.transform, false);
            RectTransform effectRect = effectObj.AddComponent<RectTransform>();
            effectRect.anchorMin = new Vector2(0.14f, 0.05f);
            effectRect.anchorMax = new Vector2(0.75f, 0.50f);
            effectRect.offsetMin = Vector2.zero;
            effectRect.offsetMax = Vector2.zero;

            TextMeshProUGUI effectTxt = effectObj.AddComponent<TextMeshProUGUI>();
            effectTxt.text = GetEffectDescription(itemData);
            if (bodyFont != null) effectTxt.font = bodyFont;
            effectTxt.fontSize = 14;
            effectTxt.alignment = TextAlignmentOptions.MidlineLeft;
            effectTxt.color = textDisabledColor;
            effectTxt.raycastTarget = false;

            // Quantity
            GameObject qtyObj = new GameObject("Quantity");
            qtyObj.transform.SetParent(row.transform, false);
            RectTransform qtyRect = qtyObj.AddComponent<RectTransform>();
            qtyRect.anchorMin = new Vector2(0.78f, 0f);
            qtyRect.anchorMax = new Vector2(0.98f, 1f);
            qtyRect.offsetMin = Vector2.zero;
            qtyRect.offsetMax = Vector2.zero;

            TextMeshProUGUI qtyTxt = qtyObj.AddComponent<TextMeshProUGUI>();
            qtyTxt.text = $"x{slot.Quantity}";
            if (bodyFont != null) qtyTxt.font = bodyFont;
            qtyTxt.fontSize = 20;
            qtyTxt.alignment = TextAlignmentOptions.Center;
            qtyTxt.color = quantityColor;
            qtyTxt.raycastTarget = false;
        }

        // -------------------------------------------------------------------------
        // Item Consumption
        // -------------------------------------------------------------------------
        private void OnItemSelected(ItemData itemData)
        {
            if (_boundCharacter == null || itemData == null) return;
            if (!_boundCharacter.IsAlive) return;

            // Consume 1 from inventory
            if (InventoryManager.Instance == null || !InventoryManager.Instance.RemoveItem(itemData, 1))
            {
                if (enableDebugLogs) Debug.LogWarning($"[CharacterActionUI] Failed to remove {itemData.ItemName} from inventory.");
                return;
            }

            // Apply restoration
            if (itemData.HungerRestore != 0f) _boundCharacter.ModifyHunger(itemData.HungerRestore);
            if (itemData.ThirstRestore != 0f) _boundCharacter.ModifyThirst(itemData.ThirstRestore);
            if (itemData.SanityRestore != 0f) _boundCharacter.ModifySanity(itemData.SanityRestore);
            if (itemData.HealthRestore != 0f) _boundCharacter.ModifyHealth(itemData.HealthRestore);

            if (enableDebugLogs)
            {
                Debug.Log($"[CharacterActionUI] {_boundCharacter.Name} consumed {itemData.ItemName}" +
                    $" → HP:{_boundCharacter.Health:F0} H:{_boundCharacter.Hunger:F0}" +
                    $" T:{_boundCharacter.Thirst:F0} S:{_boundCharacter.Sanity:F0}");
            }

            OnItemConsumed?.Invoke(_boundCharacter, itemData, _selectedAction ?? CharacterAction.Eat);

            // Refresh UI to show updated quantities
            RefreshActionButtons();
            if (_selectedAction.HasValue)
                ShowItemsForAction(_selectedAction.Value);
        }

        // -------------------------------------------------------------------------
        // Refresh
        // -------------------------------------------------------------------------
        private void RefreshActionButtons()
        {
            foreach (CharacterAction action in Enum.GetValues(typeof(CharacterAction)))
            {
                bool hasItems = GetMatchingItems(action).Count > 0;

                if (_actionButtons.TryGetValue(action, out Button btn))
                    btn.interactable = hasItems;

                if (_actionLabels.TryGetValue(action, out TextMeshProUGUI label))
                    label.color = hasItems ? textColor : textDisabledColor;

                if (_actionBgs.TryGetValue(action, out Image bg))
                    bg.color = hasItems ? actionBtnColor : actionBtnDisabledColor;
            }
        }

        // -------------------------------------------------------------------------
        // Item Matching
        // -------------------------------------------------------------------------
        private List<MatchingItem> GetMatchingItems(CharacterAction action)
        {
            var results = new List<MatchingItem>();

            if (InventoryManager.Instance == null || ItemManager.Instance == null) return results;

            ItemType targetType = action switch
            {
                CharacterAction.Eat => ItemType.Food,
                CharacterAction.Drink => ItemType.Water,
                CharacterAction.Heal => ItemType.Meds,
                _ => ItemType.Junk
            };

            foreach (var slot in InventoryManager.Instance.Items)
            {
                if (slot.Quantity <= 0) continue;

                var itemData = ItemManager.Instance.GetItem(slot.ItemId);
                if (itemData == null) continue;
                if (!itemData.IsConsumable) continue;
                if (itemData.Type != targetType) continue;

                results.Add(new MatchingItem { ItemData = itemData, Slot = slot });
            }

            return results;
        }

        private string GetEffectDescription(ItemData item)
        {
            var parts = new List<string>();
            if (item.HungerRestore > 0) parts.Add($"+{item.HungerRestore:F0} hunger");
            if (item.ThirstRestore > 0) parts.Add($"+{item.ThirstRestore:F0} thirst");
            if (item.HealthRestore > 0) parts.Add($"+{item.HealthRestore:F0} health");
            if (item.SanityRestore > 0) parts.Add($"+{item.SanityRestore:F0} sanity");
            return parts.Count > 0 ? string.Join(" | ", parts) : "No effect";
        }

        // -------------------------------------------------------------------------
        // Cache Helpers
        // -------------------------------------------------------------------------
        private void CacheReferences()
        {
            if (panel == null) return;

            if (_headerText == null)
            {
                Transform headerTf = panel.transform.Find("Header/HeaderText");
                if (headerTf != null) _headerText = headerTf.GetComponent<TextMeshProUGUI>();
            }

            if (_itemListPanel == null)
                _itemListPanel = panel.transform.Find("ItemListPanel")?.gameObject;

            if (_itemListContainer == null && _itemListPanel != null)
                _itemListContainer = _itemListPanel.transform.Find("ScrollArea/Viewport/Content");

            if (_itemListHeader == null && _itemListPanel != null)
            {
                Transform h = _itemListPanel.transform.Find("ItemListHeader");
                if (h != null) _itemListHeader = h.GetComponent<TextMeshProUGUI>();
            }

            if (_actionButtons.Count == 0)
            {
                Transform btnContainer = panel.transform.Find("ActionButtons");
                if (btnContainer != null)
                {
                    _actionButtonContainer = btnContainer;
                    foreach (CharacterAction action in Enum.GetValues(typeof(CharacterAction)))
                    {
                        Transform btnTf = btnContainer.Find($"Btn_{action}");
                        if (btnTf != null)
                        {
                            _actionButtons[action] = btnTf.GetComponent<Button>();
                            Transform labelTf = btnTf.Find("Label");
                            if (labelTf != null) _actionLabels[action] = labelTf.GetComponent<TextMeshProUGUI>();
                            _actionBgs[action] = btnTf.GetComponent<Image>();
                        }
                    }
                }
            }
        }

        // -------------------------------------------------------------------------
        // Cleanup
        // -------------------------------------------------------------------------
        private void OnDestroy()
        {
            if (_activePanel == this) _activePanel = null;

            // Clean up the overlay canvas (it's not parented to us)
            if (canvasRoot != null) Destroy(canvasRoot);
        }

        // -------------------------------------------------------------------------
        // Helper Types
        // -------------------------------------------------------------------------
        private struct MatchingItem
        {
            public ItemData ItemData;
            public InventorySlotData Slot;
        }

        // -------------------------------------------------------------------------
        // Debug
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [TitleGroup("Debug Controls")]
        [Button("Show Panel", ButtonSizes.Medium)]
        private void Debug_Show()
        {
            _boundCharacter = ResolveCharacter();
            if (_boundCharacter != null)
                Show();
            else
                Debug.LogWarning($"[CharacterActionUI] No character at index {characterIndex}.");
        }

        [TitleGroup("Debug Controls")]
        [Button("Hide Panel", ButtonSizes.Medium)]
        private void Debug_Hide() => Hide();
        #endif
    }
}
