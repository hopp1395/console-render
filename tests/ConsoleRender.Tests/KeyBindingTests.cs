namespace ConsoleRender.Tests;

public class KeyBindingTests
{
    private static ConsoleKeyInfo Key(ConsoleKey key, ConsoleModifiers modifiers = 0) =>
        new('\0', key,
            modifiers.HasFlag(ConsoleModifiers.Shift),
            modifiers.HasFlag(ConsoleModifiers.Alt),
            modifiers.HasFlag(ConsoleModifiers.Control));

    [Fact]
    public void ToString_ListsModifiersBeforeTheKey()
    {
        Assert.Equal("Ctrl+Q", KeyCombo.Ctrl(ConsoleKey.Q).ToString());
        Assert.Equal("F1", new KeyCombo(ConsoleKey.F1).ToString());
        Assert.Equal("Ctrl+Alt+Shift+S",
            new KeyCombo(ConsoleKey.S,
                ConsoleModifiers.Control | ConsoleModifiers.Alt | ConsoleModifiers.Shift).ToString());
    }

    [Fact]
    public void Handle_RunsTheBoundAction()
    {
        var manager = new KeyBindingManager();
        bool called = false;
        manager.Register(KeyCombo.Ctrl(ConsoleKey.Q), "Beenden", () => called = true);

        Assert.True(manager.Handle(Key(ConsoleKey.Q, ConsoleModifiers.Control)));
        Assert.True(called);
    }

    [Fact]
    public void Handle_IgnoresTheSameKeyWithoutTheModifier()
    {
        var manager = new KeyBindingManager();
        manager.Register(KeyCombo.Ctrl(ConsoleKey.Q), "Beenden", () => { });

        Assert.False(manager.Handle(Key(ConsoleKey.Q)));
    }

    [Fact]
    public void Register_ReplacesAnExistingBindingForTheSameCombo()
    {
        var manager = new KeyBindingManager();
        manager.Register(ConsoleKey.F1, "alt", () => { });
        manager.Register(ConsoleKey.F1, "neu", () => { });

        Assert.Single(manager.All);
        Assert.Equal("neu", manager.All.Single().Description);
    }

    [Fact]
    public void Unregister_RemovesTheBinding()
    {
        var manager = new KeyBindingManager();
        manager.Register(ConsoleKey.F1, "Hilfe", () => { });

        Assert.True(manager.Unregister(new KeyCombo(ConsoleKey.F1)));
        Assert.False(manager.Handle(Key(ConsoleKey.F1)));
    }

    [Fact]
    public void Register_RejectsInvalidArguments()
    {
        var manager = new KeyBindingManager();

        Assert.Throws<ArgumentNullException>(() => manager.Register(ConsoleKey.F1, "Hilfe", null!));
        Assert.Throws<ArgumentException>(() => manager.Register(ConsoleKey.F1, " ", () => { }));
    }
}
