using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

namespace LizardCrossPlatform;

internal sealed record MechvibesPackInfo(string Name, string Author, string AudioEntryName, IReadOnlyList<AudioSegmentRange> Segments);

internal static class MechvibesPackImporter
{
    private const long MaxConfigBytes = 1_000_000;
    private const long MaxAudioBytes = 100_000_000;

    public static MechvibesPackInfo Inspect(string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var configEntry = archive.Entries.FirstOrDefault(entry =>
            !string.IsNullOrEmpty(entry.Name) && entry.Name.Equals("config.json", StringComparison.OrdinalIgnoreCase));
        if (configEntry is null || configEntry.Length > MaxConfigBytes)
        {
            throw new InvalidDataException("The ZIP does not contain a supported Mechvibes config.json.");
        }

        using var configStream = configEntry.Open();
        using var document = JsonDocument.Parse(configStream, new JsonDocumentOptions { MaxDepth = 32 });
        var root = document.RootElement;
        var audioName = GetString(root, "audio_file");
        var audioPath = audioName?.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(audioPath) || audioPath.StartsWith('/') || audioPath.Split('/').Any(part => part == ".."))
        {
            throw new InvalidDataException("The Mechvibes config does not name a safe audio file.");
        }

        var audioEntry = archive.GetEntry(audioPath) ?? archive.Entries.FirstOrDefault(entry =>
            !string.IsNullOrEmpty(entry.Name) && entry.Name.Equals(Path.GetFileName(audioPath), StringComparison.OrdinalIgnoreCase));
        if (audioEntry is null || audioEntry.Length == 0 || audioEntry.Length > MaxAudioBytes)
        {
            throw new InvalidDataException("The audio file is missing or too large.");
        }

        if (!root.TryGetProperty("definitions", out var definitions) || definitions.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("The Mechvibes config does not contain key definitions.");
        }

        var segments = new List<AudioSegmentRange>();
        foreach (var definition in definitions.EnumerateObject().Take(250))
        {
            var appKey = MapKey(definition.Name);
            if (appKey is null || !definition.Value.TryGetProperty("timing", out var timings) || timings.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var timing in timings.EnumerateArray())
            {
                if (!TryParseTiming(timing, out var start, out var end))
                {
                    continue;
                }

                if (end > start && end - start <= 1000)
                {
                    segments.Add(new AudioSegmentRange(appKey, start, end));
                    break;
                }
            }
        }

        if (segments.Count == 0)
        {
            throw new InvalidDataException("No supported keyboard keys or audio segments were found.");
        }

        var name = GetString(root, "name") ?? Path.GetFileNameWithoutExtension(zipPath);
        var author = GetString(root, "author") ?? string.Empty;
        return new MechvibesPackInfo(name, author, audioEntry.FullName, segments);
    }

    public static IReadOnlyDictionary<string, string> Import(string zipPath, MechvibesPackInfo pack, AudioEngine audioEngine)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var audioEntry = archive.GetEntry(pack.AudioEntryName)
            ?? throw new InvalidDataException("The soundpack audio file could not be found in the ZIP.");

        var tempAudioPath = Path.Combine(Path.GetTempPath(), $"lizard-pack-{Guid.NewGuid():N}{Path.GetExtension(audioEntry.Name)}");
        var safePackName = SafeFolderName(pack.Name);
        var packFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LizardKeyboardSounds",
            "Sounds",
            "Packs",
            $"{safePackName}-{Guid.NewGuid():N}");

        try
        {
            using (var source = audioEntry.Open())
            using (var destination = File.Create(tempAudioPath))
            {
                source.CopyTo(destination);
            }

            var outputFiles = audioEngine.ExtractSegmentsToWav(tempAudioPath, pack.Segments, packFolder);
            return outputFiles.ToDictionary(
                path => Path.GetFileNameWithoutExtension(path),
                path => path,
                StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(tempAudioPath))
            {
                File.Delete(tempAudioPath);
            }
        }
    }

    private static bool TryParseTiming(JsonElement timing, out double start, out double end)
    {
        start = 0;
        end = 0;
        if (timing.ValueKind == JsonValueKind.Array && timing.GetArrayLength() == 2)
        {
            var values = timing.EnumerateArray().ToArray();
            return values[0].TryGetDouble(out start)
                && values[1].TryGetDouble(out end)
                && double.IsFinite(start)
                && double.IsFinite(end);
        }

        if (timing.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var parts = timing.GetString()?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return parts is { Length: 2 }
            && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out start)
            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out end)
            && double.IsFinite(start)
            && double.IsFinite(end);
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string SafeFolderName(string value)
    {
        var safe = string.Concat(value.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character)).Trim();
        return string.IsNullOrWhiteSpace(safe) ? "Soundpack" : safe;
    }

    private static string? MapKey(string packKey)
    {
        if (packKey.StartsWith("Key", StringComparison.Ordinal) && packKey.Length == 4 && char.IsLetter(packKey[3]))
        {
            return $"Vc{char.ToUpperInvariant(packKey[3])}";
        }

        if (packKey.StartsWith("Digit", StringComparison.Ordinal) && packKey.Length == 6 && char.IsDigit(packKey[5]))
        {
            return $"Vc{packKey[5]}";
        }

        if (packKey.StartsWith("Numpad", StringComparison.Ordinal))
        {
            var suffix = packKey[6..];
            return suffix switch
            {
                "0" or "1" or "2" or "3" or "4" or "5" or "6" or "7" or "8" or "9" => $"VcNumPad{suffix}",
                "Add" => "VcNumPadAdd",
                "Subtract" => "VcNumPadSubtract",
                "Multiply" => "VcNumPadMultiply",
                "Divide" => "VcNumPadDivide",
                "Decimal" => "VcNumPadDecimal",
                "Enter" => "VcNumPadEnter",
                _ => null
            };
        }

        if (packKey.Length is >= 2 and <= 3 && packKey[0] == 'F' && int.TryParse(packKey[1..], out var fNumber) && fNumber is >= 1 and <= 12)
        {
            return $"VcF{fNumber}";
        }

        return packKey switch
        {
            "Backquote" => "VcBackQuote",
            "Minus" => "VcMinus",
            "Equal" => "VcEquals",
            "Backspace" => "VcBackspace",
            "Tab" => "VcTab",
            "BracketLeft" => "VcOpenBracket",
            "BracketRight" => "VcCloseBracket",
            "Backslash" => "VcBackSlash",
            "CapsLock" => "VcCapsLock",
            "Enter" => "VcEnter",
            "Semicolon" => "VcSemicolon",
            "Quote" => "VcQuote",
            "ShiftLeft" => "VcLeftShift",
            "ShiftRight" => "VcRightShift",
            "Comma" => "VcComma",
            "Period" => "VcPeriod",
            "Slash" => "VcSlash",
            "ControlLeft" => "VcLeftControl",
            "ControlRight" => "VcRightControl",
            "MetaLeft" => "VcLeftMeta",
            "MetaRight" => "VcRightMeta",
            "AltLeft" => "VcLeftAlt",
            "AltRight" => "VcRightAlt",
            "Space" => "VcSpace",
            "ContextMenu" => "VcMenu",
            "Escape" => "VcEscape",
            "PrintScreen" => "VcPrintScreen",
            "ScrollLock" => "VcScrollLock",
            "Pause" => "VcPause",
            "Insert" => "VcInsert",
            "Home" => "VcHome",
            "PageUp" => "VcPageUp",
            "Delete" => "VcDelete",
            "End" => "VcEnd",
            "PageDown" => "VcPageDown",
            "ArrowUp" => "VcUp",
            "ArrowDown" => "VcDown",
            "ArrowLeft" => "VcLeft",
            "ArrowRight" => "VcRight",
            "NumLock" => "VcNumLock",
            "IntlBackslash" => "VcIso",
            _ => null
        };
    }
}
