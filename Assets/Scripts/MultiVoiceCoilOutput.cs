using System;
using System.IO.Ports;
using UnityEngine;

namespace MultiVoiceCoilDebug
{
    public class MultiVoiceCoilOutput : IDisposable
    {
        private SerialPort _serialPort;

        private readonly byte[] _message = 
        {
            0xAA, // frame header
            0x27, 0x10, // Big-endian frequency * 100: 100Hz (value = 10000)
            0x00, // amplitude
            0x27, 0x10, // Big-endian frequency * 100: 100Hz (value = 10000)
            0x00, // amplitude
            0x27, 0x10, // Big-endian frequency * 100: 100Hz (value = 10000)
            0x00, // amplitude
            0x01, // do not respond
            0xBB // frame tail
        };

        private const int ChannelCount = 3;
        private readonly int _dumpCount;
        private readonly int[] _channels;
        private int _current = 0;
        public int[] PlaySignals { get; } = new int[ChannelCount];

        public MultiVoiceCoilOutput(string port, int dumpCount)
        {
            try
            {
                _serialPort = new SerialPort(port, 115200, Parity.None, 8, StopBits.One);
                _serialPort.Open();
            }
            catch
            {
                _serialPort?.Close();
                _serialPort?.Dispose();
                _serialPort = null;
            }

            _dumpCount = dumpCount;
            _channels = new int[ChannelCount * _dumpCount];
        }

        public void Send()
        {
            for (var i = 0; i < ChannelCount; i++)
            {
                var byteIndex = GetByteIndex(i);
                if (byteIndex < 0)
                {
                    continue;
                }

                var playSignal = 0;
                for (var j = 0; j < _dumpCount; j++)
                {
                    playSignal += _channels[i * _dumpCount + j];
                }

                playSignal /= _dumpCount;
                PlaySignals[i] = playSignal;
                _message[byteIndex] = (byte)(PlaySignals[i] / 4);
            }
            
            _serialPort?.Write(_message, 0, _message.Length);
            
            _current = (_current + 1) % _dumpCount;
            
            return;
                                                             
             static int GetByteIndex(int index)
             {
                 return index switch
                 {
                     0 => 3,
                     1 => 6,
                     2 => 9,
                     _ => -1
                 };
             }
        }

        public void PlayValue(int intensity, int perceptionIndex)
        {
            if (perceptionIndex is <= 0 or > 3)
            {
                return;
            }

            _channels[(perceptionIndex - 1) * _dumpCount + _current] = intensity;
        }

        public void StopAllImmediately()
        {
            for (var i = 0; i < _channels.Length; i++)
            {
                _channels[i] = 0;
            }

            _message[3] = 0b0;
            _message[6] = 0b0;
            _message[9] = 0b0;
            
            _serialPort?.Write(_message, 0, _message.Length);
        }

        public void Dispose()
        {
            _serialPort?.Close();
            _serialPort?.Dispose();
            _serialPort = null;
        }
    }
}