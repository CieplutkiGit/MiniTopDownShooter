using System.Collections.Generic;
using Application.Weapons;
using Game.Workshop.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace MiniTopDownShooter.Tests.Workshop
{
    [TestFixture]
    public class WeaponModelAssemblerTests
    {
        private GameObject _holder;
        private WeaponModelAssembler _assembler;
        private WeaponVisualProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("TestAssemblerHolder");
            _assembler = _holder.AddComponent<WeaponModelAssembler>();

            _profile = ScriptableObject.CreateInstance<WeaponVisualProfile>();
            _profile.WeaponId = WeaponWorkshopIds.Rifle;
            _profile.DefaultMuzzleOffset = new Vector3(0f, 0f, 0.8f);

            // Configure Rifle parts in profile
            _profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard")
            {
                AssembledLocalPosition = new Vector3(0f, 0f, 0.4f),
                AssembledLocalRotation = Quaternion.identity,
                AssembledLocalScale = Vector3.one,
                ExplodedLocalOffset = new Vector3(0f, 0f, 0.5f),
                MuzzleOffset = new Vector3(0f, 0f, 0.8f)
            });

            _profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long")
            {
                AssembledLocalPosition = new Vector3(0f, 0f, 0.6f),
                AssembledLocalRotation = Quaternion.identity,
                AssembledLocalScale = Vector3.one,
                ExplodedLocalOffset = new Vector3(0f, 0f, 0.7f),
                MuzzleOffset = new Vector3(0f, 0f, 1.2f)
            });

            _profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.standard")
            {
                AssembledLocalPosition = new Vector3(0f, -0.2f, 0.1f),
                AssembledLocalRotation = Quaternion.Euler(15f, 0f, 0f),
                AssembledLocalScale = Vector3.one,
                ExplodedLocalOffset = new Vector3(0f, -0.3f, 0f)
            });

            _profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.standard")
            {
                AssembledLocalPosition = new Vector3(0f, -0.15f, -0.1f),
                AssembledLocalRotation = Quaternion.identity,
                AssembledLocalScale = Vector3.one,
                ExplodedLocalOffset = new Vector3(0f, -0.3f, -0.1f)
            });

            _profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.standard")
            {
                AssembledLocalPosition = new Vector3(0f, 0f, -0.35f),
                AssembledLocalRotation = Quaternion.identity,
                AssembledLocalScale = Vector3.one,
                ExplodedLocalOffset = new Vector3(0f, 0f, -0.4f)
            });
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null)
            {
                Object.DestroyImmediate(_holder);
                _holder = null;
            }

            if (_profile != null)
            {
                Object.DestroyImmediate(_profile);
                _profile = null;
            }
        }

        [Test]
        public void Assemble_CreatesReceiverAndPartSockets_MatchingBuild()
        {
            var selections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard" },
                { WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.standard" },
                { WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.standard" },
                { WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.standard" }
            };
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle, selections);

            _assembler.Assemble(build, _profile);

            Assert.AreEqual(4, _assembler.SpawnedParts.Count);
            Assert.IsNotNull(_assembler.GetSocket(WeaponWorkshopIds.RifleSlots.Barrel));
            Assert.IsNotNull(_assembler.GetSocket(WeaponWorkshopIds.RifleSlots.Magazine));
            Assert.IsNotNull(_assembler.GetSocket(WeaponWorkshopIds.RifleSlots.Grip));
            Assert.IsNotNull(_assembler.GetSocket(WeaponWorkshopIds.RifleSlots.Stock));
        }

        [Test]
        public void Assemble_PlacesSocketsAtAssembledPoses_SpecifiedInProfile()
        {
            var selections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard" },
                { WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.standard" }
            };
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle, selections);

            _assembler.Assemble(build, _profile);

            Transform barrelSocket = _assembler.GetSocket(WeaponWorkshopIds.RifleSlots.Barrel);
            Transform magSocket = _assembler.GetSocket(WeaponWorkshopIds.RifleSlots.Magazine);

            Assert.AreEqual(new Vector3(0f, 0f, 0.4f), barrelSocket.localPosition);
            Assert.AreEqual(Quaternion.identity, barrelSocket.localRotation);

            Assert.AreEqual(new Vector3(0f, -0.2f, 0.1f), magSocket.localPosition);
            Assert.That(Quaternion.Angle(Quaternion.Euler(15f, 0f, 0f), magSocket.localRotation), Is.LessThan(0.01f));
        }

        [Test]
        public void Assemble_UpdatesMuzzleAnchorPosition_ToMatchEquippedBarrel()
        {
            var standardSelections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard" }
            };
            var standardBuild = new WeaponBuild(WeaponWorkshopIds.Rifle, standardSelections);
            _assembler.Assemble(standardBuild, _profile);

            Assert.IsNotNull(_assembler.LiveMuzzleAnchor);
            Assert.AreEqual(new Vector3(0f, 0f, 0.8f), _assembler.LiveMuzzleAnchor.localPosition);

            // Switch to long barrel
            var longSelections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long" }
            };
            var longBuild = new WeaponBuild(WeaponWorkshopIds.Rifle, longSelections);
            _assembler.Assemble(longBuild, _profile);

            Assert.AreEqual(new Vector3(0f, 0f, 1.2f), _assembler.LiveMuzzleAnchor.localPosition);
        }

        [Test]
        public void Assemble_SwapsPartsCleanly_WhenBuildIsUpdated()
        {
            var initialSelections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard" }
            };
            var initialBuild = new WeaponBuild(WeaponWorkshopIds.Rifle, initialSelections);
            _assembler.Assemble(initialBuild, _profile);

            GameObject initialPartObj = _assembler.GetPartObject(WeaponWorkshopIds.RifleSlots.Barrel);
            Assert.IsNotNull(initialPartObj);
            Assert.AreEqual("Part_rifle.barrel.standard", initialPartObj.name);

            // Swap to long barrel
            var newSelections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long" }
            };
            var newBuild = new WeaponBuild(WeaponWorkshopIds.Rifle, newSelections);
            _assembler.Assemble(newBuild, _profile);

            GameObject newPartObj = _assembler.GetPartObject(WeaponWorkshopIds.RifleSlots.Barrel);
            Assert.IsNotNull(newPartObj);
            Assert.AreEqual("Part_rifle.barrel.long", newPartObj.name);
            Assert.AreNotEqual(initialPartObj, newPartObj);
        }

        [Test]
        public void Assemble_RemovesObsoleteSockets_WhenPartIsRemovedFromBuild()
        {
            var selections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard" },
                { WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.standard" }
            };
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle, selections);
            _assembler.Assemble(build, _profile);

            Assert.AreEqual(2, _assembler.SpawnedParts.Count);

            // Remove stock
            var reducedBuild = build.WithSelection(WeaponWorkshopIds.RifleSlots.Stock, null);
            _assembler.Assemble(reducedBuild, _profile);

            Assert.AreEqual(1, _assembler.SpawnedParts.Count);
            Assert.IsNull(_assembler.GetSocket(WeaponWorkshopIds.RifleSlots.Stock));
            Assert.IsNull(_assembler.GetPartObject(WeaponWorkshopIds.RifleSlots.Stock));
        }

        [Test]
        public void Assemble_ConfiguresCollidersAndPartClickTargets_WhenEnabled()
        {
            _assembler.AddCollidersForSelection = true;
            var selections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard" }
            };
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle, selections);

            _assembler.Assemble(build, _profile);

            GameObject barrelObj = _assembler.GetPartObject(WeaponWorkshopIds.RifleSlots.Barrel);
            Assert.IsNotNull(barrelObj);

            var collider = barrelObj.GetComponentInChildren<Collider>();
            Assert.IsNotNull(collider, "Part must have a collider for raycast inspection.");

            var clickTarget = barrelObj.GetComponent<PartClickTarget>();
            Assert.IsNotNull(clickTarget, "Part must have a PartClickTarget component.");
            Assert.AreEqual(WeaponWorkshopIds.RifleSlots.Barrel, clickTarget.SlotId);
            Assert.AreEqual("rifle.barrel.standard", clickTarget.PartId);
        }

        [Test]
        public void Clear_RemovesAllSpawnedVisualsAndSockets()
        {
            var selections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard" },
                { WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.standard" }
            };
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle, selections);
            _assembler.Assemble(build, _profile);

            _assembler.Clear();

            Assert.AreEqual(0, _assembler.SpawnedParts.Count);
            Assert.IsNull(_assembler.GetSocket(WeaponWorkshopIds.RifleSlots.Barrel));
            Assert.IsNull(_assembler.GetSocket(WeaponWorkshopIds.RifleSlots.Magazine));
        }
    }
}
