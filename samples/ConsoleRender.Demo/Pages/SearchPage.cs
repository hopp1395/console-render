using ConsoleRender;
using static ConsoleRender.Demo.Pages.PageHelpers;

namespace ConsoleRender.Demo.Pages;

/// <summary>A <see cref="SearchBox"/> over a sample list of German cities.</summary>
internal static class SearchPage
{
    public static Panel Build(Label status)
    {
        var search = new SearchBox(
            "Berlin", "Hamburg", "Munich", "Cologne", "Frankfurt",
            "Stuttgart", "Düsseldorf", "Dortmund", "Essen", "Leipzig")
        {
            Left = 0, Top = 3, Width = 32, Bottom = 0,
            EmptyText = "no matches",
        };

        search.Input.Placeholder = "Search city…";
        search.ItemActivated += (_, item) => status.Text = $"City selected: {item}";
        return Fill(
            Info(0, "Typing filters, arrow keys select, Enter activates,"),
            Info(1, "Escape clears the search. The feature list on the left is one too."),
            search);
    }
}
