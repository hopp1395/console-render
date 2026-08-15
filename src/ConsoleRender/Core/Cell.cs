namespace ConsoleRender;

/// <summary>A single character cell of the screen buffer.</summary>
public readonly record struct Cell(char Char, Color Foreground, Color Background, CellStyle Style)
{
    public static readonly Cell Empty = new(' ', Color.Default, Color.Default, CellStyle.None);
}
