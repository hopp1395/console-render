namespace ConsoleRender;

/// <summary>A fixed-width column of a <see cref="Table"/>, with its cells' text alignment.</summary>
public readonly record struct TableColumn(string Header, int Width, TextAlignment Alignment = TextAlignment.Left);
