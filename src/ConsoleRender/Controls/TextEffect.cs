namespace ConsoleRender;

/// <summary>Built-in text animations for <see cref="Label"/>.</summary>
public enum TextEffect
{
    None,
    /// <summary>Text toggles visibility twice per second.</summary>
    Blink,
    /// <summary>Animated hue gradient across the characters.</summary>
    Rainbow,
    /// <summary>Brightness oscillates smoothly.</summary>
    Pulse,
}
