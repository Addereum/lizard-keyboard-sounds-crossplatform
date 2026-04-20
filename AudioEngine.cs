using System;
using System.IO;
using ManagedBass;

namespace LizardCrossPlatform
{
    public class AudioEngine : IDisposable
    {
        private int _sampleHandle;
        private string _soundPath;
        private float _volume = 1.0f;

        public float Volume 
        { 
            get => _volume; 
            set 
            { 
                _volume = value;
                // BASS volume is channel-based or global. 
                // We apply it per channel in Play().
            } 
        }

        public AudioEngine(string soundPath)
        {
            _soundPath = soundPath;
            
            // Initialize BASS (default device, 44100Hz)
            if (!Bass.Init())
            {
                // If already initialized or failed
            }

            LoadSample();
        }

        private void LoadSample()
        {
            if (File.Exists(_soundPath))
            {
                // Load into memory for rapid fire playback (polyphony)
                _sampleHandle = Bass.SampleLoad(_soundPath, 0, 0, 16, BassFlags.Default);
            }
        }

        public void Play()
        {
            if (_sampleHandle == 0) return;

            // Get a channel from the sample to play it polyphonically
            int channel = Bass.SampleGetChannel(_sampleHandle);
            if (channel != 0)
            {
                Bass.ChannelSetAttribute(channel, ChannelAttribute.Volume, _volume);
                Bass.ChannelPlay(channel);
            }
        }

        public void Dispose()
        {
            if (_sampleHandle != 0)
            {
                Bass.SampleFree(_sampleHandle);
            }
            Bass.Free();
        }
    }
}
