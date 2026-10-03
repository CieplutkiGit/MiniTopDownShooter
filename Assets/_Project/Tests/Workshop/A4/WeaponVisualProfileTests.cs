using Application.Weapons;
using Game.Workshop.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace MiniTopDownShooter.Tests.Workshop
{
    [TestFixture]
    public class WeaponVisualProfileTests
    {
        private WeaponVisualProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _profile = ScriptableObject.CreateInstance<WeaponVisualProfile>();
            _profile.WeaponId = WeaponWorkshopIds.Rifle;
            _profile.DefaultMuzzleOffset = new Vector3(0f, 0f, 1.0f);
        }

        [TearDown]
        public void TearDown()
        {
            if (_profile != null)
            {
                Object.DestroyImmediate(_profile);
                _profile = null;
            }
        }

        [Test]
        public void TryGetPartPose_ReturnsTrueAndCorrectPose_WhenPartExists()
        {
            var partData = new PartVisualData(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard")
            {
                AssembledLocalPosition = new Vector3(0f, 0f, 0.45f),
                AssembledLocalRotation = Quaternion.Euler(0f, 90f, 0f),
                AssembledLocalScale = new Vector3(1.2f, 1.2f, 1.2f),
                ExplodedLocalOffset = new Vector3(0f, 0f, 0.5f),
                MuzzleOffset = new Vector3(0f, 0f, 0.95f)
            };
            _profile.AddOrUpdatePart(partData);

            bool found = _profile.TryGetPartPose(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard", out var result);

            Assert.IsTrue(found);
            Assert.IsNotNull(result);
            Assert.AreEqual("rifle.barrel.standard", result.PartId);
            Assert.AreEqual(WeaponWorkshopIds.RifleSlots.Barrel, result.SlotId);
            Assert.AreEqual(new Vector3(0f, 0f, 0.45f), result.AssembledLocalPosition);
            Assert.AreEqual(Quaternion.Euler(0f, 90f, 0f), result.AssembledLocalRotation);
            Assert.AreEqual(new Vector3(1.2f, 1.2f, 1.2f), result.AssembledLocalScale);
            Assert.AreEqual(new Vector3(0f, 0f, 0.5f), result.ExplodedLocalOffset);
            Assert.AreEqual(new Vector3(0f, 0f, 0.95f), result.MuzzleOffset);
        }

        [Test]
        public void TryGetPartPose_ReturnsFalse_WhenPartDoesNotExist()
        {
            var partData = new PartVisualData(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard");
            _profile.AddOrUpdatePart(partData);

            bool found = _profile.TryGetPartPose(WeaponWorkshopIds.RifleSlots.Barrel, "non_existent_part", out var result);

            Assert.IsFalse(found);
            Assert.IsNull(result);
        }

        [Test]
        public void TryGetPartPose_ReturnsFalse_WhenIdsNullOrEmpty()
        {
            bool found = _profile.TryGetPartPose(null, null, out var result);

            Assert.IsFalse(found);
            Assert.IsNull(result);
        }

        [Test]
        public void GetMuzzleOffset_ReturnsSpecificBarrelOffset_WhenConfigured()
        {
            var barrelData = new PartVisualData(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long")
            {
                MuzzleOffset = new Vector3(0f, 0f, 1.4f)
            };
            _profile.AddOrUpdatePart(barrelData);

            Vector3 offset = _profile.GetMuzzleOffset("rifle.barrel.long");

            Assert.AreEqual(new Vector3(0f, 0f, 1.4f), offset);
        }

        [Test]
        public void GetMuzzleOffset_ReturnsDefaultOffset_WhenBarrelNotConfiguredOrUnknown()
        {
            _profile.DefaultMuzzleOffset = new Vector3(0f, 0.1f, 0.88f);

            Vector3 offsetUnknown = _profile.GetMuzzleOffset("rifle.barrel.unknown");
            Vector3 offsetNull = _profile.GetMuzzleOffset(null);

            Assert.AreEqual(new Vector3(0f, 0.1f, 0.88f), offsetUnknown);
            Assert.AreEqual(new Vector3(0f, 0.1f, 0.88f), offsetNull);
        }

        [Test]
        public void AddOrUpdatePart_UpdatesExistingEntry_WhenSlotAndPartMatch()
        {
            var initial = new PartVisualData(WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.standard")
            {
                AssembledLocalPosition = new Vector3(0f, -0.1f, 0f)
            };
            _profile.AddOrUpdatePart(initial);

            var updated = new PartVisualData(WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.standard")
            {
                AssembledLocalPosition = new Vector3(0f, -0.25f, 0.05f)
            };
            _profile.AddOrUpdatePart(updated);

            Assert.AreEqual(1, _profile.Parts.Count);
            _profile.TryGetPartPose(WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.standard", out var result);
            Assert.AreEqual(new Vector3(0f, -0.25f, 0.05f), result.AssembledLocalPosition);
        }

        [Test]
        public void AddOrUpdatePart_ClonesData_PreventingExternalMutation()
        {
            var initial = new PartVisualData(WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.standard")
            {
                AssembledLocalPosition = new Vector3(0f, 0f, -0.3f)
            };
            _profile.AddOrUpdatePart(initial);

            // Mutate the original object passed into profile
            initial.AssembledLocalPosition = new Vector3(99f, 99f, 99f);

            _profile.TryGetPartPose(WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.standard", out var retrieved);
            Assert.AreEqual(new Vector3(0f, 0f, -0.3f), retrieved.AssembledLocalPosition);

            // Mutate retrieved object
            retrieved.AssembledLocalPosition = new Vector3(55f, 55f, 55f);
            _profile.TryGetPartPose(WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.standard", out var retrievedAgain);
            Assert.AreEqual(new Vector3(0f, 0f, -0.3f), retrievedAgain.AssembledLocalPosition);
        }

        [Test]
        public void SetReceiver_ConfiguresReceiverPropertiesCorrectly()
        {
            var mesh = new Mesh { name = "TestReceiverMesh" };
            var localPos = new Vector3(0.1f, 0.2f, 0.3f);
            var localRot = Quaternion.Euler(10f, 20f, 30f);
            var localScale = new Vector3(1.1f, 1.2f, 1.3f);

            _profile.SetReceiver(null, mesh, null, localPos, localRot, localScale);

            Assert.AreEqual(mesh, _profile.ReceiverMesh);
            Assert.AreEqual(localPos, _profile.ReceiverLocalPosition);
            Assert.AreEqual(localRot, _profile.ReceiverLocalRotation);
            Assert.AreEqual(localScale, _profile.ReceiverLocalScale);

            Object.DestroyImmediate(mesh);
        }
    }
}
