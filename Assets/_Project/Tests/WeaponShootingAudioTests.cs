using System.Collections.Generic;
using System.IO;
using Application;
using Core;
using Game;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace MiniTopDownShooter.Tests
{
    public class WeaponShootingAudioTests
    {
        private List<GameObject> _spawnedObjects;

        [SetUp]
        public void SetUp()
        {
            _spawnedObjects = new List<GameObject>();
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;

            if (_spawnedObjects != null)
            {
                for (int i = 0; i < _spawnedObjects.Count; i++)
                {
                    if (_spawnedObjects[i] != null)
                    {
                        Object.DestroyImmediate(_spawnedObjects[i]);
                    }
                }
                _spawnedObjects.Clear();
            }
        }

        private GameObject Track(GameObject go)
        {
            _spawnedObjects.Add(go);
            return go;
        }

        [Test]
        public void AllFiveWeaponPrefabs_HaveAuthoredAudioSourceAndGunAudioWithClipsAndSFXMixer()
        {
            var weaponConfigs = new (string Path, string ExpectedClipName)[]
            {
                ("Assets/_Project/Weapons/Gun_Rifle.prefab", "shot_rifle"),
                ("Assets/_Project/Weapons/Gun_Pistol.prefab", "shot_pistol"),
                ("Assets/_Project/Weapons/Gun_Shotgun.prefab", "shot_shotgun"),
                ("Assets/_Project/Weapons/Gun_SMG.prefab", "shot_pistol"),       // SMG fallback
                ("Assets/_Project/Weapons/Gun_Launcher.prefab", "shot_shotgun")   // Launcher fallback
            };

            for (int i = 0; i < weaponConfigs.Length; i++)
            {
                string path = weaponConfigs[i].Path;
                string expectedClip = weaponConfigs[i].ExpectedClipName;

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(prefab, $"Prefab not found at {path}");

                Gun gun = prefab.GetComponent<Gun>();
                Assert.IsNotNull(gun, $"Prefab at {path} must have a Gun component.");

                AudioSource source = prefab.GetComponent<AudioSource>();
                Assert.IsNotNull(source, $"Prefab at {path} must have an AudioSource component.");
                Assert.IsNotNull(source.outputAudioMixerGroup, $"Prefab at {path} AudioSource must have outputAudioMixerGroup assigned.");
                Assert.AreEqual("SFX", source.outputAudioMixerGroup.name, $"Prefab at {path} AudioSource must route to the SFX mixer group.");
                Assert.IsFalse(source.playOnAwake, $"Prefab at {path} AudioSource must not play on awake.");

                GunAudio gunAudio = prefab.GetComponent<GunAudio>();
                Assert.IsNotNull(gunAudio, $"Prefab at {path} must have a GunAudio component.");
                Assert.IsNotNull(gunAudio.ShotClip, $"Prefab at {path} GunAudio must have ShotClip assigned.");
                Assert.AreEqual(expectedClip, gunAudio.ShotClip.name, $"Prefab at {path} has unexpected shot clip.");
                Assert.AreEqual(gun, gunAudio.GunRef, $"Prefab at {path} GunAudio._gunRef must point to the Gun on the prefab.");

                Assert.IsTrue(gunAudio.ValidateReferences(), $"Prefab at {path} failed reference validation.");
            }
        }

        [Test]
        public void GunAudio_ValidateReferences_DetectsMissingGun_AudioSource_Clip_OrMixer()
        {
            GameObject testGo = Track(new GameObject("TestGunAudioObj"));
            GunAudio gunAudio = testGo.AddComponent<GunAudio>();

            // Completely unassigned -> invalid
            Assert.IsFalse(gunAudio.ValidateReferences());

            // Add Gun
            Gun gun = testGo.AddComponent<Gun>();
            SerializedObject so = new SerializedObject(gunAudio);
            so.FindProperty("_gunRef").objectReferenceValue = gun;
            so.ApplyModifiedProperties();

            // Still missing AudioSource & Clip -> invalid
            Assert.IsFalse(gunAudio.ValidateReferences());

            // Add AudioSource without mixer group
            AudioSource source = testGo.AddComponent<AudioSource>();
            so.FindProperty("_source").objectReferenceValue = source;
            so.ApplyModifiedProperties();

            // Missing mixer group & clip -> invalid
            Assert.IsFalse(gunAudio.ValidateReferences());

            // Assign SFX mixer group
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/_Project/Audio/GameAudioMixer.mixer");
            Assert.IsNotNull(mixer);
            AudioMixerGroup[] sfx = mixer.FindMatchingGroups("SFX");
            Assert.IsTrue(sfx != null && sfx.Length > 0);
            source.outputAudioMixerGroup = sfx[0];

            // Still missing clip -> invalid
            Assert.IsFalse(gunAudio.ValidateReferences());

            // Assign clip
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/shot_rifle.wav");
            Assert.IsNotNull(clip);
            so.FindProperty("_shotClip").objectReferenceValue = clip;
            so.ApplyModifiedProperties();

            // Now valid
            Assert.IsTrue(gunAudio.ValidateReferences());
        }

        private static void BindGameState(Gun gun, GameStateController gameState)
        {
            typeof(Gun).GetField("_gameStateRef", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(gun, gameState);
            typeof(Gun).GetField("_gameState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(gun, gameState);
        }

        [Test]
        public void GunAudio_PlaysClipOncePerAcceptedShot()
        {
            GameObject gunGo = Track(new GameObject("TestGun"));
            Gun gun = gunGo.AddComponent<Gun>();
            AudioSource source = gunGo.AddComponent<AudioSource>();
            GunAudio gunAudio = gunGo.AddComponent<GunAudio>();

            AudioClip testClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/shot_pistol.wav");
            Assert.IsNotNull(testClip);

            GameStateController gameState = Track(new GameObject("GameState")).AddComponent<GameStateController>();
            gameState.StartGame();

            SerializedObject so = new SerializedObject(gunAudio);
            so.FindProperty("_gunRef").objectReferenceValue = gun;
            so.FindProperty("_source").objectReferenceValue = source;
            so.FindProperty("_shotClip").objectReferenceValue = testClip;
            so.ApplyModifiedProperties();

            BindGameState(gun, gameState);

            gunAudio.Initialize(gun);

            int shotFiredCount = 0;
            gun.Fired += () => shotFiredCount++;

            AudioClip lastRequestedClip = null;
            gunAudio.PlaybackRequested += c => lastRequestedClip = c;

            // Configure runtime with fire interval 0.2s
            WeaponRuntimeConfig config = new WeaponRuntimeConfig
            {
                FireMode = Application.WeaponFireMode.SemiAutomatic,
                FireInterval = 0.2f,
                InfiniteAmmo = true,
                MagazineSize = 10,
                StartingReserveAmmo = 10
            };
            gun.ConfigureForTesting(new WeaponRuntime(config), null, gunGo.transform);

            // First accepted shot
            gun.Shoot(Vector3.forward);
            Assert.AreEqual(1, shotFiredCount, "Accepted shot should trigger Fired event exactly once.");
            Assert.AreEqual(1, gunAudio.PlaybackCount, "Accepted shot should issue GunAudio playback request.");
            Assert.AreSame(testClip, lastRequestedClip, "GunAudio must play the configured shot clip.");

            // Immediate second shot within 0.2s interval should be rejected
            gun.Shoot(Vector3.forward);
            Assert.AreEqual(1, shotFiredCount, "Rejected shot should not trigger Fired event.");
            Assert.AreEqual(1, gunAudio.PlaybackCount, "Rejected shot must not issue additional audio playback.");
        }

        [Test]
        public void WeaponLoadout_SwitchingWeapons_FiresAudioOnlyForActiveWeapon()
        {
            GameObject playerGo = Track(new GameObject("Player"));
            PlayerShoot playerShoot = playerGo.AddComponent<PlayerShoot>();
            WeaponLoadout loadout = playerGo.AddComponent<WeaponLoadout>();
            SerializedObject pShootSo = new SerializedObject(playerShoot);
            pShootSo.FindProperty("_loadout").objectReferenceValue = loadout;
            pShootSo.ApplyModifiedProperties();

            GameStateController gameState = Track(new GameObject("GameState")).AddComponent<GameStateController>();
            gameState.StartGame();

            // Weapon 1: Rifle
            GameObject rifleGo = Track(new GameObject("Rifle"));
            rifleGo.transform.SetParent(playerGo.transform);
            Gun rifleGun = rifleGo.AddComponent<Gun>();
            rifleGo.AddComponent<AudioSource>();
            GunAudio rifleAudio = rifleGo.AddComponent<GunAudio>();
            rifleAudio.Initialize(rifleGun);

            BindGameState(rifleGun, gameState);

            int rifleShots = 0;
            rifleGun.Fired += () => rifleShots++;
            rifleGun.ConfigureForTesting(new WeaponRuntime(new WeaponRuntimeConfig
            {
                FireMode = Application.WeaponFireMode.Automatic,
                FireInterval = 0f,
                InfiniteAmmo = true
            }), null, rifleGo.transform);

            // Weapon 2: Pistol
            GameObject pistolGo = Track(new GameObject("Pistol"));
            pistolGo.transform.SetParent(playerGo.transform);
            Gun pistolGun = pistolGo.AddComponent<Gun>();
            pistolGo.AddComponent<AudioSource>();
            GunAudio pistolAudio = pistolGo.AddComponent<GunAudio>();
            pistolAudio.Initialize(pistolGun);

            BindGameState(pistolGun, gameState);

            int pistolShots = 0;
            pistolGun.Fired += () => pistolShots++;
            pistolGun.ConfigureForTesting(new WeaponRuntime(new WeaponRuntimeConfig
            {
                FireMode = Application.WeaponFireMode.SemiAutomatic,
                FireInterval = 0f,
                InfiniteAmmo = true
            }), null, pistolGo.transform);

            // Wire loadout
            loadout.AddWeapon(rifleGun, equipImmediately: true);
            loadout.AddWeapon(pistolGun, equipImmediately: false);

            Assert.AreEqual(rifleGun, playerShoot.ActiveGun);

            // Fire active gun (Rifle)
            playerShoot.TryShoot();
            Assert.AreEqual(1, rifleShots, "Active rifle should fire.");
            Assert.AreEqual(1, rifleAudio.PlaybackCount, "Active rifle GunAudio should request playback.");
            Assert.AreEqual(0, pistolShots, "Inactive pistol must not fire.");
            Assert.AreEqual(0, pistolAudio.PlaybackCount, "Inactive pistol GunAudio must not request playback.");

            // Switch to Pistol
            bool switched = loadout.EquipSlot(1);
            Assert.IsTrue(switched);
            Assert.AreEqual(pistolGun, playerShoot.ActiveGun);

            // Fire active gun (Pistol)
            playerShoot.TryShoot();
            Assert.AreEqual(1, rifleShots, "Inactive rifle must not fire after weapon switch.");
            Assert.AreEqual(1, rifleAudio.PlaybackCount, "Inactive rifle GunAudio must remain at 1 playback.");
            Assert.AreEqual(1, pistolShots, "Active pistol should fire.");
            Assert.AreEqual(1, pistolAudio.PlaybackCount, "Active pistol GunAudio should request playback.");
        }

        [Test]
        public void PauseResume_PreventsGunfireAudioWhilePaused_RestoresOnResume()
        {
            GameObject go = Track(new GameObject("GameStateAndGun"));
            GameStateController gameState = go.AddComponent<GameStateController>();
            Gun gun = go.AddComponent<Gun>();
            go.AddComponent<AudioSource>();
            GunAudio gunAudio = go.AddComponent<GunAudio>();
            gunAudio.Initialize(gun);

            BindGameState(gun, gameState);

            int shotCount = 0;
            gun.Fired += () => shotCount++;

            gun.ConfigureForTesting(new WeaponRuntime(new WeaponRuntimeConfig
            {
                FireMode = Application.WeaponFireMode.Automatic,
                FireInterval = 0f,
                InfiniteAmmo = true
            }), null, go.transform);

            gameState.StartGame();
            Assert.AreEqual(GameState.Playing, gameState.CurrentState);

            // Playing: shot fires
            gun.Shoot(Vector3.forward);
            Assert.AreEqual(1, shotCount, "Gun should fire when GameState is Playing.");
            Assert.AreEqual(1, gunAudio.PlaybackCount, "GunAudio should play when GameState is Playing.");

            // Pause
            gameState.Pause();
            Assert.AreEqual(GameState.Paused, gameState.CurrentState);

            // Paused: shot rejected
            gun.Shoot(Vector3.forward);
            Assert.AreEqual(1, shotCount, "Gun must NOT fire while game is Paused.");
            Assert.AreEqual(1, gunAudio.PlaybackCount, "GunAudio must NOT play while game is Paused.");

            // Resume
            gameState.Resume();
            Assert.AreEqual(GameState.Playing, gameState.CurrentState);

            // Resumed: shot fires
            gun.Shoot(Vector3.forward);
            Assert.AreEqual(2, shotCount, "Gun should fire after game resumes.");
            Assert.AreEqual(2, gunAudio.PlaybackCount, "GunAudio should play after game resumes.");
        }

        [Test]
        public void Restart_RestoresWeaponsAndAudioContinuesWithoutDuplicates()
        {
            GameObject root = Track(new GameObject("GameContext"));
            GameStateController gameState = root.AddComponent<GameStateController>();
            WorldResetManager resetMgr = root.AddComponent<WorldResetManager>();

            GameObject playerGo = Track(new GameObject("Player"));
            PlayerController player = playerGo.AddComponent<PlayerController>();
            WeaponLoadout loadout = playerGo.AddComponent<WeaponLoadout>();

            GameObject gunGo = Track(new GameObject("Rifle"));
            gunGo.transform.SetParent(playerGo.transform);
            Gun gun = gunGo.AddComponent<Gun>();
            gunGo.AddComponent<AudioSource>();
            GunAudio gunAudio = gunGo.AddComponent<GunAudio>();
            gunAudio.Initialize(gun);

            BindGameState(gun, gameState);

            int shotCount = 0;
            gun.Fired += () => shotCount++;
            gun.ConfigureForTesting(new WeaponRuntime(new WeaponRuntimeConfig
            {
                FireMode = Application.WeaponFireMode.Automatic,
                FireInterval = 0f,
                InfiniteAmmo = true
            }), null, gunGo.transform);

            loadout.AddWeapon(gun, equipImmediately: true);

            gameState.StartGame();
            gun.Shoot(Vector3.forward);
            Assert.AreEqual(1, shotCount);
            Assert.AreEqual(1, gunAudio.PlaybackCount);

            // Simulate Game Over & Restart
            gameState.Pause(); // Transition out of Playing
            var managerField = typeof(GameStateController).GetField("_manager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var gsm = managerField.GetValue(gameState) as GameStateManager;
            gsm.EndGame();

            Assert.AreEqual(GameState.GameOver, gameState.CurrentState);

            resetMgr.Restart();

            // Return to playing
            gsm.StartGame();
            Assert.AreEqual(GameState.Playing, gameState.CurrentState);

            // Fire again - should trigger exactly 1 additional event (no duplicate subscriptions)
            gun.Shoot(Vector3.forward);
            Assert.AreEqual(2, shotCount, "Restart should not duplicate event subscriptions.");
            Assert.AreEqual(2, gunAudio.PlaybackCount, "Restart should not duplicate GunAudio playback requests.");
        }

        [Test]
        public void AllFiveWeapons_InLoadout_CanSwitchSequentiallyAndPlayCorrectClips()
        {
            GameObject playerGo = Track(new GameObject("Player"));
            PlayerShoot playerShoot = playerGo.AddComponent<PlayerShoot>();
            WeaponLoadout loadout = playerGo.AddComponent<WeaponLoadout>();
            SerializedObject pShootSo = new SerializedObject(playerShoot);
            pShootSo.FindProperty("_loadout").objectReferenceValue = loadout;
            pShootSo.ApplyModifiedProperties();

            GameStateController gameState = Track(new GameObject("GameState")).AddComponent<GameStateController>();
            gameState.StartGame();

            var weaponPrefabConfigs = new (string Path, string ExpectedClipName)[]
            {
                ("Assets/_Project/Weapons/Gun_Rifle.prefab", "shot_rifle"),
                ("Assets/_Project/Weapons/Gun_Pistol.prefab", "shot_pistol"),
                ("Assets/_Project/Weapons/Gun_Shotgun.prefab", "shot_shotgun"),
                ("Assets/_Project/Weapons/Gun_SMG.prefab", "shot_pistol"),
                ("Assets/_Project/Weapons/Gun_Launcher.prefab", "shot_shotgun")
            };

            List<Gun> instantiatedGuns = new List<Gun>();
            List<GunAudio> instantiatedAudios = new List<GunAudio>();

            for (int i = 0; i < weaponPrefabConfigs.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(weaponPrefabConfigs[i].Path);
                Assert.IsNotNull(prefab);

                GameObject instance = Track(Object.Instantiate(prefab, playerGo.transform));
                Gun gun = instance.GetComponent<Gun>();
                GunAudio audio = instance.GetComponent<GunAudio>();
                Assert.IsNotNull(gun);
                Assert.IsNotNull(audio);

                BindGameState(gun, gameState);
                audio.Initialize(gun);
                audio.ResetPlaybackCount();

                // Adding a weapon applies its saved build; configure the audio fixture afterwards.
                loadout.AddWeapon(gun, equipImmediately: i == 0);
                gun.ConfigureForTesting(new WeaponRuntime(new WeaponRuntimeConfig
                {
                    FireMode = Application.WeaponFireMode.Automatic,
                    FireInterval = 0f,
                    InfiniteAmmo = true
                }), null, instance.transform);

                instantiatedGuns.Add(gun);
                instantiatedAudios.Add(audio);
            }

            // Verify cycling through each weapon slot sequentially
            for (int i = 0; i < weaponPrefabConfigs.Length; i++)
            {
                bool equipped = loadout.EquipSlot(i);
                Assert.IsTrue(equipped, $"Failed to equip slot {i}");
                Assert.AreEqual(instantiatedGuns[i], playerShoot.ActiveGun);

                AudioClip clipPlayed = null;
                instantiatedAudios[i].PlaybackRequested += c => clipPlayed = c;

                playerShoot.TryShoot();

                Assert.AreEqual(1, instantiatedAudios[i].PlaybackCount, $"Active weapon {weaponPrefabConfigs[i].Path} should have recorded 1 playback.");
                Assert.IsNotNull(clipPlayed, $"Active weapon {weaponPrefabConfigs[i].Path} should have emitted a PlaybackRequested event.");
                Assert.AreEqual(weaponPrefabConfigs[i].ExpectedClipName, clipPlayed.name, $"Weapon {weaponPrefabConfigs[i].Path} fired unexpected clip.");

                // Verify all other weapons recorded 0 playbacks for this round
                for (int j = 0; j < instantiatedAudios.Count; j++)
                {
                    if (j != i)
                    {
                        Assert.AreEqual(0, instantiatedAudios[j].PlaybackCount, $"Inactive weapon {weaponPrefabConfigs[j].Path} must not have played audio.");
                    }
                }

                instantiatedAudios[i].ResetPlaybackCount();
            }
        }

        [Test]
        public void ArenaShowcase_SceneFile_IsSerializedAsTextYaml_AndHasNoStaleGunAudio()
        {
            string scenePath = "Assets/Scenes/ArenaShowcase.unity";
            Assert.IsTrue(File.Exists(scenePath), "ArenaShowcase scene file must exist.");

            using (StreamReader reader = new StreamReader(scenePath))
            {
                string firstLine = reader.ReadLine();
                Assert.IsNotNull(firstLine, "Scene file must not be empty.");
                Assert.IsTrue(firstLine.StartsWith("%YAML 1.1"), $"Scene file must start with '%YAML 1.1' text header, but was: '{firstLine}'");
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GunAudio[] audios = Object.FindObjectsByType<GunAudio>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < audios.Length; i++)
            {
                GunAudio ga = audios[i];
                Assert.IsNotNull(ga.GetComponent<Gun>() ?? ga.GetComponentInParent<Gun>(), $"GunAudio on '{ga.gameObject.name}' must be attached to a Gun (no standalone fixed-gun objects allowed).");
            }
        }
    }
}
