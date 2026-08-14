namespace ConsoleRender.Tests;

public class DialogTests
{
    private static ConsoleKeyInfo Key(ConsoleKey key, char ch = '\0')
    {
        return new(ch, key, false, false, false);
    }

    [Fact]
    public void Button_EnterAndSpaceTriggerTheClick()
    {
        var button = new Button("Ok");
        int clicks = 0;
        button.Clicked += () => clicks++;

        button.OnKey(Key(ConsoleKey.Enter));
        button.OnKey(Key(ConsoleKey.Spacebar, ' '));

        Assert.Equal(2, clicks);
    }

    [Fact]
    public void Button_IgnoresOtherKeys()
    {
        var button = new Button("Ok");
        button.Clicked += () => Assert.Fail("should not fire");

        Assert.False(button.OnKey(Key(ConsoleKey.LeftArrow)));
    }

    [Fact]
    public void Button_RendersItsLabelInBrackets()
    {
        var button = new Button("Ok") { Left = 0, Top = 0 };

        Assert.Equal("[ Ok ]", RenderControl(button, 6, 1).ToText());
    }

    [Fact]
    public void ConfirmDialog_ArrowKeysMoveTheSelectionAndWrapAround()
    {
        var dialog = new ConfirmDialog("t", "frage", "A", "B", "C");

        dialog.OnKey(Key(ConsoleKey.RightArrow));
        Assert.Equal(1, dialog.SelectedIndex);

        dialog.OnKey(Key(ConsoleKey.LeftArrow));
        dialog.OnKey(Key(ConsoleKey.LeftArrow));
        Assert.Equal(2, dialog.SelectedIndex);
    }

    [Fact]
    public void ConfirmDialog_TabMovesAlongTheButtonRow()
    {
        var dialog = new ConfirmDialog("t", "frage", "A", "B");

        dialog.OnKey(Key(ConsoleKey.Tab, '\t'));

        Assert.Equal(1, dialog.SelectedIndex);
    }

    [Fact]
    public void ConfirmDialog_EnterReportsTheChosenAnswer()
    {
        var dialog = new ConfirmDialog("t", "frage", "Speichern", "Verwerfen");
        (int Index, string Label)? chosen = null;
        dialog.Chosen += (index, label) => chosen = (index, label);

        dialog.OnKey(Key(ConsoleKey.RightArrow));
        dialog.OnKey(Key(ConsoleKey.Enter));

        Assert.Equal((1, "Verwerfen"), chosen);
    }

    [Fact]
    public void ConfirmDialog_EscapeCancelsWithoutChoosing()
    {
        var dialog = new ConfirmDialog("t", "frage", "A", "B");
        bool cancelled = false;
        dialog.Chosen += (_, _) => Assert.Fail("should not choose");
        dialog.Cancelled += () => cancelled = true;

        dialog.OnKey(Key(ConsoleKey.Escape));

        Assert.True(cancelled);
    }

    [Fact]
    public void ConfirmDialog_ClosesBeforeReportingTheAnswer()
    {
        // Order matters: a handler that opens the next dialog must not have it closed
        // by the dialog that triggered it.
        var dialog = new ConfirmDialog("t", "frage", "A");
        var events = new List<string>();
        dialog.CloseRequested += () => events.Add("close");
        dialog.Chosen += (_, _) => events.Add("chosen");

        dialog.OnKey(Key(ConsoleKey.Enter));

        Assert.Equal(new[] { "close", "chosen" }, events);
    }

    [Fact]
    public void ConfirmDialog_SetOptions_RejectsAnEmptyAnswerList()
    {
        var dialog = new ConfirmDialog();

        Assert.Throws<ArgumentException>(() => dialog.SetOptions());
    }

    [Fact]
    public void ConfirmDialog_SetOptions_ReplacesThePreviousButtons()
    {
        var dialog = new ConfirmDialog("t", "frage", "A", "B", "C");

        dialog.SetOptions("Ja", "Nein");

        Assert.Equal(new[] { "Ja", "Nein" }, dialog.Options);
        Assert.Equal(2, dialog.Children.Count);
        Assert.Equal(0, dialog.SelectedIndex);
    }

    [Fact]
    public void ConfirmDialog_IsASingleTabStop()
    {
        // The dialog steers the button row itself, so its buttons stay out of the focus cycle.
        using var app = new ConsoleApp();
        var dialog = new ConfirmDialog("t", "frage", "A", "B", "C");
        app.ShowDialog(dialog);

        app.CycleFocus();

        Assert.Same(dialog, app.FocusedControl);
    }

    [Fact]
    public void ConfirmDialog_DrawsTheAnswersAsAButtonRow()
    {
        var dialog = new ConfirmDialog("Frage", "Ja oder nein?", "Ja", "Nein");

        string text = RenderControl(dialog, 30, 8).ToText();

        Assert.Contains("[ Ja ]", text);
        Assert.Contains("[ Nein ]", text);
    }

    [Fact]
    public void ShowConfirm_ChoosingAnAnswerClosesTheDialog()
    {
        using var app = new ConsoleApp();
        string? chosen = null;
        var dialog = app.ShowConfirm("t", "frage", ["Ja", "Nein"], (_, label) => chosen = label);

        Assert.True(app.HasModal);
        dialog.OnKey(Key(ConsoleKey.Enter));

        Assert.Equal("Ja", chosen);
        Assert.False(app.HasModal);
    }

    [Fact]
    public void ShowDialog_RestoresThePreviousFocusAfterClosing()
    {
        using var app = new ConsoleApp();
        var input = new TextBox { Left = 0, Top = 0 };
        app.Root.Add(input);
        app.SetFocus(input);

        var dialog = app.ShowConfirm("t", "frage", ["Ok"], (_, _) => { });
        Assert.Same(dialog, app.FocusedControl);

        dialog.OnKey(Key(ConsoleKey.Escape));

        Assert.Same(input, app.FocusedControl);
    }

    [Fact]
    public void ShowingTheSameDialogTwice_ClosesOnlyOneModal()
    {
        using var app = new ConsoleApp();
        var dialog = new ConfirmDialog("t", "frage", "Ok");

        app.ShowDialog(dialog);
        dialog.OnKey(Key(ConsoleKey.Escape));
        app.ShowDialog(dialog);
        app.ShowInfo("zweite", "box");

        // Closing the info box must leave the dialog underneath on screen.
        app.CloseTopModal();

        Assert.True(app.HasModal);
    }

    [Fact]
    public void InfoBox_EnterClosesIt()
    {
        using var app = new ConsoleApp();
        app.ShowInfo("t", "text");

        Assert.True(app.HasModal);
        app.FocusedControl!.OnKey(Key(ConsoleKey.Enter));

        Assert.False(app.HasModal);
    }

    private static ConsoleBuffer RenderControl(Control control, int width, int height)
    {
        using var app = new ConsoleApp();
        app.Root.Add(control);
        return app.RenderOffscreen(width, height);
    }
}
