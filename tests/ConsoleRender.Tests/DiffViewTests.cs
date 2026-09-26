namespace ConsoleRender.Tests;

public class DiffViewTests
{
    private const string Sample = """
        diff --git a/src/app.cs b/src/app.cs
        index 1111111..2222222 100644
        --- a/src/app.cs
        +++ b/src/app.cs
        @@ -1,4 +1,5 @@ class App
         keep one
        -old two
        -old three
        +new two
        +new three
        +new four
         keep five
        """;

    private static ConsoleKeyInfo Key(ConsoleKey key, char ch = '\0', ConsoleModifiers modifiers = 0)
    {
        return new(ch, key,
            modifiers.HasFlag(ConsoleModifiers.Shift),
            modifiers.HasFlag(ConsoleModifiers.Alt),
            modifiers.HasFlag(ConsoleModifiers.Control));
    }

    private static ConsoleBuffer Render(DiffView view, int width, int height)
    {
        var app = new ConsoleApp();
        view.Parent?.Remove(view);
        view.Left = 0;
        view.Top = 0;
        view.Width = width;
        view.Height = height;
        app.Root.Add(view);
        return app.RenderOffscreen(width, height);
    }

    private static string[] Rows(ConsoleBuffer buffer)
    {
        return buffer.ToText().Split('\n').Select(row => row.TrimEnd()).ToArray();
    }

    [Fact]
    public void UnifiedLayoutShowsBothLineNumbersAndMarkers()
    {
        var view = new DiffView { Diff = Sample, Layout = DiffLayout.Unified };

        var rows = Rows(Render(view, 40, 9));

        Assert.StartsWith("─ src/app.cs ─", rows[0]);
        Assert.Equal("@@ -1,4 +1,5 @@ class App", rows[1]);
        Assert.Equal("1 1   keep one", rows[2]);
        Assert.Equal("2   - old two", rows[3]);
        Assert.Equal("  2 + new two", rows[5]);
        Assert.Equal("4 5   keep five", rows[8]);
    }

    [Fact]
    public void ChangedLinesAreTinted()
    {
        var view = new DiffView { Diff = Sample, Layout = DiffLayout.Unified };

        var buffer = Render(view, 40, 9);

        Assert.Equal(view.RemovedBackground, buffer[30, 3].Background);
        Assert.Equal(view.RemovedColor, buffer[6, 3].Foreground);
        Assert.Equal(view.AddedBackground, buffer[30, 5].Background);
    }

    [Fact]
    public void SideBySidePairsRemovalsWithTheAdditionsAfterThem()
    {
        var view = new DiffView { Diff = Sample, Layout = DiffLayout.SideBySide };

        var buffer = Render(view, 41, 8);
        var rows = Rows(buffer);

        // Width 41: two halves of 20 cells with the divider at column 20.
        Assert.Equal("1   keep one        │1   keep one", rows[2]);
        Assert.Equal("2 - old two         │2 + new two", rows[3]);
        Assert.Equal("3 - old three       │3 + new three", rows[4]);
        Assert.Equal("                    │4 + new four", rows[5]);
        Assert.Equal("4   keep five       │5   keep five", rows[6]);
        Assert.Equal(view.FillerBackground, buffer[0, 5].Background);
    }

    [Fact]
    public void AutoLayoutSwitchesOnTheWidth()
    {
        var view = new DiffView { Diff = Sample, SideBySideMinWidth = 50 };

        Render(view, 49, 8);
        Assert.False(view.IsSideBySide);

        var rows = Rows(Render(view, 50, 8));
        Assert.True(view.IsSideBySide);
        Assert.Contains("│", rows[2]);
    }

    [Fact]
    public void LineNumbersCanBeHidden()
    {
        var view = new DiffView { Diff = Sample, Layout = DiffLayout.Unified, ShowLineNumbers = false };

        Assert.Equal("- old two", Rows(Render(view, 40, 9))[3]);
    }

    [Fact]
    public void NAndPJumpBetweenHunks()
    {
        var view = new DiffView
        {
            Layout = DiffLayout.Unified,
            Diff = "@@ -1,3 +1,3 @@\n a\n-b\n+c\n d\n@@ -20 +20 @@\n-x\n+y\n",
        };

        // Each jump puts the hunk header on the first row.
        Render(view, 30, 3);
        Assert.True(view.OnKey(Key(ConsoleKey.N, 'n')));
        Assert.Equal("@@ -1,3 +1,3 @@", Rows(Render(view, 30, 3))[0]);

        Assert.True(view.OnKey(Key(ConsoleKey.N, 'n')));
        Assert.Equal("@@ -20 +20 @@", Rows(Render(view, 30, 3))[0]);

        Assert.True(view.OnKey(Key(ConsoleKey.P, 'p')));
        Assert.Equal("@@ -1,3 +1,3 @@", Rows(Render(view, 30, 3))[0]);
    }

    [Fact]
    public void ScrollingIsClampedOnTheNextFrame()
    {
        var view = new DiffView { Diff = Sample, Layout = DiffLayout.Unified };
        Render(view, 40, 4);

        view.OnKey(Key(ConsoleKey.End));
        Assert.Equal("4 5   keep five", Rows(Render(view, 40, 4))[3]);

        // Taller than the content: the end position heals back to the top.
        Assert.StartsWith("─ src/app.cs ─", Rows(Render(view, 40, 12))[0]);
    }

    [Fact]
    public void RightScrollsTheTextButKeepsNumbersAndMarkers()
    {
        var view = new DiffView { Diff = Sample, Layout = DiffLayout.Unified };
        Render(view, 40, 9);

        view.OnKey(Key(ConsoleKey.RightArrow));

        Assert.Equal("2   - two", Rows(Render(view, 40, 9))[3]);
    }

    [Fact]
    public void AnEmptyDiffShowsTheEmptyText()
    {
        var view = new DiffView { Diff = "commit abc\n\nNo diff here." };

        Assert.Equal("No changes.", Rows(Render(view, 30, 2))[0]);
        Assert.Equal(0, view.FileCount);
    }

    [Fact]
    public void PlainDiffUOutputWithSeveralFilesIsSplitPerFile()
    {
        var view = new DiffView
        {
            Layout = DiffLayout.Unified,
            Diff = "--- a.txt\t2024-01-01\n+++ a.txt\t2024-01-02\n@@ -1 +1 @@\n-a\n+b\n"
                + "--- /dev/null\n+++ b.txt\n@@ -0,0 +1 @@\n+new\n",
        };

        var rows = Rows(Render(view, 30, 8));

        Assert.Equal(2, view.FileCount);
        Assert.StartsWith("─ a.txt ─", rows[0]);
        Assert.StartsWith("─ b.txt ─", rows[4]);
    }

    [Fact]
    public void ARemovedLineStartingWithDashesStaysInsideItsHunk()
    {
        var view = new DiffView { Layout = DiffLayout.Unified, Diff = "@@ -1,2 +1 @@\n--- x\n-y\n+z\n" };

        var rows = Rows(Render(view, 30, 5));

        Assert.Equal(1, view.FileCount);
        Assert.Equal("1   - -- x", rows[2]);
    }

    [Fact]
    public void ANoNewlineNoteFollowsTheSideOfItsLine()
    {
        var view = new DiffView
        {
            Layout = DiffLayout.SideBySide,
            Diff = "@@ -1 +1 @@\n-a\n\\ No newline at end of file\n+b\n",
        };

        var rows = Rows(Render(view, 61, 4));

        Assert.StartsWith("1 - a", rows[2]);
        Assert.EndsWith("1 + b", rows[2]);
        Assert.StartsWith("    \\ No newline", rows[3]);
        Assert.EndsWith("│", rows[3]);
    }

    [Fact]
    public void RenamesShowBothPaths()
    {
        var view = new DiffView
        {
            Layout = DiffLayout.Unified,
            Diff = "diff --git a/old.cs b/new.cs\nrename from old.cs\nrename to new.cs\n--- a/old.cs\n+++ b/new.cs\n@@ -1 +1 @@\n-a\n+b\n",
        };

        Assert.StartsWith("─ old.cs → new.cs ─", Rows(Render(view, 40, 3))[0]);
    }
}
