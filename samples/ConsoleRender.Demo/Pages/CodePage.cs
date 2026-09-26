using ConsoleRender;
using static ConsoleRender.Demo.Pages.PageHelpers;

namespace ConsoleRender.Demo.Pages;

/// <summary>A code editor with language-aware highlighting; /code switches the language.</summary>
internal static class CodePage
{
    public static Panel Build(out TextArea editor)
    {
        editor = new TextArea
        {
            Left = 0, Top = 3, Right = 0, Bottom = 0,
        };

        ShowLanguage(editor, CodeLanguage.CSharp);
        return Fill(
            Info(0, "Keywords, strings, numbers, comments; Markdown fences use it too."),
            Info(1, "/code <csharp|json|shell> switches the sample."),
            editor);
    }

    /// <summary>Loads the language's sample and switches the highlighter to it.</summary>
    public static void ShowLanguage(TextArea editor, CodeLanguage language)
    {
        Guard.Against.Null(editor);
        Guard.Against.Null(language);

        editor.Highlighter = new CodeHighlighter(language);
        editor.Text = language == CodeLanguage.Json
            ? DemoContent.SampleJson
            : language == CodeLanguage.Shell
                ? DemoContent.SampleShell
                : DemoContent.SampleCSharp;
        editor.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, true));
    }
}
