using ConsoleRender;
using static ConsoleRender.Demo.Pages.PageHelpers;

namespace ConsoleRender.Demo.Pages;

/// <summary>The inline Markdown editor with its preview (the same workbench the modal editor uses).</summary>
internal static class EditorPage
{
    public static Panel Build(ConsoleApp app, out MarkdownWorkbench workbench)
    {
        workbench = new MarkdownWorkbench(app, DemoContent.SampleMarkdown)
        {
            Left = 0, Top = 3, Right = 0, Bottom = 0,
        };

        return Fill(
            Info(0, "Markdown is highlighted as you type; F2 cycles editor, preview and split."),
            Info(1, "Enter wraps, Ctrl+Home/End jumps, /editor opens it modally."),
            workbench);
    }
}
