using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Generates standard 16-bit PCM 44.1kHz mono WAV audio clips for all game sound effects.
    /// Executes on editor launch via [InitializeOnLoad] and can also be triggered via Menu item or test runner.
    /// </summary>
    [InitializeOnLoad]
    public static class AudioClipGenerator
    {
        private const string AudioDirectory = "Assets/_Project/Audio";

        static AudioClipGenerator()
        {
            GenerateAllAudioClips();
        }

        [MenuItem("Tools/Mini Top Down Shooter/Generate Audio Clips")]
        public static void GenerateAllAudioClips()
        {
            if (!Directory.Exists(AudioDirectory))
            {
                Directory.CreateDirectory(AudioDirectory);
            }

            bool generatedAny = false;

            // Weapon shots
            generatedAny |= EnsureClip("shot_rifle.wav", GenerateRifleShot(), "a0110000000000000000000000000001");
            generatedAny |= EnsureClip("shot_pistol.wav", GeneratePistolShot(), "a0110000000000000000000000000002");
            generatedAny |= EnsureClip("shot_shotgun.wav", GenerateShotgunShot(), "a0110000000000000000000000000003");

            // Enemy sounds
            generatedAny |= EnsureClip("enemy_hit.wav", GenerateEnemyHit(), "a0110000000000000000000000000004");
            generatedAny |= EnsureClip("enemy_death.wav", GenerateEnemyDeath(), "a0110000000000000000000000000005");

            // Player sounds
            generatedAny |= EnsureClip("player_hit.wav", GeneratePlayerHit(), "a0110000000000000000000000000006");
            generatedAny |= EnsureClip("player_death.wav", GeneratePlayerDeath(), "a0110000000000000000000000000007");

            // Lifecycle sounds
            generatedAny |= EnsureClip("wave_start.wav", GenerateWaveStart(), "a0110000000000000000000000000008");
            generatedAny |= EnsureClip("victory.wav", GenerateVictorySound(), "a0110000000000000000000000000009");
            generatedAny |= EnsureClip("game_over.wav", GenerateGameOverSound(), "a0110000000000000000000000000010");
            generatedAny |= EnsureClip("ui_click.wav", GenerateUIClick(), "a0110000000000000000000000000011");

            // Music loop
            generatedAny |= EnsureClip("combat_music.wav", GenerateCombatMusic(), "a0110000000000000000000000000012");

            if (generatedAny)
            {
                AssetDatabase.Refresh();
            }
        }

        private static bool EnsureClip(string fileName, byte[] wavData, string guid)
        {
            string filePath = Path.Combine(AudioDirectory, fileName);
            string metaPath = filePath + ".meta";
            bool created = false;

            if (!File.Exists(filePath))
            {
                File.WriteAllBytes(filePath, wavData);
                created = true;
            }

            if (!File.Exists(metaPath))
            {
                string metaContent =
$@"fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 6
  defaultBitsPerSample: 16
  format: 0
  loadType: 0
  preloadAudioData: 1
  loadInBackground: 0
  ambisonic: 0
  3D: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
";
                File.WriteAllText(metaPath, metaContent);
                created = true;
            }

            return created;
        }

        public static byte[] CreateWav(float[] samples, int sampleRate = 44100)
        {
            int numSamples = samples.Length;
            int subchunk2Size = numSamples * 2; // 16-bit mono = 2 bytes per sample
            int chunkSize = 36 + subchunk2Size;
            int byteRate = sampleRate * 2;

            byte[] wav = new byte[44 + subchunk2Size];
            using (MemoryStream ms = new MemoryStream(wav))
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                // RIFF chunk
                bw.Write(new char[] { 'R', 'I', 'F', 'F' });
                bw.Write(chunkSize);
                bw.Write(new char[] { 'W', 'A', 'V', 'E' });

                // fmt chunk
                bw.Write(new char[] { 'f', 'm', 't', ' ' });
                bw.Write(16); // Subchunk1Size (16 for PCM)
                bw.Write((short)1); // AudioFormat (1 = PCM)
                bw.Write((short)1); // NumChannels (1 = Mono)
                bw.Write(sampleRate);
                bw.Write(byteRate);
                bw.Write((short)2); // BlockAlign (NumChannels * BitsPerSample / 8)
                bw.Write((short)16); // BitsPerSample

                // data chunk
                bw.Write(new char[] { 'd', 'a', 't', 'a' });
                bw.Write(subchunk2Size);

                for (int i = 0; i < numSamples; i++)
                {
                    float clamped = Mathf.Clamp(samples[i], -1f, 1f);
                    short val = (short)(clamped * 32767f);
                    bw.Write(val);
                }
            }

            return wav;
        }

        // --- Sound Synthesizers ---

        private static byte[] GenerateRifleShot()
        {
            const int sampleRate = 44100;
            float duration = 0.12f;
            int count = (int)(sampleRate * duration);
            float[] samples = new float[count];
            System.Random rng = new System.Random(42);

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / count;
                float env = Mathf.Exp(-progress * 25f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float tone = Mathf.Sin(2f * Mathf.PI * 440f * Mathf.Exp(-progress * 15f) * t);
                samples[i] = (noise * 0.7f + tone * 0.3f) * env;
            }

            return CreateWav(samples, sampleRate);
        }

        private static byte[] GeneratePistolShot()
        {
            const int sampleRate = 44100;
            float duration = 0.15f;
            int count = (int)(sampleRate * duration);
            float[] samples = new float[count];
            System.Random rng = new System.Random(1337);

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / count;
                float env = Mathf.Exp(-progress * 20f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float freq = 600f * Mathf.Exp(-progress * 12f);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                samples[i] = (noise * 0.5f + tone * 0.5f) * env;
            }

            return CreateWav(samples, sampleRate);
        }

        private static byte[] GenerateShotgunShot()
        {
            const int sampleRate = 44100;
            float duration = 0.25f;
            int count = (int)(sampleRate * duration);
            float[] samples = new float[count];
            System.Random rng = new System.Random(999);

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / count;
                float env = Mathf.Exp(-progress * 12f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float sub = Mathf.Sin(2f * Mathf.PI * 90f * t);
                samples[i] = (noise * 0.8f + sub * 0.2f) * env;
            }

            return CreateWav(samples, sampleRate);
        }

        private static byte[] GenerateEnemyHit()
        {
            const int sampleRate = 44100;
            float duration = 0.08f;
            int count = (int)(sampleRate * duration);
            float[] samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / count;
                float env = Mathf.Exp(-progress * 30f);
                float freq = 500f - progress * 300f;
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
            }

            return CreateWav(samples, sampleRate);
        }

        private static byte[] GenerateEnemyDeath()
        {
            const int sampleRate = 44100;
            float duration = 0.35f;
            int count = (int)(sampleRate * duration);
            float[] samples = new float[count];
            System.Random rng = new System.Random(77);

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / count;
                float env = Mathf.Exp(-progress * 10f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float freq = 220f * Mathf.Exp(-progress * 8f);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                samples[i] = (tone * 0.5f + noise * 0.5f) * env;
            }

            return CreateWav(samples, sampleRate);
        }

        private static byte[] GeneratePlayerHit()
        {
            const int sampleRate = 44100;
            float duration = 0.15f;
            int count = (int)(sampleRate * duration);
            float[] samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / count;
                float env = Mathf.Exp(-progress * 20f);
                float freq = 180f * Mathf.Exp(-progress * 5f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
            }

            return CreateWav(samples, sampleRate);
        }

        private static byte[] GeneratePlayerDeath()
        {
            const int sampleRate = 44100;
            float duration = 0.6f;
            int count = (int)(sampleRate * duration);
            float[] samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / count;
                float env = Mathf.Exp(-progress * 5f);
                float freq = 200f - progress * 150f;
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
            }

            return CreateWav(samples, sampleRate);
        }

        private static byte[] GenerateWaveStart()
        {
            const int sampleRate = 44100;
            float duration = 0.4f;
            int count = (int)(sampleRate * duration);
            float[] samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / count;
                float env = 1f - progress;
                float tone1 = Mathf.Sin(2f * Mathf.PI * 440f * t);
                float tone2 = Mathf.Sin(2f * Mathf.PI * 880f * t);
                samples[i] = (tone1 * 0.5f + tone2 * 0.5f) * env;
            }

            return CreateWav(samples, sampleRate);
        }

        private static byte[] GenerateVictorySound()
        {
            const int sampleRate = 44100;
            float duration = 0.8f;
            int count = (int)(sampleRate * duration);
            float[] samples = new float[count];

            // C major triad arpeggio: C5 (523Hz), E5 (659Hz), G5 (784Hz), C6 (1046Hz)
            float[] chordFreqs = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f };
            float noteDur = duration / chordFreqs.Length;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                int noteIdx = Mathf.Clamp((int)(t / noteDur), 0, chordFreqs.Length - 1);
                float noteT = t - (noteIdx * noteDur);
                float env = Mathf.Exp(-noteT * 6f);
                float freq = chordFreqs[noteIdx];
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
            }

            return CreateWav(samples, sampleRate);
        }

        private static byte[] GenerateGameOverSound()
        {
            const int sampleRate = 44100;
            float duration = 0.8f;
            int count = (int)(sampleRate * duration);
            float[] samples = new float[count];

            // Tragic descending minor triad: G4 (392Hz), Eb4 (311Hz), C4 (261Hz)
            float[] chordFreqs = new float[] { 392.00f, 311.13f, 261.63f };
            float noteDur = duration / chordFreqs.Length;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                int noteIdx = Mathf.Clamp((int)(t / noteDur), 0, chordFreqs.Length - 1);
                float noteT = t - (noteIdx * noteDur);
                float env = Mathf.Exp(-noteT * 4f);
                float freq = chordFreqs[noteIdx];
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
            }

            return CreateWav(samples, sampleRate);
        }

        private static byte[] GenerateUIClick()
        {
            const int sampleRate = 44100;
            float duration = 0.03f;
            int count = (int)(sampleRate * duration);
            float[] samples = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / count;
                float env = 1f - progress;
                samples[i] = Mathf.Sin(2f * Mathf.PI * 1200f * t) * env * 0.5f;
            }

            return CreateWav(samples, sampleRate);
        }

        private static byte[] GenerateCombatMusic()
        {
            const int sampleRate = 44100;
            float duration = 4.0f; // 4 seconds at 120 BPM = exactly 8 beats (2 bars of 4/4)
            int count = (int)(sampleRate * duration);
            float[] samples = new float[count];

            // Bass note frequencies: A1 (55Hz), C2 (65.41Hz), D2 (73.42Hz), E2 (82.41Hz)
            float[] bassSequence = new float[] { 55.00f, 55.00f, 65.41f, 55.00f, 73.42f, 73.42f, 82.41f, 55.00f };
            float beatDur = duration / bassSequence.Length; // 0.5s per eighth-note

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                int beatIdx = Mathf.Clamp((int)(t / beatDur), 0, bassSequence.Length - 1);
                float beatT = t - (beatIdx * beatDur);
                float beatProgress = beatT / beatDur;

                // Bass synth: fundamental + 2nd harmonic with exponential decay
                float bassFreq = bassSequence[beatIdx];
                float bassEnv = Mathf.Exp(-beatProgress * 5f);
                float bass = (Mathf.Sin(2f * Mathf.PI * bassFreq * t) * 0.7f +
                              Mathf.Sin(4f * Mathf.PI * bassFreq * t) * 0.3f) * bassEnv;

                // Percussion kick transient on every quarter note (beats 0, 2, 4, 6 in eighth-notes = 0, 1, 2, 3 in quarter notes)
                float kick = 0f;
                float quarterT = t % 1.0f;
                if (quarterT < 0.2f)
                {
                    float kickEnv = Mathf.Exp(-quarterT * 22f);
                    float kickFreq = 120f * Mathf.Exp(-quarterT * 18f);
                    kick = Mathf.Sin(2f * Mathf.PI * kickFreq * quarterT) * kickEnv * 0.6f;
                }

                // Hi-hat noise on off-beats
                float hat = 0f;
                float eighthT = t % 0.5f;
                if (eighthT > 0.25f && eighthT < 0.35f)
                {
                    float hatT = eighthT - 0.25f;
                    float hatEnv = Mathf.Exp(-hatT * 40f);
                    float noise = ((float)((i * 1103515245 + 12345) & 0x7fffffff) / 0x7fffffff) * 2f - 1f;
                    hat = noise * hatEnv * 0.2f;
                }

                // Atmospheric background pad (A minor chord: A2 110Hz, C3 130.8Hz, E3 164.8Hz)
                float pad = (Mathf.Sin(2f * Mathf.PI * 110.00f * t) * 0.12f +
                             Mathf.Sin(2f * Mathf.PI * 130.81f * t) * 0.08f +
                             Mathf.Sin(2f * Mathf.PI * 164.81f * t) * 0.08f);

                samples[i] = (bass * 0.35f + kick + hat + pad) * 0.75f;
            }

            return CreateWav(samples, sampleRate);
        }
    }
}
