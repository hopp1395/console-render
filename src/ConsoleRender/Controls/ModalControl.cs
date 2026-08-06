namespace ConsoleRender;

/// <summary>
/// Base class for dialogs shown via <see cref="ConsoleApp.ShowDialog"/>. The host subscribes
/// to <see cref="CloseRequested"/> and removes the dialog when it fires, so a dialog never
/// has to know how it was presented.
/// </summary>
public abstract class ModalControl : Control
{
    /// <summary>Raised when the dialog wants to be dismissed.</summary>
    public event Action? CloseRequested;

    protected ModalControl()
    {
        Focusable = true;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Middle;
    }

    /// <summary>
    /// Asks the host to dismiss this dialog. Raise any result event <em>after</em> calling this,
    /// so a handler that opens another dialog is not closed again by this one.
    /// </summary>
    public void Close()
    {
        CloseRequested?.Invoke();
    }
}
