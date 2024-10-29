using System.IO.Ports;
using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MultiVoiceCoilDebug
{
    public class MultiVoiceCoilTest : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown _portDropdown;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _stopButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Toggle _useMaskToggle;
        [SerializeField] private MySlider[] _sliders;
        [SerializeField] private float _maskCoef;
        [SerializeField] private int _primaryIndex;
        [SerializeField] private int _channelDumpCount;

        private int _channelCount;
        private bool _useMask;

        private MultiVoiceCoilOutput _multiVoiceCoilOutput;
        private bool _isRunning;

        private Waveform _waveform;

        private void Start()
        {
            _channelCount = _sliders.Length;
            
            UpdatePortDropdown();
            _startButton.onClick.AddListener(OnStart);
            _stopButton.onClick.AddListener(OnStop);
            _stopButton.interactable = false;
            _quitButton.onClick.AddListener(OnQuit);
            _useMaskToggle.onValueChanged.AddListener(OnToggleUseMask);
            _waveform = new Waveform(100, 2, 0.1f);
        }

        private void OnStart()
        {
            _portDropdown.interactable = false;
            _startButton.interactable = false;
            _multiVoiceCoilOutput = new MultiVoiceCoilOutput(_portDropdown.options[_portDropdown.value].text, _channelDumpCount);
            _stopButton.interactable = true;
            _isRunning = true;
        }
        
        private void OnStop()
        {
            OnStopAsync().Forget();

            return;
            
            async UniTaskVoid OnStopAsync()
            {
                _isRunning = false;
                _stopButton.interactable = false;
                foreach (var slider in _sliders)
                {
                    slider.Value = 0f;
                }

                await UniTask.WaitForSeconds(0.5f);
                _multiVoiceCoilOutput.StopAllImmediately();
                await UniTask.WaitForSeconds(0.5f);
                _multiVoiceCoilOutput.Dispose();
                _multiVoiceCoilOutput = null;
                UpdatePortDropdown();
                _portDropdown.interactable = true;
                _startButton.interactable = true;
            }
        }

        private void OnQuit()
        {
            OnQuitAsync().Forget();

            return;

            async UniTaskVoid OnQuitAsync()
            {
                _startButton.onClick.RemoveAllListeners();
                _stopButton.onClick.RemoveAllListeners();
                _quitButton.onClick.RemoveAllListeners();
                _useMaskToggle.onValueChanged.RemoveAllListeners();
                if (_isRunning)
                {
                    _isRunning = false;
                    _stopButton.interactable = false;
                    foreach (var slider in _sliders)
                    {
                        slider.Value = 0f;
                    }
                    await UniTask.WaitForSeconds(0.5f);
                    _multiVoiceCoilOutput.StopAllImmediately();
                    await UniTask.WaitForSeconds(0.5f);
                    _multiVoiceCoilOutput.Dispose();
                    _multiVoiceCoilOutput = null;
                }

#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
        
        private void OnToggleUseMask(bool useMask)
        {
            _useMask = useMask;
        }

        private void FixedUpdate()
        {
            if (!_isRunning)
            {
                return;
            }
            
            var time = Time.fixedTime;
            var maskThreshold = _useMask ? _sliders[_primaryIndex].Value * _maskCoef : 0;
            for (var i = 0; i < _channelCount; i++)
            {
                var value = (int)_sliders[i].Value;
                if (i != _primaryIndex && value < maskThreshold)
                {
                    value = 0;
                }
                _multiVoiceCoilOutput.PlayValue(value, i + 1);
            }
            _multiVoiceCoilOutput.Send();
            for (var i = 0; i < _channelCount; i++)
            {
                _waveform.AddData(i, time, _multiVoiceCoilOutput.PlaySignals[i]);
            }
            
            _waveform.FixedUpdate();
        }
        
        private void UpdatePortDropdown()
         {
             var ports = SerialPort.GetPortNames();
             _portDropdown.ClearOptions();
             _portDropdown.AddOptions(ports.ToList());
         }
    }
}