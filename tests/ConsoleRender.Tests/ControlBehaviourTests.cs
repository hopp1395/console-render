namespace ConsoleRender.Tests;

public class ControlBehaviourTests
{
    private static ConsoleKeyInfo Key(ConsoleKey key, char ch = '\0', ConsoleModifiers modifiers = 0) =>
        new(ch, key,
            modifiers.HasFlag(ConsoleModifiers.Shift),
            modifiers.HasFlag(ConsoleModifiers.Alt),
            modifiers.HasFlag(ConsoleModifiers.Control));

    [Fact]
    public void TextBox_TypingAndBackspaceEditTheText()
    {
        var box = new TextBox();

        box.OnKey(Key(ConsoleKey.A, 'a'));
        box.OnKey(Key(ConsoleKey.B, 'b'));
        box.OnKey(Key(ConsoleKey.Backspace));
        box.OnKey(Key(ConsoleKey.C, 'c'));

        Assert.Equal("ac", box.Text);
    }

    [Fact]
    public void TextBox_InsertsAtTheCursorPosition()
    {
        var box = new TextBox();
        box.SetText("ac");

        box.OnKey(Key(ConsoleKey.LeftArrow));
        box.OnKey(Key(ConsoleKey.B, 'b'));

        Assert.Equal("abc", box.Text);
    }

    [Fact]
    public void TextBox_EnterRaisesSubmittedWithTheCurrentText()
    {
        var box = new TextBox();
        string? submitted = null;
        box.Submitted += text => submitted = text;
        box.SetText("hallo");

        box.OnKey(Key(ConsoleKey.Enter));

        Assert.Equal("hallo", submitted);
    }

    [Fact]
    public void TextBox_IgnoresTab_SoFocusCyclingStillWorks()
    {
        var box = new TextBox();

        Assert.False(box.OnKey(Key(ConsoleKey.Tab, '\t')));
        Assert.Equal("", box.Text);
    }

    [Fact]
    public void CommandInput_RunsSlashCommandsAndClearsTheField()
    {
        var input = new CommandInput();
        string[]? received = null;
        input.Commands.Register("echo", "test", args => received = args);
        input.SetText("/echo eins");

        input.OnKey(Key(ConsoleKey.Enter));

        Assert.Equal(new[] { "eins" }, received);
        Assert.Equal("", input.Text);
    }

    [Fact]
    public void CommandInput_PlainTextIsSubmittedRatherThanExecuted()
    {
        var input = new CommandInput();
        string? submitted = null;
        input.Submitted += text => submitted = text;
        input.SetText("kein befehl");

        input.OnKey(Key(ConsoleKey.Enter));

        Assert.Equal("kein befehl", submitted);
    }

    [Fact]
    public void CommandInput_TabCompletesAUniqueCommandName()
    {
        var input = new CommandInput();
        input.Commands.Register("status", "test", _ => { });
        input.SetText("/sta");

        input.OnKey(Key(ConsoleKey.Tab, '\t'));

        Assert.Equal("/status ", input.Text);
    }

    [Fact]
    public void CommandInput_TabCompletesToTheCommonPrefixOfSeveralMatches()
    {
        var input = new CommandInput();
        input.Commands.Register("color", "test", _ => { });
        input.Commands.Register("copy", "test", _ => { });
        input.SetText("/c");

        input.OnKey(Key(ConsoleKey.Tab, '\t'));

        Assert.Equal("/co", input.Text);
    }

    [Fact]
    public void Checkbox_SpaceTogglesAndRaisesTheEvent()
    {
        var checkbox = new Checkbox("test");
        bool? raised = null;
        checkbox.CheckedChanged += value => raised = value;

        checkbox.OnKey(Key(ConsoleKey.Spacebar, ' '));

        Assert.True(checkbox.Checked);
        Assert.True(raised);
    }

    [Fact]
    public void RadioGroup_SelectsOnlyAfterConfirmingTheHighlight()
    {
        var group = new RadioGroup("a", "b", "c");

        group.OnKey(Key(ConsoleKey.DownArrow));
        Assert.Equal(0, group.SelectedIndex);   // moving the cursor alone selects nothing

        group.OnKey(Key(ConsoleKey.Spacebar, ' '));
        Assert.Equal(1, group.SelectedIndex);
        Assert.Equal("b", group.SelectedItem);
    }

    [Fact]
    public void SelectMenu_ArrowKeysWrapAround()
    {
        var menu = new SelectMenu("a", "b", "c");

        menu.OnKey(Key(ConsoleKey.UpArrow));

        Assert.Equal(2, menu.SelectedIndex);
    }

    [Fact]
    public void SelectMenu_EnterActivatesTheSelectedItem()
    {
        var menu = new SelectMenu("a", "b");
        string? activated = null;
        menu.ItemActivated += (_, item) => activated = item;

        menu.OnKey(Key(ConsoleKey.DownArrow));
        menu.OnKey(Key(ConsoleKey.Enter));

        Assert.Equal("b", activated);
    }

    [Fact]
    public void SelectMenu_EmptyMenu_IgnoresKeys()
    {
        var menu = new SelectMenu();

        Assert.False(menu.OnKey(Key(ConsoleKey.DownArrow)));
    }

    [Fact]
    public void OutputField_DropsTheOldestLinesBeyondMaxLines()
    {
        var field = new OutputField { MaxLines = 3, Left = 0, Top = 0, Width = 20, Height = 3 };
        for (int i = 0; i < 5; i++)
            field.AppendLine($"Zeile {i}");

        var buffer = Render(field, 20, 3);

        Assert.Equal(
            "Zeile 2             \n" +
            "Zeile 3             \n" +
            "Zeile 4             ",
            buffer.ToText());
    }

    [Fact]
    public void OutputField_WrapsLinesWiderThanTheField()
    {
        var field = new OutputField { Left = 0, Top = 0, Width = 5, Height = 2 };
        field.AppendLine("abcdefgh");

        Assert.Equal("abcde\nfgh  ", Render(field, 5, 2).ToText());
    }

    [Fact]
    public void OutputField_ContinuationRowsKeepTheIndentOfTheirLine()
    {
        var field = new OutputField { Left = 0, Top = 0, Width = 8, Height = 3 };
        field.AppendLine("  abcdefghijkl");

        Assert.Equal(
            "  abcdef\n" +
            "  ghijkl\n" +
            "        ",
            Render(field, 8, 3).ToText());
    }

    [Fact]
    public void OutputField_UnindentedLinesStillWrapToTheLeftEdge()
    {
        var field = new OutputField { Left = 0, Top = 0, Width = 4, Height = 2 };
        field.AppendLine("abcdefgh");

        Assert.Equal("abcd\nefgh", Render(field, 4, 2).ToText());
    }

    [Fact]
    public void OutputField_IndentWiderThanTheFieldStillMakesProgress()
    {
        var field = new OutputField { Left = 0, Top = 0, Width = 3, Height = 3 };
        field.AppendLine("      xyz");

        // The six-space indent is capped to half the field, so text keeps flowing.
        Assert.Equal(
            "   \n" +
            "  x\n" +
            " yz",
            Render(field, 3, 3).ToText());
    }

    [Fact]
    public void OutputField_MaxLines_RejectsNonPositiveValues()
    {
        var field = new OutputField();

        Assert.Throws<ArgumentException>(() => field.MaxLines = 0);
    }

    [Fact]
    public void Label_TextAlignmentPositionsTheTextInsideTheBounds()
    {
        var label = new Label("ab")
        {
            Left = 0, Top = 0, Width = 6, Height = 1,
            TextAlign = TextAlignment.Right,
        };

        Assert.Equal("    ab", Render(label, 6, 1).ToText());
    }

    [Fact]
    public void Frame_ClipsChildrenToItsContentArea()
    {
        var frame = new Frame { Left = 0, Top = 0, Width = 8, Height = 4, Border = BorderStyle.Ascii };
        // Anchored past the bottom edge of the frame's content area.
        frame.Add(new Label("XXXXXXXX") { Left = 0, Top = 5 });

        string text = Render(frame, 8, 4).ToText();

        Assert.DoesNotContain("X", text);
        Assert.Equal(
            "+------+\n" +
            "|      |\n" +
            "|      |\n" +
            "+------+",
            text);
    }

    /// <summary>Lays out and draws a single control into a fresh buffer.</summary>
    private static ConsoleBuffer Render(Control control, int width, int height)
    {
        using var app = new ConsoleApp();
        app.Root.Add(control);
        return app.RenderOffscreen(width, height);
    }
}
