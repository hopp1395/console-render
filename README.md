# ConsoleRender

A TUI framework for .NET: full control over the console output, double-buffered, with GUI-like
controls, anchor-based layout, slash commands and clipboard support for text and images.

No dependencies beyond [Ardalis.GuardClauses](https://github.com/ardalis/GuardClauses).

```
╔═ ConsoleRender ═ a TUI framework for .NET ═╗
  ⠋ ready                                         F1 help · Tab focus · Ctrl+Q quit
┌─ Controls ─────────────────────┐┌─ Output ────────────────────┐┌─ ASCII art ────────┐
│Menu                            ││Welcome to ConsoleRender!    ││   ____             │
│› Overview                      ││                             ││  / ___|___  _ __   │
│  Colors & effects              ││Tab moves the focus.         ││ | |   / _ \| '_ \  │
│                                ││                             ││ | |__| (_) | | | | │
│Options                         ││                             ││  \____\___/|_| |_| │
│[x] Colored output              ││                             ││                    │
└────────────────────────────────┘└─────────────────────────────┘└────────────────────┘
┌─ Input ────────────────────────────────────────────────────────────────────────────┐
│› /help                                                                             │
└────────────────────────────────────────────────────────────────────────────────────┘
```

## Installation

```bash
dotnet add package ConsoleRender
```

The library targets `net8.0` and therefore also runs on .NET 9 and 10.

## Quick start

```csharp
using ConsoleRender;

using var app = new ConsoleApp();

var frame = new Frame("Example")
{
    Left = 0, Top = 0, Right = 0, Bottom = 3,
    BorderColor = Color.Cyan,
};

var output = new OutputField { Left = 0, Top = 0, Right = 0, Bottom = 0 };
frame.Add(output);

var input = new CommandInput { Left = 0, Right = 0, Bottom = 0, Height = 1 };
input.Submitted += text => output.AppendLine(text, Color.Green);
input.Commands.Register("exit", "Exits the application", _ => app.Exit());

app.Root.AddRange(frame, input);
app.KeyBindings.Register(KeyCombo.Ctrl(ConsoleKey.Q), "Quit", app.Exit);
app.SetFocus(input);
app.Run();
```

## Rendering

Drawing always goes into a back buffer. `Present` compares it cell by cell against the front
buffer and sends only the cells that actually changed to the terminal as ANSI sequences — no
flicker, no repainting of the whole screen.

* 24-bit color (`Color.Rgb`, `Color.FromHsv`, `Color.Lerp`)
* Style flags: bold, dim, italic, underline, blink, reverse, strikethrough
* Alternate screen buffer — the terminal is left untouched after the app exits
* VT processing is enabled automatically on Windows
* Automatic rescaling: the layout is recomputed whenever the window is resized
* Children are clipped to their parent's content area

## Anchor-based layout

Every control has `Left`, `Top`, `Right`, `Bottom`, `Width` and `Height` — all optional.

| Set | Behaviour |
| --- | --- |
| `Left` only | fixed distance from the left edge |
| `Right` only | fixed distance from the right edge |
| `Left` **and** `Right` | the control stretches when the terminal is resized |
| neither | `HorizontalAlignment` decides (left, center, right) |

The same applies vertically with `Top`/`Bottom` and `VerticalAlignment`. `Width`/`Height` always
override the control's natural size.

## Controls

| Control | Purpose |
| --- | --- |
| `Label` | text output with colors, styles and effects (`Blink`, `Rainbow`, `Pulse`) |
| `OutputField` | scrollable, colored multi-line log with a typewriter effect; wrapped lines keep their indent |
| `TextBox` | single-line input with caret, scrolling, clipboard and an optional border |
| `CommandInput` | a `TextBox` that runs `/command` input and completes names with Tab |
| `Frame` | a titled border; five border styles |
| `Panel` | invisible container for grouping and positioning |
| `InfoBox` | modal message with a single way out |
| `ConfirmDialog` | modal question offering several answers as a row of buttons |
| `Button` | a labelled action, triggered with Enter or Space |
| `Checkbox` | a single yes/no option |
| `RadioGroup` | option group with exactly one selection |
| `SelectMenu` | scrollable selection list |
| `Spinner` | animated activity indicator |
| `AsciiArt` | ASCII art, single-colored or as a colored glyph grid |

## Slash commands

```csharp
input.Commands.Register("color", "Colors a line: /color <name> <text>", args => { /* … */ });
```

`CommandRegistry` splits the input into tokens (double quotes group words together), looks the
command up and reports failures as a `CommandResult` instead of letting an exception escape. Tab
completes command names while the input starts with `/`.

## Key bindings

```csharp
app.KeyBindings.Register(KeyCombo.Ctrl(ConsoleKey.S), "Save", Save);
app.KeyBindings.Register(ConsoleKey.F1, "Help", ShowHelp);
```

Global shortcuts are checked before the focused control sees the key. `app.KeyBindings.All`
returns every registered shortcut with its description — handy for a `/help` command.

Built into the controls: Tab/Shift+Tab moves the focus, arrow keys move selections, Space
toggles, Page Up/Down scrolls the output, Ctrl+C/Ctrl+V copy and paste.

For anything done often, prefer a key binding over a `Button`: it costs neither screen space nor
a Tab stop. Buttons earn their place where the available choices themselves need to be visible —
which is what `ConfirmDialog` is for.

## Asking a question

```csharp
app.ShowConfirm("Quit", "Save your changes?",
    ["Save", "Discard", "Cancel"],
    (index, label) => { /* … */ });
```

The dialog steers the selection itself instead of putting every button into the focus cycle:
left/right arrows move along the answers, Enter confirms, Escape cancels. That keeps the whole
question a single Tab stop, and the highlight shows which answer is preselected.

Custom dialogs derive from `ModalControl` and call `Close()` when they want to go away — they
never need to know how they were presented.

## Custom containers

To arrange a container's children programmatically instead of anchoring each one, override
`ArrangeChildren()`. The hook runs once the container's own `Bounds` are known and before the
children measure themselves, so anchors set there take effect in the same layout pass.
`ConfirmDialog` uses it to center its button row.

## Clipboard and images

```csharp
if (Clipboard.TryGetImage(out var image))
    art.SetImage(AsciiImageConverter.Convert(image, targetWidth: 60));
else if (Clipboard.TryGetText(out string text))
    output.AppendLine(text);
```

On Windows, text (`CF_UNICODETEXT`) and images (`CF_DIB`, 24/32 bit) are read directly through
Win32; images are converted to colored ASCII art using a brightness ramp and the cell aspect
ratio. On Linux and macOS there is a text-only fallback via `xclip` and `pbcopy`/`pbpaste`.

## Sample application

```bash
dotnet run --project samples/ConsoleRender.Demo
```

The sample shows every control at once and knows the commands `/help`, `/echo`, `/clear`,
`/color`, `/info`, `/confirm`, `/border`, `/typewriter`, `/paste`, `/copy`, `/logo`, `/busy`
and `/exit`. Its user interface is in German.

A single frame can be rendered without an interactive terminal, which is useful for snapshots:

```bash
ConsoleRender.Demo --snapshot 120 32
```

The same is available as an API: `app.RenderOffscreen(width, height).ToText()`.

## Argument checking

Every public method validates its arguments with guard clauses and reports invalid values right
away as an `ArgumentException`, rather than surprising you later with a skewed layout. The
drawing hot path is excluded (`ConsoleBuffer`'s indexer and `Set`), where clipping is the defined
behaviour.

## Releasing

The workflow in `.github/workflows/ci.yml` builds and tests every pull request on Linux and
Windows. On every push to `main` — that is, on every merge — the publish job additionally sends
the package to nuget.org.

**A new version only appears when `<Version>` in `src/ConsoleRender/ConsoleRender.csproj` has
been raised.** The push uses `--skip-duplicate`: if the version already exists on nuget.org it is
silently skipped and the merge stays green. A release therefore takes exactly one step — raise
the version in the csproj and merge.

No long-lived API key is stored. The workflow uses **trusted publishing**: GitHub issues a signed
OIDC token for the job, nuget.org validates it against a registered policy and hands back a key
that is valid for one hour. There is no secret that can expire, get lost or leak.

The only one-time setup is the policy on nuget.org under *Trusted Publishing*:

| Field | Value |
| --- | --- |
| Package Owner | `hopp1395` |
| Repository Owner | `hopp1395` |
| Repository | `console-render` |
| Workflow File | `ci.yml` |
| Environment | empty |

The job needs the `id-token: write` permission for this, which is set in `ci.yml`.

The same package can be built locally with:

```bash
dotnet pack src/ConsoleRender -c Release -o artifacts
```

Alongside the `.nupkg` a `.snupkg` with the symbols is produced; together with SourceLink you can
step into the package's sources from a consuming project.

## License

MIT
