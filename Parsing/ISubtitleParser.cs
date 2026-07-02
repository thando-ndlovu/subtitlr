using System.Collections.Generic;
using Subtitlr.Models;

namespace Subtitlr.Parsing;

public interface ISubtitleParser
{
    List<SubtitleEntry> Parse(string path);
}

public interface ISubtitleWriter
{
    void Write(string path, List<SubtitleEntry> entries);
}
