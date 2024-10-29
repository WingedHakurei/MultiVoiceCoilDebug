using System.Collections.Generic;
using UnityEngine;
using XCharts.Runtime;

namespace MultiVoiceCoilDebug
{
    public class Waveform
    {
        private readonly float _timeWindow;
        private readonly float _flushDump;
        private int FlushCeiling => Mathf.Max(Mathf.RoundToInt(_flushDump / Time.fixedDeltaTime), 1);
        private int _flushCount;

        private readonly Dictionary<int, CachedSerie> _serieDatas = new();
        private readonly LineChart _chart;

        private readonly float _startTime;

        public Waveform(float amplitude, float timeWindow, float flushDump)
        {
            var canvas = GameObject.Find("Canvas").transform;
            _timeWindow = timeWindow;
            _flushDump = flushDump;

            var xCharts = new GameObject("WaveformChart");
            xCharts.transform.SetParent(canvas);

            _chart = xCharts.AddComponent<LineChart>();
            _chart.Init();
            _chart.SetSize(1000, 200);
            _chart.AnimationEnable(false);
            _chart.RemoveData();

            var rect = _chart.GetComponent<RectTransform>();
            rect.anchoredPosition3D = Vector3.zero;
            rect.localScale = Vector3.one;
            rect.pivot = Vector2.zero;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            var title = _chart.EnsureChartComponent<Title>();
            title.text = "Haptic Feedback Waveform";

            var xAxis = _chart.EnsureChartComponent<XAxis>();
            xAxis.animation.show = false;
            xAxis.type = Axis.AxisType.Value;
            xAxis.minMaxType = Axis.AxisMinMaxType.Custom;
            xAxis.axisLabel.formatterFunction =
                (_, value, _, _) => $"{value:F2}";
            var yAxis = _chart.EnsureChartComponent<YAxis>();
            yAxis.minMaxType = Axis.AxisMinMaxType.Custom;
            yAxis.min = 0;
            yAxis.max = amplitude;

            _startTime = Time.fixedTime;
        }

        public void FixedUpdate()
        {
            _flushCount++;
            if (_flushCount >= FlushCeiling)
            {
                Flush();
                _flushCount = 0;
            }
        }

        private void Flush()
        {
            var xAxis = _chart.EnsureChartComponent<XAxis>();
            xAxis.min = Mathf.Max(Time.fixedTime - _startTime - _timeWindow, 0);
            xAxis.max = Mathf.Max(Time.fixedTime - _startTime, _timeWindow);
            foreach (var serieId in _serieDatas.Keys)
            {
                Flush(serieId);
            }
        }

        private void Flush(int serieId)
        {
            if (!_serieDatas.TryGetValue(serieId, out var serieData))
            {
                return;
            }

            serieData.Flush();
        }

        public void AddData(int serieId, float time, float signal)
        {
            var serieData = EnsureSerie(serieId);
            serieData.Add(time, signal);
        }

        private CachedSerie EnsureSerie(int serieId)
        {
            if (!_serieDatas.TryGetValue(serieId, out var serieData))
            {
                Serie serie = _chart.AddSerie<Line>($"Serie {serieId}");
                serie.lineStyle.color = Color.HSVToRGB(serieId / 3f, 0.8f, 0.8f);
                serie.clip = true;
                serie.symbol.show = false;
                serieData = new CachedSerie(serie, _timeWindow, _startTime);
                _serieDatas.Add(serieId, serieData);
            }

            return serieData;
        }

        private class CachedSerie
        {
            private Serie _serie;
            private Queue<float> _timeQueue = new Queue<float>();
            private List<(float time, float signal)> _dataToAdd = new List<(float time, float signal)>();
            private float _timeWindow;
            private float _timeOffset;

            public CachedSerie(Serie serie, float timeWindow, float timeOffset)
            {
                _serie = serie;
                _timeWindow = timeWindow;
                _timeOffset = timeOffset;
            }

            public void Add(float time, float signal)
            {
                _timeQueue.Enqueue(time);
                _dataToAdd.Add((time, signal));
            }

            public void Flush()
            {
                var edgeTime = Time.fixedTime - _timeWindow;
                while (_timeQueue.Count > 0 && _timeQueue.Peek() < edgeTime)
                {
                    _timeQueue.Dequeue();
                    _serie.RemoveData(0);
                }

                foreach (var (time, signal) in _dataToAdd)
                {
                    _serie.AddXYData(time - _timeOffset, signal);
                }

                _dataToAdd.Clear();
            }
        }
    }
}