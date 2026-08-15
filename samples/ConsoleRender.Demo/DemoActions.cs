namespace ConsoleRender.Demo;

/// <summary>Behaviour shared between feature pages, slash commands and key bindings.</summary>
internal static class DemoActions
{
    /// <summary>Simulates work: a task line counts up and optionally drives the progress bar.</summary>
    public static void RunDemoTask(ConsoleApp app, OutputField output, double seconds, ProgressBar? progress)
    {
        var task = output.BeginTask("Processing data…");
        double done = 0;

        void OnTick(TimeSpan delta)
        {
            done += delta.TotalSeconds;
            var percent = Math.Min(100, done / seconds * 100);
            if (progress is not null)
            {
                progress.Indeterminate = false;
                progress.Value = percent;
            }

            task.Text = $"Processing data… {percent:0} %";
            if (done >= seconds)
            {
                task.Complete($"Data processed ({seconds:0.#} s).");
                app.Tick -= OnTick;
            }
        }

        app.Tick += OnTick;
    }

    public static void PasteClipboard(AsciiArt art, OutputField output, Label status)
    {
        if (Clipboard.TryGetImage(out var image))
        {
            var ascii = AsciiImageConverter.Convert(image, Math.Max(10, art.Bounds.Width));
            art.SetImage(ascii);
            status.Text = $"Image imported ({image.Width}×{image.Height} pixels).";
            return;
        }

        if (Clipboard.TryGetText(out var clipboardText))
        {
            foreach (var line in clipboardText.Replace("\r", "").Split('\n'))
            {
                output.AppendLine(line, Color.Cyan);
            }

            status.Text = "Text from the clipboard added to the output log.";
            return;
        }

        status.Text = "The clipboard contains neither text nor an image.";
    }

    public static void ShowEditorDialog(ConsoleApp app, Label status)
    {
        var dialog = new MarkdownEditorDialog(DemoContent.SampleMarkdown);
        dialog.CloseRequested += () => status.Text = "Editor closed.";
        app.ShowDialog(dialog);
    }

    /// <summary>Asks before quitting — the case a button row handles better than a shortcut.</summary>
    public static void ConfirmExit(ConsoleApp app, Ui ui)
    {
        app.ShowConfirm("Quit", "Really quit the demo?", ["Quit", "Cancel"], (index, _) =>
        {
            if (index == 0)
            {
                app.Exit();
            }
            else
            {
                ui.Status.Text = "Quit cancelled.";
            }
        });
    }
}
