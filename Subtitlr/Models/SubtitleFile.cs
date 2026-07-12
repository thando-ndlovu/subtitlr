using System.Collections.Generic;

namespace Subtitlr.Base.Models;

/// <summary>
/// One subtitle file on disk: its path, the language it was detected as,
/// the group it belongs to, and its parsed cues.
/// </summary>
public class SubtitleFile
{
    public string Path { get; set; }
    public string GroupKey { get; set; }
    public string LanguageCode { get; set; }
    public List<SubtitleEntry> Entries { get; set; } = new();
}

/// <summary>
/// A set of SubtitleFiles (usually different languages) that belong to the
/// same underlying video/episode, as determined by FileGrouper.
/// </summary>
public class SubtitleFileGroup
{
    public string GroupKey { get; set; }
    public List<SubtitleFile> Files { get; set; } = new();
}
