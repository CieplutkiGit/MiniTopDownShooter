using Application.Weapons;
using Game.Workshop.Presentation;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Workshop.Presentation
{
    [CustomEditor(typeof(WeaponPreviewView))]
    public class WeaponPreviewViewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var previewView = (WeaponPreviewView)target;

            // Combat component safety check
            var gun = previewView.GetComponent<Gun>();
            var affiliation = previewView.GetComponent<DamageAffiliation>();

            if (gun != null || affiliation != null)
            {
                EditorGUILayout.HelpBox(
                    "CRITICAL: WeaponPreviewView has combat components attached! Preview views must be visual-only and can NEVER fire or interact with health.",
                    MessageType.Error);

                if (GUILayout.Button("Remove Forbidden Combat Components"))
                {
                    Undo.RecordObject(previewView.gameObject, "Sanitize Preview View");
                    previewView.SanitizeCombatComponents();
                }

                EditorGUILayout.Space();
            }

            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Workshop Preview Controls", EditorStyles.boldLabel);

            // Exploded view controls
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(previewView.IsExploded ? "Collapse View (Assembled)" : "Explode View"))
            {
                previewView.SetExploded(!previewView.IsExploded);
            }

            if (GUILayout.Button("Instant Toggle"))
            {
                previewView.SetExplodedImmediate(!previewView.IsExploded);
            }
            EditorGUILayout.EndHorizontal();

            float newProgress = EditorGUILayout.Slider("Explosion Progress", previewView.ExplosionProgress, 0f, 1f);
            if (!Mathf.Approximately(newProgress, previewView.ExplosionProgress))
            {
                previewView.SetExplodedImmediate(newProgress > 0.5f);
            }

            // Interactive slot selection
            if (previewView.Assembler != null && previewView.Assembler.SpawnedParts.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Slot Selection & Highlights", EditorStyles.boldLabel);

                foreach (var kvp in previewView.Assembler.SpawnedParts)
                {
                    string slotId = kvp.Key;
                    bool isSelected = string.Equals(previewView.SelectedSlot, slotId, System.StringComparison.Ordinal);

                    GUI.backgroundColor = isSelected ? Color.cyan : Color.white;
                    if (GUILayout.Button($"Select {slotId}"))
                    {
                        previewView.TriggerSlotSelected(isSelected ? null : slotId);
                    }
                    GUI.backgroundColor = Color.white;
                }

                if (!string.IsNullOrEmpty(previewView.SelectedSlot))
                {
                    if (GUILayout.Button("Clear Highlight"))
                    {
                        previewView.SelectSlot(null);
                    }
                }
            }
        }
    }
}
