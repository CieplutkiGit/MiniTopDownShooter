using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Unified design system for the minimalist geometric shooter UI.
    /// Standardizes color palette, typography hierarchy, button states, spacing, and layouts.
    /// </summary>
    public static class UITheme
    {
        // =========================================================================
        // Geometric Shooter Color Palette
        // =========================================================================
        public static readonly Color ColorBackgroundDeep    = new Color(0.045f, 0.065f, 0.095f, 0.96f);
        public static readonly Color ColorPanelSurface      = new Color(0.080f, 0.110f, 0.160f, 0.92f);
        public static readonly Color ColorCardSurface       = new Color(0.120f, 0.160f, 0.220f, 0.90f);
        public static readonly Color ColorCardSurfaceAlt    = new Color(0.150f, 0.200f, 0.270f, 0.85f);
        public static readonly Color ColorBorderSubtle      = new Color(0.200f, 0.260f, 0.350f, 0.60f);

        // Accents
        public static readonly Color ColorAccentCyan        = new Color(0.00f, 0.88f, 0.78f, 1f);  // Primary CTA, selected state
        public static readonly Color ColorAccentAmber       = new Color(1.00f, 0.75f, 0.20f, 1f);  // High scores, warnings
        public static readonly Color ColorAccentRed         = new Color(0.94f, 0.25f, 0.30f, 1f);  // Negative delta, errors, dead
        public static readonly Color ColorAccentGreen       = new Color(0.18f, 0.80f, 0.44f, 1f);  // Positive delta, health

        // Text
        public static readonly Color ColorTextPrimary       = new Color(1.00f, 1.00f, 1.00f, 1f);
        public static readonly Color ColorTextSecondary     = new Color(0.60f, 0.68f, 0.78f, 1f);
        public static readonly Color ColorTextMuted         = new Color(0.40f, 0.47f, 0.56f, 1f);
        public static readonly Color ColorTextDark          = new Color(0.04f, 0.08f, 0.10f, 1f);

        // Buttons
        public static readonly Color ColorButtonNormal      = new Color(0.14f, 0.18f, 0.25f, 1f);
        public static readonly Color ColorButtonHighlight   = new Color(0.22f, 0.28f, 0.38f, 1f);
        public static readonly Color ColorButtonPressed     = new Color(0.09f, 0.12f, 0.17f, 1f);
        public static readonly Color ColorButtonDisabled    = new Color(0.10f, 0.12f, 0.15f, 0.5f);

        // =========================================================================
        // Typography Configuration
        // =========================================================================
        public const float FontSizeHeaderLarge = 24f;
        public const float FontSizeHeaderMedium = 18f;
        public const float FontSizeBody = 14f;
        public const float FontSizeCaption = 11f;

        public static void ApplyTextStyle(
            TextMeshProUGUI tmp,
            float fontSize = FontSizeBody,
            FontStyles style = FontStyles.Normal,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center,
            Color? color = null)
        {
            if (tmp == null) return;
            if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = color ?? ColorTextPrimary;
            tmp.raycastTarget = false;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Mathf.Max(9f, fontSize * 0.7f);
            tmp.fontSizeMax = fontSize;
        }

        public static void ApplyButtonColors(Button button, Color baseColor, Color? pressedColor = null, Color? highlightColor = null)
        {
            if (button == null) return;
            var colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = highlightColor ?? Color.Lerp(baseColor, Color.white, 0.25f);
            colors.pressedColor = pressedColor ?? Color.Lerp(baseColor, Color.black, 0.30f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = ColorButtonDisabled;
            button.colors = colors;
        }

        // =========================================================================
        // Layout Utilities
        // =========================================================================
        public static RectTransform CreateHorizontalScrollView(
            Transform parent,
            string name,
            Vector2 size,
            Vector2 anchoredPos,
            Vector2 anchorMin,
            Vector2 anchorMax,
            out RectTransform content)
        {
            var scrollRoot = new GameObject(name, typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollRoot.transform.SetParent(parent, false);

            var rootRt = scrollRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = anchorMin;
            rootRt.anchorMax = anchorMax;
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = anchoredPos;
            rootRt.sizeDelta = size;

            var bgImg = scrollRoot.GetComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.01f); // Transparent raycast blocker for scrolling

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewport.transform.SetParent(scrollRoot.transform, false);
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero;

            var mask = viewport.GetComponent<Mask>();
            mask.showMaskGraphic = false;
            viewport.GetComponent<Image>().color = Color.white;

            var contentObj = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(viewport.transform, false);
            content = contentObj.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 0.5f);
            content.anchorMax = new Vector2(0f, 0.5f);
            content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, size.y);

            var layout = contentObj.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 8f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = contentObj.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var scrollRect = scrollRoot.GetComponent<ScrollRect>();
            scrollRect.viewport = vpRt;
            scrollRect.content = content;
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 15f;

            return rootRt;
        }
    }
}
