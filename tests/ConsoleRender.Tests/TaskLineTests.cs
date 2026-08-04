namespace ConsoleRender.Tests;

public class TaskLineTests
{
    private static string Render(OutputField output, int width = 40, int height = 5)
    {
        var app = new ConsoleApp();
        output.Left = 0;
        output.Top = 0;
        output.Width = width;
        output.Height = height;
        app.Root.Add(output);
        return app.RenderOffscreen(width, height).ToText();
    }

    [Fact]
    public void ARunningTaskShowsASpinnerFrameInFrontOfTheText()
    {
        var output = new OutputField();
        output.BeginTask("lädt…");

        string text = Render(output);

        Assert.Contains("⠋ lädt…", text);
    }

    [Fact]
    public void TheSpinnerAdvancesWithTheFieldsClock()
    {
        var output = new OutputField();
        output.BeginTask("lädt…");

        output.Update(TimeSpan.FromSeconds(0.08));

        Assert.Contains("⠙ lädt…", Render(output));
    }

    [Fact]
    public void TheTextCanChangeWhileTheTaskRuns()
    {
        var output = new OutputField();
        var task = output.BeginTask("0 %");

        task.Text = "50 %";

        Assert.Contains("⠋ 50 %", Render(output));
    }

    [Fact]
    public void CompleteFreezesTheLineWithACheckMark()
    {
        var output = new OutputField();
        var task = output.BeginTask("lädt…");

        task.Complete("fertig.");
        output.Update(TimeSpan.FromSeconds(1));

        Assert.False(task.Running);
        Assert.Contains("✓ fertig.", Render(output));
    }

    [Fact]
    public void FailFreezesTheLineWithACross_KeepingTheTextWhenNoneIsGiven()
    {
        var output = new OutputField();
        var task = output.BeginTask("lädt…");

        task.Fail();

        Assert.Contains("✗ lädt…", Render(output));
    }

    [Fact]
    public void ATaskLineAppearsImmediately_EvenWhileTheTypewriterIsOn()
    {
        var output = new OutputField { Typewriter = true };
        output.AppendLine("eine langsame Zeile");
        var task = output.BeginTask("sofort da");

        // No Update has run, so the typewriter has not revealed anything yet.
        string text = Render(output);

        Assert.DoesNotContain("eine langsame Zeile", text);
        Assert.Contains("⠋ sofort da", text);
    }

    [Fact]
    public void OrdinaryLinesBeforeAndAfterTheTaskKeepTheirOrder()
    {
        var output = new OutputField();
        output.AppendLine("davor");
        var task = output.BeginTask("lädt…");
        output.AppendLine("danach");
        task.Complete();

        string[] rows = Render(output).Split('\n');

        Assert.Contains("davor", rows[0]);
        Assert.Contains("✓ lädt…", rows[1]);
        Assert.Contains("danach", rows[2]);
    }
}
