using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MultiVoiceCoilDebug
{
    public class MySlider : MonoBehaviour
    { 
        private Slider _slider;
        private TMP_Text _valueText;
        private string _label;
        
        public float Value 
        { 
            get => _slider.value;
            set => _slider.value = value;
        }

        private void Start()
        {
            _slider = GetComponent<Slider>();
            _valueText = GetComponentInChildren<TMP_Text>();
            _label = _valueText.text;
            _valueText.text = $"{_label}: {_slider.value:0}";
            _slider.onValueChanged.AddListener(value =>
            {
                _valueText.text = $"{_label}: {_slider.value:0}";
            });
        }
    }
}