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
    /// Gameplay HUD overlaying the main game screen.
    /// Matches the hand-drawn / war-journal aesthetic:
    ///   • Top-left torn-paper strip: DAY X · clock · temperature
    ///   • Below strip: OUR THINGS (diary icon) + END DAY (▶❙) buttons
    ///   • Left column: stacked polaroid-style character thumbnails with mini health/hunger bars
    ///   • Bottom-right: large taped-photo detail card for the selected character
    ///     – portrait, health/hunger/thirst bars, status condition icons
    ///   • Top-right: vertical resource icon column (food, water, meds, tools, junk)
    /// Auto-creates full hierarchy via AutoSetup(), following ThemeSelectUI pattern.
    /// </summary>
    public class GameplayHudUI : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static GameplayHudUI Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------
        public static event Action OnEndDayClicked;
        public static event Action OnOurThingsClicked;
        public static event Action OnDiaryClicked;
        public static event Action OnPrevCharacterClicked;
        public static event Action OnNextCharacterClicked;
        /// <summary>Fired when a character thumbnail is clicked. Passes the selected CharacterData.</summary>
        public static event Action<CharacterData> OnCharacterSelected;

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Configuration")]
        #endif
        [SerializeField] private int canvasSortOrder = 50;
        [SerializeField] private bool enableDebugLogs = true;

        #if ODIN_INSPECTOR
        [Title("UI References")]
        #endif
        [SerializeField] private PlayerActionUI playerActionUI;

        #if ODIN_INSPECTOR
        [Title("Family Body Slots (drag scene GameObjects with Image)")]
        #endif
        [SerializeField] private Image[] familyBodySlots = new Image[3];

        // -------------------------------------------------------------------------
        // Visual Assets (drag & drop in Inspector)
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Visual Assets – Frames & Backgrounds")]
        #endif
        [SerializeField] private TMP_FontAsset titleFont;
        [SerializeField] private TMP_FontAsset subtitleFont;
        [SerializeField] private Sprite cardFrameSprite;       // Theme.png – weathered paper card
        [SerializeField] private Sprite buttonSprite;           // Button.png – torn paper button
        [SerializeField] private Sprite dayBannerSprite;        // Paper1.png or similar strip
        [SerializeField] private Sprite portraitFrameSprite;    // Theme2.png or frame for polaroid
        [SerializeField] private Sprite arrowPrevSprite;        // PREV.png – left arrow
        [SerializeField] private Sprite arrowNextSprite;        // Right.png – right arrow

        #if ODIN_INSPECTOR
        [Title("Visual Assets – Icons")]
        #endif
        [SerializeField] private Sprite iconDiary;              // Icon_Diary.png
        [SerializeField] private Sprite iconHeart;              // Icon_Heart.png
        [SerializeField] private Sprite iconFood;               // Icon_Food.png  (hunger)
        [SerializeField] private Sprite iconWater;              // Icon_Water.png (thirst)
        [SerializeField] private Sprite iconBrain;              // Icon_Brain.png (sanity)
        [SerializeField] private Sprite iconClock;              // (optional)
        [SerializeField] private Sprite iconThermometer;        // Icon_Thermo.png
        [SerializeField] private Sprite iconCalendar;           // Icon_Calendar.png
        [SerializeField] private Sprite iconMeds;               // Icon_Medecine.png
        [SerializeField] private Sprite iconTools;              // Icon_Tools.png
        [SerializeField] private Sprite iconJunk;               // Icon_Thrash.png
        
        [Space]
        [SerializeField] private Sprite barFillSprite;          // Solid white sprite for bars

        // -------------------------------------------------------------------------
        // Style
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Style")]
        #endif
        [SerializeField] private Color panelBgColor   = new Color(0.88f, 0.84f, 0.78f, 0.95f);
        [SerializeField] private Color textColor      = new Color(0.12f, 0.10f, 0.08f, 1f);
        [SerializeField] private Color barBgColor     = new Color(0.20f, 0.18f, 0.15f, 0.60f);
        [SerializeField] private Color healthColor    = new Color(0.80f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color hungerColor    = new Color(0.20f, 0.70f, 0.20f, 1f);
        [SerializeField] private Color thirstColor    = new Color(0.20f, 0.60f, 0.90f, 1f);
        [SerializeField] private Color sanityColor    = new Color(0.70f, 0.40f, 0.90f, 1f);

        // -------------------------------------------------------------------------
        // Generated References (populated by AutoSetup)
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Generated References")]
        [ReadOnly]
        #endif
        [SerializeField] private GameObject canvasRoot;
        [SerializeField] private GameObject panel;

        // ---- Serialized UI references (assigned by AutoSetup or Inspector) ----
        #if ODIN_INSPECTOR
        [Title("Day Strip")]
        #endif
        public TextMeshProUGUI dayText;
        public TextMeshProUGUI timeText;
        public TextMeshProUGUI temperatureText;

        #if ODIN_INSPECTOR
        [Title("Character List")]
        #endif
        public Transform characterListContainer;
        private readonly List<ThumbRefs> thumbs = new List<ThumbRefs>();

        #if ODIN_INSPECTOR
        [Title("Detail Card")]
        #endif
        public TextMeshProUGUI detailNameText;
        public Image   detailPortrait;
        public Image   detailHealthFill;
        public TextMeshProUGUI detailHealthText;
        public Image   detailHungerFill;
        public TextMeshProUGUI detailHungerText;
        public Image   detailThirstFill;
        public TextMeshProUGUI detailThirstText;
        public Image   detailSanityFill;
        public TextMeshProUGUI detailSanityText;
        public Button  prevCharButton;
        public Button  nextCharButton;

        #if ODIN_INSPECTOR
        [Title("Resource Counts")]
        #endif
        public TextMeshProUGUI foodCountText;
        public TextMeshProUGUI waterCountText;
        public TextMeshProUGUI medsCountText;
        public TextMeshProUGUI toolsCountText;
        public TextMeshProUGUI junkCountText;

        // selection state
        private CharacterData selectedCharacter;
        private int selectedIndex = -1;

        // ---- helper ---------------------------------------------------------
        [Serializable]
        private class ThumbRefs
        {
            public GameObject root;
            public Image portrait;
            public Image healthFill;
            public Image hungerFill;
            public Image border;
            public CharacterData character;
        }

        // =====================================================================
        //  Unity Lifecycle
        // =====================================================================
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (enableDebugLogs) Debug.Log("[GameplayHudUI] Start() - Self-initializing...");
            
            // Generate fallback sprite if needed
            if (barFillSprite == null) barFillSprite = UIBuilderUtils.CreateWhiteSprite();

            // Patch existing detail bars if they have no sprite (fixes "Always Full" bug)
            if (detailHealthFill != null && detailHealthFill.sprite == null) detailHealthFill.sprite = barFillSprite;
            if (detailHungerFill != null && detailHungerFill.sprite == null) detailHungerFill.sprite = barFillSprite;
            if (detailThirstFill != null && detailThirstFill.sprite == null) detailThirstFill.sprite = barFillSprite;
            if (detailSanityFill != null && detailSanityFill.sprite == null) detailSanityFill.sprite = barFillSprite;

            WireButtons();
            PopulateCharacterList();
            RefreshFamilyBodies();
            UpdateDayDisplay();
            UpdateResourceDisplay();
        }

        private void LateUpdate()
        {
            if (canvasRoot == null || !canvasRoot.activeSelf) return;
            RefreshLiveData();
        }

        // =====================================================================
        //  Auto Setup
        // =====================================================================
        #if ODIN_INSPECTOR
        [Title("Auto Setup")]
        [Button("Auto Setup", ButtonSizes.Large)]
        [GUIColor(0.2f, 0.8f, 0.2f)]
        #endif
        [ContextMenu("Auto Setup")]
        public void AutoSetup()
        {
            if (canvasRoot != null) { DestroyImmediate(canvasRoot); canvasRoot = null; }

            // Attempt to load missing icons before building
            #if UNITY_EDITOR
            TryLoadMissingIcons();
            #endif

            canvasRoot = UIBuilderUtils.CreateCanvasRoot(transform, "GameplayHudCanvas", canvasSortOrder);
            UIBuilderUtils.EnsureEventSystem();
            panel = UIBuilderUtils.CreatePanel(canvasRoot.transform, "HudPanel", Color.clear);

            Build_DayStrip(panel.transform);
            Build_ActionButtons(panel.transform);
            Build_CharacterColumn(panel.transform);
            Build_DetailCard(panel.transform);
            Build_ResourceColumn(panel.transform);

            // populate preview portraits from FamilyManager in scene (editor mode)
            PopulateEditorPreview();

            // Auto-assign button refs
            if (panel != null)
            {
                diaryButton     = UIBuilderUtils.FindButton(panel, "DiaryBtn");
                endDayButton    = UIBuilderUtils.FindButton(panel, "EndDayBtn");
                ourThingsButton = UIBuilderUtils.FindButton(panel, "OurThingsBtn");
            }

            if (enableDebugLogs) Debug.Log("[GameplayHudUI] Auto Setup complete.");
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }

        #if UNITY_EDITOR
        private void TryLoadMissingIcons()
        {
            // Helper to find sprite by name in Assets
            Sprite FindSprite(string spriteName)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets($"{spriteName} t:Sprite");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
                return null;
            }

            if (iconFood == null)        iconFood        = FindSprite("Icon_Food");
            if (iconWater == null)       iconWater       = FindSprite("Icon_Water");
            if (iconMeds == null)        iconMeds        = FindSprite("Icon_Medecine"); // Note spelling
            if (iconTools == null)       iconTools       = FindSprite("Icon_Tools");
            if (iconJunk == null)        iconJunk        = FindSprite("Icon_Thrash");   // Note spelling
            
            if (iconHeart == null)       iconHeart       = FindSprite("Icon_Heart");
            if (iconBrain == null)       iconBrain       = FindSprite("Icon_Brain");
            if (iconClock == null)       iconClock       = FindSprite("Icon_Clock");
            if (iconThermometer == null) iconThermometer = FindSprite("Icon_Thermo");
            if (iconCalendar == null)    iconCalendar    = FindSprite("Icon_Calendar");
            if (iconDiary == null)       iconDiary       = FindSprite("Icon_Diary");

            if (enableDebugLogs) Debug.Log("[GameplayHudUI] Attempted to load missing icons.");
        }
        #endif

        /// <summary>
        /// Populates character thumbnails and detail card with preview portraits
        /// from the FamilyManager's DefaultFamilyProfile. Works in editor (no runtime singletons needed).
        /// </summary>
        private void PopulateEditorPreview()
        {
            // Find FamilyManager in scene (works in edit mode, no singleton needed)
            FamilyListSO profile = null;
            var fm = FindFirstObjectByType<FamilyManager>();
            if (fm != null) profile = fm.DefaultFamilyProfile;
            if (profile == null || profile.DefaultFamilyMembers == null) return;

            if (characterListContainer == null) return;

            // clear any existing children
            for (int i = characterListContainer.childCount - 1; i >= 0; i--)
                DestroyImmediate(characterListContainer.GetChild(i).gameObject);
            thumbs.Clear();

            var members = profile.DefaultFamilyMembers;
            for (int i = 0; i < members.Count; i++)
            {
                var def = members[i];
                if (def == null) continue;

                var tr = MakeThumb(characterListContainer, def.CharacterName);
                tr.character = null; // no runtime data in editor

                // assign portrait from definition
                if (def.Portrait != null)
                {
                    tr.portrait.sprite = def.Portrait;
                    tr.portrait.color  = Color.white;
                }

                thumbs.Add(tr);
            }

            // set first character portrait on detail card
            if (members.Count > 0 && members[0] != null && members[0].Portrait != null && detailPortrait != null)
            {
                detailPortrait.sprite = members[0].Portrait;
                detailPortrait.color  = Color.white;
            }

            // highlight first
            if (thumbs.Count > 0 && thumbs[0].border != null)
                thumbs[0].border.color = portraitFrameSprite != null
                    ? new Color(1f, 0.85f, 0.4f, 1f)
                    : new Color(1f, 1f, 1f, 0.15f);
        }

        // =====================================================================
        //  BUILD — Day / Time / Temp strip  (top-left torn-paper banner)
        // =====================================================================
        private void Build_DayStrip(Transform parent)
        {
            // ---- outer banner (torn paper strip) ----
            var strip = MakeRect(parent, "DayStrip",
                new Vector2(0.005f, 0.90f), new Vector2(0.30f, 0.99f));
            var stripImg = strip.gameObject.AddComponent<Image>();
            ApplySprite(stripImg, dayBannerSprite, panelBgColor);

            var hlg = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment    = TextAnchor.MiddleCenter;
            hlg.spacing           = 0;
            hlg.padding           = new RectOffset(8, 8, 2, 2);
            hlg.childControlWidth = true;  hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

            // DAY X (with calendar icon)
            dayText = MakeIconLabel(strip, "Day", iconCalendar, "DAY 1", 26, true, 0);

            // clock
            timeText = MakeIconLabel(strip, "Clock", iconClock, "08:50 am", 22, true, 0);

            // temperature
            temperatureText = MakeIconLabel(strip, "Temp", iconThermometer, "+8\u00B0C", 22, false, 0);
        }

        // =====================================================================
        //  BUILD — Action Buttons (OUR THINGS + END DAY)  below day strip
        // =====================================================================
        private void Build_ActionButtons(Transform parent)
        {
            // END DAY — right below the day strip
            MakeActionBtn(parent, "EndDayBtn", "END DAY", null,
                new Vector2(0.005f, 0.84f), new Vector2(0.12f, 0.895f));

            // DIARY — right next to END DAY
            MakeActionBtn(parent, "DiaryBtn", "DIARY", iconDiary,
                new Vector2(0.125f, 0.84f), new Vector2(0.24f, 0.895f));
        }

        private void MakeActionBtn(Transform parent, string name, string label,
            Sprite icon, Vector2 aMin, Vector2 aMax, string iconFallbackText = null)
        {
            var rt = MakeRect(parent, name, aMin, aMax);

            var bg = rt.gameObject.AddComponent<Image>();
            if (buttonSprite != null)
            {
                bg.sprite = buttonSprite;
                bg.type   = Image.Type.Simple;
                bg.preserveAspect = false;
                bg.color  = Color.white;
            }
            else
            {
                bg.color = panelBgColor;
            }

            var btn = rt.gameObject.AddComponent<Button>();
            var c = btn.colors;
            c.normalColor      = Color.white;
            c.highlightedColor = new Color(0.90f, 0.88f, 0.84f, 1f);
            c.pressedColor     = new Color(0.75f, 0.72f, 0.68f, 1f);
            btn.colors = c;

            // label — white text (button sprite is dark)
            var labelRT = MakeRect(rt, "Label",
                new Vector2(0.06f, 0f), new Vector2(0.72f, 1f));
            var tmp = labelRT.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 24;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color     = Color.white;
            tmp.enableAutoSizing = true; tmp.fontSizeMin = 14; tmp.fontSizeMax = 26;
            if (titleFont != null) tmp.font = titleFont;

            // icon (right side)
            var iconRT = MakeRect(rt, "Icon",
                new Vector2(0.74f, 0.08f), new Vector2(0.96f, 0.92f));

            if (icon != null)
            {
                var img = iconRT.gameObject.AddComponent<Image>();
                img.sprite = icon; img.preserveAspect = true;
                img.color = Color.white;
            }
            else if (!string.IsNullOrEmpty(iconFallbackText))
            {
                var itmp = iconRT.gameObject.AddComponent<TextMeshProUGUI>();
                itmp.text      = iconFallbackText;
                itmp.fontSize  = 28;
                itmp.alignment = TextAlignmentOptions.Center;
                itmp.color     = Color.white;
                if (titleFont != null) itmp.font = titleFont;
            }
        }

        // =====================================================================
        //  BUILD — Character Column  (left side portrait thumbnails)
        // =====================================================================
        private void Build_CharacterColumn(Transform parent)
        {
            // small portrait thumbnails stacked on left edge, below the action buttons
            var col = MakeRect(parent, "CharacterList",
                new Vector2(0.005f, 0.10f), new Vector2(0.06f, 0.83f));

            var vlg = col.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment    = TextAnchor.UpperCenter;
            vlg.spacing           = 3;
            vlg.padding           = new RectOffset(0, 0, 0, 0);
            vlg.childControlWidth = true;  vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            characterListContainer = col;
            // thumbnails are created at runtime in PopulateCharacterList()
        }

        /// <summary>Build one small portrait thumbnail (polaroid-style frame with portrait inside).</summary>
        private ThumbRefs MakeThumb(Transform parent, string charName)
        {
            var refs = new ThumbRefs();

            // root — acts as the framed portrait card
            var root = new GameObject("Thumb_" + charName, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            refs.root = root;

            var rootLE = root.AddComponent<LayoutElement>();
            rootLE.preferredHeight = 65;
            rootLE.minHeight       = 45;

            // border / frame image — fully transparent if no sprite
            refs.border = root.AddComponent<Image>();
            if (portraitFrameSprite != null)
            {
                refs.border.sprite = portraitFrameSprite;
                refs.border.type   = Image.Type.Sliced;
                refs.border.color  = Color.white;
            }
            else
            {
                refs.border.color = Color.clear;
            }

            // make clickable
            var btn = root.AddComponent<Button>();
            btn.targetGraphic = refs.border;
            var bc = btn.colors;
            bc.highlightedColor = new Color(1f, 0.95f, 0.85f, 1f);
            bc.pressedColor     = new Color(0.8f, 0.75f, 0.7f, 1f);
            btn.colors = bc;

            // inner portrait image (fills the card with small padding)
            var pImg = MakeRect(root.GetComponent<RectTransform>(), "Portrait",
                new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f));
            refs.portrait = pImg.gameObject.AddComponent<Image>();
            refs.portrait.color = new Color(0.35f, 0.35f, 0.35f, 1f);
            refs.portrait.preserveAspect = true;

            // thin health bar at bottom of portrait
            var hpBar = MakeRect(root.GetComponent<RectTransform>(), "HP",
                new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.08f));
            hpBar.gameObject.AddComponent<Image>().color = barBgColor;
            var hpFill = MakeRect(hpBar, "Fill", Vector2.zero, Vector2.one);
            hpFill.pivot = new Vector2(0, 0.5f);
            refs.healthFill = hpFill.gameObject.AddComponent<Image>();
            refs.healthFill.sprite     = barFillSprite;
            refs.healthFill.color      = healthColor;
            refs.healthFill.type       = Image.Type.Filled;
            refs.healthFill.fillMethod = Image.FillMethod.Horizontal;
            refs.healthFill.fillAmount = 1f;

            // hunger bar not shown on thumbnail (keep ref null for now)
            refs.hungerFill = null;

            return refs;
        }

        private Image MakeMiniBar(Transform parent, string name, Color fill, Sprite icon)
        {
            var bar = new GameObject(name, typeof(RectTransform));
            bar.transform.SetParent(parent, false);
            bar.AddComponent<LayoutElement>().preferredHeight = 16;

            // bg
            bar.AddComponent<Image>().color = barBgColor;

            // fill
            var fGo = new GameObject("Fill", typeof(RectTransform));
            fGo.transform.SetParent(bar.transform, false);
            var fRT = fGo.GetComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
            fRT.offsetMin = Vector2.zero; fRT.offsetMax = Vector2.zero;
            fRT.pivot     = new Vector2(0, 0.5f);

            var fImg = fGo.AddComponent<Image>();
            fImg.color      = fill;
            fImg.type       = Image.Type.Filled;
            fImg.fillMethod = Image.FillMethod.Horizontal;
            fImg.fillAmount = 1f;

            // small icon to the left
            if (icon != null)
            {
                var iGo = new GameObject("Icon", typeof(RectTransform));
                iGo.transform.SetParent(bar.transform, false);
                var iRT = iGo.GetComponent<RectTransform>();
                iRT.anchorMin = new Vector2(-0.22f, 0f);
                iRT.anchorMax = new Vector2(0.02f, 1f);
                iRT.offsetMin = Vector2.zero; iRT.offsetMax = Vector2.zero;

                var iImg = iGo.AddComponent<Image>();
                iImg.sprite = icon; iImg.preserveAspect = true;
                iImg.color = Color.white;
            }

            return fImg;
        }

        // =====================================================================
        //  BUILD — Detail Card  (right / bottom-right  taped-photo polaroid)
        // =====================================================================
        private void Build_DetailCard(Transform parent)
        {
            // compact polaroid card — bottom-right
            var card = MakeRect(parent, "DetailCard",
                new Vector2(0.74f, 0.02f), new Vector2(0.90f, 0.48f));

            var cardImg = card.gameObject.AddComponent<Image>();
            ApplySprite(cardImg, cardFrameSprite, new Color(0.94f, 0.92f, 0.87f, 0.97f));

            // slight rotation for taped-photo feel
            card.localRotation = Quaternion.Euler(0, 0, 1.5f);

            // ---- navigation row (prev arrow | name | next arrow) at very top ----
            var navRow = MakeRect(card, "NavRow",
                new Vector2(0.04f, 0.92f), new Vector2(0.96f, 0.99f));

            var navHlg = navRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            navHlg.childAlignment    = TextAnchor.MiddleCenter;
            navHlg.spacing           = 2;
            navHlg.padding           = new RectOffset(2, 2, 0, 0);
            navHlg.childControlWidth = false; navHlg.childControlHeight = true;
            navHlg.childForceExpandWidth = false; navHlg.childForceExpandHeight = true;

            prevCharButton = MakeArrowBtn(navRow, "PrevCharBtn", arrowPrevSprite, "<", 24);
            // character name label
            var nameGo = new GameObject("CharName", typeof(RectTransform));
            nameGo.transform.SetParent(navRow, false);
            nameGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            detailNameText = nameGo.AddComponent<TextMeshProUGUI>();
            detailNameText.text = "";
            detailNameText.fontSize = 18;
            detailNameText.fontStyle = FontStyles.Bold;
            detailNameText.alignment = TextAlignmentOptions.Center;
            detailNameText.color = textColor;
            detailNameText.enableAutoSizing = true;
            detailNameText.fontSizeMin = 10;
            detailNameText.fontSizeMax = 18;
            if (titleFont != null) detailNameText.font = titleFont;
            nextCharButton = MakeArrowBtn(navRow, "NextCharBtn", arrowNextSprite, ">", 24);

            // ---- portrait area (below nav, top ~55%) ----
            var pArea = MakeRect(card, "PortraitArea",
                new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.91f));

            // portrait frame
            var pFrame = pArea.gameObject.AddComponent<Image>();
            ApplySprite(pFrame, portraitFrameSprite, new Color(0.96f, 0.94f, 0.90f, 1f));

            // inner portrait image
            var pInner = MakeRect(pArea, "PortraitImage",
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f));
            detailPortrait = pInner.gameObject.AddComponent<Image>();
            detailPortrait.color = new Color(0.35f, 0.35f, 0.35f, 0.5f);
            detailPortrait.preserveAspect = true;

            // ---- stat bars (below portrait — 4 bars: health, hunger, thirst, sanity) ----
            var statsArea = MakeRect(card, "StatsArea",
                new Vector2(0.12f, 0.14f), new Vector2(0.92f, 0.34f));

            var sVlg = statsArea.gameObject.AddComponent<VerticalLayoutGroup>();
            sVlg.childAlignment    = TextAnchor.UpperLeft;
            sVlg.spacing           = 2;
            sVlg.padding           = new RectOffset(0, 0, 0, 0);
            sVlg.childControlWidth = true;  sVlg.childControlHeight = true;
            sVlg.childForceExpandWidth = true; sVlg.childForceExpandHeight = true;

            (detailHealthFill, detailHealthText) = MakeDetailBar(statsArea, "HealthBar", iconHeart, healthColor);
            (detailHungerFill, detailHungerText) = MakeDetailBar(statsArea, "HungerBar", iconFood,  hungerColor);
            (detailThirstFill, detailThirstText) = MakeDetailBar(statsArea, "ThirstBar", iconWater, thirstColor);
            (detailSanityFill, detailSanityText) = MakeDetailBar(statsArea, "SanityBar", iconBrain, sanityColor);

            // ---- status condition icons row (very bottom) ----
            var statusRow = MakeRect(card, "StatusRow",
                new Vector2(0.12f, 0.03f), new Vector2(0.92f, 0.12f));

            var srHlg = statusRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            srHlg.childAlignment    = TextAnchor.MiddleCenter;
            srHlg.spacing           = 6;
            srHlg.childControlWidth = false; srHlg.childControlHeight = true;
            srHlg.childForceExpandWidth = false; srHlg.childForceExpandHeight = true;

            MakeStatusIcon(statusRow, "iHP",     iconHeart);
            MakeStatusIcon(statusRow, "iHun",    iconFood);
            MakeStatusIcon(statusRow, "iThirst", iconWater);
            MakeStatusIcon(statusRow, "iMeds",   iconMeds);
            MakeStatusIcon(statusRow, "iTools",  iconTools);
        }

        private (Image fill, TextMeshProUGUI text) MakeDetailBar(RectTransform parent, string name,
            Sprite icon, Color fillColor)
        {
            var bar = new GameObject(name, typeof(RectTransform));
            bar.transform.SetParent(parent, false);

            // icon on the left (square, anchored to left edge)
            if (icon != null)
            {
                var iRT = MakeRect(bar.GetComponent<RectTransform>(), "Icon",
                    new Vector2(0f, 0.05f), new Vector2(0f, 0.95f));
                iRT.sizeDelta = new Vector2(16, 0); // fixed 16px wide
                var iImg = iRT.gameObject.AddComponent<Image>();
                iImg.sprite = icon; iImg.preserveAspect = true;
                iImg.color = Color.white;
            }

            // bar bg — thin line, offset past icon
            var bgRT = MakeRect(bar.GetComponent<RectTransform>(), "BG",
                new Vector2(0f, 0.20f), new Vector2(1f, 0.80f));
            bgRT.offsetMin = new Vector2(20, bgRT.offsetMin.y); // 20px left margin for icon
            bgRT.offsetMax = new Vector2(-40, bgRT.offsetMax.y); // 40px right margin for text
            bgRT.gameObject.AddComponent<Image>().color = barBgColor;

            var fRT = MakeRect(bgRT, "Fill", Vector2.zero, Vector2.one);
            fRT.pivot = new Vector2(0, 0.5f);
            var fImg = fRT.gameObject.AddComponent<Image>();
            fImg.sprite     = barFillSprite;
            fImg.color      = fillColor;
            fImg.type       = Image.Type.Filled;
            fImg.fillMethod = Image.FillMethod.Horizontal;
            fImg.fillAmount = 0.75f;

            // Value Text (right side)
            var valRT = MakeRect(bar.GetComponent<RectTransform>(), "Val",
                new Vector2(1f, 0f), new Vector2(1f, 1f));
            valRT.pivot = new Vector2(1, 0.5f);
            valRT.sizeDelta = new Vector2(35, 0); // fixed width
            valRT.anchorMin = new Vector2(1, 0); valRT.anchorMax = new Vector2(1, 1);
            valRT.offsetMin = new Vector2(-35, 0); valRT.offsetMax = new Vector2(0, 0);

            var tmp = valRT.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = "100";
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Right;
            tmp.color = textColor;
            if (subtitleFont != null) tmp.font = subtitleFont;
            else if (titleFont != null) tmp.font = titleFont;

            return (fImg, tmp);
        }

        private void MakeStatusIcon(RectTransform parent, string name, Sprite icon)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(22, 22);
            go.AddComponent<LayoutElement>().preferredWidth = 22;

            var img = go.AddComponent<Image>();
            if (icon != null) { img.sprite = icon; img.preserveAspect = true; }
            img.color = new Color(0.75f, 0.72f, 0.68f, 0.80f);
        }

        // =====================================================================
        //  BUILD — Resource Column  (top-right icons + counts)
        // =====================================================================
        private void Build_ResourceColumn(Transform parent)
        {
            // resource icons column — right side, stacked vertically
            var col = MakeRect(parent, "Resources",
                new Vector2(0.92f, 0.40f), new Vector2(0.995f, 0.96f));

            var vlg = col.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment    = TextAnchor.UpperCenter;
            vlg.spacing           = 4;
            vlg.padding           = new RectOffset(2, 2, 4, 4);
            vlg.childControlWidth = true;  vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            foodCountText  = MakeResRow(col, "Food",  iconFood);
            waterCountText = MakeResRow(col, "Water", iconWater);
            medsCountText  = MakeResRow(col, "Meds",  iconMeds);
            toolsCountText = MakeResRow(col, "Tools", iconTools);
            junkCountText  = MakeResRow(col, "Junk",  iconJunk);
        }

        private TextMeshProUGUI MakeResRow(RectTransform parent, string name, Sprite icon)
        {
            var row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            row.AddComponent<LayoutElement>().preferredHeight = 44;

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment    = TextAnchor.MiddleCenter;
            hlg.spacing           = 4;
            hlg.childControlWidth = false; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;

            // icon
            var iGo = new GameObject("Icon", typeof(RectTransform));
            iGo.transform.SetParent(row.transform, false);
            iGo.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40);
            iGo.AddComponent<LayoutElement>().preferredWidth = 40;
            var iImg = iGo.AddComponent<Image>();
            if (icon != null) { iImg.sprite = icon; iImg.preserveAspect = true; iImg.color = Color.white; }
            else iImg.color = new Color(0.6f, 0.6f, 0.6f, 0.5f);

            // count
            var tGo = new GameObject("Count", typeof(RectTransform));
            tGo.transform.SetParent(row.transform, false);
            tGo.GetComponent<RectTransform>().sizeDelta = new Vector2(36, 40);
            tGo.AddComponent<LayoutElement>().preferredWidth = 36;
            var tmp = tGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = "0";
            tmp.fontSize  = 26;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color     = Color.white;
            tmp.enableAutoSizing = true; tmp.fontSizeMin = 14; tmp.fontSizeMax = 26;
            if (titleFont != null) tmp.font = titleFont;
            return tmp;
        }

        // =====================================================================
        //  Tiny helpers (shared by all Build_ methods)
        // =====================================================================
        private RectTransform MakeRect(Transform parent, string name,
            Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        private RectTransform MakeRect(RectTransform parent, string name,
            Vector2 aMin, Vector2 aMax)
        {
            return MakeRect((Transform)parent, name, aMin, aMax);
        }

        private void ApplySprite(Image img, Sprite sprite, Color fallback)
        {
            if (sprite != null)
            { img.sprite = sprite; img.type = Image.Type.Sliced; img.color = Color.white; }
            else
            { img.color = fallback; }
        }

        /// <summary>Small arrow button using sprite directly. Returns the Button component.</summary>
        private Button MakeArrowBtn(RectTransform parent, string name, Sprite arrowSprite,
            string fallbackChar, float size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(size, size);
            go.AddComponent<LayoutElement>().preferredWidth = size;

            var img = go.AddComponent<Image>();
            img.raycastTarget = true;

            if (arrowSprite != null)
            {
                img.sprite = arrowSprite;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                img.color = Color.clear; // invisible bg, use text fallback
                var inner = MakeRect(go.GetComponent<RectTransform>(), "Arrow",
                    Vector2.zero, Vector2.one);
                var tmp = inner.gameObject.AddComponent<TextMeshProUGUI>();
                tmp.text      = fallbackChar;
                tmp.fontSize  = 22;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color     = textColor;
                tmp.raycastTarget = false;
                if (titleFont != null) tmp.font = titleFont;
            }

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var c = btn.colors;
            c.normalColor      = Color.white;
            c.highlightedColor = new Color(0.85f, 0.80f, 0.70f, 1f);
            c.pressedColor     = new Color(0.65f, 0.60f, 0.55f, 1f);
            btn.colors = c;

            return btn;
        }

        private void MakeSep(RectTransform parent)
        {
            var go = new GameObject("Sep", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(2, 0);
            go.AddComponent<LayoutElement>().preferredWidth = 2;
            go.AddComponent<Image>().color = new Color(textColor.r, textColor.g, textColor.b, 0.3f);
        }

        private TextMeshProUGUI MakeIconLabel(RectTransform parent, string name,
            Sprite icon, string defaultText, float fontSize, bool bold, float width)
        {
            var box = new GameObject(name, typeof(RectTransform));
            box.transform.SetParent(parent, false);

            var le = box.AddComponent<LayoutElement>();
            if (width > 0)
            {
                box.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 40);
                le.preferredWidth = width;
            }
            else
            {
                le.flexibleWidth = 1; // spread evenly
            }

            var hlg = box.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment    = TextAnchor.MiddleCenter;
            hlg.spacing           = 4;
            hlg.childControlWidth = false; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;

            if (icon != null)
            {
                var iGo = new GameObject("Icon", typeof(RectTransform));
                iGo.transform.SetParent(box.transform, false);
                iGo.GetComponent<RectTransform>().sizeDelta = new Vector2(28, 28);
                iGo.AddComponent<LayoutElement>().preferredWidth = 28;
                var iImg = iGo.AddComponent<Image>();
                iImg.sprite = icon; iImg.preserveAspect = true; iImg.color = Color.white;
            }

            var tGo = new GameObject("Text", typeof(RectTransform));
            tGo.transform.SetParent(box.transform, false);
            tGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            var tmp = tGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = defaultText;
            tmp.fontSize  = fontSize;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color     = textColor;
            tmp.enableAutoSizing = true; tmp.fontSizeMin = 12; tmp.fontSizeMax = fontSize;
            if (bold && titleFont != null) tmp.font = titleFont;
            else if (subtitleFont != null) tmp.font = subtitleFont;

            return tmp;
        }

        // =====================================================================
        //  Show / Hide
        // =====================================================================
        public void Show()
        {
            if (canvasRoot == null) return;
            canvasRoot.SetActive(true);
            WireButtons();
            PopulateCharacterList();
            RefreshFamilyBodies();
            UpdateDayDisplay();
            UpdateResourceDisplay();
            if (enableDebugLogs) Debug.Log("[GameplayHudUI] Shown.");
        }

        public void Hide()
        {
            if (canvasRoot != null) canvasRoot.SetActive(false);
        }

        // -------------------------------------------------------------------------
        // UI References (Buttons)
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Buttons")]
        #endif
        [SerializeField] private Button diaryButton;
        [SerializeField] private Button endDayButton;
        [SerializeField] private Button ourThingsButton;

        // =====================================================================
        //  Wire Buttons
        // =====================================================================
        private void WireButtons()
        {
            // Auto-find PlayerActionUI if not assigned
            if (playerActionUI == null)
                playerActionUI = FindFirstObjectByType<PlayerActionUI>(FindObjectsInactive.Include);

            // Wire character navigation arrows on detail card
            if (prevCharButton != null)
            {
                prevCharButton.onClick.RemoveAllListeners();
                prevCharButton.onClick.AddListener(CyclePrevCharacter);
            }
            if (nextCharButton != null)
            {
                nextCharButton.onClick.RemoveAllListeners();
                nextCharButton.onClick.AddListener(CycleNextCharacter);
            }

            if (ourThingsButton != null)
            {
                ourThingsButton.onClick.RemoveAllListeners();
                ourThingsButton.onClick.AddListener(() => OnOurThingsClicked?.Invoke());
            }

            if (endDayButton != null)
            {
                endDayButton.onClick.RemoveAllListeners();
                endDayButton.onClick.AddListener(() => OnEndDayClicked?.Invoke());
            }

            if (diaryButton != null)
            {
                diaryButton.onClick.RemoveAllListeners();
                diaryButton.onClick.AddListener(() =>
                {
                    OnDiaryClicked?.Invoke();
                    
                    if (enableDebugLogs) Debug.Log("[GameplayHudUI] Diary clicked!");

                    // Lazy find if needed
                    if (playerActionUI == null)
                        playerActionUI = FindFirstObjectByType<PlayerActionUI>(FindObjectsInactive.Include);

                    if (playerActionUI != null)
                    {
                        playerActionUI.Show();
                        if (enableDebugLogs) Debug.Log("[GameplayHudUI] Showing PlayerActionUI.");
                    }
                    else
                    {
                        Debug.LogWarning("[GameplayHudUI] PlayerActionUI not found!");
                    }
                });
            }
        }
        

        // =====================================================================
        //  Populate Character List  (runtime — needs FamilyManager)
        // =====================================================================
        public void PopulateCharacterList()
        {
            if (characterListContainer == null) return;
            UIBuilderUtils.ClearChildren(characterListContainer);
            thumbs.Clear();

            if (FamilyManager.Instance == null) return;

            var family = FamilyManager.Instance.FamilyMembers;
            for (int i = 0; i < family.Count; i++)
            {
                var ch    = family[i];
                var tr    = MakeThumb(characterListContainer, ch.Name);
                tr.character = ch;

                // assign portrait
                Sprite portrait = FindPortrait(ch.Name);
                if (portrait != null)
                { tr.portrait.sprite = portrait; tr.portrait.color = Color.white; }

                // click wiring
                int idx = i;
                var btn = tr.root.GetComponentInChildren<Button>();
                if (btn != null)
                { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(() => SelectCharacter(idx)); }

                thumbs.Add(tr);
            }

            if (family.Count > 0) SelectCharacter(0);
        }

        private Sprite FindPortrait(string characterName)
        {
            if (FamilyManager.Instance == null || FamilyManager.Instance.DefaultFamilyProfile == null) return null;
            foreach (var d in FamilyManager.Instance.DefaultFamilyProfile.DefaultFamilyMembers)
                if (d != null && d.CharacterName == characterName && d.Portrait != null) return d.Portrait;
            return null;
        }

        private Sprite FindBodyImage(string characterName)
        {
            if (FamilyManager.Instance == null || FamilyManager.Instance.DefaultFamilyProfile == null) return null;
            foreach (var d in FamilyManager.Instance.DefaultFamilyProfile.DefaultFamilyMembers)
                if (d != null && d.CharacterName == characterName && d.BodyImage != null) return d.BodyImage;
            return null;
        }

        /// <summary>
        /// Updates the family body slot Images with the body sprites from the current family members.
        /// Slots are matched by index: slot 0 = first family member, slot 1 = second, etc.
        /// </summary>
        public void RefreshFamilyBodies()
        {
            if (familyBodySlots == null) return;

            // Get family from runtime or editor profile
            List<CharacterDefinitionSO> defs = null;
            List<CharacterData> runtimeFamily = null;

            if (FamilyManager.Instance != null)
            {
                runtimeFamily = FamilyManager.Instance.FamilyMembers;
                if (FamilyManager.Instance.DefaultFamilyProfile != null)
                    defs = FamilyManager.Instance.DefaultFamilyProfile.DefaultFamilyMembers;
            }
            else
            {
                // Editor fallback
                var fm = FindFirstObjectByType<FamilyManager>();
                if (fm != null && fm.DefaultFamilyProfile != null)
                    defs = fm.DefaultFamilyProfile.DefaultFamilyMembers;
            }

            for (int i = 0; i < familyBodySlots.Length; i++)
            {
                if (familyBodySlots[i] == null) continue;

                Sprite body = null;

                // Try runtime family first
                if (runtimeFamily != null && i < runtimeFamily.Count)
                    body = FindBodyImage(runtimeFamily[i].Name);

                // Fallback to definition
                if (body == null && defs != null && i < defs.Count && defs[i] != null)
                    body = defs[i].BodyImage;

                if (body != null)
                {
                    familyBodySlots[i].sprite = body;
                    familyBodySlots[i].color = Color.white;
                    familyBodySlots[i].preserveAspect = true;
                    familyBodySlots[i].gameObject.SetActive(true);
                }
                else
                {
                    familyBodySlots[i].gameObject.SetActive(false);
                }
            }

            if (enableDebugLogs) Debug.Log("[GameplayHudUI] Family bodies refreshed.");
        }

        // =====================================================================
        //  Selection
        // =====================================================================
        private void SelectCharacter(int idx)
        {
            if (FamilyManager.Instance == null) return;
            var fam = FamilyManager.Instance.FamilyMembers;
            if (idx < 0 || idx >= fam.Count) return;
            selectedIndex     = idx;
            selectedCharacter = fam[idx];
            OnCharacterSelected?.Invoke(selectedCharacter);

            // highlight border
            for (int i = 0; i < thumbs.Count; i++)
            {
                if (thumbs[i].border != null)
                {
                    if (portraitFrameSprite != null)
                        thumbs[i].border.color = (i == idx) ? new Color(1f, 0.85f, 0.4f, 1f) : Color.white;
                    else
                        thumbs[i].border.color = (i == idx) ? new Color(1f, 1f, 1f, 0.15f) : Color.clear;
                }
            }

            UpdateDetailCard();
        }

        private void CyclePrevCharacter()
        {
            if (thumbs.Count == 0) return;
            int newIdx = selectedIndex - 1;
            if (newIdx < 0) newIdx = thumbs.Count - 1;
            SelectCharacter(newIdx);
            OnPrevCharacterClicked?.Invoke();
        }

        private void CycleNextCharacter()
        {
            if (thumbs.Count == 0) return;
            int newIdx = selectedIndex + 1;
            if (newIdx >= thumbs.Count) newIdx = 0;
            SelectCharacter(newIdx);
            OnNextCharacterClicked?.Invoke();
        }

        // =====================================================================
        //  Live Refresh  (called every LateUpdate while visible)
        // =====================================================================
        private void RefreshLiveData()
        {
            // thumbnail bars
            foreach (var t in thumbs)
            {
                if (t.character == null) continue;
                if (t.healthFill != null) t.healthFill.fillAmount = t.character.Health / 100f;
                if (t.hungerFill != null) t.hungerFill.fillAmount = t.character.Hunger / 100f;
            }

            UpdateDetailCard();
            UpdateDetailCard();
            UpdateResourceDisplay();

            if (enableDebugLogs && UnityEngine.Time.time > nextLogTime && thumbs.Count > 0 && thumbs[0].character != null)
            {
                nextLogTime = UnityEngine.Time.time + 2f;
                var c = thumbs[0].character;
                Debug.Log($"[GameplayHudUI] RefreshLiveData: FirstChar={c.Name} HP={c.Health} Fill={c.Health/100f}. DetailChar={selectedCharacter?.Name} HP={selectedCharacter?.Health}");
            }
        }

        private float nextLogTime;

        // =====================================================================
        //  Public API — for game logic to read/drive the HUD
        // =====================================================================

        /// <summary>Currently selected character data (null if none selected).</summary>
        public CharacterData SelectedCharacter => selectedCharacter;

        /// <summary>Index of currently selected character in the family list (-1 if none).</summary>
        public int SelectedCharacterIndex => selectedIndex;

        /// <summary>Select a character by family list index (updates thumbnail highlight + detail card).</summary>
        public void SelectCharacterByIndex(int index) => SelectCharacter(index);

        /// <summary>Number of character thumbnails currently displayed.</summary>
        public int CharacterCount => thumbs.Count;

        /// <summary>Whether the HUD canvas is currently active/visible.</summary>
        public bool IsVisible => canvasRoot != null && canvasRoot.activeSelf;

        // ---- Day / Time / Temp setters & getters ----------------------------

        public void UpdateDayDisplay()
        {
            if (GameManager.Instance != null && dayText != null)
                dayText.text = $"DAY {GameManager.Instance.CurrentDay}";
        }

        /// <summary>
        /// Full refresh of the entire HUD. 
        /// Called by GameFlowController after game state changes (e.g. New Game, Advance Day).
        /// </summary>
        public void RefreshAll()
        {
            if (enableDebugLogs) Debug.Log("[GameplayHudUI] RefreshAll() called.");
            
            PopulateCharacterList();
            RefreshFamilyBodies();
            UpdateResourceDisplay();
            UpdateDayDisplay();

            // Ensure detail card is consistent if selection was lost or changed
            if (selectedCharacter == null && thumbs.Count > 0)
            {
                SelectCharacter(0);
            }
            else if (selectedCharacter != null)
            {
                // re-select to refresh highlighting
                SelectCharacter(selectedIndex);
            }
        }

        /// <summary>Set the day text directly (e.g. "DAY 5").</summary>
        public void SetDayText(string text)   { if (dayText != null) dayText.text = text; }

        public void SetTime(string t)         { if (timeText != null)        timeText.text = t; }
        public void SetTemperature(string t)   { if (temperatureText != null) temperatureText.text = t; }

        // ---- Resource count setters (bypass auto-inventory scan) ------------

        /// <summary>Manually set a resource count display (bypasses InventoryManager scan).</summary>
        public void SetResourceCount(ItemType type, int count)
        {
            switch (type)
            {
                case ItemType.Food:  if (foodCountText  != null) foodCountText.text  = count.ToString(); break;
                case ItemType.Water: if (waterCountText != null) waterCountText.text = count.ToString(); break;
                case ItemType.Meds:  if (medsCountText  != null) medsCountText.text  = count.ToString(); break;
                case ItemType.Tools: if (toolsCountText != null) toolsCountText.text = count.ToString(); break;
                case ItemType.Junk:  if (junkCountText  != null) junkCountText.text  = count.ToString(); break;
            }
        }

        // ---- Detail card direct access --------------------------------------

        /// <summary>Set the detail card portrait sprite directly.</summary>
        public void SetDetailPortrait(Sprite portrait)
        {
            if (detailPortrait == null) return;
            detailPortrait.sprite = portrait;
            detailPortrait.color  = portrait != null ? Color.white : new Color(0.35f, 0.35f, 0.35f, 0.5f);
        }

        /// <summary>Set detail card stat bars directly (0-1 range).</summary>
        public void SetDetailStats(float healthNorm, float hungerNorm, float thirstNorm, float sanityNorm = -1f)
        {
            if (detailHealthFill != null) detailHealthFill.fillAmount = Mathf.Clamp01(healthNorm);
            if (detailHealthText != null) detailHealthText.text = Mathf.RoundToInt(healthNorm * 100).ToString();

            if (detailHungerFill != null) detailHungerFill.fillAmount = Mathf.Clamp01(hungerNorm);
            if (detailHungerText != null) detailHungerText.text = Mathf.RoundToInt(hungerNorm * 100).ToString();

            if (detailThirstFill != null) detailThirstFill.fillAmount = Mathf.Clamp01(thirstNorm);
            if (detailThirstText != null) detailThirstText.text = Mathf.RoundToInt(thirstNorm * 100).ToString();

            if (sanityNorm >= 0f)
            {
                if (detailSanityFill != null) detailSanityFill.fillAmount = Mathf.Clamp01(sanityNorm);
                if (detailSanityText != null) detailSanityText.text = Mathf.RoundToInt(sanityNorm * 100).ToString();
            }
        }

        // ---- Button enable/disable -----------------------------------------

        /// <summary>Enable or disable the End Day button.</summary>
        public void SetEndDayButtonInteractable(bool interactable)
        {
            if (panel == null) return;
            var btn = UIBuilderUtils.FindButton(panel, "EndDayBtn");
            if (btn != null) btn.interactable = interactable;
        }

        /// <summary>Enable or disable the Our Things button.</summary>
        public void SetOurThingsButtonInteractable(bool interactable)
        {
            if (panel == null) return;
            var btn = UIBuilderUtils.FindButton(panel, "OurThingsBtn");
            if (btn != null) btn.interactable = interactable;
        }

        /// <summary>Enable or disable the prev/next character arrows.</summary>
        public void SetCharacterArrowsInteractable(bool interactable)
        {
            if (prevCharButton != null) prevCharButton.interactable = interactable;
            if (nextCharButton != null) nextCharButton.interactable = interactable;
        }

        // =====================================================================
        //  Internal updates
        // =====================================================================
        private void UpdateDetailCard()
        {
            if (selectedCharacter == null) return;

            if (detailNameText != null) detailNameText.text = selectedCharacter.Name;
            if (detailPortrait != null)
            {
                var sp = FindPortrait(selectedCharacter.Name);
                if (sp != null) { detailPortrait.sprite = sp; detailPortrait.color = Color.white; }
            }
            if (detailHealthFill != null) detailHealthFill.fillAmount = selectedCharacter.Health / 100f;
            if (detailHealthText != null) detailHealthText.text = Mathf.RoundToInt(selectedCharacter.Health).ToString();

            if (detailHungerFill != null) detailHungerFill.fillAmount = selectedCharacter.Hunger / 100f;
            if (detailHungerText != null) detailHungerText.text = Mathf.RoundToInt(selectedCharacter.Hunger).ToString();

            if (detailThirstFill != null) detailThirstFill.fillAmount = selectedCharacter.Thirst / 100f;
            if (detailThirstText != null) detailThirstText.text = Mathf.RoundToInt(selectedCharacter.Thirst).ToString();

            if (detailSanityFill != null) detailSanityFill.fillAmount = selectedCharacter.Sanity / 100f;
            if (detailSanityText != null) detailSanityText.text = Mathf.RoundToInt(selectedCharacter.Sanity).ToString();
        }

        private void UpdateResourceDisplay()
        {
            if (InventoryManager.Instance == null) return;
            SetResCount(foodCountText,  ItemType.Food);
            SetResCount(waterCountText, ItemType.Water);
            SetResCount(medsCountText,  ItemType.Meds);
            SetResCount(toolsCountText, ItemType.Tools);
            SetResCount(junkCountText,  ItemType.Junk);
        }

        private void SetResCount(TextMeshProUGUI txt, ItemType type)
        {
            if (txt == null) return;
            int n = 0;
            if (InventoryManager.Instance != null && ItemManager.Instance != null)
                foreach (var s in InventoryManager.Instance.Items)
                { var d = ItemManager.Instance.GetItem(s.ItemId); if (d != null && d.Type == type) n += s.Quantity; }
            txt.text = n.ToString();
        }

        // =====================================================================
        //  Debug
        // =====================================================================
        #if ODIN_INSPECTOR
        [TitleGroup("Debug Controls")]
        [Button("Show",  ButtonSizes.Medium)] private void Debug_Show()    => Show();
        [Button("Hide",  ButtonSizes.Medium)] private void Debug_Hide()    => Hide();
        [Button("Refresh Characters", ButtonSizes.Medium)] private void Debug_Refresh() => PopulateCharacterList();
        [Button("Refresh Family Bodies", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void Debug_RefreshBodies() => RefreshFamilyBodies();
        #endif
    }
}
