using System;
using System.Collections.Generic;
using System.Linq;
using Subtitlr.Models;
using Subtitlr.Romanization;

namespace Subtitlr.Merging;

/// <summary>
/// Combines every language's cues for a group into a single timeline. Times
/// are taken from whichever file has the most cues (the "base" track);
/// other tracks' text is aligned to it by index when cue counts match,
/// otherwise by nearest start time (within a small tolerance, so a genuinely
/// unmatched cue is dropped rather than paired with something unrelated).
/// </summary>
public static class CueMerger
{
    private static readonly TimeSpan MaxTimeMatchTolerance = TimeSpan.FromSeconds(2);

    public static List<SubtitleEntry> Merge(SubtitleFileGroup group, List<LanguageLineSpec> order)
    {
        if (group.Files.Count == 0) return [];

        var baseFile = group.Files.OrderByDescending(f => f.Entries.Count).First();
        var result = new List<SubtitleEntry>();

        foreach (var baseEntry in baseFile.Entries)
        {
            SubtitleEntry? top = null, bottom = null;

            foreach (var spec in order)
            {
                var file = group.Files.FirstOrDefault(f =>
                    string.Equals(f.LanguageCode, spec.LanguageCode, StringComparison.OrdinalIgnoreCase));
                if (file == null) continue;

                var matched = ResolveMatchingEntry(file, baseFile, baseEntry);
                if (matched == null) continue;

                var text = string.Join(" ", matched.Lines).Trim();

                if (text.Length == 0) continue;

                if (spec.Romanize && RomanizerRegistry.TryGet(spec.LanguageCode, out var romanizer))
                {
                    text = romanizer.Romanize(text);
                }

                switch (spec.Position)
                {
                    case CuePosition.Top:
                    case CuePosition.TopLeft:
                    case CuePosition.TopRight:
                        top ??= new SubtitleEntry
                        {
                            Start = baseEntry.Start,
                            End = baseEntry.End,
                            Lines = [spec.Position switch
                            {
                                CuePosition.TopLeft => "{\\an7}",
                                CuePosition.Top => "{\\an8}",
                                CuePosition.TopRight or _ => "{\\an9}",
                            }]
                        };

                        top.Lines.Add(text);
                        break;

                    case CuePosition.Bottom:
                    default:

                        bottom ??= new SubtitleEntry
                        {
                            Start = baseEntry.Start,
                            End = baseEntry.End,
                            Lines = []
                        };

                        bottom.Lines.Add(text);
                        break;
                }
            }

            if (top is not null) result.Add(top);
            if (bottom is not null) result.Add(bottom);
        }

        for (int i = 0; i < result.Count; i++) result[i].Index = i + 1;
        return result;
    }

    private static SubtitleEntry ResolveMatchingEntry(SubtitleFile file, SubtitleFile baseFile, SubtitleEntry baseEntry)
    {
        if (ReferenceEquals(file, baseFile))
            return baseEntry;

        if (file.Entries.Count == baseFile.Entries.Count)
        {
            var idx = baseFile.Entries.IndexOf(baseEntry);
            return idx >= 0 && idx < file.Entries.Count ? file.Entries[idx] : null;
        }

        return FindClosestByTime(file.Entries, baseEntry.Start);
    }

    private static SubtitleEntry FindClosestByTime(List<SubtitleEntry> entries, TimeSpan target)
    {
        SubtitleEntry best = null;
        var bestDiff = TimeSpan.MaxValue;

        foreach (var e in entries)
        {
            var diff = (e.Start - target).Duration();
            if (diff < bestDiff)
            {
                bestDiff = diff;
                best = e;
            }
        }

        return best != null && bestDiff <= MaxTimeMatchTolerance ? best : null;
    }
}
