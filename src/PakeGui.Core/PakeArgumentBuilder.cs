using System.Globalization;

namespace PakeGui.Core;

/// <summary>Ergebnis der Argument-Erzeugung.</summary>
public sealed class PakeArguments
{
    public PakeArguments(IReadOnlyList<string> arguments, IReadOnlyList<string> warnings)
    {
        Arguments = arguments;
        Warnings = warnings;
    }

    public IReadOnlyList<string> Arguments { get; }

    /// <summary>Hinweise zu Optionen, die auf der gewaehlten Plattform nicht greifen.</summary>
    public IReadOnlyList<string> Warnings { get; }
}

/// <summary>
/// Uebersetzt <see cref="PakeSettings"/> in die Argumentliste von pake.
/// Reihenfolge und Namen folgen der offiziellen CLI (pake --help).
/// </summary>
public static class PakeArgumentBuilder
{
    public static PakeArguments Build(PakeSettings s)
    {
        ArgumentNullException.ThrowIfNull(s);

        var args = new List<string>();
        var warnings = new List<string>();

        // --- Eingaben des Online-Workflows (Reihenfolge wie im YAML) ---
        args.Add(s.Url.Trim());

        if (!string.IsNullOrWhiteSpace(s.Name))
            AddValue(args, "--name", s.Name);

        if (!string.IsNullOrWhiteSpace(s.Identifier))
            AddValue(args, "--identifier", s.Identifier);

        if (!string.IsNullOrWhiteSpace(s.Icon))
            AddValue(args, "--icon", s.Icon);

        if (s.Width > 0)
            AddValue(args, "--width", Num(s.Width));

        if (s.Height > 0)
            AddValue(args, "--height", Num(s.Height));

        if (s.MinWidth > 0)
            AddValue(args, "--min-width", Num(s.MinWidth));

        if (s.MinHeight > 0)
            AddValue(args, "--min-height", Num(s.MinHeight));

        if (!string.IsNullOrWhiteSpace(s.AppVersion))
            AddValue(args, "--app-version", s.AppVersion);

        if (s.Fullscreen)
            args.Add("--fullscreen");

        if (s.HideTitleBar)
        {
            args.Add("--hide-title-bar");
            if (PakePlatforms.Family(s.Platform) != PakePlatforms.MacRunner)
                warnings.Add("--hide-title-bar wirkt nur auf macOS und wird auf der Zielplattform ignoriert.");
        }

        if (s.HideWindowDecorations)
            args.Add("--hide-window-decorations");

        if (s.NewWindow)
            args.Add("--new-window");

        if (s.MultiArch)
        {
            args.Add("--multi-arch");
            if (PakePlatforms.Family(s.Platform) != PakePlatforms.MacRunner)
                warnings.Add("--multi-arch (Universal Binary) wirkt nur auf macOS.");
        }

        if (!string.IsNullOrWhiteSpace(s.Targets))
        {
            var targets = NormalizeTargets(s.Targets);
            var allowed = PakePlatforms.TargetsFor(s.Platform);
            var unknown = targets
                .Where(t => !allowed.Contains(t, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (unknown.Count > 0)
            {
                warnings.Add(
                    $"Zielformat \"{string.Join(",", unknown)}\" ist fuer {s.Platform} unbekannt " +
                    $"(erlaubt: {string.Join(", ", allowed)}) und wird nicht uebergeben.");
                targets = targets
                    .Where(t => allowed.Contains(t, StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }

            if (targets.Count > 0)
                AddValue(args, "--targets", string.Join(",", targets));
        }

        // --- Alle weiteren Pake-CLI-Optionen ---

        if (s.UseLocalFile)
            args.Add("--use-local-file");

        if (!string.IsNullOrWhiteSpace(s.Inject))
            AddValue(args, "--inject", JoinList(s.Inject));

        if (!string.IsNullOrWhiteSpace(s.DownloadDir))
            AddValue(args, "--download-dir", s.DownloadDir);

        if (s.Debug)
            args.Add("--debug");

        if (s.BasicAuth)
            args.Add("--basic-auth");

        if (!string.IsNullOrWhiteSpace(s.ProxyUrl))
            AddValue(args, "--proxy-url", s.ProxyUrl);

        if (!string.IsNullOrWhiteSpace(s.UserAgent))
            AddValue(args, "--user-agent", s.UserAgent);

        if (!string.IsNullOrWhiteSpace(s.WindowsToolchain))
        {
            AddValue(args, "--windows-toolchain", s.WindowsToolchain);
            if (PakePlatforms.Family(s.Platform) != PakePlatforms.WindowsRunner)
                warnings.Add("--windows-toolchain wirkt nur bei Windows-Builds.");
        }

        if (s.AlwaysOnTop)
            args.Add("--always-on-top");

        if (s.Maximize)
            args.Add("--maximize");

        if (s.DarkMode)
            args.Add("--dark-mode");

        if (s.DisabledWebShortcuts)
            args.Add("--disabled-web-shortcuts");

        if (!string.IsNullOrWhiteSpace(s.ActivationShortcut))
            AddValue(args, "--activation-shortcut", s.ActivationShortcut);

        if (s.ShowSystemTray)
            args.Add("--show-system-tray");
        else if (s.StartToTray)
            warnings.Add("--start-to-tray ohne --show-system-tray wird von Pake ignoriert.");

        if (!string.IsNullOrWhiteSpace(s.SystemTrayIcon))
            AddValue(args, "--system-tray-icon", s.SystemTrayIcon);

        if (s.HideOnClose != HideOnCloseMode.Default)
            AddValue(args, "--hide-on-close", s.HideOnClose == HideOnCloseMode.Yes ? "true" : "false");

        if (!string.IsNullOrWhiteSpace(s.Title))
            AddValue(args, "--title", s.Title);

        if (s.NoRestoreLastUrl)
            args.Add("--no-restore-last-url");

        if (s.Incognito)
            args.Add("--incognito");

        if (s.PasswordAutosave)
        {
            args.Add("--password-autosave");
            if (s.Incognito)
                warnings.Add("--password-autosave ist bei --incognito wirkungslos.");
        }

        if (s.Wasm)
            args.Add("--wasm");

        if (s.EnableDragDrop)
            args.Add("--enable-drag-drop");

        if (s.KeepBinary)
            args.Add("--keep-binary");

        if (s.NoBundle)
        {
            args.Add("--no-bundle");
            if (PakePlatforms.Family(s.Platform) != PakePlatforms.LinuxRunner)
                warnings.Add("--no-bundle wird ausserhalb von Linux von Pake ignoriert.");
        }

        if (s.MultiInstance)
            args.Add("--multi-instance");

        if (s.MultiWindow)
            args.Add("--multi-window");

        if (s.StartToTray && s.ShowSystemTray)
            args.Add("--start-to-tray");

        if (s.ForceInternalNavigation)
            args.Add("--force-internal-navigation");

        if (!string.IsNullOrWhiteSpace(s.InternalUrlRegex))
            AddValue(args, "--internal-url-regex", s.InternalUrlRegex);

        if (!string.IsNullOrWhiteSpace(s.SafeDomain))
            AddValue(args, "--safe-domain", s.SafeDomain);

        if (!string.IsNullOrWhiteSpace(s.InternalUrlRegex) && !string.IsNullOrWhiteSpace(s.SafeDomain))
            warnings.Add("--internal-url-regex hat Vorrang vor --safe-domain.");

        if (s.EnableFind)
            args.Add("--enable-find");

        if (!string.IsNullOrWhiteSpace(s.InstallerLanguage))
        {
            AddValue(args, "--installer-language", s.InstallerLanguage);
            if (PakePlatforms.Family(s.Platform) != PakePlatforms.WindowsRunner)
                warnings.Add("--installer-language betrifft nur den Windows-Installer.");
        }

        if (s.Zoom > 0)
            AddValue(args, "--zoom", Num(s.Zoom));

        if (s.IgnoreCertificateErrors)
            args.Add("--ignore-certificate-errors");

        if (s.IterativeBuild)
            args.Add("--iterative-build");

        if (s.Install)
            args.Add("--install");

        if (s.Camera)
            args.Add("--camera");

        if (s.Microphone)
            args.Add("--microphone");

        if (!string.IsNullOrWhiteSpace(s.ExtraArgs))
            args.AddRange(CommandLineSplitter.Split(s.ExtraArgs));

        return new PakeArguments(args, warnings);
    }

    /// <summary>Erzeugt eine lesbare Befehlszeile (nur zur Anzeige/Kontrolle).</summary>
    public static string Describe(PakeArguments arguments, string executable)
        => executable + " " + CommandLineJoin.Join(arguments.Arguments, quoteAll: false);

    private static void AddValue(List<string> args, string name, string value)
    {
        args.Add(name);
        args.Add(value);
    }

    private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static List<string> NormalizeTargets(string targets)
        => targets
            .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    /// <summary>"a.css, b.js" -> ["a.css", "b.js"] (Pake erwartet ein Argument je Option).</summary>
    private static string JoinList(string list)
        => string.Join(",", list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    /// <summary>Fuer Tests/Tooltips: die Argumentliste als einzelne, bereits gequotete Zeile.</summary>
    public static string ToQuotedLine(PakeArguments arguments)
        => CommandLineJoin.Join(arguments.Arguments, quoteAll: false);
}
