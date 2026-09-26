using System.Diagnostics;

namespace ConsoleRender.Tests;

/// <summary>Runs the real git against throwaway repositories.</summary>
public sealed class GitClientTests : IDisposable
{
    private readonly string folder = Directory.CreateDirectory(
        Path.Combine(Path.GetTempPath(), "console-render-tests", Guid.NewGuid().ToString("N"))).FullName;
    private readonly GitClient client = new();

    public void Dispose()
    {
        // git marks object files read-only, which Directory.Delete refuses on Windows.
        foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(folder, recursive: true);
    }

    private string Repo(string name)
    {
        var path = Path.Combine(folder, name);
        Directory.CreateDirectory(path);
        Git(path, "init", "--quiet", "--initial-branch=main");
        return path;
    }

    private static void Git(string path, params string[] arguments)
    {
        var start = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in new[] { "-C", path, "-c", "user.name=Test", "-c", "user.email=test@example.org",
                     "-c", "commit.gpgsign=false" }.Concat(arguments))
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)!;
        process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, $"git {string.Join(' ', arguments)} failed: {error}");
    }

    private static void Commit(string path, string file, string content)
    {
        File.WriteAllText(Path.Combine(path, file), content);
        Git(path, "add", file);
        Git(path, "commit", "--quiet", "-m", "c");
    }

    [Fact]
    public void TrackedAndUntrackedChangesAreCombined()
    {
        var repo = Repo("r");
        Commit(repo, "a.txt", "one\ntwo\n");
        File.WriteAllText(Path.Combine(repo, "a.txt"), "one\nzwei\n");
        File.WriteAllText(Path.Combine(repo, "new.md"), "# Neu\n\ntext\n");
        File.WriteAllBytes(Path.Combine(repo, "blob.bin"), [1, 0, 2]);
        File.WriteAllText(Path.Combine(repo, ".gitignore"), "ignored.log\n");
        File.WriteAllText(Path.Combine(repo, "ignored.log"), "x\n");

        var changes = client.Read(repo);

        Assert.Null(changes.Error);
        Assert.Equal("main", changes.Branch);
        Assert.Null(changes.Ahead);
        Assert.Contains(new GitFileChange("a.txt", 1, 1, IsNew: false, IsBinary: false), changes.Files);
        Assert.Contains(new GitFileChange("new.md", 3, 0, IsNew: true, IsBinary: false), changes.Files);
        Assert.Contains(new GitFileChange("blob.bin", 0, 0, IsNew: true, IsBinary: true), changes.Files);
        Assert.DoesNotContain(changes.Files, file => file.Path == "ignored.log");
        Assert.Contains("-two\n+zwei\n", changes.Diff.Replace("\r\n", "\n"));
        Assert.Contains("+++ b/new.md\n@@ -0,0 +1,3 @@\n+# Neu\n", changes.Diff);
    }

    [Fact]
    public void AFolderInsideTheRepositoryFindsItsRoot()
    {
        var repo = Repo("r");
        Commit(repo, "a.txt", "x\n");
        var sub = Directory.CreateDirectory(Path.Combine(repo, "sub")).FullName;
        File.WriteAllText(Path.Combine(sub, "b.txt"), "y\n");

        var changes = client.Read(sub);

        Assert.Equal("sub/b.txt", Assert.Single(changes.Files).Path);
    }

    [Fact]
    public void ARepositoryWithoutCommitsShowsStagedAndUntrackedFiles()
    {
        var repo = Repo("r");
        File.WriteAllText(Path.Combine(repo, "staged.txt"), "s\n");
        Git(repo, "add", "staged.txt");
        File.WriteAllText(Path.Combine(repo, "loose.txt"), "l\n");

        var changes = client.Read(repo);

        Assert.Null(changes.Error);
        Assert.Equal("main", changes.Branch);
        Assert.Equal(["loose.txt", "staged.txt"], changes.Files.Select(file => file.Path).Order());
    }

    [Fact]
    public void OutgoingAndIncomingCommitsAreCountedAgainstTheUpstream()
    {
        var origin = Repo("origin");
        Commit(origin, "a.txt", "1\n");
        Git(folder, "clone", "--quiet", origin, "clone");
        var clone = Path.Combine(folder, "clone");

        Commit(origin, "a.txt", "2\n");
        Commit(origin, "a.txt", "3\n");
        Git(clone, "fetch", "--quiet");
        Commit(clone, "b.txt", "local\n");

        var changes = client.Read(clone);

        Assert.Equal(1, changes.Ahead);
        Assert.Equal(2, changes.Behind);
    }

    [Theory]
    [InlineData("dir/a.txt", "other/a.txt")]
    [InlineData("a.txt", "b.txt")]
    [InlineData("x/dir/a.txt", "x/a.txt")]
    public void RenamedFilesAreListedUnderTheirNewPath(string from, string to)
    {
        var repo = Repo("r");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(repo, from))!);
        Commit(repo, from, "same content\nline two\nline three\n");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(repo, to))!);
        Git(repo, "mv", from, to);

        var changes = client.Read(repo);

        Assert.Equal(to, Assert.Single(changes.Files).Path);
    }

    [Fact]
    public void ADetachedHeadShowsTheCommit()
    {
        var repo = Repo("r");
        Commit(repo, "a.txt", "1\n");
        Git(repo, "checkout", "--quiet", "--detach");

        Assert.StartsWith("detached at ", client.Read(repo).Branch);
    }

    [Fact]
    public void AFolderOutsideAnyRepositoryReportsAnError()
    {
        // The temp folder lies outside any repository.
        var plain = Directory.CreateDirectory(Path.Combine(folder, "plain")).FullName;

        var changes = client.Read(plain);

        Assert.NotNull(changes.Error);
        Assert.Empty(changes.Files);
    }
}
