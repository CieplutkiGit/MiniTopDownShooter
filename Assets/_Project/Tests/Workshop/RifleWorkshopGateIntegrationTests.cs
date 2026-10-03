using System.Collections.Generic;
using Application;
using Application.Weapons;
using Application.Workshop;
using NUnit.Framework;

namespace MiniTopDownShooter.Tests.Workshop
{
    [TestFixture]
    public class RifleWorkshopGateIntegrationTests
    {
        private FakeWeaponCatalog _catalog;
        private WeaponBuildResolver _resolver;
        private FakeWeaponBuildStore _store;
        private FakeWeaponBuildTarget _target;
        private FakeWeaponPreviewView _previewView;
        private WeaponPlatformSpec _riflePlatform;

        [SetUp]
        public void SetUp()
        {
            _catalog = WeaponWorkshopTestFixtures.CreateCatalogWithRifle();
            _resolver = new WeaponBuildResolver();
            _store = new FakeWeaponBuildStore();
            _target = new FakeWeaponBuildTarget();
            _previewView = new FakeWeaponPreviewView();
            _riflePlatform = _catalog.GetPlatform(WeaponWorkshopIds.Rifle);
        }

        [Test]
        public void FullWorkflow_Select_Disassemble_Apply_Shoot_SaveLoad_Succeeds()
        {
            // 1. Initial State & Select
            WeaponBuild initialBuild = _riflePlatform.CreateDefaultBuild();
            Assert.AreEqual("rifle.magazine.standard", initialBuild.GetPart(WeaponWorkshopIds.RifleSlots.Magazine));

            var session = new WorkshopSession(
                WeaponWorkshopIds.Rifle,
                initialBuild,
                _catalog,
                _resolver,
                _store,
                _target);

            Assert.IsFalse(session.HasUnappliedChanges);
            Assert.IsTrue(session.IsValid);
            Assert.AreEqual(30, session.DraftStats.MagazineCapacity);
            Assert.AreEqual(15f, session.DraftStats.Damage);

            // 2. Disassemble (preview explode)
            _previewView.ShowBuild(session.DraftBuild);
            Assert.AreEqual(session.DraftBuild, _previewView.DisplayedBuild);

            _previewView.SetExploded(true);
            Assert.IsTrue(_previewView.IsExploded);

            // 3. Select Alternative Part (Extended Magazine)
            session.SelectPart(WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.extended");
            Assert.IsTrue(session.HasUnappliedChanges);
            Assert.IsTrue(session.IsValid);
            Assert.AreEqual(45, session.DraftStats.MagazineCapacity);
            Assert.AreEqual(1.95f, session.DraftStats.ReloadDuration); // 1.55 + 0.4

            // Also select Long Barrel
            session.SelectPart(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long");
            Assert.IsTrue(session.HasUnappliedChanges);
            Assert.AreEqual(18f, session.DraftStats.Damage); // 15 + 3

            // 4. Apply
            ApplyResult applyResult = session.Apply();
            Assert.IsTrue(applyResult.IsSuccess);
            Assert.IsFalse(session.HasUnappliedChanges);
            Assert.AreEqual("rifle.magazine.extended", session.CommittedBuild.GetPart(WeaponWorkshopIds.RifleSlots.Magazine));
            Assert.AreEqual("rifle.barrel.long", session.CommittedBuild.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));

            // Verify target received updated build and stats
            Assert.AreEqual(1, _target.ApplyCallCount);
            Assert.AreEqual(session.CommittedBuild, _target.CurrentBuild);
            Assert.AreEqual(45, _target.CurrentStats.MagazineCapacity);
            Assert.AreEqual(18f, _target.CurrentStats.Damage);

            // 5. Simulate Shoot on Live Target
            WeaponRuntimeConfig runtimeConfig = new WeaponRuntimeConfig
            {
                MagazineSize = _target.CurrentStats.MagazineCapacity,
                MaxReserveAmmo = _target.CurrentStats.MaxReserveAmmo,
                FireInterval = _target.CurrentStats.FireInterval,
                SpreadPerShot = _target.CurrentStats.RecoilPerShot,
                BaseSpreadAngle = _target.CurrentStats.BaseSpreadAngle,
                MaxSpreadAngle = _target.CurrentStats.MaxSpreadAngle,
                InfiniteAmmo = false
            };
            WeaponRuntime runtime = new WeaponRuntime(runtimeConfig);
            Assert.AreEqual(45, runtime.Ammo.InMagazine);

            bool fired = runtime.TryFire(1.0f);
            Assert.IsTrue(fired);
            Assert.AreEqual(44, runtime.Ammo.InMagazine);
            Assert.IsTrue(runtime.CurrentSpreadAngle > runtimeConfig.BaseSpreadAngle);

            // 6. Save & Load Verification
            Assert.IsTrue(_store.Stored.ContainsKey(WeaponWorkshopIds.Rifle));
            WeaponBuild savedBuild = _store.Stored[WeaponWorkshopIds.Rifle];
            Assert.AreEqual("rifle.magazine.extended", savedBuild.GetPart(WeaponWorkshopIds.RifleSlots.Magazine));
            Assert.AreEqual("rifle.barrel.long", savedBuild.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));

            BuildLoadResult loadResult = _store.Load(WeaponWorkshopIds.Rifle);
            Assert.IsTrue(loadResult.IsSuccess);
            Assert.AreEqual(savedBuild, loadResult.Build);

            // Verify normalization of loaded build reports clean
            WeaponBuild normalized = _resolver.Normalize(loadResult.Build, _catalog, out bool repaired);
            Assert.IsFalse(repaired);
            Assert.AreEqual(savedBuild, normalized);
        }

        [Test]
        public void Discard_RevertsDraftChangesWithoutAffectingTargetOrStore()
        {
            WeaponBuild initialBuild = _riflePlatform.CreateDefaultBuild();
            var session = new WorkshopSession(
                WeaponWorkshopIds.Rifle,
                initialBuild,
                _catalog,
                _resolver,
                _store,
                _target);

            // Apply initial
            session.Apply();
            Assert.AreEqual(1, _target.ApplyCallCount);

            // Change draft
            session.SelectPart(WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.heavy");
            Assert.IsTrue(session.HasUnappliedChanges);
            Assert.AreEqual("rifle.stock.heavy", session.DraftBuild.GetPart(WeaponWorkshopIds.RifleSlots.Stock));

            // Discard
            session.Discard();
            Assert.IsFalse(session.HasUnappliedChanges);
            Assert.AreEqual("rifle.stock.standard", session.DraftBuild.GetPart(WeaponWorkshopIds.RifleSlots.Stock));

            // Target was not modified again
            Assert.AreEqual(1, _target.ApplyCallCount);
            Assert.AreEqual("rifle.stock.standard", _target.CurrentBuild.GetPart(WeaponWorkshopIds.RifleSlots.Stock));
        }
    }
}
