# ConsoleRender

Ein TUI-Framework für .NET: volle Kontrolle über die Konsolenausgabe, doppelt gepuffert,
mit GUI-ähnlichen Steuerelementen, Anker-Layout, Slash-Befehlen und Copy/Paste für Text und Bilder.

Keine externen Abhängigkeiten außer [Ardalis.GuardClauses](https://github.com/ardalis/GuardClauses).

```
╔═ ConsoleRender ═ TUI-Framework für .NET ═╗
  ⠋ bereit                                        F1 Hilfe · Tab Fokus · Strg+Q Ende
┌─ Steuerelemente ───────────────┐┌─ Ausgabe ───────────────────┐┌─ ASCII-Grafik ─────┐
│Menü                            ││Willkommen bei ConsoleRender!││   ____             │
│› Übersicht                     ││                             ││  / ___|___  _ __   │
│  Farben & Effekte              ││Tab wechselt den Fokus.      ││ | |   / _ \| '_ \  │
│                                ││                             ││ | |__| (_) | | | | │
│Optionen                        ││                             ││  \____\___/|_| |_| │
│[x] Farbige Ausgabe             ││                             ││                    │
└────────────────────────────────┘└─────────────────────────────┘└────────────────────┘
┌─ Eingabe ──────────────────────────────────────────────────────────────────────────┐
│› /help                                                                             │
└────────────────────────────────────────────────────────────────────────────────────┘
```

## Installation

```bash
dotnet add package ConsoleRender
```

Die Bibliothek zielt auf `net8.0` und läuft damit auch unter .NET 9 und 10.

## Schnellstart

```csharp
using ConsoleRender;

using var app = new ConsoleApp();

var frame = new Frame("Beispiel")
{
    Left = 0, Top = 0, Right = 0, Bottom = 3,
    BorderColor = Color.Cyan,
};

var output = new OutputField { Left = 0, Top = 0, Right = 0, Bottom = 0 };
frame.Add(output);

var input = new CommandInput { Left = 0, Right = 0, Bottom = 0, Height = 1 };
input.Submitted += text => output.AppendLine(text, Color.Green);
input.Commands.Register("exit", "Beendet die Anwendung", _ => app.Exit());

app.Root.AddRange(frame, input);
app.KeyBindings.Register(KeyCombo.Ctrl(ConsoleKey.Q), "Beenden", app.Exit);
app.SetFocus(input);
app.Run();
```

## Rendering

Gezeichnet wird immer in einen Rückpuffer. `Present` vergleicht ihn Zelle für Zelle mit dem
Vorderpuffer und schickt nur die tatsächlich geänderten Zellen als ANSI-Sequenzen an das Terminal —
kein Flackern, kein Neuzeichnen des ganzen Bildschirms.

* 24-Bit-Farben (`Color.Rgb`, `Color.FromHsv`, `Color.Lerp`)
* Stilflags: fett, dünn, kursiv, unterstrichen, blinkend, invers, durchgestrichen
* Alternativer Bildschirmpuffer — nach dem Beenden ist das Terminal wieder unverändert
* Automatische VT-Aktivierung unter Windows
* Automatische Skalierung: das Layout wird bei jeder Größenänderung des Fensters neu berechnet
* Kind-Elemente werden am Inhaltsbereich ihres Elternteils abgeschnitten

## Layout mit Ankern

Jedes Element kennt `Left`, `Top`, `Right`, `Bottom`, `Width` und `Height` — alle optional.

| Gesetzt | Verhalten |
| --- | --- |
| nur `Left` | fester Abstand zum linken Rand |
| nur `Right` | fester Abstand zum rechten Rand |
| `Left` **und** `Right` | Element wird beim Resize mitgedehnt |
| keiner von beiden | `HorizontalAlignment` entscheidet (links, zentriert, rechts) |

Vertikal gilt dasselbe mit `Top`/`Bottom` und `VerticalAlignment`. `Width`/`Height` überschreiben
immer die natürliche Größe des Elements.

## Elemente

| Element | Zweck |
| --- | --- |
| `Label` | Textausgabe mit Farben, Stilen und Effekten (`Blink`, `Rainbow`, `Pulse`) |
| `OutputField` | scrollbares, farbiges Mehrzeilen-Log mit Schreibmaschineneffekt; umgebrochene Zeilen behalten ihren Einzug |
| `TextBox` | einzeiliges Eingabefeld mit Cursor, Scrolling und Zwischenablage |
| `CommandInput` | `TextBox` mit `/befehl`-Auswertung und Tab-Vervollständigung |
| `Frame` | Rahmen mit Titel; fünf Rahmenstile |
| `Panel` | unsichtbarer Container zum Gruppieren und Ausrichten |
| `InfoBox` | modale Meldung mit einer einzigen Art, sie wegzuklicken |
| `ConfirmDialog` | modale Rückfrage mit mehreren Antworten als Schaltflächenreihe |
| `Button` | beschriftete Aktion, mit Enter oder Leertaste ausgelöst |
| `Checkbox` | einzelne Ja/Nein-Option |
| `RadioGroup` | Optionsgruppe mit genau einer Auswahl |
| `SelectMenu` | scrollbares Auswahlmenü |
| `Spinner` | animierte Aktivitätsanzeige |
| `AsciiArt` | ASCII-Grafiken, einfarbig oder als farbiges Zeichenraster |

## Befehlseingabe

```csharp
input.Commands.Register("color", "Färbt eine Zeile: /color <name> <text>", args => { /* … */ });
```

`CommandRegistry` zerlegt die Eingabe in Tokens (doppelte Anführungszeichen gruppieren Wörter),
sucht den Befehl und meldet Fehler als `CommandResult` zurück, statt eine Exception nach oben
durchzureichen. Tab vervollständigt Befehlsnamen, solange die Eingabe mit `/` beginnt.

## Tastenkürzel

```csharp
app.KeyBindings.Register(KeyCombo.Ctrl(ConsoleKey.S), "Speichern", Save);
app.KeyBindings.Register(ConsoleKey.F1, "Hilfe", ShowHelp);
```

Globale Kürzel werden vor dem fokussierten Element geprüft. `app.KeyBindings.All` liefert alle
registrierten Kürzel samt Beschreibung — praktisch für einen `/help`-Befehl.

Eingebaut in den Elementen: Tab/Shift+Tab wechselt den Fokus, Pfeiltasten bewegen Auswahlen,
Leertaste schaltet um, Bild auf/ab scrollt die Ausgabe, Strg+C/Strg+V kopiert und fügt ein.

Für häufige Aktionen ist ein Tastenkürzel einem `Button` vorzuziehen: es kostet weder Bildfläche
noch einen Tab-Stopp. Schaltflächen lohnen sich dort, wo die Auswahlmöglichkeiten selbst sichtbar
sein müssen — dafür gibt es den `ConfirmDialog`.

## Rückfragen

```csharp
app.ShowConfirm("Beenden", "Änderungen speichern?",
    ["Speichern", "Verwerfen", "Abbrechen"],
    (index, label) => { /* … */ });
```

Der Dialog steuert die Auswahl selbst, statt jede Schaltfläche einzeln in den Fokuszyklus zu
hängen: Pfeiltasten links/rechts wandern durch die Antworten, Enter bestätigt, Escape bricht ab.
Die ganze Rückfrage bleibt damit ein einziger Tab-Stopp, und die Hervorhebung zeigt, welche
Antwort vorbelegt ist.

Eigene Dialoge leiten von `ModalControl` ab und rufen `Close()` auf, wenn sie verschwinden
wollen — wie sie angezeigt wurden, müssen sie nicht wissen.

## Eigene Container

Wer die Kinder eines Containers programmgesteuert anordnen will, statt sie einzeln mit Ankern zu
versehen, überschreibt `ArrangeChildren()`. Der Haken läuft, sobald die eigenen `Bounds` feststehen,
und noch bevor sich die Kinder selbst ausmessen — dort gesetzte Anker greifen also im selben
Layoutdurchlauf. `ConfirmDialog` zentriert damit seine Schaltflächenreihe.

## Zwischenablage und Bilder

```csharp
if (Clipboard.TryGetImage(out var image))
    art.SetImage(AsciiImageConverter.Convert(image, targetWidth: 60));
else if (Clipboard.TryGetText(out string text))
    output.AppendLine(text);
```

Unter Windows werden Text (`CF_UNICODETEXT`) und Bilder (`CF_DIB`, 24/32 Bit) direkt über Win32
gelesen; Bilder werden mit Helligkeitsrampe und Zellenseitenverhältnis in farbige ASCII-Art
umgerechnet. Unter Linux/macOS gibt es einen Textfallback über `xclip` bzw. `pbcopy`/`pbpaste`.

## Demo

```bash
dotnet run --project samples/ConsoleRender.Demo
```

Die Demo zeigt alle Elemente gleichzeitig und kennt die Befehle `/help`, `/echo`, `/clear`,
`/color`, `/info`, `/typewriter`, `/paste`, `/copy`, `/logo`, `/busy` und `/exit`.

Ein einzelnes Bild lässt sich ohne interaktives Terminal rendern — nützlich für Snapshots:

```bash
ConsoleRender.Demo --snapshot 120 32
```

Dasselbe steht als API zur Verfügung: `app.RenderOffscreen(width, height).ToText()`.

## Argumentprüfung

Alle öffentlichen Methoden prüfen ihre Argumente mit Guard Clauses und melden ungültige Werte
sofort als `ArgumentException`, statt später mit einem verzerrten Layout zu überraschen.
Ausgenommen ist der Zeichen-Hot-Path (`ConsoleBuffer`-Indexer und `Set`), wo Clipping die
definierte Semantik ist.

## Veröffentlichen

Der Workflow unter `.github/workflows/ci.yml` baut und testet jeden Pull Request auf Linux und
Windows. Bei jedem Push auf `main` — also auch bei jedem Merge — läuft zusätzlich der
Veröffentlichungsschritt und schickt das Paket an nuget.org.

**Eine neue Version erscheint nur, wenn `<Version>` in `src/ConsoleRender/ConsoleRender.csproj`
erhöht wurde.** Der Push benutzt `--skip-duplicate`: ist die Version auf nuget.org schon
vorhanden, wird sie stillschweigend übersprungen und der Merge bleibt grün. Ein Release besteht
damit aus genau einem Schritt — Versionsnummer im csproj anheben und mergen.

Es wird kein dauerhafter API-Schlüssel gespeichert. Der Workflow nutzt **Trusted Publishing**:
GitHub stellt für den Job ein signiertes OIDC-Token aus, nuget.org prüft es gegen eine hinterlegte
Richtlinie und gibt dafür einen Schlüssel zurück, der eine Stunde gilt. Es gibt also kein Secret,
das auslaufen, verloren gehen oder abfließen kann.

Einmalig einzurichten ist nur die Richtlinie auf nuget.org unter *Trusted Publishing*:

| Feld | Wert |
| --- | --- |
| Package Owner | `hopp1395` |
| Repository Owner | `hopp1395` |
| Repository | `console-render` |
| Workflow File | `ci.yml` |
| Environment | leer |

Der Job braucht dafür die Berechtigung `id-token: write`, die in `ci.yml` gesetzt ist.

Lokal lässt sich dasselbe Paket erzeugen mit:

```bash
dotnet pack src/ConsoleRender -c Release -o artifacts
```

Neben dem `.nupkg` entsteht ein `.snupkg` mit den Symbolen; zusammen mit SourceLink kann man aus
einem konsumierenden Projekt heraus in die Quellen des Pakets hineindebuggen.

## Lizenz

MIT
