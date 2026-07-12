using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Subtitlr.Base.Models;

namespace Subtitlr.Base.Grouping;

/// <summary>
/// Groups a flat list of subtitle file paths into SubtitleFileGroups: sets of
/// files (typically different languages) that belong to the same underlying
/// video, based on filenames that differ only by a language tag.
/// </summary>
public static class FileGrouper
{
    public static List<SubtitleFileGroup> Group(IEnumerable<string> filePaths)
    {
        var withLang = new List<(string Path, string GroupKey, string Lang)>();
        var withoutLang = new List<string>();

        foreach (var path in filePaths)
        {
            var nameNoExt = Path.GetFileNameWithoutExtension(path);
            if (LanguageDetector.TryDetectFromFileName(nameNoExt, out var groupKey, out var lang))
                withLang.Add((path, groupKey, lang));
            else
                withoutLang.Add(path);
        }

        var groups = new Dictionary<string, SubtitleFileGroup>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in withLang)
        {
            if (!groups.TryGetValue(item.GroupKey, out var g))
            {
                g = new SubtitleFileGroup { GroupKey = item.GroupKey };
                groups[item.GroupKey] = g;
            }
            g.Files.Add(new SubtitleFile { Path = item.Path, LanguageCode = item.Lang, GroupKey = item.GroupKey });
        }

        // Files with no recognizable language tag in their name: fall back to
        // grouping by directory (one folder == one set of subs) and detect
        // their language from the text itself.
        foreach (var dirGroup in withoutLang.GroupBy(Path.GetDirectoryName))
        {
            var sameDirGroups = groups.Values
                .Where(g => g.Files.Any(f => string.Equals(Path.GetDirectoryName(f.Path), dirGroup.Key, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            SubtitleFileGroup target;
            if (sameDirGroups.Count == 1)
            {
                // Exactly one existing group already anchored in this folder -> merge into it.
                target = sameDirGroups[0];
            }
            else
            {
                var key = "dir:" + dirGroup.Key;
                if (!groups.TryGetValue(key, out target))
                {
                    var dirName = Path.GetFileName(dirGroup.Key.TrimEnd(Path.DirectorySeparatorChar));
                    var friendlyName = string.IsNullOrEmpty(dirName) ? key : dirName;
                    target = new SubtitleFileGroup { GroupKey = friendlyName };
                    groups[key] = target;
                }
            }

            foreach (var path in dirGroup)
            {
                var lang = LanguageDetector.DetectFromContent(SafeReadSample(path));
                target.Files.Add(new SubtitleFile { Path = path, LanguageCode = lang, GroupKey = target.GroupKey });
            }
        }

        return groups.Values.ToList();
    }

    private static string SafeReadSample(string path)
    {
        try
        {
            var text = File.ReadAllText(path);
            return text.Length > 4000 ? text.Substring(0, 4000) : text;
        }
        catch
        {
            return string.Empty;
        }
    }
}
