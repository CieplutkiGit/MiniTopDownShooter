using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class WeaponHUD : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private WeaponLoadout _loadoutRef;

        [Header("UI Elements")]
        [SerializeField] private TMP_Text _weaponNameLabel;
        [SerializeField] private TMP_Text _ammoLabel;
        [SerializeField] private TMP_Text _reloadStatusLabel;
        [SerializeField] private Slider _reloadProgressSlider;
        [SerializeField] private TMP_Text _loadoutLabel;

        private WeaponLoadout _loadout;
        private Gun _activeGun;
        private bool _isSubscribedLoadout;

        public void Initialize(WeaponLoadout loadout)
        {
            UnsubscribeLoadout();
            UnbindGun();

            _loadoutRef = loadout;
            _loadout = loadout;

            if (isActiveAndEnabled)
            {
                SubscribeLoadout();
                if (_loadout != null && _loadout.ActiveGun != null)
                {
                    HandleWeaponEquipped(_loadout.ActiveGun, _loadout.ActiveIndex);
                }
            }
        }

        private void Awake()
        {
            if (_loadoutRef == null)
            {
                _loadoutRef = FindFirstObjectByType<WeaponLoadout>();
            }

            _loadout = _loadoutRef;
        }

        private void OnEnable()
        {
            SubscribeLoadout();
            if (_loadout != null && _loadout.ActiveGun != null)
            {
                HandleWeaponEquipped(_loadout.ActiveGun, _loadout.ActiveIndex);
            }
        }

        private void OnDisable()
        {
            UnsubscribeLoadout();
            UnbindGun();
        }

        private void SubscribeLoadout()
        {
            if (_isSubscribedLoadout || _loadout == null)
            {
                return;
            }

            _loadout.WeaponEquipped += HandleWeaponEquipped;
            _isSubscribedLoadout = true;
        }

        private void UnsubscribeLoadout()
        {
            if (!_isSubscribedLoadout || _loadout == null)
            {
                return;
            }

            _loadout.WeaponEquipped -= HandleWeaponEquipped;
            _isSubscribedLoadout = false;
        }

        private void Update()
        {
            if (_activeGun != null && _activeGun.IsReloading)
            {
                if (_reloadStatusLabel != null)
                {
                    _reloadStatusLabel.text = "Reloading...";
                }

                if (_reloadProgressSlider != null)
                {
                    if (!_reloadProgressSlider.gameObject.activeSelf)
                    {
                        _reloadProgressSlider.gameObject.SetActive(true);
                    }
                    _reloadProgressSlider.value = _activeGun.ReloadProgress;
                }
            }
            else
            {
                if (_reloadStatusLabel != null)
                {
                    _reloadStatusLabel.text = "";
                }

                if (_reloadProgressSlider != null && _reloadProgressSlider.gameObject.activeSelf)
                {
                    _reloadProgressSlider.gameObject.SetActive(false);
                }
            }

            if (_loadoutLabel != null && _loadout != null && _loadout.Count > 0)
            {
                _loadoutLabel.text = $"Weapon {_loadout.ActiveIndex + 1}/{_loadout.Count}";
            }
        }

        private void HandleWeaponEquipped(Gun gun, int slotIndex)
        {
            UnbindGun();

            _activeGun = gun;

            if (_activeGun != null)
            {
                _activeGun.AmmoChanged += HandleAmmoChanged;
                _activeGun.ReloadStarted += HandleReloadStarted;
                _activeGun.ReloadCompleted += HandleReloadCompleted;
                _activeGun.ReloadCanceled += HandleReloadCanceled;

                if (_weaponNameLabel != null)
                {
                    _weaponNameLabel.text = _activeGun.Definition != null
                        ? _activeGun.Definition.name
                        : _activeGun.gameObject.name;
                }

                UpdateAmmoDisplay(_activeGun.AmmoInMagazine, _activeGun.ReserveAmmo);
            }
        }

        private void UnbindGun()
        {
            if (_activeGun != null)
            {
                _activeGun.AmmoChanged -= HandleAmmoChanged;
                _activeGun.ReloadStarted -= HandleReloadStarted;
                _activeGun.ReloadCompleted -= HandleReloadCompleted;
                _activeGun.ReloadCanceled -= HandleReloadCanceled;
                _activeGun = null;
            }
        }

        private void HandleAmmoChanged(int inMagazine, int reserve)
        {
            UpdateAmmoDisplay(inMagazine, reserve);
        }

        private void HandleReloadStarted()
        {
            if (_reloadStatusLabel != null)
            {
                _reloadStatusLabel.text = "Reloading...";
            }
        }

        private void HandleReloadCompleted()
        {
            if (_reloadStatusLabel != null)
            {
                _reloadStatusLabel.text = "";
            }
        }

        private void HandleReloadCanceled()
        {
            if (_reloadStatusLabel != null)
            {
                _reloadStatusLabel.text = "";
            }
        }

        private void UpdateAmmoDisplay(int inMagazine, int reserve)
        {
            if (_ammoLabel == null)
            {
                return;
            }

            if (_activeGun != null && _activeGun.InfiniteAmmo)
            {
                _ammoLabel.text = $"{inMagazine} / \u221E";
            }
            else
            {
                _ammoLabel.text = $"{inMagazine} / {reserve}";
            }
        }
    }
}
