using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    [ExecuteAlways]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class SettingsValueLabel : MonoBehaviour
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private string _format = "{0:0.00}";

        public Slider TargetSlider
        {
            get => _slider;
            set
            {
                if (_slider == value) return;
                if (isActiveAndEnabled && _slider != null)
                {
                    _slider.onValueChanged.RemoveListener(OnValueChanged);
                }
                _slider = value;
                if (isActiveAndEnabled && _slider != null)
                {
                    _slider.onValueChanged.AddListener(OnValueChanged);
                }
                UpdateText(_slider != null ? _slider.value : 0f);
            }
        }

        public Slider Slider
        {
            get => TargetSlider;
            set => TargetSlider = value;
        }

        public string Format
        {
            get => _format;
            set
            {
                _format = value;
                UpdateText(_slider != null ? _slider.value : 0f);
            }
        }

        private void Awake()
        {
            if (_label == null)
            {
                _label = GetComponent<TextMeshProUGUI>();
            }
        }

        private void OnEnable()
        {
            if (_label == null)
            {
                _label = GetComponent<TextMeshProUGUI>();
            }

            if (_slider == null)
            {
                _slider = GetComponentInParent<Slider>();
            }

            if (_slider != null)
            {
                _slider.onValueChanged.AddListener(OnValueChanged);
                UpdateText(_slider.value);
            }
        }

        private void OnDisable()
        {
            if (_slider != null)
            {
                _slider.onValueChanged.RemoveListener(OnValueChanged);
            }
        }

        public void OnValueChanged(float value)
        {
            UpdateText(value);
        }

        public void UpdateText(float value)
        {
            if (_label == null)
            {
                _label = GetComponent<TextMeshProUGUI>();
            }

            if (_label == null) return;

            if (string.IsNullOrEmpty(_format))
            {
                _label.text = (_slider != null && _slider.wholeNumbers)
                    ? value.ToString("0")
                    : value.ToString("0.00");
            }
            else if (_format.Contains("{0"))
            {
                _label.text = string.Format(_format, value);
            }
            else
            {
                _label.text = value.ToString(_format);
            }
        }
    }
}
