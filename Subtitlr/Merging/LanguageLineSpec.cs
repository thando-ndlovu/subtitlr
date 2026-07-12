namespace Subtitlr.Base.Merging;

/// <summary>
/// One line of the final merged cue: which language's text to pull, and
/// whether to show it romanized or in its native script.
/// </summary>
public class LanguageLineSpec
{
    public string LanguageCode { get; set; }
    public bool Romanize { get; set; }
    public CuePosition Position { get; set; }
    public TimeSpan? Offset { get; set; }
}
