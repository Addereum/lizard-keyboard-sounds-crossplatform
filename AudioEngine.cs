using System;
using System.IO;
using System.Threading.Tasks;
using NetCoreAudio;

namespace LizardCrossPlatform
{
    public class AudioEngine
    {
        private Player _player;
        private string _soundPath;
        public float Volume { get; set; } = 1.0f;

        public AudioEngine(string soundPath)
        {
            _player = new Player();
            _soundPath = soundPath;
        }

        public void Play()
        {
            if (!File.Exists(_soundPath)) return;

            // In a real high-perf app, we'd use a mixer.
            // NetCoreAudio might have latency depending on the system player.
            Task.Run(async () => {
                try {
                    // NetCoreAudio volume is 0-100
                    await _player.SetVolume((byte)(Volume * 100));
                    await _player.Play(_soundPath);
                } catch { }
            });
        }
    }
}
