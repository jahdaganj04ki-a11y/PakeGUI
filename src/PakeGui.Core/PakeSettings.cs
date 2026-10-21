namespace PakeGui.Core;

/// <summary>Wie das Paket erzeugt werden soll.</summary>
public enum PakeRunnerMode
{
    /// <summary>pake im PATH, sonst npx pake-cli@latest.</summary>
    Auto = 0,
    /// <summary>Immer ueber npx (kein globales pake notwendig).</summary>
    Npx = 1,
    /// <summary>Global installiertes pake.</summary>
    Global = 2,
    /// <summary>Eigener Befehl / Pfad aus <see cref="PakeSettings.CustomRunnerPath"/>.</summary>
    Custom = 3,
}

/// <summary>Option fuer --hide-on-close (dreiwertig, damit der Plattform-Standard gilt).</summary>
public enum HideOnCloseMode
{
    Default = 0,
    Yes = 1,
    No = 2,
}

/// <summary>
/// Saemtliche Einstellungen. Die ersten 14 Eigenschaften sind exakt die Eingaben
/// des GitHub-Workflows "Build App With Pake CLI" (pake-cli.yaml), der Rest sind
/// alle weiteren Optionen der Pake-CLI.
/// </summary>
public sealed class PakeSettings
{
    // ------------------------------------------------------------------
    // 1:1 die Eingaben des Online-Workflows (Reihenfolge wie im YAML)
    // ------------------------------------------------------------------

    /// <summary>Workflow-Eingabe "platform": windows-latest | macos-latest | ubuntu-24.04.</summary>
    public string Platform { get; set; } = PakePlatforms.WindowsRunner;

    /// <summary>Workflow-Eingabe "url": Webseite, lokale HTML-Datei oder dist-Ordner.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Workflow-Eingabe "name".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Workflow-Eingabe "icon": URL oder lokale Datei, leer = automatisch.</summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>Workflow-Eingabe "width" (px).</summary>
    public int Width { get; set; } = 1200;

    /// <summary>Workflow-Eingabe "height" (px).</summary>
    public int Height { get; set; } = 780;

    /// <summary>Workflow-Eingabe "min_width" (px), 0 = nicht setzen.</summary>
    public int MinWidth { get; set; }

    /// <summary>Workflow-Eingabe "min_height" (px), 0 = nicht setzen.</summary>
    public int MinHeight { get; set; }

    /// <summary>Workflow-Eingabe "app_version".</summary>
    public string AppVersion { get; set; } = "1.0.0";

    /// <summary>Workflow-Eingabe "fullscreen".</summary>
    public bool Fullscreen { get; set; }

    /// <summary>Workflow-Eingabe "hide_title_bar" (nur macOS wirksam).</summary>
    public bool HideTitleBar { get; set; }

    /// <summary>Workflow-Eingabe "new_window".</summary>
    public bool NewWindow { get; set; }

    /// <summary>Workflow-Eingabe "multi_arch" (nur macOS wirksam).</summary>
    public bool MultiArch { get; set; }

    /// <summary>Workflow-Eingabe "targets": z. B. deb,appimage,rpm,zst bzw. x64/arm64.</summary>
    public string Targets { get; set; } = "x64";

    // ------------------------------------------------------------------
    // Weitere Optionen der Pake-CLI
    // ------------------------------------------------------------------

    public string Identifier { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    /// <summary>--zoom, 0 = Standard (100).</summary>
    public int Zoom { get; set; }

    public bool Maximize { get; set; }
    public bool AlwaysOnTop { get; set; }
    public bool DarkMode { get; set; }
    public bool HideWindowDecorations { get; set; }
    public bool DisabledWebShortcuts { get; set; }
    public string ActivationShortcut { get; set; } = string.Empty;

    public bool ShowSystemTray { get; set; }
    public string SystemTrayIcon { get; set; } = string.Empty;
    public HideOnCloseMode HideOnClose { get; set; } = HideOnCloseMode.Default;
    public bool StartToTray { get; set; }

    public bool NoRestoreLastUrl { get; set; }
    public bool Incognito { get; set; }
    public bool PasswordAutosave { get; set; }
    public bool Wasm { get; set; }
    public bool EnableDragDrop { get; set; }
    public bool MultiInstance { get; set; }
    public bool MultiWindow { get; set; }
    public bool EnableFind { get; set; }

    public bool ForceInternalNavigation { get; set; }
    public string InternalUrlRegex { get; set; } = string.Empty;
    public string SafeDomain { get; set; } = string.Empty;

    public string UserAgent { get; set; } = string.Empty;
    public string ProxyUrl { get; set; } = string.Empty;
    public bool IgnoreCertificateErrors { get; set; }
    public bool BasicAuth { get; set; }

    /// <summary>Kommagetrennte Liste lokaler CSS/JS-Dateien.</summary>
    public string Inject { get; set; } = string.Empty;
    public string DownloadDir { get; set; } = string.Empty;
    public bool UseLocalFile { get; set; }

    public bool Debug { get; set; }
    public bool KeepBinary { get; set; }
    public bool NoBundle { get; set; }
    public bool IterativeBuild { get; set; }
    public bool Install { get; set; }
    public bool Camera { get; set; }
    public bool Microphone { get; set; }

    /// <summary>--windows-toolchain: "", "msvc" oder "gnu".</summary>
    public string WindowsToolchain { get; set; } = string.Empty;

    /// <summary>--installer-language, z. B. de-DE.</summary>
    public string InstallerLanguage { get; set; } = string.Empty;

    /// <summary>Zusaetzliche, frei eingegebene CLI-Argumente.</summary>
    public string ExtraArgs { get; set; } = string.Empty;

    // ------------------------------------------------------------------
    // Nur fuer das Programm selbst (nicht Teil des Pake-Aufrufs)
    // ------------------------------------------------------------------

    /// <summary>Ordner, in dem gebaut wird (Arbeitsverzeichnis = Ablage der Artefakte).</summary>
    public string OutputDirectory { get; set; } = string.Empty;

    public PakeRunnerMode RunnerMode { get; set; } = PakeRunnerMode.Auto;

    /// <summary>Nur bei <see cref="PakeRunnerMode.Custom"/> verwendet.</summary>
    public string CustomRunnerPath { get; set; } = string.Empty;

    public PakeSettings Clone() => (PakeSettings)MemberwiseClone();
}
