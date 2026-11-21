using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ManagedBass;

namespace LizardCrossPlatform;

public sealed class AudioEngine : IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<string, int> _samples = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialized;
    private bool _disposed;

    public string? Error { get; private set; }

    public AudioEngine()
    {
        try
        {
            _initialized = Bass.Init();
            if (!_initialized)
            {
                Error = $"BASS konnte nicht gestartet werden ({Bass.LastError}).";
            }
        }
        catch (Exception ex)
        {
            Error = "Die Audio-Bibliothek BASS fehlt. Lege bass.dll neben die App.";
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    public bool Play(string? soundPath, float masterVolume)
    {
        if (string.IsNullOrWhiteSpace(soundPath) || !File.Exists(soundPath))
        {
            Error = "Bitte wähle zuerst eine vorhandene Audiodatei aus.";
            return false;
        }

        lock (_gate)
        {
            if (!_initialized || _disposed)
            {
                return false;
            }

            try
            {
                var sample = GetOrLoadSample(soundPath);
                if (sample == 0)
                {
                    Error = $"Die Audiodatei konnte nicht geladen werden ({Bass.LastError}).";
                    return false;
                }

                var channel = Bass.SampleGetChannel(sample);
                if (channel == 0)
                {
                    Error = $"Der Sound konnte nicht abgespielt werden ({Bass.LastError}).";
                    return false;
                }

                Bass.ChannelSetAttribute(channel, ChannelAttribute.Volume, Math.Clamp(masterVolume, 0f, 1f));
                Bass.ChannelPlay(channel);
                Error = null;
                return true;
            }
            catch (Exception ex)
            {
                Error = "Die Audio-Bibliothek konnte den Sound nicht abspielen.";
                System.Diagnostics.Debug.WriteLine(ex);
                return false;
            }
        }
    }

    public IReadOnlyList<string> ExtractSegmentsToWav(string audioPath, IReadOnlyList<AudioSegmentRange> segments, string destinationFolder)
    {
        lock (_gate)
        {
            if (!_initialized || _disposed)
            {
                throw new InvalidOperationException(Error ?? "Audio decoder is unavailable.");
            }

            Directory.CreateDirectory(destinationFolder);
            var stream = Bass.CreateStream(audioPath, 0, 0, BassFlags.Decode | BassFlags.Prescan);
            if (stream == 0)
            {
                throw new InvalidDataException($"Could not decode the sound pack ({Bass.LastError}).");
            }

            try
            {
                var info = Bass.ChannelGetInfo(stream);
                if (info.Frequency <= 0 || info.Channels <= 0 || info.Channels > 8)
                {
                    throw new InvalidDataException("The sound pack has an unsupported audio format.");
                }

                var output = new List<string>(segments.Count);
                foreach (var segment in segments)
                {
                    if (segment.StartMilliseconds < 0 || segment.EndMilliseconds <= segment.StartMilliseconds || segment.EndMilliseconds - segment.StartMilliseconds > 1000)
                    {
                        continue;
                    }

                    var start = Bass.ChannelSeconds2Bytes(stream, segment.StartMilliseconds / 1000d);
                    var end = Bass.ChannelSeconds2Bytes(stream, segment.EndMilliseconds / 1000d);
                    if (start < 0 || end <= start || end - start > 2_000_000 || !Bass.ChannelSetPosition(stream, start, PositionFlags.Bytes))
                    {
                        continue;
                    }

                    var remaining = checked((int)(end - start));
                    using var pcm = new MemoryStream(remaining);
                    while (remaining > 0)
                    {
                        var buffer = new byte[Math.Min(remaining, 32 * 1024)];
                        var bytesRead = Bass.ChannelGetData(stream, buffer, buffer.Length);
                        if (bytesRead <= 0)
                        {
                            break;
                        }

                        pcm.Write(buffer, 0, bytesRead);
                        remaining -= bytesRead;
                    }

                    var pcmData = pcm.ToArray();
                    var blockAlign = info.Channels * 2;
                    var usableLength = pcmData.Length - (pcmData.Length % blockAlign);
                    if (usableLength == 0)
                    {
                        continue;
                    }

                    var fileName = string.Concat(segment.Name.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character)) + ".wav";
                    var destination = Path.Combine(destinationFolder, fileName);
                    WritePcmWave(destination, pcmData, usableLength, info.Frequency, info.Channels);
                    output.Add(destination);
                }

                return output;
            }
            finally
            {
                Bass.StreamFree(stream);
            }
        }
    }

    private static void WritePcmWave(string path, byte[] pcmData, int dataLength, int sampleRate, int channels)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataLength);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * 2);
        writer.Write((short)(channels * 2));
        writer.Write((short)16);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(dataLength);
        writer.Write(pcmData, 0, dataLength);
    }

    private int GetOrLoadSample(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (_samples.TryGetValue(fullPath, out var sample))
        {
            return sample;
        }

        sample = Bass.SampleLoad(fullPath, 0, 0, 16, BassFlags.Default);
        if (sample != 0)
        {
            _samples[fullPath] = sample;
        }

        return sample;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (!_initialized)
            {
                return;
            }

            try
            {
                foreach (var sample in _samples.Values)
                {
                    Bass.SampleFree(sample);
                }

                Bass.Free();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }
}

public readonly record struct AudioSegmentRange(string Name, double StartMilliseconds, double EndMilliseconds);
