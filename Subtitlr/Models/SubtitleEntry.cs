using System;
using System.Collections.Generic;

namespace Subtitlr.Base.Models;

/// <summary>
/// A single timed subtitle cue. One or more text lines (e.g. one per language)
/// can share the same cue window.
/// </summary>
public class SubtitleEntry
{
    public int Index { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public List<string> Lines { get; set; } = new();

    public SubtitleEntry Clone()
    {
        return new SubtitleEntry
        {
            Index = Index,
            Start = Start,
            End = End,
            Lines = new List<string>(Lines)
        };
    }
}
