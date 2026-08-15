namespace ConsoleRender.Demo;

/// <summary>Static text content shared by several feature pages.</summary>
internal static class DemoContent
{
    public const string Logo = """
          ____                      _
         / ___|___  _ __  ___  ___ | | ___
        | |   / _ \| '_ \/ __|/ _ \| |/ _ \
        | |__| (_) | | | \__ \ (_) | |  __/
         \____\___/|_| |_|___/\___/|_|\___|
              R  E  N  D  E  R
        """;

    public const string SampleMarkdown = """
        # Markdown Editor

        The editor highlights **bold**, *italic*, `code` and ~~strikethrough~~,
        plus ***both at once***.

        - List item with [a link](https://example.org)
        1. Numbered list

        > Quotes appear in italic gray.

        ```csharp
        var app = new ConsoleApp();
        app.Run();
        ```

        ---
        Esc closes the editor.
        """;
}
