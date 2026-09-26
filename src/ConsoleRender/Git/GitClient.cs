using System.Diagnostics;
using System.Text;

namespace ConsoleRender;

/// <summary>
/// Collects the uncommitted changes of a repository by running git: <c>git diff HEAD</c>
/// for tracked files (staged and unstaged alike) and the untracked, non-ignored files as
/// newly added ones — so an overview shows everything that was created or touched.
///
/// Requires git on the PATH. <see cref="Read"/> runs a handful of git processes
/// synchronously (each capped at a few seconds) and never throws for git failures: they
/// come back as <see cref="GitChanges.Error"/>.
/// </summary>
public sealed class GitClient : IGitChangesSource
{
    /// <summary>Untracked files above this size are listed but not diffed.</summary>
    private const int MaxUntrackedBytes = 256 * 1024;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public GitChanges Read(string folder)
    {
        Guard.Against.NullOrWhiteSpace(folder);

        try
        {
            var (ok, output) = Run(folder, "rev-parse", "--show-toplevel");
            if (!ok)
            {
                return GitChanges.Failed($"Not a git repository: {folder}");
            }

            var root = output.Trim();
            var branch = Branch(root);
            var (ahead, behind) = AheadBehind(root);

            // A repository without commits has no HEAD to diff against; there, everything
            // staged counts as new and the rest shows up as untracked.
            var hasHead = Run(root, "rev-parse", "--verify", "--quiet", "HEAD").Ok;
            var range = hasHead ? new[] { "HEAD" } : ["--cached"];

            var diff = new StringBuilder(Run(root, ["diff", .. range, "--no-color", "--no-ext-diff", "-M"]).Output);
            var files = NumStat(Run(root, ["diff", .. range, "--numstat", "-M"]).Output);

            foreach (var path in Run(root, "ls-files", "--others", "--exclude-standard", "-z").Output
                .Split('\0', StringSplitOptions.RemoveEmptyEntries))
            {
                files.Add(Untracked(root, path, diff));
            }

            return new GitChanges(root, branch, ahead, behind, diff.ToString(), files, null);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException or InvalidOperationException)
        {
            return GitChanges.Failed($"git could not be run: {ex.Message}");
        }
    }

    /// <summary>The branch name; a detached HEAD shows its short commit id instead.</summary>
    private static string Branch(string root)
    {
        var (ok, name) = Run(root, "symbolic-ref", "--quiet", "--short", "HEAD");
        if (ok)
        {
            return name.Trim();
        }

        var (hasCommit, id) = Run(root, "rev-parse", "--short", "HEAD");
        return hasCommit ? $"detached at {id.Trim()}" : "(no branch)";
    }

    /// <summary>
    /// Commits only on this branch (outgoing) and only on its upstream (incoming), as of the
    /// last fetch — the viewer never fetches by itself. Null for both without an upstream.
    /// </summary>
    private static (int? Ahead, int? Behind) AheadBehind(string root)
    {
        var (ok, output) = Run(root, "rev-list", "--left-right", "--count", "@{upstream}...HEAD");
        var parts = output.Split('\t', StringSplitOptions.TrimEntries);
        if (!ok || parts.Length != 2
            || !int.TryParse(parts[0], System.Globalization.CultureInfo.InvariantCulture, out var behind)
            || !int.TryParse(parts[1], System.Globalization.CultureInfo.InvariantCulture, out var ahead))
        {
            return (null, null);
        }

        return (ahead, behind);
    }

    /// <summary>"added\tremoved\tpath" per line; binary files report "-" for both counts.</summary>
    private static List<GitFileChange> NumStat(string output)
    {
        var files = new List<GitFileChange>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.TrimEnd('\r').Split('\t', 3);
            if (parts.Length < 3)
            {
                continue;
            }

            var binary = parts[0] == "-";
            files.Add(new GitFileChange(RenamedPath(parts[2]),
                binary ? 0 : int.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
                binary ? 0 : int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                IsNew: false, IsBinary: binary));
        }

        return files;
    }

    /// <summary>
    /// The new path of a numstat entry: git writes renames as "old => new" or, sharing the
    /// rest of the path, "dir/{old => new}/file".
    /// </summary>
    private static string RenamedPath(string path)
    {
        const string Arrow = " => ";
        var arrow = path.IndexOf(Arrow, StringComparison.Ordinal);
        if (arrow < 0)
        {
            return path;
        }

        var open = path.LastIndexOf('{', arrow);
        var close = path.IndexOf('}', arrow);
        if (open < 0 || close < 0)
        {
            return path[(arrow + Arrow.Length)..];
        }

        // "a/{b => }/c" (a folder dropped) must not leave a double slash behind.
        var renamed = path[..open] + path[(arrow + Arrow.Length)..close] + path[(close + 1)..];
        return renamed.Replace("//", "/");
    }

    /// <summary>An untracked file as an all-added diff; binary or large files only get a stat line.</summary>
    private static GitFileChange Untracked(string root, string path, StringBuilder diff)
    {
        var full = Path.Combine(root, path);
        var info = new FileInfo(full);
        if (!info.Exists || info.Length > MaxUntrackedBytes)
        {
            return new GitFileChange(path, 0, 0, IsNew: true, IsBinary: true);
        }

        var content = File.ReadAllText(full);
        if (content.Contains('\0'))
        {
            return new GitFileChange(path, 0, 0, IsNew: true, IsBinary: true);
        }

        var lines = content.Replace("\r\n", "\n").Split('\n').ToList();
        if (lines.Count > 0 && lines[^1].Length == 0)
        {
            // A trailing newline terminates the last line; it does not start another one.
            lines.RemoveAt(lines.Count - 1);
        }

        diff.Append("diff --git a/").Append(path).Append(" b/").Append(path).Append('\n');
        diff.Append("new file mode 100644\n");
        diff.Append("--- /dev/null\n");
        diff.Append("+++ b/").Append(path).Append('\n');
        if (lines.Count > 0)
        {
            diff.Append("@@ -0,0 +1,").Append(lines.Count).Append(" @@\n");
            foreach (var line in lines)
            {
                diff.Append('+').Append(line).Append('\n');
            }
        }

        return new GitFileChange(path, lines.Count, 0, IsNew: true, IsBinary: false);
    }

    private static (bool Ok, string Output) Run(string folder, params string[] arguments)
    {
        var start = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        start.ArgumentList.Add("-C");
        start.ArgumentList.Add(folder);
        // Unicode paths verbatim instead of octal escapes.
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add("core.quotepath=off");
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("git did not start.");
        // Both streams are drained concurrently, so a chatty stderr cannot block stdout.
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        if (!process.WaitForExit(Timeout))
        {
            process.Kill(entireProcessTree: true);
            return (false, "");
        }

        error.Wait(Timeout);
        return (process.ExitCode == 0, output);
    }
}
