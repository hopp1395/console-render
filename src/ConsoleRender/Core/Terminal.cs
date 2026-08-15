using System.Runtime.InteropServices;
using System.Text;

namespace ConsoleRender;

/// <summary>
/// Low-level terminal setup: enables ANSI/VT processing on Windows, switches to the
/// alternate screen buffer and hides the hardware cursor while an app is running.
/// </summary>
public static class Terminal
{
    private const int StdOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 0x0004;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    /// <summary>Prepares the terminal for rendering. Call <see cref="Shutdown"/> before exiting.</summary>
    public static void Init()
    {
        if (OperatingSystem.IsWindows())
        {
            var handle = GetStdHandle(StdOutputHandle);
            if (GetConsoleMode(handle, out var mode))
            {
                SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
            }
        }

        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch (IOException)
        {
            // Output is redirected; the encoding of the target stream applies instead.
        }

        // Ctrl+C must reach the app as a key, but only a real console can deliver it.
        if (!Console.IsInputRedirected)
        {
            Console.TreatControlCAsInput = true;
        }

        // Alternate screen buffer + hide cursor.
        Console.Out.Write("\x1b[?1049h\x1b[?25l\x1b[2J\x1b[H");
        Console.Out.Flush();
    }

    /// <summary>Restores the terminal to its previous state.</summary>
    public static void Shutdown()
    {
        Console.Out.Write("\x1b[0m\x1b[?25h\x1b[?1049l");
        Console.Out.Flush();
        if (!Console.IsInputRedirected)
        {
            Console.TreatControlCAsInput = false;
        }
    }
}
