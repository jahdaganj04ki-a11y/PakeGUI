# Pake GUI für Windows

[![Release](https://github.com/jahdaganj04ki-a11y/PakeGUIWin/actions/workflows/release.yml/badge.svg)](https://github.com/jahdaganj04ki-a11y/PakeGUIWin/releases/latest)

Native Windows-Oberfläche (C# / WinForms, .NET 10) für den Pake-Online-Workflow
[`pake-cli.yaml`](https://github.com/jahdaganj04ki-a11y/PakeGUIWin/blob/main/.github/workflows/pake-cli.yaml)
aus [tw93/Pake](https://github.com/tw93/Pake).

Das Programm bildet **alle Eingaben des Workflows** als Formular ab, zeigt den daraus
erzeugten Pake-CLI-Aufruf live an, baut die App lokal und kann denselben Workflow
zusätzlich per GitHub CLI (gh) in der Cloud starten.

| | |
|---|---|
| **Fertige EXE** | `dist/win-x64/PakeGUIWin.exe` (49 MB, self-contained – keine .NET-Installation nötig) |
| **ZIP** | `dist/PakeGUIWin-win-x64.zip` |
| **Zielsystem** | Windows 10/11, x64 (Windows 11 ARM läuft über x64-Emulation) |
| **Quellcode** | `src/PakeGui.Core` (Logik), `src/PakeGuiWin` (GUI), `tests/` (46 Tests) |

---

## 1. Schnellstart

1. `PakeGUIWin.exe` starten (kein Setup, keine Adminrechte).
2. Im Tab **Workflow** URL und App-Name eintragen, Fenstergröße prüfen.
3. Rechts oben den **Ausgabeordner** wählen (Standard: `Dokumente\PakeBuilds`).
4. Einmalig **Build → Umgebung prüfen** ausführen.
   Fehlt etwas, helfen **Build → Pake-CLI installieren (npm -g)** und die Links im Protokoll.
5. **Build starten** (F5). Das Protokoll zeigt den kompletten Pake-Output;
   am Ende wird das erzeugte Paket (z. B. `MeineApp_1.0.0_x64_en-US.msi`) gemeldet.

> Der erste Build dauert lange (Rust kompiliert Tauri). Folgebuilds sind deutlich schneller.

### Voraussetzungen für lokale Builds

| Werkzeug | Version | Wofür |
|---|---|---|
| Node.js | ≥ 20.9 (empfohlen 22) | Pake-CLI |
| Rust (rustup) | ≥ 1.85 | Tauri-Kompilierung |
| Visual Studio Build Tools | aktuell, Workload „Desktop development with C++“ | MSVC-Linker (Windows-Standard) |
| WebView2 Runtime | – | Laufzeit der fertigen App (Windows 11: vorinstalliert) |

Ohne MSVC, aber mit MSYS2/MinGW: im Tab **Build** die Rust-Toolchain auf `gnu` stellen.
Die GUI erkennt fehlende Werkzeuge über **Umgebung prüfen** (node, npm, rustc, cargo, cl, WebView2).

---

## 2. Abbildung des Online-Workflows

Die 14 Eingaben des Workflows sind 1:1 vorhanden – gleiche Bedeutung, gleiche Standardwerte,
gleiche Wirkung auf die Pake-CLI:

| Workflow-Eingabe | Feld im Programm | Erzeugte CLI-Option | Standard |
|---|---|---|---|
| `platform` | Workflow → Plattform | – (steuert Zielformat-Prüfung und gh-Dispatch) | `windows-latest` |
| `url` | Workflow → URL | 1. Argument | – (Pflicht) |
| `name` | Workflow → App-Name | `--name` | – (Pflicht) |
| `icon` | Workflow → Icon | `--icon` (leer = automatisch) | leer |
| `width` | Workflow → Breite | `--width` | `1200` |
| `height` | Workflow → Höhe | `--height` | `780` |
| `min_width` | Workflow → Min. Breite | `--min-width` | leer (`0`) |
| `min_height` | Workflow → Min. Höhe | `--min-height` | leer (`0`) |
| `app_version` | Workflow → App-Version | `--app-version` | `1.0.0` |
| `fullscreen` | Workflow → Vollbild starten | `--fullscreen` | `false` |
| `hide_title_bar` | Workflow → Titelleiste ausblenden | `--hide-title-bar` | `false` |
| `new_window` | Workflow → Neue Fenster erlauben | `--new-window` | `false` |
| `multi_arch` | Workflow → Universal Binary | `--multi-arch` | `false` |
| `targets` | Workflow → Zielformat | `--targets` | `x64` (Workflow: `deb`) |

Bewusste Unterschiede zum Workflow, damit lokal nichts kaputtgeht:

* **`targets`** wird nur übergeben, wenn das Format zur gewählten Plattform passt
  (`x64/arm64` Windows, `deb/appimage/rpm/zst` Linux, `dmg/app/intel/apple/universal` macOS).
  Unbekannte Formate werden verworfen und im Protokoll als Hinweis gemeldet.
  Der Workflow übergibt `--targets` nur unter Linux; die GUI macht das plattformabhängig.
* **`platform`** beeinflusst den lokalen Build nicht – gebaut wird immer für das laufende
  System. Die Auswahl steuert die Gültigkeitsprüfung der Zielformate und den gh-Dispatch.
  Deshalb ist der Standard `x64`, nicht `deb`.
* **Artefakte**: Der Workflow lädt sie als GitHub-Artifacts hoch (3 Tage), die GUI legt sie
  im Ausgabeordner ab und öffnet diesen auf Wunsch.

---

## 3. Zusätzliche Pake-Optionen (alle vorhanden)

Alles, was die Pake-CLI kennt, lässt sich einstellen – verteilt auf vier Tabs:

| Tab | Optionen |
|---|---|
| **Fenster** | `--zoom`, `--title`, `--maximize`, `--always-on-top`, `--hide-window-decorations`, `--dark-mode`, `--disabled-web-shortcuts`, `--activation-shortcut`, `--hide-on-close`, `--no-restore-last-url`, `--incognito`, `--password-autosave` |
| **Funktionen** | `--show-system-tray`, `--system-tray-icon`, `--start-to-tray`, `--multi-instance`, `--multi-window`, `--enable-find`, `--enable-drag-drop`, `--wasm` |
| **Netzwerk** | `--proxy-url`, `--user-agent`, `--ignore-certificate-errors`, `--basic-auth`, `--force-internal-navigation`, `--safe-domain`, `--internal-url-regex`, `--inject`, `--download-dir`, `--use-local-file`, `--identifier` |
| **Build** | `--windows-toolchain`, `--installer-language`, `--keep-binary`, `--no-bundle`, `--iterative-build`, `--install`, `--camera`, `--microphone`, `--debug`, freie Zusatzargumente |

Nicht als Formularfeld vorhanden sind nur die reinen Aufruf-Schalter
`--help`, `--version`, `--json` und `--config`: Sie verändern keinen App-Inhalt,
sondern steuern die CLI selbst. `--json`/`--config` sind über **Pake-Aufruf → Eigener Befehl**
bzw. **Zusätzliche Argumente** trotzdem nutzbar.

---

## 4. Online-Workflow aus der GUI starten

Menü **Workflow**:

* **Workflow starten (gh CLI)…** – schickt die aktuellen Eingaben als `workflow_dispatch`
  an `jahdaganj04ki-a11y/PakeGUIWin` (Voraussetzung: [GitHub CLI](https://cli.github.com),
  `gh auth login`). Danach baut GitHub auf dem gewählten Runner und stellt `.msi`/`.dmg`/`.deb` bereit.
* **Letzte Workflow-Läufe anzeigen** – `gh run list` im Protokoll.
* **gh-Befehl kopieren / Dispatch-JSON kopieren / Dispatch-JSON speichern…** – dieselben
  Eingaben als Befehl bzw. als Payload für die REST-API.
* **Workflow im Browser öffnen**.

Hinweis: GitHub-Runner bauen nur dann macOS-/Linux-Pakete, wenn `platform` entsprechend gesetzt ist –
genau wie im Original-Workflow.

---

## 5. Bedienung

* **Befehl** (rechts oben) zeigt immer den aktuellen Aufruf inkl. `cd` in den Ausgabeordner.
  `Strg+Shift+C` oder **Befehl kopieren** übernimmt ihn in die Zwischenablage.
* **Statuszeile über dem Protokoll** prüft laufend: rote Meldung = Build wird blockiert,
  orange = Hinweis (z. B. „`--hide-title-bar` wirkt nur auf macOS“), grün = bereit.
* **Profil**: `Strg+S` speichert alle Einstellungen als JSON, `Strg+O` lädt sie.
  Beim Beenden wird automatisch unter `%APPDATA%\PakeGUIWin\settings.json` gesichert.
* **Pake-Aufruf**: *Automatisch* nutzt ein global installiertes `pake`, sonst `npx --yes pake-cli@latest`;
  *Eigener Befehl* erlaubt z. B. `pnpm pake` oder einen festen Pfad.
* **Ausgabeordner öffnen** zeigt die fertigen Pakete im Explorer.

Tastenkürzel: `F5` Build starten · `Strg+Shift+C` Befehl kopieren · `Strg+S` Profil speichern · `Strg+O` Profil laden.

---

## 6. Selbst bauen

Voraussetzung: [.NET SDK 10](https://dotnet.microsoft.com/download) (oder neuer).

```powershell
.\build.ps1 -RunTests      # Tests + Release-Build nach dist\win-x64\PakeGUIWin.exe
```

oder manuell:

```powershell
dotnet test tests/PakeGui.Core.Tests/PakeGui.Core.Tests.csproj
dotnet publish src/PakeGuiWin/PakeGuiWin.csproj -c Release -r win-x64 -o dist/win-x64
```

Auf Linux/macOS lässt sich die Windows-EXE ebenfalls erzeugen
(`EnableWindowsTargeting`), ausführen lässt sie sich dort nicht:

```bash
dotnet publish src/PakeGuiWin/PakeGuiWin.csproj -c Release -r win-x64 -o dist/win-x64
```

### Projektstruktur

```
.github/workflows/
  release.yml              Tag/Manuell -> baut EXE und legt GitHub-Release an
src/PakeGui.Core/          Logik ohne UI (plattformunabhängig, testbar)
  PakeSettings.cs          alle Einstellungen inkl. Workflow-Eingaben
  PakeArgumentBuilder.cs   Einstellungen -> pake-Argumente (+ Hinweise)
  PakeCommandLineBuilder.cs Aufruf, PATH-Suche, Build-Skript
  PakeValidation.cs        Feldprüfungen (Fehler/Warnungen)
  WorkflowDispatch.cs      workflow_dispatch-JSON und gh-Befehl
src/PakeGuiWin/            WinForms-Oberfläche
  MainForm.cs              Rahmen, Befehl, Protokoll, Build-Steuerung, gh
  SettingsPanel.cs         5 Tabs mit allen Optionen
  BuildRunner.cs           Prozessstart (cmd/sh) mit Streaming und Abbruch
tests/PakeGui.Core.Tests/  46 xUnit-Tests
tools/make-icon.py         erzeugt assets/app.ico
```

---

## 7. Releases auf GitHub

Die Builds landen automatisch in den [Releases](https://github.com/jahdaganj04ki-a11y/PakeGUIWin/releases) –
gesteuert von [`.github/workflows/release.yml`](.github/workflows/release.yml).

**Variante A – Tag pushen** (üblich):

```bash
git tag v1.2.0
git push origin v1.2.0
```

**Variante B – manuell** (Actions → *Release* → *Run workflow*):
Version ohne `v` eingeben (z. B. `1.2.0`), optional „Als Pre-Release veröffentlichen“ ankreuzen.
Der Tag `v1.2.0` wird dabei automatisch am aktuellen Commit angelegt.

Der Workflow erledigt:

1. .NET 10 SDK einrichten, **46 Tests** ausführen (bricht bei Fehlern ab).
2. Version aus Tag bzw. Eingabe ableiten, als `Version`/`FileVersion` in die EXE schreiben.
3. `PakeGUIWin.exe` self-contained für `win-x64` bauen.
4. `PakeGUIWin-win-x64.zip` und `SHA256SUMS.txt` erzeugen (ZIP enthält die EXE direkt im Wurzelverzeichnis).
5. Release `v<Version>` anlegen bzw. aktualisieren und beide Dateien anhängen; zusätzlich
   eine Job-Zusammenfassung mit Größe und SHA256.

Vorab-Versionen wie `v2.0.0-beta.1` werden automatisch als **Pre-Release** markiert
(die Windows-Dateiversion wird dabei auf `2.0.0.0` normalisiert).
Ein erneuter Lauf mit derselben Version aktualisiert das vorhandene Release und ersetzt die Assets.

Der Dateiname bleibt stabil (`PakeGUIWin-win-x64.zip`), damit
`releases/latest/download/PakeGUIWin-win-x64.zip` dauerhaft funktioniert.

---

## 8. Qualitätssicherung

Automatisiert geprüft (in dieser Umgebung ausgeführt):

* **46 Unit-Tests**: Standardwerte, jede Workflow-Eingabe → richtige CLI-Option,
  Plattform-/Zielformat-Prüfung, alle Zusatzoptionen, Quoting, Skripterzeugung,
  Validierung, Dispatch-JSON und gh-Befehl.
* **CLI-Akzeptanztest gegen `pake-cli@latest`**: die erzeugte Argumentliste wurde dem
  echten `pake` übergeben – keine unbekannte Option; der Lauf endet erwartungsgemäß mit
  `EnvironmentError` (Exitcode 4), weil im Testsystem kein Rust installiert ist.
* **PE-Prüfung der EXE**: `PE32+`, Subsystem `GUI`, x86-64, Icon-Gruppe mit 7 Größen,
  Versionsressource (`Pake GUI`, 1.0.0), Manifest vorhanden.
* **Release-Workflow**: `actionlint` ohne Befund; die PowerShell-Schritte (Version ableiten,
  ZIP und SHA256 erzeugen) wurden lokal mit PowerShell 7 ausgeführt – inklusive Tag-, Dispatch-,
  Pre-Release- und Fehlerfall – und die Prüfsumme mit `sha256sum -c` gegengeprüft.
  Der Publish-Aufruf mit `-p:Version`/`-p:FileVersion` erzeugt nachweislich eine EXE mit
  den erwarteten Versionsressourcen. Der Workflow selbst kann nur auf GitHub-Runnern laufen.

**Nicht geprüft:** Die Oberfläche selbst konnte hier nicht gestartet werden – die
Build-Umgebung ist Linux, und WinForms läuft ausschließlich unter Windows (kein Wine
verfügbar). Layout, Dialoge und der erste echte MSI-Build sollten einmal auf einem
Windows-Rechner gegengeprüft werden; die Logik dahinter ist durch die Tests abgedeckt.

---

## 9. Rechtliches

Dieses Programm ist eine inoffizielle Oberfläche und kein Bestandteil von Pake.
[Pake](https://github.com/tw93/Pake) steht unter **GPL-3.0**; mit Pake erzeugte Apps
gehören vollständig dir (Pake Output Exception). Beim Weitergeben eigener Forks von Pake
sollte ein eigener Name verwendet und Pake als Quelle genannt werden.
