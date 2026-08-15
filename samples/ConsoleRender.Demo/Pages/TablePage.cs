using ConsoleRender;
using static ConsoleRender.Demo.Pages.PageHelpers;

namespace ConsoleRender.Demo.Pages;

/// <summary>A <see cref="Table"/> listing German cities by population.</summary>
internal static class TablePage
{
    public static Panel Build(Label status)
    {
        var table = new Table { Left = 0, Top = 3, Right = 0, Bottom = 0 };
        table.AddColumn("City", 16);
        table.AddColumn("Population", 12, TextAlignment.Right);
        table.AddColumn("State", 16);
        table.AddRow("Berlin", "3,700,000", "Berlin");
        table.AddRow("Hamburg", "1,900,000", "Hamburg");
        table.AddRow("Munich", "1,500,000", "Bavaria");
        table.AddRow("Cologne", "1,100,000", "North Rhine-Westphalia");
        table.AddRow("Frankfurt", "770,000", "Hesse");
        table.AddRow("Stuttgart", "630,000", "Baden-Württemberg");
        table.AddRow("Düsseldorf", "620,000", "North Rhine-Westphalia");
        table.AddRow("Leipzig", "600,000", "Saxony");
        table.SelectionChanged += i => status.Text = $"Row selected: {table.Rows[i][0]}";
        table.RowActivated += i => status.Text = $"Row activated: {table.Rows[i][0]}";

        return Fill(
            Info(0, "Arrow keys or Home/End move the selection, Enter activates the row."),
            Info(1, "Cells too long to fit scroll automatically, but only in the selected row."),
            table);
    }
}
