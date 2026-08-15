using ConsoleRender;
using static ConsoleRender.Demo.Pages.PageHelpers;

namespace ConsoleRender.Demo.Pages;

/// <summary>The scrollable output log, including buttons to append lines and run a demo task.</summary>
internal static class OutputPage
{
    public static Panel Build(ConsoleApp app, OutputField output, Checkbox typewriter, Label status)
    {
        var append = new Button("Append line") { Left = 0, Bottom = 0 };
        var counter = 0;
        append.Clicked += () => output.AppendLine($"Line {++counter} – wrapped lines keep their indentation.", Color.Cyan);

        var task = new Button("Start task") { Left = 19, Bottom = 0 };
        task.Clicked += () => DemoActions.RunDemoTask(app, output, seconds: 3, progress: null);

        output.AppendLine("Welcome to ConsoleRender!", Color.Cyan);
        output.AppendLine("");
        output.AppendLine("/echo, /color and /task write here; Page Up/Down scrolls.", Color.Gray);
        output.AppendLine("");

        return Fill(
            Info(0, "A scrollable color log. Task lines carry a spinner,"),
            Info(1, "until Complete/Fail freezes them with a check or a cross."),
            output, append, task, typewriter);
    }
}
