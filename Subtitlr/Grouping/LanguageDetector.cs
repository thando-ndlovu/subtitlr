using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Subtitlr.Base.Grouping;

/// <summary>
/// Detects a subtitle's language two ways: from a language token embedded in
/// the filename (e.g. "Show.S01E01.ko.srt"), or, failing that, from the
/// script/character makeup of the file's own text.
/// </summary>
public static class LanguageDetector
{
    // Extend this map to recognize more filename language tags.
    private static readonly Dictionary<string, string> TokenMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "en", ["eng"] = "en", ["english"] = "en",
            ["ko"] = "ko", ["kor"] = "ko", ["korean"] = "ko", ["kr"] = "ko",
            ["ja"] = "ja", ["jp"] = "ja", ["jpn"] = "ja", ["japanese"] = "ja",
            ["zh"] = "zh", ["chi"] = "zh", ["chs"] = "zh", ["cht"] = "zh", ["chinese"] = "zh",
            ["es"] = "es", ["spa"] = "es", ["spanish"] = "es",
            ["fr"] = "fr", ["fre"] = "fr", ["fra"] = "fr", ["french"] = "fr",
            ["de"] = "de", ["ger"] = "de", ["deu"] = "de", ["german"] = "de",
            ["pt"] = "pt", ["por"] = "pt", ["portuguese"] = "pt",
            ["ru"] = "ru", ["rus"] = "ru", ["russian"] = "ru",
        };

    /// <summary>
    /// Looks for a recognizable language token among the filename's
    /// dot/underscore/dash-separated segments (scanned right-to-left, since
    /// the language tag conventionally sits just before the extension).
    /// If found, returns the remaining segments joined back together as the
    /// group key (i.e. everything that identifies "which episode/video" this
    /// file belongs to, with the language part removed).
    /// </summary>
    public static bool TryDetectFromFileName(string fileNameNoExt, out string groupKey, out string languageCode)
    {
        fileNameNoExt = fileNameNoExt.Replace("[cc]", "");

        var tokens = Regex.Split(fileNameNoExt, @"[._\-\s]+").Where(t => t.Length > 0).ToArray();

        for (int i = tokens.Length - 1; i >= 0; i--)
        {
            if (TokenMap.TryGetValue(tokens[i], out var code))
            {
                var remaining = tokens.Where((_, idx) => idx != i);
                groupKey = string.Join(".", remaining).ToLowerInvariant();
                languageCode = code;
                return true;
            }
        }

        groupKey = null;
        languageCode = null;
        return false;
    }

    /// <summary>
    /// Rough script-based fallback: counts characters in a few Unicode blocks
    /// to guess the dominant language. Good enough to tell Korean/Japanese/
    /// Chinese/Latin-script text apart; not a substitute for a real langid.
    /// </summary>
    public static string DetectFromContent(string sampleText)
    {
        int hangul = 0, latin = 0, hiragana = 0, katakana = 0, han = 0;

        foreach (var ch in sampleText)
        {
            if (ch >= 0xAC00 && ch <= 0xD7A3) hangul++;
            else if (ch >= 0x1100 && ch <= 0x11FF) hangul++;
            else if (ch >= 0x3040 && ch <= 0x309F) hiragana++;
            else if (ch >= 0x30A0 && ch <= 0x30FF) katakana++;
            else if (ch >= 0x4E00 && ch <= 0x9FFF) han++;
            else if ((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z')) latin++;
        }

        if (hangul > 0 && hangul >= latin) return "ko";
        if (hiragana + katakana > 0 && hiragana + katakana >= latin) return "ja";
        if (han > 0 && han >= latin) return "zh";
        return "en";
    }
}
