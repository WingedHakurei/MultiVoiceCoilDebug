using System;
using System.Linq;
using System.Numerics;
using UnityEngine;

namespace MultiVoiceCoilDebug
{
    public static class MaskCodec
    {
        public static void Mask(float[][] waves, int primaryIndex)
        {
            var primaryWave = waves[primaryIndex];
            var sample = primaryWave.Length;
            if (waves.Any(arr => arr.Length != sample))
            {
                throw new ArgumentException("Mask signal lengths are not the same.");
            }

            var mainFrequency = GetMainFrequency(primaryWave, 50);
            var maskCoef = GetMaskCoef(mainFrequency);

            for (var i = 0; i < sample; i++)
            {
                var threshold = Mathf.Abs(primaryWave[i] * maskCoef);
                for (var j = 0; j < waves.Length; j++)
                {
                    if (j == primaryIndex)
                    {
                        continue;
                    }

                    var secondary = waves[j];
                    if (Mathf.Abs(secondary[i]) < threshold)
                    {
                        secondary[i] = 0f;
                    }
                }
            }

            return;

            static float GetMainFrequency(float[] wave, float sampleRate)
            {
                var directComponent = wave.Sum() / wave.Length;
                // pad the alternating component to the power of 2
                var alternatingComponent = new Complex[Mathf.NextPowerOfTwo(wave.Length)];
                // remove the direct component
                for (var i = 0; i < wave.Length; i++)
                {
                    alternatingComponent[i] = new Complex(wave[i] - directComponent, 0);
                }

                var spectrum = FastFourierTransform(alternatingComponent);
                var maxIndex = 0;
                var maxMagnitude = 0.0;
                for (var i = 0; i < spectrum.Length; i++)
                {
                    if (spectrum[i].Magnitude <= maxMagnitude)
                    {
                        continue;
                    }

                    maxMagnitude = spectrum[i].Magnitude;
                    maxIndex = i;
                }

                return sampleRate * maxIndex / spectrum.Length;

                static Complex[] FastFourierTransform(Complex[] wave)
                {
                    var n = wave.Length;
                    if (n <= 1)
                    {
                        return wave;
                    }

                    var even = new Complex[n / 2];
                    var odd = new Complex[n / 2];

                    for (var i = 0; i < n / 2; i++)
                    {
                        even[i] = wave[i * 2];
                        odd[i] = wave[i * 2 + 1];
                    }

                    var evenFFT = FastFourierTransform(even);
                    var oddFFT = FastFourierTransform(odd);

                    var output = new Complex[n];
                    for (var i = 0; i < n / 2; i++)
                    {
                        var angle = -2 * Math.PI * i / n;
                        var w = new Complex(Math.Cos(angle), Math.Sin(angle));
                        output[i] = evenFFT[i] + w * oddFFT[i];
                        output[i + n / 2] = evenFFT[i] - w * oddFFT[i];
                    }

                    return output;
                }
            }

            static float GetMaskCoef(float mainFrequency)
            {
                mainFrequency /= 1000;
                var ret = 0f;
                var power = 1f;
                ReadOnlySpan<float> maskModel = stackalloc float[4]
                {
                    0.32317964f,
                    -0.74925659f,
                    2.7732905f,
                    -1.30907623f
                };
                foreach (var coef in maskModel)
                {
                    ret += coef * power;
                    power *= mainFrequency;
                }

                return ret;
            }
        }
    }
}