using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace PakeGui.Win;

/// <summary>
/// Einfache Logdatei neben der EXE. Schreibt Zeitstempel, Level und Thread-ID,
/// rotiert bei 5 MB nach <c>PakeGUIWin.1.log</c> und wirft nie – Logging darf
/// das Programm nicht zum Absturz bringen.
///
/// Ist der Programmordner nicht beschreibbar (z. B. unter Programme),
/// landet die Datei in <c>%LOCALAPPDATA%\PakeGUIWin\</c>.
/// </summary>
internal static class AppLog
{
    private const string FileName = "PakeGUIWin.log";
    private const long MaxBytes = 5 * 1024 * 1024;

    private static readonly object Gate = new();

    private static bool _initialized;
    private static string _directory = string.Empty;
    private static string _path = string.Empty;
    private static bool _usedFallback;

    public static string LogFilePath
    {
        get
        {
            EnsureInitialized();
            return _path;
        }
    }

    /// <summary>Schreibt den Startblock (Version, Pfade, System).</summary>
    public static void Initialize()
    {
        EnsureInitialized();

        Info("================ PAKE GUI START ================");
        Info($"Version: {Application.ProductVersion}");
        Info($"EXE: {Application.ExecutablePath}");
        Info($"Arbeitsverzeichnis: {Environment.CurrentDirectory}");
        Info($"System: {RuntimeInformation.OSDescription} / {RuntimeInformation.ProcessArchitecture}");
        Info($"Framework: {RuntimeInformation.FrameworkDescription}");
        Info($"Logdatei: {_path}{(_usedFallback ? " (Fallback, Programmordner nicht beschreibbar)" : string.Empty)}");

        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
            Info("Argumente: " + string.Join(" ", args.Skip(1)));
    }

    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message, Exception? exception = null)
        => Write("ERROR", exception is null ? message : message + Environment.NewLine + exception);

    private static void EnsureInitialized()
    {
        if (_initialized)
            return;

        lock (Gate)
        {
            if (_initialized)
                return;

            _directory = DetermineDirectory();
            _path = Path.Combine(_directory, FileName);
            _initialized = true;

            Rotate();
        }
    }

    private static string DetermineDirectory()
    {
        // AppContext.BaseDirectory ist bei Single-File-Apps der Ordner der EXE.
        var exeDirectory = AppContext.BaseDirectory;

        try
        {
            var probe = Path.Combine(exeDirectory, $".pakegui-write-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            _usedFallback = false;
            return exeDirectory;
        }
        catch (Exception)
        {
            var fallback = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PakeGUIWin");

            Directory.CreateDirectory(fallback);
            _usedFallback = true;
            return fallback;
        }
    }

    private static void Rotate()
    {
        try
        {
            var info = new FileInfo(_path);
            if (!info.Exists || info.Length < MaxBytes)
                return;

            var backup = Path.Combine(_directory, "PakeGUIWin.1.log");
            File.Delete(backup);
            File.Move(_path, backup);
        }
        catch (Exception)
        {
            // Rotation ist optional.
        }
    }

    private static void Write(string level, string message)
    {
        try
        {
            EnsureInitialized();

            var line = string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level,-5}] [T{Environment.CurrentManagedThreadId:D2}] {message}{Environment.NewLine}");

            lock (Gate)
            {
                File.AppendAllText(_path, line, new UTF8Encoding(false));
            }
        }
        catch (Exception)
        {
            // Logging darf nie eine Ausnahme nach außen tragen.
        }
    }
}
