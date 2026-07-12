using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Subtitlr.Base.Models;

namespace Subtitlr.Base.Parsing;

/// <summary>
/// Parses standard SubRip (.srt) files. Tolerant of missing/duplicate index
/// numbers, "." or "," decimal separators in timecodes, and stray blank lines.
/// Note: intentional blank lines *inside* a single cue's text are collapsed;
/// this is a deliberate simplification since real-world .srt files rarely rely
/// on them and it makes block-splitting far more robust against malformed files.
/// </summary>
public class SrtParser : ISubtitleParser
{
    private static readonly Regex TimeLineRegex = new(
        @"(?<start>\d{1,2}:\d{2}:\d{2}[,.]\d{1,3})\s*-{2,3}>\s*(?<end>\d{1,2}:\d{2}:\d{2}[,.]\d{1,3})",
        RegexOptions.Compiled);

    public List<SubtitleEntry> Parse(string path)
    {
        var raw = File.ReadAllText(path);
        return ParseText(raw);
    }

    public List<SubtitleEntry> ParseText(string raw)
    {
        var entries = new List<SubtitleEntry>();

        // Strip a leading BOM if File.ReadAllText missed it, normalize newlines.
        raw = raw.TrimStart('\uFEFF').Replace("\r\n", "\n").Replace("\r", "\n").Trim();
        if (raw.Length == 0) return entries;

        var blocks = Regex.Split(raw, @"\n\s*\n");
        int autoIndex = 1;

        foreach (var block in blocks)
        {
            var lines = block.Split('\n');
            if (lines.Length == 0) continue;

            int lineIdx = 0;
            int index = autoIndex;

            if (int.TryParse(lines[lineIdx].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedIndex))
            {
                index = parsedIndex;
                lineIdx++;
            }

            if (lineIdx >= lines.Length) continue;

            var timeMatch = TimeLineRegex.Match(lines[lineIdx]);
            if (!timeMatch.Success) continue;
            lineIdx++;

            var start = ParseTime(timeMatch.Groups["start"].Value);
            var end = ParseTime(timeMatch.Groups["end"].Value);

            var textLines = new List<string>();
            for (; lineIdx < lines.Length; lineIdx++)
            {
                var line = lines[lineIdx].TrimEnd();
                if (line.Length > 0) textLines.Add(line);
            }

            if (textLines.Count == 0) continue;

            entries.Add(new SubtitleEntry
            {
                Index = index,
                Start = start,
                End = end,
                Lines = textLines
            });
            autoIndex++;
        }

        return entries;
    }

    private static TimeSpan ParseTime(string s)
    {
        s = s.Replace('.', ',').Trim();
        // Pad to hh:mm:ss,fff in case of single-digit hours or short millis.
        var parts = s.Split(',');
        var msPart = parts.Length > 1 ? parts[1].PadRight(3, '0').Substring(0, 3) : "000";
        var hmsParts = parts[0].Split(':');
        var hh = hmsParts[0].PadLeft(2, '0');
        var mm = hmsParts.Length > 1 ? hmsParts[1].PadLeft(2, '0') : "00";
        var ss = hmsParts.Length > 2 ? hmsParts[2].PadLeft(2, '0') : "00";
        var normalized = $"{hh}:{mm}:{ss},{msPart}";
        return TimeSpan.ParseExact(normalized, @"hh\:mm\:ss\,fff", CultureInfo.InvariantCulture);
    }
}
