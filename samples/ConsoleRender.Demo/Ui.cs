namespace ConsoleRender.Demo;

/// <summary>The controls the command handlers and key bindings need to reach after construction.</summary>
internal sealed record Ui(
    OutputField Output,
    CommandInput Input,
    AsciiArt Art,
    Label Status,
    Spinner Spinner,
    ProgressBar Progress,
    Checkbox TypewriterOption,
    OutputField HelpOutput,
    IReadOnlyList<string> FeatureNames,
    Action<string> ShowFeature,
    Action<int> ApplyResponsiveLayout);
