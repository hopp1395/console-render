using System.Diagnostics;

namespace ConsoleRender;

/// <summary>
/// Hosts the render loop: sizes the renderer to the terminal, lays out the control tree,
/// dispatches keyboard input and presents a frame at a fixed rate.
///
/// Input is dispatched in this order: modal dialog → global key bindings → Tab focus
/// cycling → the focused control.
/// </summary>
public sealed class ConsoleApp : IDisposable
{
    private readonly Renderer renderer;
    private readonly List<Control> modals = new();
    private readonly Stack<Control?> focusBeforeModal = new();
    private readonly Stopwatch clock = new();
    private readonly List<Control> focusables = new();

    private bool running;
    private bool disposed;
    private int lastWidth;
    private int lastHeight;
    private int targetFps = 30;

    /// <summary>The root container; add your controls here.</summary>
    public Panel Root { get; } = new();

    /// <summary>Global keyboard shortcuts, checked before the focused control.</summary>
    public KeyBindingManager KeyBindings { get; } = new();

    /// <summary>The control currently receiving key input, or null.</summary>
    public Control? FocusedControl { get; private set; }

    /// <summary>Current width of the render surface in character cells.</summary>
    public int Width => renderer.Width;

    /// <summary>Current height of the render surface in character cells.</summary>
    public int Height => renderer.Height;

    /// <summary>Target frame rate of the render loop.</summary>
    public int TargetFps
    {
        get => targetFps;
        set => targetFps = Guard.Against.NegativeOrZero(value);
    }

    /// <summary>Raised once per frame before layout and rendering.</summary>
    public event Action<TimeSpan>? Tick;

    /// <summary>True while a modal dialog is on screen.</summary>
    public bool HasModal => modals.Count > 0;

    public ConsoleApp()
    {
        (lastWidth, lastHeight) = GetTerminalSize();
        renderer = new Renderer(lastWidth, lastHeight);
    }

    /// <summary>Gives keyboard focus to <paramref name="control"/>.</summary>
    public void SetFocus(Control? control)
    {
        if (ReferenceEquals(FocusedControl, control)) return;
        if (FocusedControl is not null) FocusedControl.Focused = false;
        FocusedControl = control;
        if (FocusedControl is not null) FocusedControl.Focused = true;
    }

    /// <summary>Moves focus to the next (or previous) focusable control in tree order.</summary>
    public void CycleFocus(bool backwards = false)
    {
        RefreshFocusables();
        if (focusables.Count == 0)
        {
            SetFocus(null);
            return;
        }

        int index = FocusedControl is null ? -1 : focusables.IndexOf(FocusedControl);
        int next = index < 0
            ? (backwards ? focusables.Count - 1 : 0)
            : (index + (backwards ? -1 : 1) + focusables.Count) % focusables.Count;
        SetFocus(focusables[next]);
    }

    private void RefreshFocusables()
    {
        focusables.Clear();
        var scope = modals.Count > 0 ? modals[^1] : (Control)Root;
        scope.CollectFocusable(focusables);
    }

    /// <summary>
    /// Shows <paramref name="modal"/> on top of everything else. It receives all input
    /// until <see cref="CloseTopModal"/> is called; an <see cref="InfoBox"/> wires that up itself.
    /// </summary>
    public void ShowDialog(Control modal)
    {
        Guard.Against.Null(modal);

        focusBeforeModal.Push(FocusedControl);
        modals.Add(modal);
        if (modal is ModalControl dialog)
        {
            // Re-subscribing would close two modals per request if the same instance is reused.
            dialog.CloseRequested -= CloseTopModal;
            dialog.CloseRequested += CloseTopModal;
        }
        RefreshFocusables();
        SetFocus(focusables.FirstOrDefault() ?? modal);
        renderer.Invalidate();
    }

    /// <summary>Convenience helper that shows a simple <see cref="InfoBox"/> dialog.</summary>
    public InfoBox ShowInfo(string title, string text)
    {
        Guard.Against.Null(title);
        Guard.Against.Null(text);

        var box = new InfoBox { Title = title, Text = text };
        ShowDialog(box);
        return box;
    }

    /// <summary>
    /// Shows a modal question. <paramref name="onChosen"/> receives the index and label of the
    /// selected answer; Escape dismisses the dialog without calling it.
    /// </summary>
    public ConfirmDialog ShowConfirm(string title, string text, string[] options, Action<int, string> onChosen)
    {
        Guard.Against.Null(title);
        Guard.Against.Null(text);
        Guard.Against.NullOrEmpty(options);
        Guard.Against.Null(onChosen);

        var dialog = new ConfirmDialog(title, text, options);
        dialog.Chosen += (index, label) => onChosen(index, label);
        ShowDialog(dialog);
        return dialog;
    }

    /// <summary>Closes the topmost modal dialog, if any.</summary>
    public void CloseTopModal()
    {
        if (modals.Count == 0) return;

        var closing = modals[^1];
        modals.RemoveAt(modals.Count - 1);
        if (closing is ModalControl dialog)
            dialog.CloseRequested -= CloseTopModal;
        RefreshFocusables();

        // Restore whatever had focus before the dialog opened, if it is still reachable.
        var previous = focusBeforeModal.Count > 0 ? focusBeforeModal.Pop() : null;
        SetFocus(previous is not null && focusables.Contains(previous)
            ? previous
            : focusables.FirstOrDefault());
        renderer.Invalidate();
    }

    /// <summary>Stops the render loop started by <see cref="Run"/>.</summary>
    public void Exit() => running = false;

    /// <summary>
    /// Runs the render loop until <see cref="Exit"/> is called. Initializes the terminal
    /// on entry and restores it on exit, including on unhandled exceptions.
    /// </summary>
    public void Run()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        Terminal.Init();
        running = true;
        clock.Restart();

        if (FocusedControl is null)
            CycleFocus();

        try
        {
            var last = clock.Elapsed;
            while (running)
            {
                var now = clock.Elapsed;
                var delta = now - last;
                last = now;

                DrainInput();
                if (!running) break;

                SyncTerminalSize();

                Tick?.Invoke(delta);
                Root.UpdateAll(delta);
                foreach (var modal in modals)
                    modal.UpdateAll(delta);

                RenderFrame();

                int frameMs = Math.Max(1, 1000 / TargetFps);
                int elapsedMs = (int)(clock.Elapsed - now).TotalMilliseconds;
                if (elapsedMs < frameMs)
                    Thread.Sleep(frameMs - elapsedMs);
            }
        }
        finally
        {
            Terminal.Shutdown();
            clock.Stop();
        }
    }

    /// <summary>
    /// Lays out and draws one frame into a fresh off-screen buffer without touching the
    /// terminal. Useful for snapshot tests and for inspecting a layout headlessly.
    /// </summary>
    public ConsoleBuffer RenderOffscreen(int width, int height)
    {
        Guard.Against.NegativeOrZero(width);
        Guard.Against.NegativeOrZero(height);

        var buffer = new ConsoleBuffer(width, height);
        LayoutAndDraw(buffer, new Rect(0, 0, width, height));
        return buffer;
    }

    private void LayoutAndDraw(ConsoleBuffer buffer, Rect full)
    {
        buffer.ResetClip();

        Root.Left = 0;
        Root.Top = 0;
        Root.Width = full.Width;
        Root.Height = full.Height;
        Root.PerformLayout(full);
        foreach (var modal in modals)
            modal.PerformLayout(full);

        buffer.Clear();
        Root.Render(buffer);

        if (modals.Count > 0)
        {
            // Dim the background so the dialog stands out.
            DimBuffer(buffer);
            foreach (var modal in modals)
                modal.Render(buffer);
        }
    }

    private void RenderFrame()
    {
        LayoutAndDraw(renderer.Buffer, new Rect(0, 0, renderer.Width, renderer.Height));
        renderer.Present(Console.Out);
    }

    private static void DimBuffer(ConsoleBuffer buffer)
    {
        for (int y = 0; y < buffer.Height; y++)
            for (int x = 0; x < buffer.Width; x++)
            {
                var cell = buffer[x, y];
                buffer[x, y] = cell with
                {
                    Foreground = cell.Foreground.IsDefault ? Color.DarkGray : cell.Foreground.Scale(0.45),
                    Background = cell.Background.Scale(0.45),
                };
            }
    }

    private void DrainInput()
    {
        // Console.KeyAvailable throws when stdin is not a real console (piped or redirected).
        if (Console.IsInputRedirected) return;

        // Process every buffered key so fast typing and paste bursts stay responsive,
        // but cap the batch so input can never starve rendering.
        for (int i = 0; i < 64 && Console.KeyAvailable; i++)
        {
            var key = Console.ReadKey(intercept: true);
            DispatchKey(key);
            if (!running) return;
        }
    }

    private void DispatchKey(ConsoleKeyInfo key)
    {
        if (modals.Count > 0)
        {
            var modal = modals[^1];
            if (FocusedControl is not null && FocusedControl.OnKey(key)) return;
            modal.OnKey(key);
            return;
        }

        if (KeyBindings.Handle(key)) return;

        if (FocusedControl is not null && FocusedControl.OnKey(key)) return;

        if (key.Key == ConsoleKey.Tab)
            CycleFocus(key.Modifiers.HasFlag(ConsoleModifiers.Shift));
    }

    private void SyncTerminalSize()
    {
        var (width, height) = GetTerminalSize();
        if (width == lastWidth && height == lastHeight) return;
        lastWidth = width;
        lastHeight = height;
        renderer.Resize(width, height);
    }

    private static (int Width, int Height) GetTerminalSize()
    {
        try
        {
            return (Math.Max(20, Console.WindowWidth), Math.Max(5, Console.WindowHeight));
        }
        catch (IOException)
        {
            // No attached console (redirected output) — fall back to a sane default.
            return (80, 24);
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        running = false;
    }
}
