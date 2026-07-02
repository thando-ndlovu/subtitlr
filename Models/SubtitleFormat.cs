using System;
using System.Collections.Generic;

namespace Subtitlr.Models;

public enum SubtitleFormat
{
    Srt,
    Unknown
}

public static class SubtitleFormatHelper
{
    // Extend this map when new formats (e.g. .ass, .vtt) get parser/writer support.
    private static readonly Dictionary<string, SubtitleFormat> ExtensionMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".srt"] = SubtitleFormat.Srt,
        };

    public static SubtitleFormat FromExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension)) return SubtitleFormat.Unknown;
        return ExtensionMap.TryGetValue(extension, out var fmt) ? fmt : SubtitleFormat.Unknown;
    }

    public static IEnumerable<string> SupportedExtensions => ExtensionMap.Keys;
}
