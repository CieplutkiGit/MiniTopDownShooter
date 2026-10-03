using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Flow.Editor
{
    public static class ProductionSettingsPresentation
    {
        public static void Apply() => Apply(SceneManager.GetActiveScene());

        public static void Apply(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) scene = SceneManager.GetActiveScene();

            SettingsUI ui = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if ((ui = root.GetComponentInChildren<SettingsUI>(true)) != null) break;
            if (ui == null) return;

            CanvasScaler scaler = ui.GetComponentInParent<CanvasScaler>(true);
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            SerializedObject so = new SerializedObject(ui);
            GameObject panelGo = so.FindProperty("_panel").objectReferenceValue as GameObject;
            if (panelGo == null)
            {
                Transform p = ui.transform.Find("SettingsPanel") ?? ui.transform.Find("Panel");
                panelGo = p != null ? p.gameObject : ui.gameObject;
            }
            RectTransform panel = panelGo.GetComponent<RectTransform>();
            SetRect(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 640f));
            var background = panelGo.GetComponent<Image>() ?? panelGo.AddComponent<Image>();
            background.color = new Color(.025f, .045f, .065f, .98f);

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                TextMeshProUGUI existing = ui.GetComponentInChildren<TextMeshProUGUI>(true);
                if (existing != null) font = existing.font;
            }

            for (int i = panel.childCount - 1; i >= 0; i--)
            {
                Transform child = panel.GetChild(i);
                string n = child.name;
                if ((n.EndsWith("Label") && !n.StartsWith("Row_")) || n == "MasterLabel" || n == "SensitivityLabel")
                    Object.DestroyImmediate(child.gameObject);
            }

            RectTransform title = GetOrCreateRect(panel, "Title");
            SetRect(title, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(700f, 50f));
            TextMeshProUGUI titleTmp = GetOrCreateTMP(title, "Text", font);
            titleTmp.text = "SETTINGS"; titleTmp.fontSize = 32f; titleTmp.alignment = TextAlignmentOptions.Center;

            Slider master = GetSlider(so, "_masterVolumeSlider", panel, "MasterSlider");
            Slider music = GetSlider(so, "_musicVolumeSlider", panel, "MusicSlider");
            Slider sfx = GetSlider(so, "_sfxVolumeSlider", panel, "SFXSlider");
            Slider sens = GetSlider(so, "_sensitivitySlider", panel, "SensitivitySlider");
            Slider dead = GetSlider(so, "_deadzoneSlider", panel, "DeadzoneSlider");
            Slider touchScale = GetSlider(so, "_touchScaleSlider", panel, "TouchScaleSlider");

            float startY = -85f;
            var rows = new (string name, string label, Slider s)[] {
                ("Row_Master", "Master Volume", master), ("Row_Music", "Music Volume", music),
                ("Row_SFX", "Sound effects volume", sfx), ("Row_Sens", "Aim sensitivity", sens),
                ("Row_Deadzone", "Stick deadzone", dead)
            };
            for (int i = 0; i < rows.Length; i++)
                SetupSliderRow(panel, rows[i].name, rows[i].label, rows[i].s, font, startY, i);

            Toggle toggle = SetupToggleRow(so, panel, font, startY, 5);
            SetupSliderRow(panel, "Row_TouchScale", "Touch button size", touchScale, font, startY, 6);

            Button saveBtn = SetupButton(GetButton(so, "_saveButton", panel, "SaveButton"), "Save", new Vector2(-230f, -540f), font);
            Button defBtn = SetupButton(GetButton(so, "_resetDefaultsButton", panel, "DefaultsButton", "ResetDefaultsButton"), "Defaults", new Vector2(0f, -540f), font);
            Button closeBtn = SetupButton(GetButton(so, "_closeButton", panel, "CloseButton"), "Close", new Vector2(230f, -540f), font);

            var propBindings = new (string p, Object v)[] {
                ("_panel", panelGo), ("_masterVolumeSlider", master), ("_musicVolumeSlider", music),
                ("_sfxVolumeSlider", sfx), ("_sensitivitySlider", sens), ("_deadzoneSlider", dead),
                ("_mobileTouchControlsToggle", toggle), ("_touchScaleSlider", touchScale),
                ("_saveButton", saveBtn), ("_resetDefaultsButton", defBtn), ("_closeButton", closeBtn)
            };
            foreach (var (p, v) in propBindings) so.FindProperty(p).objectReferenceValue = v;
            if (so.FindProperty("_firstSelected").objectReferenceValue == null)
                so.FindProperty("_firstSelected").objectReferenceValue = closeBtn != null ? (Selectable)closeBtn : saveBtn;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(ui);
            if (!UnityEngine.Application.isPlaying && scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void SetupSliderRow(RectTransform panel, string rowName, string labelText, Slider slider, TMP_FontAsset font, float startY, int index)
        {
            RectTransform row = GetOrCreateRect(panel, rowName);
            SetRect(row, new Vector2(0.5f, 1f), new Vector2(0f, startY - index * 54f), new Vector2(700f, 54f));
            TextMeshProUGUI label = GetOrCreateTMP(row, "Label", font);
            SetRect(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(220f, 40f));
            label.text = labelText; label.fontSize = 20f; label.alignment = TextAlignmentOptions.MidlineLeft;
            if (slider == null) return;

            RectTransform sRect = slider.GetComponent<RectTransform>();
            sRect.SetParent(row, false);
            SetRect(sRect, new Vector2(0f, 0.5f), new Vector2(240f, 0f), new Vector2(360f, 32f));
            for (int i = sRect.childCount - 1; i >= 0; i--)
                if (sRect.GetChild(i).name.EndsWith("Label")) Object.DestroyImmediate(sRect.GetChild(i).gameObject);

            TextMeshProUGUI valTmp = GetOrCreateTMP(row, "ValueLabel", font);
            SetRect(valTmp.rectTransform, new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(70f, 40f));
            valTmp.fontSize = 20f; valTmp.alignment = TextAlignmentOptions.MidlineRight;

            SettingsValueLabel svl = valTmp.GetComponent<SettingsValueLabel>() ?? valTmp.gameObject.AddComponent<SettingsValueLabel>();
            svl.Format = rowName == "Row_Sens" || rowName == "Row_TouchScale" ? "{0:0.00}x" : "{0:0%}";
            svl.TargetSlider = slider; svl.UpdateText(slider.value);
        }

        private static Toggle SetupToggleRow(SerializedObject so, RectTransform panel, TMP_FontAsset font, float startY, int index)
        {
            RectTransform row = GetOrCreateRect(panel, "Row_TouchToggle");
            SetRect(row, new Vector2(0.5f, 1f), new Vector2(0f, startY - index * 54f), new Vector2(700f, 54f));

            Toggle toggle = so.FindProperty("_mobileTouchControlsToggle").objectReferenceValue as Toggle ?? row.GetComponentInChildren<Toggle>(true);
            if (toggle == null)
            {
                RectTransform tRect = GetOrCreateRect(row, "TouchToggle");
                toggle = tRect.gameObject.AddComponent<Toggle>();
                RectTransform bg = GetOrCreateRect(tRect, "Background");
                SetRect(bg, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(32f, 32f));
                Image bgImg = bg.gameObject.AddComponent<Image>(); bgImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
                RectTransform chk = GetOrCreateRect(bg, "Checkmark");
                SetRect(chk, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f));
                Image chkImg = chk.gameObject.AddComponent<Image>(); chkImg.color = Color.white;
                toggle.targetGraphic = bgImg; toggle.graphic = chkImg; toggle.isOn = true;
            }
            else toggle.transform.SetParent(row, false);

            SetRect(toggle.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(240f, 0f), new Vector2(400f, 40f));
            TextMeshProUGUI label = GetOrCreateTMP(row, "Label", font);
            SetRect(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(220f, 40f));
            label.text = "Mobile Touch Controls"; label.fontSize = 20f; label.alignment = TextAlignmentOptions.MidlineLeft;

            TextMeshProUGUI caption = GetOrCreateTMP(toggle.GetComponent<RectTransform>(), "Caption", font);
            SetRect(caption.rectTransform, new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(250f, 32f));
            caption.text = "Enabled"; caption.fontSize = 18f; caption.alignment = TextAlignmentOptions.MidlineLeft;
            return toggle;
        }

        private static Button SetupButton(Button btn, string caption, Vector2 pos, TMP_FontAsset font)
        {
            if (btn == null) return null;
            RectTransform rt = btn.GetComponent<RectTransform>();
            SetRect(rt, new Vector2(0.5f, 1f), pos, new Vector2(200f, 60f));
            TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true) ?? GetOrCreateTMP(rt, "Text", font);
            tmp.text = caption; tmp.fontSize = 20f; tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
            tmp.enableAutoSizing = true; tmp.fontSizeMin = 13f; tmp.fontSizeMax = 20f;
            if (font != null) tmp.font = font;
            tmp.rectTransform.anchorMin = Vector2.zero; tmp.rectTransform.anchorMax = Vector2.one;
            tmp.rectTransform.sizeDelta = tmp.rectTransform.anchoredPosition = Vector2.zero;
            return btn;
        }

        private static Slider GetSlider(SerializedObject so, string prop, RectTransform panel, string fallback)
        {
            Slider s = so.FindProperty(prop).objectReferenceValue as Slider;
            if (s != null) return s;
            foreach (Slider c in panel.GetComponentsInChildren<Slider>(true)) if (c.name == fallback) return c;
            return null;
        }

        private static Button GetButton(SerializedObject so, string prop, RectTransform panel, params string[] fallbacks)
        {
            Button b = so.FindProperty(prop).objectReferenceValue as Button;
            if (b != null) return b;
            foreach (Button c in panel.GetComponentsInChildren<Button>(true))
                foreach (string f in fallbacks) if (c.name.Equals(f, System.StringComparison.OrdinalIgnoreCase)) return c;
            return null;
        }

        private static RectTransform GetOrCreateRect(RectTransform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t != null) return t as RectTransform;
            RectTransform rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private static TextMeshProUGUI GetOrCreateTMP(RectTransform parent, string name, TMP_FontAsset font)
        {
            RectTransform rt = GetOrCreateRect(parent, name);
            TextMeshProUGUI tmp = rt.GetComponent<TextMeshProUGUI>() ?? rt.gameObject.AddComponent<TextMeshProUGUI>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            tmp.color = new Color(.78f, .95f, 1f);
            tmp.raycastTarget = false;
            if (font != null) tmp.font = font;
            return tmp;
        }

        private static void SetRect(RectTransform rt, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}
