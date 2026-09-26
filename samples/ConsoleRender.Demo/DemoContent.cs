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

        ```diff
        @@ -1,2 +1,2 @@
        -var editor = new TextArea();
        +var editor = new TextArea { ReadOnly = true };
         app.Root.Add(editor);
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

    public const string SampleDiff = """
        diff --git a/src/Counter.cs b/src/Counter.cs
        index 3b18e51..a4f2c07 100644
        --- a/src/Counter.cs
        +++ b/src/Counter.cs
        @@ -3,14 +3,20 @@ using ConsoleRender;
         public sealed class Counter : Control
         {
        -    private int count;
        +    private int count = 0x10; // hex literal
        +    private int step = 1;
         
             public override bool OnKey(ConsoleKeyInfo key)
             {
        -        if (key.Key == ConsoleKey.Enter)
        +        if (key.Key == ConsoleKey.Spacebar)
                 {
        -            count++;
        +            count += step;
                     return true;
                 }
         
        +        if (key.Key == ConsoleKey.Escape)
        +        {
        +            count = 0;
        +        }
        +
                 return false;
             }
        diff --git a/README.md b/README.md
        index 81c4d0e..0f7b2aa 100644
        --- a/README.md
        +++ b/README.md
        @@ -1,3 +1,3 @@
         # Counter
         
        -Counts Enter presses.
        +Counts Space presses; Escape resets.
        \ No newline at end of file
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
