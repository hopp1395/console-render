# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build ConsoleRender.slnx -c Release          # build everything (warnings are errors)
dotnet test ConsoleRender.slnx -c Release           # run all tests (xUnit)
dotnet test ConsoleRender.slnx -c Release --filter "FullyQualifiedName~TextAreaTests"   # one test class
dotnet test ConsoleRender.slnx -c Release --filter "FullyQualifiedName~TextAreaTests.EnterSplitsTheLineAtTheCursor"  # one test
dotnet pack src/ConsoleRender -c Release -o artifacts
```

The library targets `net8.0`; the demo and tests target `net10.0` (the locally installed runtime is 10.x — don't lower these). The demo can render one frame headlessly, which is the way to inspect layout changes without an interactive terminal:

```bash
dotnet build ConsoleRender.slnx -c Release
samples/ConsoleRender.Demo/bin/Release/net10.0/ConsoleRender.Demo.exe --snapshot 120 32 --run "/befehl" [--modal] [--confirm]
```

Do NOT `dotnet run` the demo without `--snapshot`: with redirected stdout it enters the render loop and emits an endless ANSI stream.

## Releasing

Merging to `main` triggers the publish job (`.github/workflows/ci.yml`), which pushes to nuget.org via Trusted Publishing (OIDC, no stored secret). A release is exactly: raise `<Version>` in `src/ConsoleRender/ConsoleRender.csproj` and merge — `--skip-duplicate` makes merges without a version bump no-ops. nuget.org metadata (description, embedded README) is immutable per version, so README/description changes also need a version bump to become visible. Indexing lags 5–10 minutes behind a green publish job.

`main` is protected: changes go through a branch + PR (squash merge only; both CI jobs are required checks and the branch must be up to date). The repo owner approves PRs; their own PRs merge via admin override since GitHub forbids self-approval.

## Code conventions

- Private fields are camelCase **without** a leading underscore (enforced via `.editorconfig`). On field/parameter collisions (constructors), qualify with `this.`.
- Methods and constructors always use a **block body**, never an expression body (`=>`); a constructor initializer (`: this()` / `: base()`) sits on its own indented line. Properties, accessors and indexers may keep the expression form.
- Both rules live in `.editorconfig` and are enforced at build time (`EnforceCodeStyleInBuild` + `TreatWarningsAsErrors`), so a violation fails the build rather than just showing an IDE squiggle. Note that the `csharp_style_*` options alone are only IDE hints — the paired `dotnet_diagnostic.IDE00xx.severity` entries are what make them fire during a build.
- Every public method/setter validates arguments with Ardalis.GuardClauses (`Guard.Against.…`, globally imported via `GlobalUsings.cs`). Exception: the per-cell drawing hot path (`ConsoleBuffer` indexer/`Set`, per-cell loops inside `Draw`) — there, clipping is the defined semantics.
- `TreatWarningsAsErrors` is on; CS1591 (missing XML doc) is suppressed, but load-bearing types and non-obvious members carry doc comments.
- Language split: library code, comments and README are English; the demo app's UI strings and commit messages are German.

## Architecture

Single namespace `ConsoleRender`; folders under `src/ConsoleRender/` (Core, Controls, Text, Input, Ascii, Clipboard, App) are organization only.

**Rendering pipeline** — `Control.Draw` writes into a `ConsoleBuffer` (grid of `Cell`: char + 24-bit colors + `CellStyle` flags). `Renderer` keeps front/back buffers, diffs per cell and emits only changed cells as ANSI sequences. Nothing is ever printed directly; `ConsoleApp.RenderOffscreen(w, h)` runs the same layout+draw against a detached buffer, which is what the snapshot tests use (`.ToText()` for text, indexer for color/style asserts).

**Clipping** — `ConsoleBuffer` holds a clip-region stack. `Control.Render` pushes `Bounds` around `Draw` and `ContentRect` around child rendering, so children can't paint outside their parent. `Write`/`FillRect`/indexer-set clip silently; the indexer **getter does not bounds-check** — guard reads with `buffer.ClipRect.Contains(x, y)` (see the caret code in `TextArea`).

**Layout** — CSS-absolute-style anchors on every `Control` (`Left/Top/Right/Bottom` + `Width/Height`; both opposing anchors = stretch, none = alignment enum). Containers that place children programmatically override `ArrangeChildren()` — it runs after the container's own `Bounds` are known and before children lay out, so anchors set there apply in the same pass.

**Input dispatch** (`ConsoleApp.DispatchKey`) — with a modal open: focused control first, then the modal itself (that's why controls must NOT consume Tab or Escape they don't use). Without a modal: global `KeyBindings`, then the focused control, then Tab focus cycling. Focus scope is the topmost modal, else `Root`.

**Composite controls owning selection** — the established pattern (see `ConfirmDialog`, `SearchBox`, the demo's `MarkdownEditorDialog`): the container is the single Tab stop, inner controls get `Focusable = false`, keys are forwarded in `OnKey`. `Control.Focused` has an internal setter — in-assembly composites mirror it onto inner controls (e.g. `SearchBox` → its `TextBox`) so carets render.

**Modals** — derive from `ModalControl`, call `Close()` to request dismissal; `ConsoleApp` restores prior focus. Raise result events *after* `Close()` so a handler that opens the next dialog isn't closed with the old one.

**Scroll/cursor invariants** — scroll clamping lives exclusively in `Draw`, never in key handlers (heals resize and external state changes on the next frame). This pattern repeats across `TextBox`, `OutputField`, `SelectMenu`, `SearchBox`, `TextArea`.

**Syntax highlighting** (`Text/`) — `ISyntaxHighlighter` deliberately takes the whole document (fenced code blocks make line meaning depend on preceding lines) and returns per-line `HighlightSpan` lists that are sorted and overlap-free; nesting is expressed as adjacent spans with OR-ed style flags. `TextArea` caches the result and recomputes once per edit (version counter), not per frame.

**Animation** — controls accumulate elapsed time in `Update(TimeSpan)` and derive frames from it in `Draw` (`Spinner`, `ProgressBar` sweep, `OutputField.taskClock` for `TaskLine` spinners). `RenderOffscreen` never calls `Update`, so animation state in tests is exactly what the test set — animations are deterministic to assert.

**Line entries never contain `\n`/`\r`/`\t`** — `ConsoleBuffer.Write` stops at control characters. Multi-line controls normalize at every text entry point (`TextArea.Normalize` is the reference).

## Tests

Headless and deterministic; no interactive terminal needed. Conventions: a `Key(ConsoleKey, char, modifiers)` helper per test class builds `ConsoleKeyInfo`; rendering asserts go through `ConsoleApp.RenderOffscreen`. A control instance can only be added to one app — detach with `control.Parent?.Remove(control)` before re-rendering the same instance (see `TextAreaTests.Render`). Clipboard contents are never asserted (flaky in CI); only key consumption is.
