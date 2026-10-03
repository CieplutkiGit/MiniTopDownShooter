using System;
using System.Collections.Generic;
using Application.Weapons;
using Application.Workshop;
using Game;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.Workshop
{
    public class WorkshopPartsUI : MonoBehaviour
    {
        [Header("Controllers & References")]
        [SerializeField] private WorkshopUIController _controller;
        [SerializeField] private WeaponLoadout _loadout;

        [Header("Roots")]
        [SerializeField] private RectTransform _slotsRoot;
        [SerializeField] private RectTransform _partsRoot;
        [SerializeField] private RectTransform _weaponsRoot;

        private IWeaponWorkshopSession _boundSession;
        private string _lastSelectedSlot;

        public WorkshopUIController Controller
        {
            get => _controller;
            set => _controller = value;
        }

        public RectTransform SlotsRoot
        {
            get => _slotsRoot;
            set => _slotsRoot = value;
        }

        public RectTransform PartsRoot
        {
            get => _partsRoot;
            set => _partsRoot = value;
        }

        public WeaponLoadout Loadout
        {
            get => _loadout;
            set => _loadout = value;
        }

        public RectTransform WeaponsRoot
        {
            get => _weaponsRoot;
            set => _weaponsRoot = value;
        }

        public void Initialize(
            WorkshopUIController controller,
            RectTransform slotsRoot,
            RectTransform partsRoot,
            WeaponLoadout loadout = null,
            RectTransform weaponsRoot = null)
        {
            UnbindSession();
            _controller = controller;
            _slotsRoot = slotsRoot;
            _partsRoot = partsRoot;
            _loadout = loadout;
            _weaponsRoot = weaponsRoot;
            UpdateSessionBinding();
        }

        private void Awake()
        {
            if (_controller == null)
            {
                _controller = GetComponent<WorkshopUIController>();
            }

            if (_loadout == null)
            {
                _loadout = FindFirstObjectByType<WeaponLoadout>();
            }
        }

        private void OnEnable()
        {
            UpdateSessionBinding();
        }

        private void OnDisable()
        {
            UnbindSession();
            ClearAllButtons();
        }

        private void OnDestroy()
        {
            UnbindSession();
            ClearAllButtons();
        }

        private void Update()
        {
            IWeaponWorkshopSession currentSession = _controller != null ? _controller.Session : null;

            if (currentSession != _boundSession)
            {
                UpdateSessionBinding();
                return;
            }

            if (_boundSession != null && _controller != null)
            {
                string currentSlot = _controller.SelectedSlot;
                if (!string.Equals(currentSlot, _lastSelectedSlot, StringComparison.Ordinal))
                {
                    _lastSelectedSlot = currentSlot;
                    RefreshPartChoices();
                }
            }
        }

        private void UpdateSessionBinding()
        {
            IWeaponWorkshopSession currentSession = _controller != null ? _controller.Session : null;

            if (_boundSession != currentSession)
            {
                UnbindSession();
                _boundSession = currentSession;

                if (_boundSession != null)
                {
                    _boundSession.SessionChanged += HandleSessionChanged;
                    _lastSelectedSlot = _controller != null ? _controller.SelectedSlot : null;
                    RefreshAllChoices();
                }
                else
                {
                    ClearAllButtons();
                }
            }
        }

        private void UnbindSession()
        {
            if (_boundSession != null)
            {
                _boundSession.SessionChanged -= HandleSessionChanged;
                _boundSession = null;
            }

            _lastSelectedSlot = null;
        }

        private void HandleSessionChanged(IWeaponWorkshopSession session)
        {
            if (_controller != null)
            {
                _lastSelectedSlot = _controller.SelectedSlot;
            }

            RefreshAllChoices();
        }

        public void RefreshAllChoices()
        {
            if (_controller == null || _controller.Session == null)
            {
                ClearAllButtons();
                return;
            }

            RefreshSlotChoices();
            RefreshPartChoices();
            RefreshWeaponChoices();
        }

        public void RefreshSlotChoices()
        {
            ClearButtons(_slotsRoot);

            if (_controller == null || _controller.Session == null || _slotsRoot == null)
            {
                return;
            }

            IReadOnlyList<string> slots = _controller.AvailableSlots;
            if (slots == null || slots.Count == 0)
            {
                return;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                string slotId = slots[i];
                if (string.IsNullOrEmpty(slotId))
                {
                    continue;
                }

                CreateButton(_slotsRoot, slotId, () =>
                {
                    if (_controller != null)
                    {
                        _controller.SelectSlot(slotId);
                        _lastSelectedSlot = slotId;
                        RefreshPartChoices();
                    }
                });
            }
        }

        public void RefreshPartChoices()
        {
            ClearButtons(_partsRoot);

            if (_controller == null || _controller.Session == null || _partsRoot == null)
            {
                return;
            }

            IReadOnlyList<WeaponPartSpec> parts = _controller.AvailableParts;
            if (parts == null || parts.Count == 0)
            {
                return;
            }

            for (int i = 0; i < parts.Count; i++)
            {
                WeaponPartSpec part = parts[i];
                if (part == null)
                {
                    continue;
                }

                string partId = part.PartId;
                string title = !string.IsNullOrEmpty(part.DisplayName) ? part.DisplayName : partId;

                CreateButton(_partsRoot, title, () =>
                {
                    if (_controller != null)
                    {
                        _controller.SelectPart(partId);
                    }
                });
            }
        }

        public void RefreshWeaponChoices()
        {
            ClearButtons(_weaponsRoot);

            if (_controller == null || _controller.Session == null || _weaponsRoot == null || _loadout == null)
            {
                return;
            }

            IReadOnlyList<Gun> weapons = _loadout.Weapons;
            if (weapons == null || weapons.Count == 0)
            {
                return;
            }

            for (int i = 0; i < weapons.Count; i++)
            {
                int index = i;
                Gun weapon = weapons[i];
                if (weapon == null)
                {
                    continue;
                }

                string weaponTitle = !string.IsNullOrEmpty(weapon.WeaponId) ? weapon.WeaponId : weapon.name;
                if (weapon.Definition != null && !string.IsNullOrEmpty(weapon.Definition.WeaponId))
                {
                    weaponTitle = weapon.Definition.WeaponId;
                }

                CreateButton(_weaponsRoot, weaponTitle, () =>
                {
                    if (_loadout != null)
                    {
                        _loadout.EquipSlot(index);
                    }
                });
            }
        }

        public void ClearAllButtons()
        {
            ClearButtons(_slotsRoot);
            ClearButtons(_partsRoot);
            ClearButtons(_weaponsRoot);
        }

        private void ClearButtons(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            int count = root.childCount;
            if (count == 0)
            {
                return;
            }

            var snapshot = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                snapshot[i] = root.GetChild(i).gameObject;
            }

            for (int i = snapshot.Length - 1; i >= 0; i--)
            {
                GameObject go = snapshot[i];
                if (go != null)
                {
                    if (UnityEngine.Application.isPlaying)
                    {
                        Destroy(go);
                    }
                    else
                    {
                        DestroyImmediate(go);
                    }
                }
            }
        }

        private GameObject CreateButton(RectTransform parent, string label, UnityAction onClick)
        {
            GameObject buttonObj = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160f, 60f);

            Image image = buttonObj.GetComponent<Image>();
            image.color = new Color(0.22f, 0.24f, 0.28f, 1f);
            image.raycastTarget = true;

            Button button = buttonObj.GetComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.22f, 0.24f, 0.28f, 1f);
            colors.highlightedColor = new Color(0.32f, 0.36f, 0.42f, 1f);
            colors.pressedColor = new Color(0.15f, 0.17f, 0.20f, 1f);
            colors.selectedColor = new Color(0.28f, 0.42f, 0.58f, 1f);
            button.colors = colors;

            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            LayoutElement layout = buttonObj.GetComponent<LayoutElement>();
            layout.minHeight = 60f;
            layout.preferredHeight = 60f;
            layout.minWidth = 120f;
            layout.preferredWidth = 160f;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 4f);
            textRect.offsetMax = new Vector2(-8f, -4f);

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }
            tmp.text = label;
            tmp.fontSize = 16f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return buttonObj;
        }

        public void Exit()
        {
            if (_controller != null)
            {
                _controller.Exit();
            }
        }
    }
}
