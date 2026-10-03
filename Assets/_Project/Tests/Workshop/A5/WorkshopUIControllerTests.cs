using System;
using System.Collections.Generic;
using System.Linq;
using Application;
using Application.Weapons;
using Application.Workshop;
using Game;
using Game.Workshop;
using NUnit.Framework;
using UnityEngine;

namespace MiniTopDownShooter.Tests.Workshop.A5
{
    public sealed class FakeWorkshopSession : IWeaponWorkshopSession
    {
        private readonly Dictionary<string, string> _draftParts = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _committedParts = new Dictionary<string, string>(StringComparer.Ordinal);

        public string WeaponId { get; set; } = WeaponWorkshopIds.Rifle;
        public IWeaponBuildTarget Target { get; set; }
        public bool HasUnappliedChanges { get; set; }
        public bool IsValid { get; set; } = true;
        public bool LastSaveFailed { get; set; }
        public string LastSaveError { get; set; } = string.Empty;

        public ResolvedWeaponStats DraftStats { get; set; }
        public BuildResolution DraftResolution { get; set; }

        public int ApplyCallCount { get; private set; }
        public int DiscardCallCount { get; private set; }
        public int SelectPartCallCount { get; private set; }

        public event Action<IWeaponWorkshopSession> SessionChanged;

        public FakeWorkshopSession()
        {
            _committedParts[WeaponWorkshopIds.RifleSlots.Barrel] = "rifle.barrel.standard";
            _committedParts[WeaponWorkshopIds.RifleSlots.Magazine] = "rifle.magazine.standard";
            _committedParts[WeaponWorkshopIds.RifleSlots.Grip] = "rifle.grip.standard";
            _committedParts[WeaponWorkshopIds.RifleSlots.Stock] = "rifle.stock.standard";

            foreach (var kvp in _committedParts)
            {
                _draftParts[kvp.Key] = kvp.Value;
            }

            DraftStats = CreateDefaultStats();
            DraftResolution = BuildResolution.Valid(DraftStats);
        }

        public WeaponBuild CommittedBuild => new WeaponBuild(WeaponId, _committedParts);
        public WeaponBuild DraftBuild => new WeaponBuild(WeaponId, _draftParts);

        public void SelectPart(string slotId, string partId)
        {
            SelectPartCallCount++;
            _draftParts[slotId] = partId;
            HasUnappliedChanges = true;
            SessionChanged?.Invoke(this);
        }

        public ApplyResult Apply()
        {
            ApplyCallCount++;
            if (!IsValid)
            {
                return ApplyResult.Failure("InvalidDraft", "Draft is invalid.");
            }

            foreach (var kvp in _draftParts)
            {
                _committedParts[kvp.Key] = kvp.Value;
            }

            HasUnappliedChanges = false;
            SessionChanged?.Invoke(this);
            return ApplyResult.Success();
        }

        public void Discard()
        {
            DiscardCallCount++;
            _draftParts.Clear();
            foreach (var kvp in _committedParts)
            {
                _draftParts[kvp.Key] = kvp.Value;
            }

            HasUnappliedChanges = false;
            SessionChanged?.Invoke(this);
        }

        public void BindTarget(IWeaponBuildTarget target)
        {
            Target = target;
        }

        public void SwitchWeapon(string weaponId, IWeaponBuildTarget target)
        {
            WeaponId = weaponId;
            Target = target;
            SessionChanged?.Invoke(this);
        }

        public void SetInvalid(string errorMessage)
        {
            IsValid = false;
            DraftResolution = BuildResolution.Invalid(new List<string> { errorMessage }, new List<string>());
            SessionChanged?.Invoke(this);
        }

        public void SetSaveFailure(string errorMessage)
        {
            LastSaveFailed = true;
            LastSaveError = errorMessage;
            SessionChanged?.Invoke(this);
        }

        private static ResolvedWeaponStats CreateDefaultStats()
        {
            return new ResolvedWeaponStats(
                damage: 15f,
                fireInterval: 0.11f,
                magazineCapacity: 30,
                maxReserveAmmo: 240,
                startingReserveAmmo: 120,
                reloadDuration: 1.55f,
                baseSpreadAngle: 1.2f,
                maxSpreadAngle: 7.0f,
                recoilPerShot: 0.55f,
                spreadRecoveryRate: 10f,
                range: 50f,
                projectileSpeed: 28f,
                projectileLifetime: 2.5f,
                pelletCount: 1,
                fireMode: WeaponFireMode.Automatic,
                burstCount: 3,
                burstInterval: 0.08f,
                aimTurnSpeed: 180f,
                deliveryMode: WeaponDeliveryMode.Projectile,
                infiniteAmmo: false,
                autoReloadOnEmpty: true,
                cancelReloadOnFire: true);
        }
    }

    public class WorkshopUIControllerTests
    {
        private GameObject _uiGo;
        private WorkshopUIController _uiController;
        private GameStateController _stateController;
        private FakeWeaponCatalog _catalog;
        private FakeWorkshopSession _session;
        private FakeWeaponPreviewView _previewView;

        [SetUp]
        public void SetUp()
        {
            _uiGo = new GameObject("WorkshopUITest");
            _stateController = _uiGo.AddComponent<GameStateController>();
            _uiController = _uiGo.AddComponent<WorkshopUIController>();

            _catalog = WeaponWorkshopTestFixtures.CreateCatalogWithRifle();
            _session = new FakeWorkshopSession();
            _previewView = new FakeWeaponPreviewView();

            _uiController.Initialize(_stateController);
            _uiController.Bind(_session, _previewView, _catalog);
        }

        [TearDown]
        public void TearDown()
        {
            if (_uiGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_uiGo);
            }
        }

        [Test]
        public void Bind_DisplaysWeaponName_AndPopulatesSlots()
        {
            Assert.AreEqual("Assault Rifle", _uiController.CurrentWeaponName);
            Assert.AreEqual(4, _uiController.AvailableSlots.Count);
            Assert.AreEqual(WeaponWorkshopIds.RifleSlots.Barrel, _uiController.AvailableSlots[0]);
            Assert.AreEqual(WeaponWorkshopIds.RifleSlots.Barrel, _uiController.SelectedSlot);
            Assert.IsNotNull(_previewView.DisplayedBuild);
        }

        [Test]
        public void ExplodeButton_TogglesExplodedView_OnPreview()
        {
            Assert.IsFalse(_uiController.IsExploded);
            Assert.IsFalse(_previewView.IsExploded);

            _uiController.ToggleExploded();

            Assert.IsTrue(_uiController.IsExploded);
            Assert.IsTrue(_previewView.IsExploded);

            _uiController.ToggleExploded();

            Assert.IsFalse(_uiController.IsExploded);
            Assert.IsFalse(_previewView.IsExploded);
        }

        [Test]
        public void SelectSlot_UpdatesPreview_AndAvailableParts()
        {
            _uiController.SelectSlot(WeaponWorkshopIds.RifleSlots.Magazine);

            Assert.AreEqual(WeaponWorkshopIds.RifleSlots.Magazine, _uiController.SelectedSlot);
            Assert.AreEqual(WeaponWorkshopIds.RifleSlots.Magazine, _previewView.SelectedSlot);
            Assert.AreEqual(3, _uiController.AvailableParts.Count); // standard, extended, drum
        }

        [Test]
        public void PreviewView_SlotSelected_UpdatesUISelectedSlot()
        {
            _previewView.TriggerSlotSelected(WeaponWorkshopIds.RifleSlots.Stock);

            Assert.AreEqual(WeaponWorkshopIds.RifleSlots.Stock, _uiController.SelectedSlot);
        }

        [Test]
        public void SelectPart_ModifiesDraft_UpdatesPreview_AndEnablesButtons()
        {
            _uiController.SelectSlot(WeaponWorkshopIds.RifleSlots.Barrel);

            Assert.IsFalse(_uiController.IsApplyInteractable);
            Assert.IsFalse(_uiController.IsDiscardInteractable);

            _uiController.SelectPart("rifle.barrel.long");

            Assert.AreEqual(1, _session.SelectPartCallCount);
            Assert.IsTrue(_session.HasUnappliedChanges);
            Assert.IsTrue(_uiController.IsApplyInteractable);
            Assert.IsTrue(_uiController.IsDiscardInteractable);
            Assert.AreEqual("rifle.barrel.long", _previewView.DisplayedBuild.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));
        }

        [Test]
        public void Apply_CallsSessionApply_AndDisablesButtons()
        {
            _uiController.SelectSlot(WeaponWorkshopIds.RifleSlots.Barrel);
            _uiController.SelectPart("rifle.barrel.long");

            Assert.IsTrue(_uiController.IsApplyInteractable);

            _uiController.Apply();

            Assert.AreEqual(1, _session.ApplyCallCount);
            Assert.IsFalse(_session.HasUnappliedChanges);
            Assert.IsFalse(_uiController.IsApplyInteractable);
            Assert.IsFalse(_uiController.IsDiscardInteractable);
        }

        [Test]
        public void Discard_CallsSessionDiscard_AndDisablesButtons()
        {
            _uiController.SelectSlot(WeaponWorkshopIds.RifleSlots.Barrel);
            _uiController.SelectPart("rifle.barrel.long");

            Assert.IsTrue(_uiController.IsDiscardInteractable);

            _uiController.Discard();

            Assert.AreEqual(1, _session.DiscardCallCount);
            Assert.IsFalse(_session.HasUnappliedChanges);
            Assert.IsFalse(_uiController.IsApplyInteractable);
            Assert.IsFalse(_uiController.IsDiscardInteractable);
        }

        [Test]
        public void ApplyButton_IsDisabled_WhenDraftIsInvalid()
        {
            _uiController.SelectSlot(WeaponWorkshopIds.RifleSlots.Barrel);
            _uiController.SelectPart("rifle.barrel.long");
            Assert.IsTrue(_uiController.IsApplyInteractable);

            _session.SetInvalid("Conflict between barrel and attachment.");

            Assert.IsFalse(_uiController.IsApplyInteractable);
            Assert.IsTrue(_uiController.IsDiscardInteractable);
            Assert.IsTrue(_uiController.HasError);
            StringAssert.Contains("Conflict between barrel and attachment.", _uiController.ErrorMessage);
        }

        [Test]
        public void ErrorMessage_DisplaysSaveFailure()
        {
            Assert.IsFalse(_uiController.HasError);

            _session.SetSaveFailure("Disk write error.");

            Assert.IsTrue(_uiController.HasError);
            StringAssert.Contains("Disk write error.", _uiController.ErrorMessage);
        }

        [Test]
        public void Exit_TransitionsToWorkshopRoaming()
        {
            _stateController.EnterWorkshopRoaming();
            _stateController.EnterWorkshopEditing();
            Assert.AreEqual(GameState.WorkshopEditing, _stateController.CurrentState);

            bool exitEventFired = false;
            _uiController.OnExitRequested += () => exitEventFired = true;

            _uiController.Exit();

            Assert.AreEqual(GameState.WorkshopRoaming, _stateController.CurrentState);
            Assert.IsTrue(exitEventFired);
        }

        [Test]
        public void StatsDiffs_ReflectsChanges_BetweenDraftAndBaseline()
        {
            Assert.Greater(_uiController.CurrentDiffs.Count, 0);

            // Create modified stats (e.g. higher damage)
            var modifiedStats = new ResolvedWeaponStats(
                damage: 18f, // 15 + 3
                fireInterval: 0.11f,
                magazineCapacity: 45, // 30 + 15
                maxReserveAmmo: 285,
                startingReserveAmmo: 120,
                reloadDuration: 1.95f, // 1.55 + 0.4
                baseSpreadAngle: 0.8f,
                maxSpreadAngle: 7.0f,
                recoilPerShot: 0.55f,
                spreadRecoveryRate: 10f,
                range: 65f, // 50 + 15
                projectileSpeed: 34f, // 28 + 6
                projectileLifetime: 2.5f,
                pelletCount: 1,
                fireMode: WeaponFireMode.Automatic,
                burstCount: 3,
                burstInterval: 0.08f,
                aimTurnSpeed: 160f, // 180 - 20
                deliveryMode: WeaponDeliveryMode.Projectile,
                infiniteAmmo: false,
                autoReloadOnEmpty: true,
                cancelReloadOnFire: true);

            _session.DraftStats = modifiedStats;
            _uiController.RefreshUI();

            var damageDiff = _uiController.CurrentDiffs.FirstOrDefault(d => d.StatName == "Damage");
            Assert.IsNotNull(damageDiff);
            Assert.AreEqual(3f, damageDiff.Delta, 0.01f);
            StringAssert.Contains("+3", damageDiff.Formatted);

            var magDiff = _uiController.CurrentDiffs.FirstOrDefault(d => d.StatName == "Mag Capacity");
            Assert.IsNotNull(magDiff);
            Assert.AreEqual(15f, magDiff.Delta, 0.01f);
            StringAssert.Contains("+15", magDiff.Formatted);
        }
    }
}
