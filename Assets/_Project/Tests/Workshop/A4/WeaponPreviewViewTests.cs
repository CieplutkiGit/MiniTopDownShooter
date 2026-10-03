using System;
using System.Collections.Generic;
using Application.Weapons;
using Game;
using Game.Workshop.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace MiniTopDownShooter.Tests.Workshop
{
    [TestFixture]
    public class WeaponPreviewViewTests
    {
        private GameObject _viewGo;
        private WeaponPreviewView _previewView;
        private WeaponVisualProfile _profile;
        private float _originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            _originalTimeScale = Time.timeScale;
            Time.timeScale = 1f;

            _viewGo = new GameObject("TestPreviewView");
            _previewView = _viewGo.AddComponent<WeaponPreviewView>();
            _previewView.Assembler = _viewGo.AddComponent<WeaponModelAssembler>();

            _profile = ScriptableObject.CreateInstance<WeaponVisualProfile>();
            _profile.WeaponId = WeaponWorkshopIds.Rifle;
            _profile.DefaultMuzzleOffset = new Vector3(0f, 0f, 0.8f);

            _profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard")
            {
                AssembledLocalPosition = new Vector3(0f, 0f, 0.4f),
                ExplodedLocalOffset = new Vector3(0f, 0f, 0.6f),
                MuzzleOffset = new Vector3(0f, 0f, 0.8f)
            });

            _profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long")
            {
                AssembledLocalPosition = new Vector3(0f, 0f, 0.55f),
                ExplodedLocalOffset = new Vector3(0f, 0f, 0.75f),
                MuzzleOffset = new Vector3(0f, 0f, 1.25f)
            });

            _profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.standard")
            {
                AssembledLocalPosition = new Vector3(0f, -0.2f, 0.05f),
                ExplodedLocalOffset = new Vector3(0f, -0.4f, 0f)
            });

            _previewView.VisualProfile = _profile;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _originalTimeScale;

            if (_viewGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_viewGo);
                _viewGo = null;
            }

            if (_profile != null)
            {
                UnityEngine.Object.DestroyImmediate(_profile);
                _profile = null;
            }
        }

        [Test]
        public void PreviewView_HasNoCombatComponents_CannotFire()
        {
            // Verify WeaponPreviewView has no combat or damage capabilities
            Assert.IsNull(_previewView.GetComponent<Gun>(), "Preview view must never have Gun attached.");
            Assert.IsNull(_previewView.GetComponent<DamageAffiliation>(), "Preview view must never have DamageAffiliation attached.");
            Assert.IsNull(_previewView.GetComponent<Projectile>(), "Preview view must never have Projectile attached.");
            Assert.IsNull(_previewView.GetComponent<IWeaponDelivery>(), "Preview view must never have IWeaponDelivery attached.");
        }

        [Test]
        public void SanitizeCombatComponents_RemovesGunAndAffiliation_IfPresent()
        {
            // Simulate accidental attachment of combat components
            var gun = _viewGo.AddComponent<Gun>();
            var affiliation = _viewGo.AddComponent<DamageAffiliation>();

            Assert.IsNotNull(_viewGo.GetComponent<Gun>());
            Assert.IsNotNull(_viewGo.GetComponent<DamageAffiliation>());

            _previewView.SanitizeCombatComponents();

            Assert.IsNull(_viewGo.GetComponent<Gun>(), "SanitizeCombatComponents must destroy Gun.");
            Assert.IsNull(_viewGo.GetComponent<DamageAffiliation>(), "SanitizeCombatComponents must destroy DamageAffiliation.");
        }

        [Test]
        public void ShowBuild_InstantiatesVisualRepresentationMatchingBuild()
        {
            var selections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard" },
                { WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.standard" }
            };
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle, selections);

            _previewView.ShowBuild(build);

            Assert.AreEqual(build, _previewView.CurrentBuild);
            Assert.AreEqual(2, _previewView.Assembler.SpawnedParts.Count);
            Assert.IsNotNull(_previewView.Assembler.GetPartObject(WeaponWorkshopIds.RifleSlots.Barrel));
            Assert.IsNotNull(_previewView.Assembler.GetPartObject(WeaponWorkshopIds.RifleSlots.Magazine));
        }

        [Test]
        public void SelectSlot_HighlightsTargetSlotAndClearsPrevious()
        {
            var selections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard" },
                { WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.standard" }
            };
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle, selections);
            _previewView.ShowBuild(build);

            // Select barrel
            _previewView.SelectSlot(WeaponWorkshopIds.RifleSlots.Barrel);
            Assert.AreEqual(WeaponWorkshopIds.RifleSlots.Barrel, _previewView.SelectedSlot);

            // Select magazine (should clear barrel)
            _previewView.SelectSlot(WeaponWorkshopIds.RifleSlots.Magazine);
            Assert.AreEqual(WeaponWorkshopIds.RifleSlots.Magazine, _previewView.SelectedSlot);

            // Clear selection
            _previewView.SelectSlot(null);
            Assert.IsNull(_previewView.SelectedSlot);
        }

        [Test]
        public void SlotSelected_EventFires_WhenTriggerSlotSelectedCalled()
        {
            var selections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard" }
            };
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle, selections);
            _previewView.ShowBuild(build);

            string reportedSlot = null;
            _previewView.SlotSelected += slot => reportedSlot = slot;

            _previewView.TriggerSlotSelected(WeaponWorkshopIds.RifleSlots.Barrel);

            Assert.AreEqual(WeaponWorkshopIds.RifleSlots.Barrel, reportedSlot);
            Assert.AreEqual(WeaponWorkshopIds.RifleSlots.Barrel, _previewView.SelectedSlot);
        }

        [Test]
        public void SetExploded_TrueAndFalse_AnimatesUsingUnscaledDeltaTime()
        {
            var selections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard" }
            };
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle, selections);
            _previewView.ShowBuild(build);
            _previewView.ExplosionDuration = 0.2f;

            Transform barrelSocket = _previewView.Assembler.GetSocket(WeaponWorkshopIds.RifleSlots.Barrel);
            Vector3 assembledPos = new Vector3(0f, 0f, 0.4f);
            Vector3 explodedPos = assembledPos + new Vector3(0f, 0f, 0.6f);

            Assert.AreEqual(assembledPos, barrelSocket.localPosition);
            Assert.AreEqual(0f, _previewView.ExplosionProgress);

            // Explode
            _previewView.SetExploded(true);
            Assert.IsTrue(_previewView.IsExploded);

            // Step animation halfway (0.1s out of 0.2s)
            _previewView.StepAnimation(0.1f);
            Assert.That(_previewView.ExplosionProgress, Is.GreaterThan(0.4f).And.LessThan(0.6f));
            Assert.That(barrelSocket.localPosition.z, Is.GreaterThan(assembledPos.z).And.LessThan(explodedPos.z));

            // Complete animation to exploded
            _previewView.StepAnimation(0.2f);
            Assert.AreEqual(1f, _previewView.ExplosionProgress);
            Assert.That(Vector3.Distance(explodedPos, barrelSocket.localPosition), Is.LessThan(0.001f));

            // Collapse back to assembled
            _previewView.SetExploded(false);
            Assert.IsFalse(_previewView.IsExploded);

            _previewView.StepAnimation(0.25f);
            Assert.AreEqual(0f, _previewView.ExplosionProgress);
            Assert.That(Vector3.Distance(assembledPos, barrelSocket.localPosition), Is.LessThan(0.001f));
        }

        [Test]
        public void SetExploded_AnimatesEvenWhenGameTimeIsFrozen()
        {
            // Simulate WorkshopEditing activity policy where IsTimeFrozen is true (Time.timeScale == 0)
            Time.timeScale = 0f;

            var selections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard" }
            };
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle, selections);
            _previewView.ShowBuild(build);
            _previewView.ExplosionDuration = 0.3f;

            _previewView.SetExploded(true);

            // Step using unscaled delta time
            _previewView.StepAnimation(0.15f);
            Assert.That(_previewView.ExplosionProgress, Is.GreaterThan(0.4f).And.LessThan(0.6f),
                "Explosion must progress even when Time.timeScale is 0f.");

            _previewView.StepAnimation(0.2f);
            Assert.AreEqual(1f, _previewView.ExplosionProgress);
        }

        [Test]
        public void SetExploded_RepeatedToggling_HasZeroPositionDrift()
        {
            var selections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard" },
                { WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.standard" }
            };
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle, selections);
            _previewView.ShowBuild(build);
            _previewView.ExplosionDuration = 0.1f;

            Transform barrelSocket = _previewView.Assembler.GetSocket(WeaponWorkshopIds.RifleSlots.Barrel);
            Transform magSocket = _previewView.Assembler.GetSocket(WeaponWorkshopIds.RifleSlots.Magazine);

            Vector3 initialBarrelPos = barrelSocket.localPosition;
            Vector3 initialMagPos = magSocket.localPosition;

            // Perform 100 explosion/collapse cycles
            for (int i = 0; i < 100; i++)
            {
                _previewView.SetExploded(true);
                _previewView.StepAnimation(0.15f); // finish explosion

                _previewView.SetExploded(false);
                _previewView.StepAnimation(0.15f); // finish collapse
            }

            Assert.AreEqual(0f, _previewView.ExplosionProgress);
            Assert.AreEqual(initialBarrelPos, barrelSocket.localPosition, "Barrel socket position drifted after 100 cycles!");
            Assert.AreEqual(initialMagPos, magSocket.localPosition, "Magazine socket position drifted after 100 cycles!");
        }

        [Test]
        public void MuzzleAnchor_UpdatesWhenBarrelChanged()
        {
            var standardSelections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard" }
            };
            var standardBuild = new WeaponBuild(WeaponWorkshopIds.Rifle, standardSelections);
            _previewView.ShowBuild(standardBuild);

            Assert.AreEqual(new Vector3(0f, 0f, 0.8f), _previewView.Assembler.LiveMuzzleAnchor.localPosition);

            // Switch to long barrel
            var longSelections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long" }
            };
            var longBuild = new WeaponBuild(WeaponWorkshopIds.Rifle, longSelections);
            _previewView.ShowBuild(longBuild);

            Assert.AreEqual(new Vector3(0f, 0f, 1.25f), _previewView.Assembler.LiveMuzzleAnchor.localPosition);
        }
    }
}
