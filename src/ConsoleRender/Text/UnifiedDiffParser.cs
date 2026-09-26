namespace ConsoleRender;

/// <summary>
/// Reads unified diff text — the output of <c>git diff</c>, <c>git show</c> or
/// <c>diff -u</c> — into files, hunks and numbered lines. Tolerant by design: text it does
/// not understand (a commit message, "index …" lines) is skipped, never an error.
/// </summary>
internal static class UnifiedDiffParser
{
    public static IReadOnlyList<DiffFile> Parse(string text)
    {
        Guard.Against.Null(text);

        var files = new List<DiffFile>();
        List<DiffHunk>? hunks = null;
        List<DiffLine>? hunkLines = null;
        var oldPath = "";
        var newPath = "";
        var oldNumber = 0;
        var newNumber = 0;
        var oldLeft = 0;
        var newLeft = 0;

        // A file entry is created lazily, so the paths gathered from the header lines are
        // complete by the time the first hunk needs it.
        void StartFile()
        {
            hunks = new List<DiffHunk>();
            files.Add(new DiffFile(oldPath, newPath, hunks));
        }

        foreach (var line in SplitLines(text))
        {
            if (oldLeft > 0 || newLeft > 0)
            {
                var prefix = line.Length > 0 ? line[0] : ' ';
                var body = line.Length > 0 ? line[1..] : "";
                if (prefix == ' ' && oldLeft > 0 && newLeft > 0)
                {
                    hunkLines!.Add(new DiffLine(DiffLineKind.Context, body, oldNumber++, newNumber++));
                    oldLeft--;
                    newLeft--;
                    continue;
                }

                if (prefix == '-' && oldLeft > 0)
                {
                    hunkLines!.Add(new DiffLine(DiffLineKind.Removed, body, oldNumber++, null));
                    oldLeft--;
                    continue;
                }

                if (prefix == '+' && newLeft > 0)
                {
                    hunkLines!.Add(new DiffLine(DiffLineKind.Added, body, null, newNumber++));
                    newLeft--;
                    continue;
                }

                if (prefix != '\\')
                {
                    // The counts promised more lines than came; the hunk ends here.
                    oldLeft = newLeft = 0;
                }
            }

            if (line.StartsWith('\\') && hunkLines is not null)
            {
                hunkLines.Add(new DiffLine(DiffLineKind.Note, line, null, null));
                continue;
            }

            if (line.StartsWith("diff ", StringComparison.Ordinal))
            {
                (oldPath, newPath) = ParseGitPaths(line);
                hunks = null;
                hunkLines = null;
                continue;
            }

            if (line.StartsWith("--- ", StringComparison.Ordinal))
            {
                // Outside a hunk, "---" opens a file; after earlier hunks (a plain "diff -u"
                // run over several files) it starts the next one.
                oldPath = StripPath(line[4..]);
                hunks = null;
                hunkLines = null;
                continue;
            }

            if (line.StartsWith("+++ ", StringComparison.Ordinal))
            {
                newPath = StripPath(line[4..]);
                continue;
            }

            if (line.StartsWith("rename from ", StringComparison.Ordinal))
            {
                oldPath = line["rename from ".Length..];
                continue;
            }

            if (line.StartsWith("rename to ", StringComparison.Ordinal))
            {
                newPath = line["rename to ".Length..];
                continue;
            }

            if (TryParseHunkHeader(line, out var header))
            {
                if (hunks is null)
                {
                    StartFile();
                }

                hunkLines = new List<DiffLine>();
                hunks!.Add(new DiffHunk(line, hunkLines));
                oldNumber = header.OldStart;
                newNumber = header.NewStart;
                oldLeft = header.OldCount;
                newLeft = header.NewCount;
            }
        }

        return files;
    }

    /// <summary>Parses "@@ -a[,b] +c[,d] @@"; a missing count means 1.</summary>
    public static bool TryParseHunkHeader(string line, out DiffHunkHeader header)
    {
        header = default;
        if (!line.StartsWith("@@ -", StringComparison.Ordinal))
        {
            return false;
        }

        var pos = 4;
        if (!TryParseRange(line, ref pos, out var oldStart, out var oldCount)
            || !Expect(line, ref pos, " +")
            || !TryParseRange(line, ref pos, out var newStart, out var newCount)
            || !Expect(line, ref pos, " @@"))
        {
            return false;
        }

        header = new DiffHunkHeader(oldStart, oldCount, newStart, newCount, pos);
        return true;
    }

    /// <summary>Splits into lines without their breaks; tabs become four spaces for the buffer.</summary>
    public static IEnumerable<string> SplitLines(string text)
    {
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\t", "    ").Split('\n');
        // A trailing newline is a terminator, not an extra empty line.
        var count = lines.Length > 0 && lines[^1].Length == 0 ? lines.Length - 1 : lines.Length;
        return lines.Take(count);
    }

    private static bool TryParseRange(string line, ref int pos, out int start, out int count)
    {
        count = 1;
        if (!TryParseNumber(line, ref pos, out start))
        {
            return false;
        }

        if (pos < line.Length && line[pos] == ',')
        {
            pos++;
            return TryParseNumber(line, ref pos, out count);
        }

        return true;
    }

    private static bool TryParseNumber(string line, ref int pos, out int value)
    {
        value = 0;
        var begin = pos;
        while (pos < line.Length && char.IsAsciiDigit(line[pos]) && pos - begin < 9)
        {
            value = value * 10 + (line[pos] - '0');
            pos++;
        }

        return pos > begin;
    }

    private static bool Expect(string line, ref int pos, string token)
    {
        if (string.CompareOrdinal(line, pos, token, 0, token.Length) != 0)
        {
            return false;
        }

        pos += token.Length;
        return true;
    }

    /// <summary>"diff --git a/x b/y" → ("x", "y"); anything else yields empty paths.</summary>
    private static (string Old, string New) ParseGitPaths(string line)
    {
        const string Git = "diff --git a/";
        if (!line.StartsWith(Git, StringComparison.Ordinal))
        {
            return ("", "");
        }

        var rest = line[Git.Length..];
        var split = rest.IndexOf(" b/", StringComparison.Ordinal);
        return split < 0 ? ("", "") : (rest[..split], rest[(split + 3)..]);
    }

    /// <summary>Drops the "a/"/"b/" prefix and a trailing timestamp ("file\t2024-…").</summary>
    private static string StripPath(string path)
    {
        // Timestamps follow a tab, which SplitLines already turned into spaces.
        var stamp = path.IndexOf("    ", StringComparison.Ordinal);
        if (stamp > 0)
        {
            path = path[..stamp];
        }

        return path.StartsWith("a/", StringComparison.Ordinal) || path.StartsWith("b/", StringComparison.Ordinal)
            ? path[2..]
            : path;
    }
}
