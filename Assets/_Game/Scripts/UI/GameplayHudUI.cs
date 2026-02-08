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
        [SerializeField] private bool enableDebugLogs = false;

        #if ODIN_INSPECTOR
        [Title("UI References")]
        #endif
        [SerializeField] private PlayerActionUI playerActionUI;

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

        // ---- runtime references (found after auto-setup or cached on Show) ----
        private TextMeshProUGUI dayText;
        private TextMeshProUGUI timeText;
        private TextMeshProUGUI temperatureText;
        private Transform characterListContainer;
        private readonly List<ThumbRefs> thumbs = new List<ThumbRefs>();

        // detail card
        private Image   detailPortrait;
        private Image   detailHealthFill;
        private Image   detailHungerFill;
        private Image   detailThirstFill;

        // resource texts
        private TextMeshProUGUI foodCountText;
        private TextMeshProUGUI waterCountText;
        private TextMeshProUGUI medsCountText;
        private TextMeshProUGUI toolsCountText;
        private TextMeshProUGUI junkCountText;

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

            if (enableDebugLogs) Debug.Log("[GameplayHudUI] Auto Setup complete.");
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }

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

            // DIARY — below END DAY, opens Player Action UI
            MakeActionBtn(parent, "DiaryBtn", "DIARY", iconDiary,
                new Vector2(0.005f, 0.78f), new Vector2(0.12f, 0.835f));
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
            // small portrait thumbnails stacked on left edge
            var col = MakeRect(parent, "CharacterList",
                new Vector2(0.005f, 0.10f), new Vector2(0.06f, 0.82f));

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

            // ---- portrait area (top ~60%) ----
            var pArea = MakeRect(card, "PortraitArea",
                new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.96f));

            // portrait frame
            var pFrame = pArea.gameObject.AddComponent<Image>();
            ApplySprite(pFrame, portraitFrameSprite, new Color(0.96f, 0.94f, 0.90f, 1f));

            // inner portrait image
            var pInner = MakeRect(pArea, "PortraitImage",
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f));
            detailPortrait = pInner.gameObject.AddComponent<Image>();
            detailPortrait.color = new Color(0.35f, 0.35f, 0.35f, 0.5f);
            detailPortrait.preserveAspect = true;

            // ---- stat bars (below portrait — thin lines) ----
            var statsArea = MakeRect(card, "StatsArea",
                new Vector2(0.12f, 0.18f), new Vector2(0.92f, 0.36f));

            var sVlg = statsArea.gameObject.AddComponent<VerticalLayoutGroup>();
            sVlg.childAlignment    = TextAnchor.UpperLeft;
            sVlg.spacing           = 2;
            sVlg.padding           = new RectOffset(0, 0, 0, 0);
            sVlg.childControlWidth = true;  sVlg.childControlHeight = true;
            sVlg.childForceExpandWidth = true; sVlg.childForceExpandHeight = true;

            detailHealthFill = MakeDetailBar(statsArea, "HealthBar", iconHeart, healthColor);
            detailHungerFill = MakeDetailBar(statsArea, "HungerBar", iconFood,  hungerColor);
            detailThirstFill = MakeDetailBar(statsArea, "ThirstBar", iconWater, thirstColor);

            // ---- status condition icons row (very bottom) ----
            var statusRow = MakeRect(card, "StatusRow",
                new Vector2(0.12f, 0.05f), new Vector2(0.92f, 0.16f));

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

        private Image MakeDetailBar(RectTransform parent, string name,
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
            bgRT.gameObject.AddComponent<Image>().color = barBgColor;

            // fill
            var fRT = MakeRect(bgRT, "Fill", Vector2.zero, Vector2.one);
            fRT.pivot = new Vector2(0, 0.5f);
            var fImg = fRT.gameObject.AddComponent<Image>();
            fImg.color      = fillColor;
            fImg.type       = Image.Type.Filled;
            fImg.fillMethod = Image.FillMethod.Horizontal;
            fImg.fillAmount = 0.75f;

            return fImg;
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

        /// <summary>Small arrow button inside the day strip for prev/next day.</summary>
        private void MakeArrowBtn(RectTransform parent, string name, Sprite arrowSprite,
            string fallbackChar, float size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(size, size);
            go.AddComponent<LayoutElement>().preferredWidth = size;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.05f); // very subtle bg
            bg.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            var c = btn.colors;
            c.highlightedColor = new Color(0.80f, 0.75f, 0.70f, 0.30f);
            c.pressedColor     = new Color(0.60f, 0.55f, 0.50f, 0.40f);
            btn.colors = c;

            if (arrowSprite != null)
            {
                var inner = MakeRect(go.GetComponent<RectTransform>(), "Arrow",
                    new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f));
                var img = inner.gameObject.AddComponent<Image>();
                img.sprite = arrowSprite; img.preserveAspect = true;
                img.color = textColor;
                img.raycastTarget = false;
            }
            else
            {
                var inner = MakeRect(go.GetComponent<RectTransform>(), "Arrow",
                    new Vector2(0f, 0f), new Vector2(1f, 1f));
                var tmp = inner.gameObject.AddComponent<TextMeshProUGUI>();
                tmp.text      = fallbackChar;
                tmp.fontSize  = 22;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color     = textColor;
                tmp.raycastTarget = false;
                if (titleFont != null) tmp.font = titleFont;
            }
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
            CacheReferences();
            WireButtons();
            PopulateCharacterList();
            UpdateDayDisplay();
            UpdateResourceDisplay();
            if (enableDebugLogs) Debug.Log("[GameplayHudUI] Shown.");
        }

        public void Hide()
        {
            if (canvasRoot != null) canvasRoot.SetActive(false);
        }

        // =====================================================================
        //  Cache references after AutoSetup (or on Show)
        // =====================================================================
        private void CacheReferences()
        {
            if (panel == null) return;

            var strip = panel.transform.Find("DayStrip");
            if (strip != null)
            {
                dayText         = strip.Find("Day/Text")?.GetComponent<TextMeshProUGUI>();
                timeText        = strip.Find("Clock/Text")?.GetComponent<TextMeshProUGUI>();
                temperatureText = strip.Find("Temp/Text")?.GetComponent<TextMeshProUGUI>();
            }

            characterListContainer = panel.transform.Find("CharacterList");

            var dc = panel.transform.Find("DetailCard");
            if (dc != null)
            {
                detailPortrait   = dc.Find("PortraitArea/PortraitImage")?.GetComponent<Image>();
                var sa = dc.Find("StatsArea");
                if (sa != null)
                {
                    detailHealthFill = sa.Find("HealthBar/BG/Fill")?.GetComponent<Image>();
                    detailHungerFill = sa.Find("HungerBar/BG/Fill")?.GetComponent<Image>();
                    detailThirstFill = sa.Find("ThirstBar/BG/Fill")?.GetComponent<Image>();
                }
            }

            var res = panel.transform.Find("Resources");
            if (res != null)
            {
                foodCountText  = res.Find("Food/Count")?.GetComponent<TextMeshProUGUI>();
                waterCountText = res.Find("Water/Count")?.GetComponent<TextMeshProUGUI>();
                medsCountText  = res.Find("Meds/Count")?.GetComponent<TextMeshProUGUI>();
                toolsCountText = res.Find("Tools/Count")?.GetComponent<TextMeshProUGUI>();
                junkCountText  = res.Find("Junk/Count")?.GetComponent<TextMeshProUGUI>();
            }
        }

        // =====================================================================
        //  Wire Buttons
        // =====================================================================
        private void WireButtons()
        {
            if (panel == null) return;

            Wire("OurThingsBtn", () => { OnOurThingsClicked?.Invoke(); });
            Wire("EndDayBtn",    () => { OnEndDayClicked?.Invoke(); });
            Wire("DiaryBtn",     () =>
            {
                OnDiaryClicked?.Invoke();
                if (playerActionUI != null) playerActionUI.Show();
            });
        }

        private void WireChild(Transform parent, string childName, UnityEngine.Events.UnityAction action)
        {
            var child = parent.Find(childName);
            if (child == null) return;
            var btn = child.GetComponent<Button>();
            if (btn != null) { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(action); }
        }

        private void Wire(string name, UnityEngine.Events.UnityAction action)
        {
            var btn = UIBuilderUtils.FindButton(panel, name);
            if (btn != null) { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(action); }
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
            UpdateResourceDisplay();
        }

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
        public void SetDetailStats(float healthNorm, float hungerNorm, float thirstNorm)
        {
            if (detailHealthFill != null) detailHealthFill.fillAmount = Mathf.Clamp01(healthNorm);
            if (detailHungerFill != null) detailHungerFill.fillAmount = Mathf.Clamp01(hungerNorm);
            if (detailThirstFill != null) detailThirstFill.fillAmount = Mathf.Clamp01(thirstNorm);
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
            if (panel == null) return;
            var strip = panel.transform.Find("DayStrip");
            if (strip == null) return;
            var prev = strip.Find("PrevDayBtn")?.GetComponent<Button>();
            var next = strip.Find("NextDayBtn")?.GetComponent<Button>();
            if (prev != null) prev.interactable = interactable;
            if (next != null) next.interactable = interactable;
        }

        // =====================================================================
        //  Internal updates
        // =====================================================================
        private void UpdateDetailCard()
        {
            if (selectedCharacter == null) return;

            if (detailPortrait != null)
            {
                var sp = FindPortrait(selectedCharacter.Name);
                if (sp != null) { detailPortrait.sprite = sp; detailPortrait.color = Color.white; }
            }
            if (detailHealthFill != null) detailHealthFill.fillAmount = selectedCharacter.Health / 100f;
            if (detailHungerFill != null) detailHungerFill.fillAmount = selectedCharacter.Hunger / 100f;
            if (detailThirstFill != null) detailThirstFill.fillAmount = selectedCharacter.Thirst / 100f;
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
        #endif
    }
}
