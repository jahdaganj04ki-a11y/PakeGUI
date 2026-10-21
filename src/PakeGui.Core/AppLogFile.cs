using System.Globalization;
using System.Text;

namespace PakeGui.Core;

/// <summary>
/// Schreibt eine Logdatei in den Programmordner (Fallback: Benutzerprofil/Temp),
/// rotiert bei Überschreiten der Maximalgröße und wirft niemals – Logging darf
/// ein Programm nicht zum Absturz bringen, auch nicht beim Start.
/// </summary>
public sealed class AppLogFile
{
    public const string DefaultFileName = "PakeGUIWin.log";
    public const long DefaultMaxBytes = 5 * 1024 * 1024;

    private readonly object _gate = new();
    private readonly string _baseDirectory;
    private readonly string _fallbackDirectory;
    private readonly long _maxBytes;

    private bool _initialized;

    public AppLogFile(
        string baseDirectory,
        string? fallbackDirectory = null,
        string fileName = DefaultFileName,
        long maxBytes = DefaultMaxBytes)
    {
        _baseDirectory = baseDirectory ?? string.Empty;
        _fallbackDirectory = string.IsNullOrWhiteSpace(fallbackDirectory)
            ? Path.Combine(Path.GetTempPath(), "PakeGUIWin")
            : fallbackDirectory;
        FileName = string.IsNullOrWhiteSpace(fileName) ? DefaultFileName : fileName;
        _maxBytes = maxBytes > 0 ? maxBytes : DefaultMaxBytes;
    }

    public string FileName { get; }

    /// <summary>Ordner, in dem tatsächlich geschrieben wird.</summary>
    public string DirectoryPath { get; private set; } = string.Empty;

    public string FilePath
    {
        get
        {
            EnsureInitialized();
            return Path.Combine(DirectoryPath, FileName);
        }
    }

    /// <summary>True, wenn der Programmordner nicht beschreibbar war.</summary>
    public bool UsedFallback { get; private set; }

    public string BackupFilePath =>
        Path.Combine(DirectoryPath, Path.GetFileNameWithoutExtension(FileName) + ".1" + Path.GetExtension(FileName));

    /// <summary>Legt die Datei an (inkl. Rotation) und schreibt den Startblock.</summary>
    public void Initialize(string? version, string? executablePath)
    {
        EnsureInitialized();

        Info("================ PAKE GUI START ================");
        if (!string.IsNullOrWhiteSpace(version))
            Info("Version: " + version);
        if (!string.IsNullOrWhiteSpace(executablePath))
            Info("EXE: " + executablePath);
        Info("Arbeitsverzeichnis: " + Environment.CurrentDirectory);
        Info("System: " + Environment.OSVersion + " / " + Environment.ProcessorCount + " Kerne");
        Info("Logdatei: " + FilePath + (UsedFallback ? " (Fallback: Programmordner nicht beschreibbar)" : string.Empty));
    }

    public void Info(string message) => Write("INFO", message);

    public void Warn(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null)
        => Write("ERROR", exception is null ? message : message + Environment.NewLine + exception);

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        lock (_gate)
        {
            if (_initialized)
                return;

            DirectoryPath = ChooseDirectory();
            UsedFallback = !PathsEqual(DirectoryPath, _baseDirectory);
            _initialized = true;

            Rotate();
        }
    }

    private string ChooseDirectory()
    {
        foreach (var candidate in new[] { _baseDirectory, _fallbackDirectory, Path.GetTempPath() })
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            try
            {
                Directory.CreateDirectory(candidate);

                if (CanWrite(candidate))
                    return candidate;
            }
            catch (Exception)
            {
                // Nächsten Kandidaten versuchen.
            }
        }

        // Letzte Reserve: aktuelles Verzeichnis (schlägt notfalls beim Schreiben fehl).
        return Path.GetTempPath();
    }

    private static bool CanWrite(string directory)
    {
        try
        {
            var probe = Path.Combine(directory, $".pakegui-probe-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void Rotate()
    {
        try
        {
            var info = new FileInfo(FilePath);
            if (!info.Exists || info.Length < _maxBytes)
                return;

            var backup = BackupFilePath;
            File.Delete(backup);
            File.Move(FilePath, backup);
        }
        catch (Exception)
        {
            // Rotation ist optional.
        }
    }

    private void Write(string level, string message)
    {
        try
        {
            EnsureInitialized();

            var line = string.Create(
                CultureInfo.InvariantCulture,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level,-5}] [T{Environment.CurrentManagedThreadId:D2}] {message}{Environment.NewLine}");

            lock (_gate)
            {
                File.AppendAllText(FilePath, line, new UTF8Encoding(false));
            }
        }
        catch (Exception)
        {
            // Niemals nach außen tragen.
        }
    }

    private static bool PathsEqual(string? left, string? right)
        => string.Equals(
            (left ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar),
            (right ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}
