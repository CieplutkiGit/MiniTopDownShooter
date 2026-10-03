using Application.Weapons;
using Game.Workshop.Presentation;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Workshop.Presentation
{
    [CustomEditor(typeof(WeaponVisualProfile))]
    public class WeaponVisualProfileEditor : UnityEditor.Editor
    {
        private bool _partsFoldout = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var profile = (WeaponVisualProfile)target;

            EditorGUILayout.LabelField("Weapon Visual Profile", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Defines assembled poses, exploded offsets, meshes, and muzzle anchor offsets for modular weapons.", MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_weaponId"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Base Receiver", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_receiverPrefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_receiverMesh"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_receiverMaterial"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_receiverLocalPosition"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_receiverLocalRotation"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_receiverLocalScale"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Muzzle Anchor", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_defaultMuzzleOffset"));

            EditorGUILayout.Space();
            var partsProp = serializedObject.FindProperty("_parts");
            _partsFoldout = EditorGUILayout.Foldout(_partsFoldout, $"Part Visual Entries ({partsProp.arraySize})", true);

            if (_partsFoldout)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < partsProp.arraySize; i++)
                {
                    SerializedProperty elem = partsProp.GetArrayElementAtIndex(i);
                    SerializedProperty slotIdProp = elem.FindPropertyRelative("_slotId");
                    SerializedProperty partIdProp = elem.FindPropertyRelative("_partId");

                    string title = $"[{i}] {slotIdProp.stringValue} -> {partIdProp.stringValue}";
                    EditorGUILayout.LabelField(string.IsNullOrEmpty(title.Trim()) ? $"Part [{i}]" : title, EditorStyles.boldLabel);

                    EditorGUILayout.PropertyField(slotIdProp);
                    EditorGUILayout.PropertyField(partIdProp);
                    EditorGUILayout.PropertyField(elem.FindPropertyRelative("_prefab"));
                    EditorGUILayout.PropertyField(elem.FindPropertyRelative("_mesh"));
                    EditorGUILayout.PropertyField(elem.FindPropertyRelative("_material"));
                    EditorGUILayout.PropertyField(elem.FindPropertyRelative("_assembledLocalPosition"));
                    EditorGUILayout.PropertyField(elem.FindPropertyRelative("_assembledLocalRotation"));
                    EditorGUILayout.PropertyField(elem.FindPropertyRelative("_assembledLocalScale"));
                    EditorGUILayout.PropertyField(elem.FindPropertyRelative("_explodedLocalOffset"));
                    EditorGUILayout.PropertyField(elem.FindPropertyRelative("_muzzleOffset"));

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Capture Assembled Pose from Selected Transform", EditorStyles.miniButton))
                    {
                        if (Selection.activeTransform != null)
                        {
                            Undo.RecordObject(profile, "Capture Assembled Pose");
                            elem.FindPropertyRelative("_assembledLocalPosition").vector3Value = Selection.activeTransform.localPosition;
                            elem.FindPropertyRelative("_assembledLocalRotation").quaternionValue = Selection.activeTransform.localRotation;
                            elem.FindPropertyRelative("_assembledLocalScale").vector3Value = Selection.activeTransform.localScale;
                            serializedObject.ApplyModifiedProperties();
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Capture Pose", "Please select a Transform in the scene hierarchy first.", "OK");
                        }
                    }

                    if (GUILayout.Button("Capture Exploded Offset", EditorStyles.miniButton))
                    {
                        if (Selection.activeTransform != null)
                        {
                            Undo.RecordObject(profile, "Capture Exploded Offset");
                            Vector3 assembledPos = elem.FindPropertyRelative("_assembledLocalPosition").vector3Value;
                            elem.FindPropertyRelative("_explodedLocalOffset").vector3Value = Selection.activeTransform.localPosition - assembledPos;
                            serializedObject.ApplyModifiedProperties();
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Capture Pose", "Please select a Transform in the scene hierarchy first.", "OK");
                        }
                    }

                    if (GUILayout.Button("Remove", EditorStyles.miniButtonRight))
                    {
                        partsProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();
                }

                if (GUILayout.Button("Add Part Entry"))
                {
                    partsProp.InsertArrayElementAtIndex(partsProp.arraySize);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Preset Generators", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Populate Standard Rifle Parts"))
            {
                Undo.RecordObject(profile, "Populate Standard Rifle Parts");
                PopulateRiflePreset(profile);
                EditorUtility.SetDirty(profile);
            }
            if (GUILayout.Button("Populate Standard Pistol Parts"))
            {
                Undo.RecordObject(profile, "Populate Standard Pistol Parts");
                PopulatePistolPreset(profile);
                EditorUtility.SetDirty(profile);
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private static void PopulateRiflePreset(WeaponVisualProfile profile)
        {
            profile.WeaponId = WeaponWorkshopIds.Rifle;
            profile.DefaultMuzzleOffset = new Vector3(0f, 0f, 0.85f);

            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard")
            {
                AssembledLocalPosition = new Vector3(0f, 0f, 0.4f),
                ExplodedLocalOffset = new Vector3(0f, 0f, 0.5f),
                MuzzleOffset = new Vector3(0f, 0f, 0.85f)
            });

            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long")
            {
                AssembledLocalPosition = new Vector3(0f, 0f, 0.55f),
                ExplodedLocalOffset = new Vector3(0f, 0f, 0.65f),
                MuzzleOffset = new Vector3(0f, 0f, 1.15f)
            });

            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.short")
            {
                AssembledLocalPosition = new Vector3(0f, 0f, 0.28f),
                ExplodedLocalOffset = new Vector3(0f, 0f, 0.35f),
                MuzzleOffset = new Vector3(0f, 0f, 0.65f)
            });

            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.standard")
            {
                AssembledLocalPosition = new Vector3(0f, -0.2f, 0.1f),
                ExplodedLocalOffset = new Vector3(0f, -0.45f, 0f)
            });

            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.standard")
            {
                AssembledLocalPosition = new Vector3(0f, -0.18f, -0.15f),
                ExplodedLocalOffset = new Vector3(0f, -0.4f, -0.15f)
            });

            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.standard")
            {
                AssembledLocalPosition = new Vector3(0f, 0f, -0.4f),
                ExplodedLocalOffset = new Vector3(0f, 0f, -0.55f)
            });
        }

        private static void PopulatePistolPreset(WeaponVisualProfile profile)
        {
            profile.WeaponId = WeaponWorkshopIds.Pistol;
            profile.DefaultMuzzleOffset = new Vector3(0f, 0.05f, 0.45f);

            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.PistolSlots.Barrel, "pistol.barrel.standard")
            {
                AssembledLocalPosition = new Vector3(0f, 0.05f, 0.2f),
                ExplodedLocalOffset = new Vector3(0f, 0f, 0.4f),
                MuzzleOffset = new Vector3(0f, 0.05f, 0.45f)
            });

            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.PistolSlots.Magazine, "pistol.magazine.standard")
            {
                AssembledLocalPosition = new Vector3(0f, -0.15f, -0.05f),
                ExplodedLocalOffset = new Vector3(0f, -0.35f, 0f)
            });

            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.PistolSlots.Grip, "pistol.grip.standard")
            {
                AssembledLocalPosition = new Vector3(0f, -0.12f, -0.08f),
                ExplodedLocalOffset = new Vector3(0f, -0.25f, -0.2f)
            });

            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.PistolSlots.Slide, "pistol.slide.standard")
            {
                AssembledLocalPosition = new Vector3(0f, 0.08f, 0.05f),
                ExplodedLocalOffset = new Vector3(0f, 0.35f, 0f)
            });
        }
    }
}
