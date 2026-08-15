namespace ConsoleRender;

/// <summary>How a control frames itself.</summary>
public enum BorderMode
{
    /// <summary>No border; the control is nothing but its content.</summary>
    None,

    /// <summary>A closed box around the content, costing one cell on every side.</summary>
    Full,

    /// <summary>A rule above and below the content, open at the left and right.</summary>
    TopAndBottom,
}
