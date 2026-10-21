using System.Text.RegularExpressions;

namespace PakeGui.Core;

public enum ValidationSeverity
{
    Warning,
    Error,
}

/// <summary>Ein einzelnes Pruefergebnis zu einem Feld.</summary>
public sealed record ValidationIssue(string Field, string Message, ValidationSeverity Severity)
{
    public bool IsError => Severity == ValidationSeverity.Error;
}

/// <summary>Prueft die Einstellungen, bevor pake gestartet wird.</summary>
public static class PakeValidation
{
    private static readonly Regex VersionPattern = new(
        @"^\d+\.\d+\.\d+([-+][0-9A-Za-z.\-+]*)?$",
        RegexOptions.Compiled);

    private static readonly Regex IdentifierPattern = new(
        @"^[A-Za-z][A-Za-z0-9\-.]*[A-Za-z0-9]$",
        RegexOptions.Compiled);

    private static readonly char[] InvalidNameChars =
        ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    public static IReadOnlyList<ValidationIssue> Validate(PakeSettings s)
    {
        ArgumentNullException.ThrowIfNull(s);

        var issues = new List<ValidationIssue>();

        // URL / lokaler Pfad
        var url = s.Url.Trim();
        if (url.Length == 0)
        {
            issues.Add(new("url", "Die Webseite bzw. der lokale Pfad darf nicht leer sein.", ValidationSeverity.Error));
        }
        else if (IsWebAddress(url))
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                issues.Add(new("url", "Bitte eine gueltige http(s)-Adresse angeben.", ValidationSeverity.Error));
            }
        }
        else if (!File.Exists(url) && !Directory.Exists(url))
        {
            issues.Add(new("url",
                "Weder eine http(s)-Adresse noch ein vorhandener lokaler Pfad.",
                ValidationSeverity.Warning));
        }

        // Name
        var name = s.Name.Trim();
        if (name.Length == 0)
        {
            issues.Add(new("name", "Der App-Name ist ein Pflichtfeld des Workflows.", ValidationSeverity.Error));
        }
        else
        {
            var bad = name.Where(c => InvalidNameChars.Contains(c)).Distinct().ToList();
            if (bad.Count > 0)
            {
                issues.Add(new("name",
                    $"Der Name enthaelt fuer Windows-Dateinamen ungueltige Zeichen: {string.Join(" ", bad)}",
                    ValidationSeverity.Error));
            }

            if (name.Any(char.IsControl))
                issues.Add(new("name", "Der Name enthaelt Steuerzeichen.", ValidationSeverity.Error));

            if (PakePlatforms.Family(s.Platform) == PakePlatforms.LinuxRunner
                && (name.Any(char.IsUpper) || name.Contains(' ')))
            {
                issues.Add(new("name",
                    "Fuer Linux wandelt Pake den Namen in Kleinbuchstaben mit Bindestrichen um.",
                    ValidationSeverity.Warning));
            }
        }

        // Fenstergroessen
        if (s.Width is < 1 or > 10000)
            issues.Add(new("width", "Breite muss zwischen 1 und 10000 px liegen.", ValidationSeverity.Error));

        if (s.Height is < 1 or > 10000)
            issues.Add(new("height", "Hoehe muss zwischen 1 und 10000 px liegen.", ValidationSeverity.Error));

        if (s.MinWidth > 0 && s.MinWidth > s.Width)
            issues.Add(new("minWidth", "Mindestbreite ist groesser als die Fensterbreite.", ValidationSeverity.Warning));

        if (s.MinHeight > 0 && s.MinHeight > s.Height)
            issues.Add(new("minHeight", "Mindesthoehe ist groesser als die Fensterhoehe.", ValidationSeverity.Warning));

        // App-Version
        if (string.IsNullOrWhiteSpace(s.AppVersion))
        {
            issues.Add(new("appVersion", "Ohne App-Version verwendet Pake 1.0.0.", ValidationSeverity.Warning));
        }
        else if (!VersionPattern.IsMatch(s.AppVersion.Trim()))
        {
            issues.Add(new("appVersion",
                "Die Version sollte dem Muster 1.0.0 (SemVer) folgen.",
                ValidationSeverity.Error));
        }

        // Identifier
        if (!string.IsNullOrWhiteSpace(s.Identifier) && !IdentifierPattern.IsMatch(s.Identifier.Trim()))
        {
            issues.Add(new("identifier",
                "Identifier: mit Buchstaben beginnen, nur Buchstaben/Ziffern/Bindestrich/Punkt, Ende alphanumerisch.",
                ValidationSeverity.Error));
        }

        // Zoom
        if (s.Zoom is not 0 and (< 50 or > 200))
            issues.Add(new("zoom", "Zoom muss zwischen 50 und 200 liegen (0 = Standard).", ValidationSeverity.Error));

        // Regulaerer Ausdruck
        if (!string.IsNullOrWhiteSpace(s.InternalUrlRegex) && !IsValidRegex(s.InternalUrlRegex.Trim()))
            issues.Add(new("internalUrlRegex", "Der regulaere Ausdruck ist ungueltig.", ValidationSeverity.Error));

        // Download-Verzeichnis
        if (!string.IsNullOrWhiteSpace(s.DownloadDir))
        {
            var dir = s.DownloadDir.Trim();
            var absolute = Path.IsPathRooted(dir) || dir.StartsWith("~", StringComparison.Ordinal);
            if (!absolute)
                issues.Add(new("downloadDir",
                    "Pake verlangt einen absoluten Pfad oder ~/... fuer das Download-Verzeichnis.",
                    ValidationSeverity.Error));
        }

        // Icon
        if (!string.IsNullOrWhiteSpace(s.Icon) && !IsWebAddress(s.Icon.Trim()) && !File.Exists(s.Icon.Trim()))
        {
            issues.Add(new("icon", "Die lokale Icon-Datei existiert nicht.", ValidationSeverity.Warning));
        }

        // Inject-Dateien
        if (!string.IsNullOrWhiteSpace(s.Inject))
        {
            foreach (var file in s.Inject.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!File.Exists(file))
                    issues.Add(new("inject", $"Inject-Datei nicht gefunden: {file}", ValidationSeverity.Warning));
            }
        }

        // System-Tray-Icon
        if (!string.IsNullOrWhiteSpace(s.SystemTrayIcon) && !File.Exists(s.SystemTrayIcon.Trim()))
        {
            issues.Add(new("systemTrayIcon", "Die Icon-Datei fuer den System-Tray existiert nicht.", ValidationSeverity.Warning));
        }

        // Proxy
        if (!string.IsNullOrWhiteSpace(s.ProxyUrl))
        {
            var proxy = s.ProxyUrl.Trim();
            if (!(proxy.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                  || proxy.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                  || proxy.StartsWith("socks5://", StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add(new("proxyUrl", "Proxy bitte als http://, https:// oder socks5:// angeben.", ValidationSeverity.Error));
            }
        }

        // Plattform-Hinweise
        if (PakePlatforms.Family(s.Platform) != PakePlatforms.Family(PakePlatforms.LocalPlatformName))
        {
            issues.Add(new("platform", PakePlatforms.LocalBuildHint(s.Platform), ValidationSeverity.Warning));
        }

        return issues;
    }

    public static bool HasErrors(IEnumerable<ValidationIssue> issues) => issues.Any(i => i.IsError);

    private static bool IsWebAddress(string value)
        => value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
           || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
           || value.StartsWith("file://", StringComparison.OrdinalIgnoreCase);

    private static bool IsValidRegex(string pattern)
    {
        try
        {
            _ = new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(200));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
