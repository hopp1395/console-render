namespace ConsoleRender;

/// <summary>Text style flags of a single cell, mapped to ANSI SGR attributes.</summary>
[Flags]
public enum CellStyle
{
    None = 0,
    Bold = 1 << 0,
    Dim = 1 << 1,
    Italic = 1 << 2,
    Underline = 1 << 3,
    Blink = 1 << 4,
    Reverse = 1 << 5,
    Strikethrough = 1 << 6,
}
