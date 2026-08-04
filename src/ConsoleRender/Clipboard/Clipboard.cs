using System.Runtime.InteropServices;

namespace ConsoleRender;

/// <summary>
/// System clipboard access. On Windows, text (CF_UNICODETEXT) and images (CF_DIB) are
/// supported via Win32. On Linux/macOS a best-effort text fallback uses xclip/pbpaste.
/// </summary>
public static class Clipboard
{
    private const uint CfUnicodeText = 13;
    private const uint CfDib = 8;
    private const uint GmemMoveable = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern UIntPtr GlobalSize(IntPtr hMem);

    private static bool TryOpenClipboard()
    {
        // The clipboard is a shared resource; retry briefly if another process holds it.
        for (int i = 0; i < 5; i++)
        {
            if (OpenClipboard(IntPtr.Zero)) return true;
            Thread.Sleep(10);
        }
        return false;
    }

    public static bool TryGetText(out string text)
    {
        text = "";
        if (!OperatingSystem.IsWindows())
            return TryGetTextUnix(out text);

        if (!TryOpenClipboard()) return false;
        try
        {
            IntPtr handle = GetClipboardData(CfUnicodeText);
            if (handle == IntPtr.Zero) return false;
            IntPtr ptr = GlobalLock(handle);
            if (ptr == IntPtr.Zero) return false;
            try
            {
                text = Marshal.PtrToStringUni(ptr) ?? "";
                return text.Length > 0;
            }
            finally
            {
                GlobalUnlock(handle);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    public static bool TrySetText(string text)
    {
        Guard.Against.Null(text);

        if (!OperatingSystem.IsWindows())
            return TrySetTextUnix(text);

        if (!TryOpenClipboard()) return false;
        try
        {
            EmptyClipboard();
            int bytes = (text.Length + 1) * 2;
            IntPtr hMem = GlobalAlloc(GmemMoveable, (UIntPtr)bytes);
            if (hMem == IntPtr.Zero) return false;
            IntPtr ptr = GlobalLock(hMem);
            if (ptr == IntPtr.Zero) return false;
            try
            {
                Marshal.Copy(text.ToCharArray(), 0, ptr, text.Length);
                Marshal.WriteInt16(ptr, text.Length * 2, 0);
            }
            finally
            {
                GlobalUnlock(hMem);
            }
            // On success the system owns the memory; on failure we leak a small block, which
            // is the standard trade-off with SetClipboardData.
            return SetClipboardData(CfUnicodeText, hMem) != IntPtr.Zero;
        }
        finally
        {
            CloseClipboard();
        }
    }

    /// <summary>Reads an image (CF_DIB) from the clipboard, if present. Windows only.</summary>
    public static bool TryGetImage(out IPixelSource image)
    {
        image = null!;
        if (!OperatingSystem.IsWindows()) return false;
        if (!TryOpenClipboard()) return false;
        try
        {
            IntPtr handle = GetClipboardData(CfDib);
            if (handle == IntPtr.Zero) return false;
            IntPtr ptr = GlobalLock(handle);
            if (ptr == IntPtr.Zero) return false;
            try
            {
                int size = (int)GlobalSize(handle);
                var data = new byte[size];
                Marshal.Copy(ptr, data, 0, size);
                return DibPixelSource.TryParse(data, out image);
            }
            finally
            {
                GlobalUnlock(handle);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static bool TryGetTextUnix(out string text)
    {
        text = "";
        string? tool = OperatingSystem.IsMacOS() ? "pbpaste" : "xclip";
        string args = OperatingSystem.IsMacOS() ? "" : "-selection clipboard -o";
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(tool, args)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            using var p = System.Diagnostics.Process.Start(psi);
            if (p is null) return false;
            text = p.StandardOutput.ReadToEnd();
            p.WaitForExit(2000);
            return p.ExitCode == 0 && text.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool TrySetTextUnix(string text)
    {
        Guard.Against.Null(text);

        string tool = OperatingSystem.IsMacOS() ? "pbcopy" : "xclip";
        string args = OperatingSystem.IsMacOS() ? "" : "-selection clipboard";
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(tool, args)
            {
                RedirectStandardInput = true,
                UseShellExecute = false,
            };
            using var p = System.Diagnostics.Process.Start(psi);
            if (p is null) return false;
            p.StandardInput.Write(text);
            p.StandardInput.Close();
            p.WaitForExit(2000);
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
