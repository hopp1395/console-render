using System.Diagnostics.CodeAnalysis;

namespace ConsoleRender;

/// <summary>
/// The lexical rules <see cref="CodeHighlighter"/> needs to color a language: its keywords,
/// comment and string delimiters and a few language-specific switches. Deliberately a
/// table rather than a grammar — enough for readable colors, not for parsing.
///
/// Three definitions ship with the package (<see cref="CSharp"/>, <see cref="Json"/>,
/// <see cref="Shell"/>); further languages are plain instances of this class.
/// </summary>
public sealed class CodeLanguage
{
    public CodeLanguage(string name, IEnumerable<string> aliases, IEnumerable<string> keywords)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.Null(aliases);
        Guard.Against.Null(keywords);

        Name = name;
        Aliases = aliases.ToArray();
        Keywords = new HashSet<string>(keywords, StringComparer.Ordinal);
    }

    /// <summary>Display name, e.g. "C#".</summary>
    public string Name { get; }

    /// <summary>Fence info strings that select this language, e.g. "cs" or "csharp".</summary>
    public IReadOnlyList<string> Aliases { get; }

    public IReadOnlySet<string> Keywords { get; }

    /// <summary>Starts a comment that runs to the end of the line, e.g. "//"; null for none.</summary>
    public string? LineComment { get; init; }

    public string? BlockCommentStart { get; init; }
    public string? BlockCommentEnd { get; init; }

    /// <summary>Characters that open and close a string literal; a backslash escapes.</summary>
    public string StringQuotes { get; init; } = "\"";

    /// <summary>Marks a variable reference, like '$' in shell scripts; null for none.</summary>
    public char? VariablePrefix { get; init; }

    /// <summary>Colors a string directly followed by ':' as an object key (JSON).</summary>
    public bool HighlightObjectKeys { get; init; }

    public static CodeLanguage CSharp { get; } = new(
        "C#",
        ["csharp", "cs", "c#"],
        [
            "abstract", "as", "async", "await", "base", "bool", "break", "byte", "case", "catch",
            "char", "checked", "class", "const", "continue", "decimal", "default", "delegate",
            "do", "double", "dynamic", "else", "enum", "event", "explicit", "extern", "false",
            "finally", "fixed", "float", "for", "foreach", "get", "goto", "if", "implicit", "in",
            "init", "int", "interface", "internal", "is", "lock", "long", "namespace", "new",
            "null", "object", "operator", "out", "override", "params", "private", "protected",
            "public", "readonly", "record", "ref", "required", "return", "sbyte", "sealed", "set",
            "short", "sizeof", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
            "var", "virtual", "void", "volatile", "when", "where", "while", "with", "yield",
        ])
    {
        LineComment = "//",
        BlockCommentStart = "/*",
        BlockCommentEnd = "*/",
        StringQuotes = "\"'",
    };

    public static CodeLanguage Json { get; } = new(
        "JSON",
        ["json", "jsonc"],
        ["true", "false", "null"])
    {
        // Comments are not JSON, but jsonc and most config files allow them.
        LineComment = "//",
        BlockCommentStart = "/*",
        BlockCommentEnd = "*/",
        HighlightObjectKeys = true,
    };

    public static CodeLanguage Shell { get; } = new(
        "Shell",
        ["sh", "bash", "shell", "zsh"],
        [
            "if", "then", "else", "elif", "fi", "for", "while", "until", "do", "done", "case",
            "esac", "in", "function", "return", "local", "export", "readonly", "exit", "break",
            "continue", "select", "time",
        ])
    {
        LineComment = "#",
        StringQuotes = "\"'",
        VariablePrefix = '$',
    };

    /// <summary>The built-in languages.</summary>
    public static IReadOnlyList<CodeLanguage> All { get; } = [CSharp, Json, Shell];

    /// <summary>Finds a built-in language by one of its aliases, ignoring case.</summary>
    public static bool TryFind(string alias, [NotNullWhen(true)] out CodeLanguage? language)
    {
        Guard.Against.Null(alias);

        foreach (var candidate in All)
        {
            foreach (var known in candidate.Aliases)
            {
                if (string.Equals(known, alias, StringComparison.OrdinalIgnoreCase))
                {
                    language = candidate;
                    return true;
                }
            }
        }

        language = null;
        return false;
    }
}
