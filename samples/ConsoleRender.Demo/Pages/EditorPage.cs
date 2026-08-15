using ConsoleRender;
using static ConsoleRender.Demo.Pages.PageHelpers;

namespace ConsoleRender.Demo.Pages;

/// <summary>The inline Markdown editor (the same control the modal editor dialog uses).</summary>
internal static class EditorPage
{
    public static Panel Build()
    {
        var editor = new TextArea
        {
            Left = 0, Top = 3, Right = 0, Bottom = 0,
            Highlighter = new MarkdownHighlighter(),
            Text = DemoContent.SampleMarkdown,
        };

        editor.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, true));
        return Fill(
            Info(0, "Multi-line editor, Markdown is highlighted as you type."),
            Info(1, "Enter wraps, Ctrl+Home/End jumps, /editor opens it modally."),
            editor);
    }
}
