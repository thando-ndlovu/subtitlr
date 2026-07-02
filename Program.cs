using Subtitlr.Grouping;
using Subtitlr.Merging;
using Subtitlr.Models;
using Subtitlr.Parsing;
using Subtitlr.Romanization;

namespace SubtitleMerger;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var inputPaths = new List<string>();
        string? outputDir = null;
        bool recursive = false;
        List<string>? customOrderTokens = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--output":
                case "-o":
                    outputDir = RequireValue(args, ref i, "--output");
                    break;
                case "--recursive":
                case "-r":
                    recursive = true;
                    break;
                case "--order":
                    var raw = RequireValue(args, ref i, "--order");
                    customOrderTokens = [.. raw.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)];
                    break;
                case "--help":
                case "-h":
                    PrintUsage();
                    return 0;
                default:
                    inputPaths.Add(args[i]);
                    break;
            }
        }

        if (inputPaths.Count == 0)
        {
            Console.Error.WriteLine("No input files or directories provided.");
            PrintUsage();
            return 1;
        }

        var files = ExpandInputs(inputPaths, recursive);
        if (files.Count == 0)
        {
            Console.Error.WriteLine("No supported subtitle files found (currently: " +
                string.Join(", ", SubtitleFormatHelper.SupportedExtensions) + ").");
            return 1;
        }

        Console.WriteLine($"Found {files.Count} subtitle file(s).");

        var groups = FileGrouper.Group(files);
        Console.WriteLine($"Grouped into {groups.Count} subtitle set(s).");

        var parser = new SrtParser();
        var writer = new SrtWriter();

        foreach (var group in groups)
        {
            foreach (var file in group.Files)
            {
                try
                {
                    file.Entries = parser.Parse(file.Path);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"  Failed to parse '{file.Path}': {ex.Message}");
                }
            }

            var languagesPresent = group.Files.Select(f => f.LanguageCode).Distinct().ToList();
            Console.WriteLine($"- Group '{group.GroupKey}': languages = {string.Join(", ", languagesPresent)} " +
                $"({string.Join(", ", group.Files.Select(f => Path.GetFileName(f.Path)))})");

            var order = customOrderTokens != null
                ? ParseOrderTokens(customOrderTokens)
                : BuildDefaultOrder(languagesPresent);

            if (order.Count == 0)
            {
                Console.WriteLine("  Skipped: no usable line order for this group's languages.");
                continue;
            }

            var merged = CueMerger.Merge(group, order);
            if (merged.Count == 0)
            {
                Console.WriteLine("  Skipped: nothing to write (no overlapping content).");
                continue;
            }

            var targetDir = outputDir ?? Path.GetDirectoryName(group.Files[0].Path);
            if (string.IsNullOrEmpty(targetDir)) targetDir = ".";
            Directory.CreateDirectory(targetDir);

            var outPath = Path.Combine(targetDir, SanitizeFileName(group.GroupKey) + ".merged.srt");
            writer.Write(outPath, merged);

            Console.WriteLine($"  Wrote {merged.Count} cues -> {outPath}");
        }

        return 0;
    }

    private static string RequireValue(string[] args, ref int i, string optionName)
    {
        if (i + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {optionName}");
        }
        i++;
        return args[i];
    }

    /// <summary>
    /// Default stacking: every non-English language's native text (and its
    /// romanized form, when a romanizer is registered for it) goes in the
    /// Top box; English goes alone in the Bottom box. This matches "Hangul,
    /// then romanized Korean, then English at the bottom" generalized to any
    /// language set, with the Top box tagged {\an8} so both boxes are
    /// visible on screen simultaneously.
    /// </summary>
    private static List<LanguageLineSpec> BuildDefaultOrder(List<string> languagesPresent)
    {
        var order = new List<LanguageLineSpec>();

        var nonEnglish = languagesPresent
            .Where(l => !string.Equals(l, "en", StringComparison.OrdinalIgnoreCase))
            .OrderBy(l => l, StringComparer.OrdinalIgnoreCase);

        foreach (var lang in nonEnglish)
        {
            order.Add(new LanguageLineSpec { LanguageCode = lang, Romanize = false, Position = CuePosition.Top });
            if (RomanizerRegistry.TryGet(lang, out _))
            {
                order.Add(new LanguageLineSpec { LanguageCode = lang, Romanize = true, Position = CuePosition.Top });
            }
        }

        if (languagesPresent.Any(l => string.Equals(l, "en", StringComparison.OrdinalIgnoreCase)))
        {
            order.Add(new LanguageLineSpec { LanguageCode = "en", Romanize = false, Position = CuePosition.Bottom });
        }

        return order;
    }

    /// <summary>
    /// Parses tokens like "ko", "ko-romanized", "ko:top", "en:bottom".
    /// The "-romanized" suffix requests the romanized form; the ":top" /
    /// ":bottom" suffix picks which on-screen box the line joins. If the
    /// position is omitted, it defaults to Top for every language except
    /// English, which defaults to Bottom.
    /// </summary>
    private static List<LanguageLineSpec> ParseOrderTokens(List<string> tokens)
    {
        var result = new List<LanguageLineSpec>();
        const string romanizedSuffix = "-romanized";

        foreach (var raw in tokens)
        {
            var t = raw;
            CuePosition? explicitPosition = null;

            var colonIdx = t.IndexOf(':');
            if (colonIdx >= 0)
            {
                var posText = t[(colonIdx + 1)..].Trim();
                t = t[..colonIdx];

                if (Enum.TryParse(posText, true, out CuePosition _cueposition))
                    explicitPosition = _cueposition;
                else Console.Error.WriteLine($"Unknown position '{posText}' in --order token '{raw}', expected 'top', 'topleft', 'topright' or 'bottom'.");
            }

            bool romanize = t.EndsWith(romanizedSuffix, StringComparison.OrdinalIgnoreCase);
            var lang = romanize ? t[..^romanizedSuffix.Length] : t;

            var position = explicitPosition ??
                (string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase) ? CuePosition.Bottom : CuePosition.Top);

            result.Add(new LanguageLineSpec { LanguageCode = lang, Romanize = romanize, Position = position });
        }

        return result;
    }

    private static List<string> ExpandInputs(List<string> inputPaths, bool recursive)
    {
        var supportedExtensions = SubtitleFormatHelper.SupportedExtensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var p in inputPaths)
        {
            if (Directory.Exists(p))
            {
                var opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                foreach (var ext in supportedExtensions)
                {
                    result.AddRange(Directory.GetFiles(p, "*" + ext, opt));
                }
            }
            else if (File.Exists(p))
            {
                if (supportedExtensions.Contains(Path.GetExtension(p)))
                    result.Add(p);
                else
                    Console.Error.WriteLine($"Skipping unsupported file: {p}");
            }
            else
            {
                Console.Error.WriteLine($"Path not found: {p}");
            }
        }

        return [.. result.Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    private static void PrintUsage()
    {
        Console.WriteLine(@"
SubtitleMerger - combine multi-language .srt files into one stacked subtitle file.

Usage:
  SubtitleMerger <file-or-directory> [more paths...] [options]

Options:
  -o, --output <dir>   Directory to write merged .srt files into (default: alongside source files)
  -r, --recursive       Recurse into subdirectories when a directory is given
  --order <spec>        Comma separated line order, e.g. ""ko,ko-romanized,en""
                         Use ""<lang>-romanized"" to request a romanized line for a language.
                         Default: every non-English language (native script, then romanized
                         if a romanizer is available for it), followed by English last.
  -h, --help             Show this help

Language/file grouping:
  Files are grouped by filename with the language tag removed, e.g.
  ""MyShow.S01E01.en.srt"" and ""MyShow.S01E01.ko.srt"" both map to group
  ""MyShow.S01E01"". Recognized tags include en/eng/english, ko/kor/korean/kr,
  ja/jp/jpn/japanese, zh/chi/chinese, es/fr/de/pt/ru and a few variants.
  Files without a recognizable tag fall back to per-directory grouping with
  language guessed from the text itself (Hangul/Kana/Han/Latin ratios).

Examples:
  SubtitleMerger ./MyShow -r
  SubtitleMerger ep01.en.srt ep01.ko.srt -o ./out --order ko,ko-romanized,en
");
    }
}
