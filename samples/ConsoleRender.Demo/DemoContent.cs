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
        var app = new ConsoleApp(); // fenced code is highlighted by language
        app.Run();
        ```

        ```json
        { "name": "demo", "enabled": true, "retries": 3 }
        ```

        ```bash
        dotnet build -c Release && echo "done: $?"
        ```

        ---
        Esc closes the editor.
        """;

    public const string SampleCSharp = """
        using ConsoleRender;

        /* A block comment
           spanning two lines. */
        public sealed class Counter : Control
        {
            private int count = 0x10; // hex literal

            public override bool OnKey(ConsoleKeyInfo key)
            {
                if (key.Key == ConsoleKey.Spacebar)
                {
                    count++;
                    return true;
                }

                return false;
            }

            public string Caption => $"Count: {count}, rate {1.5f}";
        }
        """;

    public const string SampleJson = """
        {
          // jsonc allows comments
          "name": "ConsoleRender.Demo",
          "version": "0.6.0",
          "features": ["highlighting", "diff", "preview"],
          "options": { "typewriter": false, "speed": 160, "theme": null }
        }
        """;

    public const string SampleShell = """
        #!/usr/bin/env bash
        set -euo pipefail

        # Build everything and run the tests.
        for config in Debug Release; do
            echo "Building $config in ${PWD}"
            dotnet build ConsoleRender.slnx -c "$config" || exit $?
        done

        if [ "$#" -gt 0 ]; then
            dotnet test --filter "$1"
        fi
        """;
}
