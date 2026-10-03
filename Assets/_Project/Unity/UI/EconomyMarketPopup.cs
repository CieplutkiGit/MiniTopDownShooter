using Application.Economy;
using Application.Weapons;
using Game.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Hosts the scrollable market modal used by the lobby and workshop.</summary>
    public sealed class EconomyMarketPopup : MonoBehaviour
    {
        private EconomyPanel _panel;
        private GameObject _overlay;
        private Button _toggle;
        private TMPro.TMP_Text _toggleLabel;
        private UnityEconomyService _service;

        public EconomyPanel Panel => _panel;

        public static EconomyMarketPopup Attach(Transform root, Button toggle, TMPro.TMP_Text label,
            IEconomyService service, IWeaponCatalog catalog)
        {
            if (root == null || toggle == null) return null;
            var hostObject = new GameObject("EconomyMarketPopup", typeof(RectTransform));
            hostObject.transform.SetParent(root, false);
            var host = hostObject.AddComponent<EconomyMarketPopup>();
            host.Initialize(toggle, label, service, catalog);
            return host;
        }

        private void Initialize(Button toggle, TMPro.TMP_Text label, IEconomyService service, IWeaponCatalog catalog)
        {
            _toggle = toggle;
            _toggleLabel = label;
            _service = service as UnityEconomyService ?? UnityEconomyService.Instance;
            _overlay = BuildOverlay(catalog);
            _toggle.onClick.AddListener(Toggle);
            if (_service != null) _service.OnEconomyStateChanged += RefreshLabel;
            RefreshLabel();
        }

        private void OnDestroy()
        {
            if (_toggle != null) _toggle.onClick.RemoveListener(Toggle);
            if (_service != null) _service.OnEconomyStateChanged -= RefreshLabel;
        }

        public void Toggle()
        {
            if (_overlay != null) _overlay.SetActive(!_overlay.activeSelf);
        }

        private void RefreshLabel()
        {
            if (_toggleLabel != null)
                _toggleLabel.text = _service != null ? $"MARKET  ·  {_service.Coins:N0} CR" : "MARKET";
        }

        private GameObject BuildOverlay(IWeaponCatalog catalog)
        {
            var overlay = new GameObject("MarketOverlay", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(transform.parent, false);
            var overlayRt = overlay.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.52f);

            var frame = new GameObject("MarketFrame", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(overlay.transform, false);
            var frameRt = frame.GetComponent<RectTransform>();
            frameRt.anchorMin = new Vector2(1f, 0f);
            frameRt.anchorMax = new Vector2(1f, 1f);
            frameRt.pivot = new Vector2(1f, 0.5f);
            frameRt.offsetMin = new Vector2(-330f, 92f);
            frameRt.offsetMax = new Vector2(-12f, -82f);
            frame.GetComponent<Image>().color = UITheme.ColorPanelSurface;

            var close = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            close.transform.SetParent(frame.transform, false);
            var closeRt = close.GetComponent<RectTransform>();
            closeRt.anchorMin = closeRt.anchorMax = Vector2.one;
            closeRt.pivot = Vector2.one;
            closeRt.anchoredPosition = new Vector2(-10f, -10f);
            closeRt.sizeDelta = new Vector2(72f, 34f);
            close.GetComponent<Image>().color = UITheme.ColorButtonNormal;
            var closeButton = close.GetComponent<Button>();
            UITheme.ApplyButtonColors(closeButton, UITheme.ColorButtonNormal);
            closeButton.onClick.AddListener(Toggle);
            var closeText = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            closeText.transform.SetParent(close.transform, false);
            var closeTextRt = closeText.GetComponent<RectTransform>();
            closeTextRt.anchorMin = Vector2.zero;
            closeTextRt.anchorMax = Vector2.one;
            closeTextRt.offsetMin = closeTextRt.offsetMax = Vector2.zero;
            var closeTmp = closeText.GetComponent<TMPro.TextMeshProUGUI>();
            closeTmp.text = "CLOSE";
            closeTmp.fontSize = 12f;
            closeTmp.alignment = TMPro.TextAlignmentOptions.Center;
            closeTmp.color = UITheme.ColorTextPrimary;

            var scrollObject = new GameObject("MarketScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
            scrollObject.transform.SetParent(frame.transform, false);
            var scrollRt = scrollObject.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(12f, 12f);
            scrollRt.offsetMax = new Vector2(-12f, -56f);
            scrollObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            scrollObject.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("MarketContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(scrollObject.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = Vector2.zero;
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = scrollRt;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            _panel = EconomyPanel.Create(content.transform, _service, catalog);
            overlay.SetActive(false);
            return overlay;
        }
    }
}
