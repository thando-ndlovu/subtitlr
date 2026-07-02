using System;
using System.Collections.Generic;

namespace Subtitlr.Romanization;

/// <summary>
/// Add an entry here whenever a new IRomanizer is implemented for another
/// language (e.g. "ja" -> a Hepburn romanizer for Japanese).
/// </summary>
public static class RomanizerRegistry
{
    private static readonly Dictionary<string, IRomanizer> Map =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ko"] = new KoreanRomanizer(),
        };

    public static bool TryGet(string languageCode, out IRomanizer romanizer) =>
        Map.TryGetValue(languageCode, out romanizer);
}
