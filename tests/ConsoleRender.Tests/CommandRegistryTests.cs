namespace ConsoleRender.Tests;

public class CommandRegistryTests
{
    [Fact]
    public void Tokenize_SplitsOnWhitespace()
    {
        Assert.Equal(new[] { "echo", "hallo", "welt" }, CommandRegistry.Tokenize("echo hallo welt"));
    }

    [Fact]
    public void Tokenize_KeepsQuotedSegmentsTogether()
    {
        Assert.Equal(new[] { "echo", "hallo welt", "x" }, CommandRegistry.Tokenize("echo \"hallo welt\" x"));
    }

    [Fact]
    public void Tokenize_CollapsesRepeatedWhitespace()
    {
        Assert.Equal(new[] { "a", "b" }, CommandRegistry.Tokenize("   a     b   "));
    }

    [Fact]
    public void Tokenize_EmptyInput_YieldsNoTokens()
    {
        Assert.Empty(CommandRegistry.Tokenize("   "));
    }

    [Fact]
    public void Execute_InvokesTheHandlerWithArguments()
    {
        var registry = new CommandRegistry();
        string[]? received = null;
        registry.Register("echo", "test", args => received = args);

        var result = registry.Execute("/echo eins zwei");

        Assert.True(result.Success);
        Assert.Equal(new[] { "eins", "zwei" }, received);
    }

    [Fact]
    public void Execute_UnknownCommand_ReportsFailureInsteadOfThrowing()
    {
        var registry = new CommandRegistry();

        var result = registry.Execute("/gibtsnicht");

        Assert.False(result.Success);
        Assert.Contains("gibtsnicht", result.Message);
    }

    [Fact]
    public void Execute_HandlerException_IsReportedAsFailure()
    {
        var registry = new CommandRegistry();
        registry.Register("boom", "test", _ => throw new InvalidOperationException("kaputt"));

        var result = registry.Execute("/boom");

        Assert.False(result.Success);
        Assert.Contains("kaputt", result.Message);
    }

    [Fact]
    public void Execute_IsCaseInsensitive()
    {
        var registry = new CommandRegistry();
        var called = false;
        registry.Register("Help", "test", _ => called = true);

        Assert.True(registry.Execute("/HELP").Success);
        Assert.True(called);
    }

    [Fact]
    public void Complete_ReturnsMatchesSorted()
    {
        var registry = new CommandRegistry();
        registry.Register("copy", "c", _ => { });
        registry.Register("color", "c", _ => { });
        registry.Register("exit", "e", _ => { });

        Assert.Equal(new[] { "color", "copy" }, registry.Complete("co"));
    }

    [Fact]
    public void Register_RejectsInvalidArguments()
    {
        var registry = new CommandRegistry();

        Assert.Throws<ArgumentException>(() => registry.Register("  ", "d", _ => { }));
        Assert.Throws<ArgumentNullException>(() => registry.Register("x", "d", null!));
    }
}
