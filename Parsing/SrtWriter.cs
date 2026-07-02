using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Subtitlr.Models;

namespace Subtitlr.Parsing;

public class SrtWriter : ISubtitleWriter
{
    public void Write(string path, List<SubtitleEntry> entries)
    {
        var sb = new StringBuilder();
        int i = 1;

        foreach (var e in entries)
        {
            sb.Append(i.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append(FormatTime(e.Start)).Append(" --> ").Append(FormatTime(e.End)).Append('\n');
            foreach (var line in e.Lines)
            {
                sb.Append(line).Append('\n');
            }
            sb.Append('\n');
            i++;
        }

        // UTF-8 without BOM keeps the file friendly to the widest range of players.
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }

    private static string FormatTime(TimeSpan t)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00},{3:000}",
            (int)t.TotalHours, t.Minutes, t.Seconds, t.Milliseconds);
    }
}
