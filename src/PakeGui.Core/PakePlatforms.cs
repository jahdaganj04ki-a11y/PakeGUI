namespace PakeGui.Core;

/// <summary>Plattformwissen rund um die Workflow-Eingabe "platform".</summary>
public static class PakePlatforms
{
    public const string WindowsRunner = "windows-latest";
    public const string MacRunner = "macos-latest";
    public const string LinuxRunner = "ubuntu-24.04";

    public static readonly string[] Runners = [WindowsRunner, MacRunner, LinuxRunner];

    /// <summary>Ordnet beliebige Schreibweisen (z. B. "ubuntu-24.04 (Linux)") einer Runner-Familie zu.</summary>
    public static string Family(string? platform)
    {
        var value = (platform ?? string.Empty).Trim();

        if (value.Contains("linux", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("ubuntu", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("debian", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("fedora", StringComparison.OrdinalIgnoreCase))
        {
            return LinuxRunner;
        }

        if (value.Contains("mac", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("osx", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("darwin", StringComparison.OrdinalIgnoreCase))
        {
            return MacRunner;
        }

        if (value.Contains("win", StringComparison.OrdinalIgnoreCase))
            return WindowsRunner;

        return value;
    }

    /// <summary>Standard-Zielformat je Runner (Windows: x64, Linux: deb, macOS: dmg).</summary>
    public static string DefaultTarget(string runner) => Family(runner) switch
    {
        LinuxRunner => "deb",
        MacRunner => "dmg",
        _ => "x64",
    };

    public static string[] TargetsFor(string runner) => Family(runner) switch
    {
        LinuxRunner => ["deb", "appimage", "rpm", "zst", "deb-arm64", "appimage-arm64", "rpm-arm64", "zst-arm64"],
        MacRunner => ["dmg", "app", "intel", "apple", "universal"],
        _ => ["x64", "arm64"],
    };

    /// <summary>Zielformate, die ein lokaler Build auf diesem Rechner erzeugen kann.</summary>
    public static string[] LocalBuildTargets =>
        OperatingSystem.IsWindows() ? ["x64", "arm64"]
        : OperatingSystem.IsMacOS() ? ["dmg", "app", "intel", "apple", "universal"]
        : ["deb", "appimage", "rpm", "zst", "deb-arm64", "appimage-arm64", "rpm-arm64", "zst-arm64"];

    /// <summary>Menschenlesbarer Name des lokalen Systems.</summary>
    public static string LocalPlatformName =>
        OperatingSystem.IsWindows() ? "windows-latest"
        : OperatingSystem.IsMacOS() ? "macos-latest"
        : "ubuntu-24.04 (Linux)";

    /// <summary>Kurzer Hinweis, ob der gewaehlte Runner lokal ueberhaupt gebaut werden kann.</summary>
    public static string LocalBuildHint(string runner)
    {
        if (Family(runner) == Family(LocalPlatformName))
            return "Lokaler Build moeglich.";

        return Family(runner) switch
        {
            WindowsRunner => "Windows-Pakete (MSI) lassen sich nur auf Windows bauen. Dieser Rechner erzeugt ein anderes Format.",
            MacRunner => "DMG/App-Bundles lassen sich nur auf macOS bauen.",
            _ => "Linux-Pakete (deb/rpm/AppImage) lassen sich nur auf Linux bauen.",
        };
    }
}
