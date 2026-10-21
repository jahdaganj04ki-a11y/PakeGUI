using PakeGui.Core;

namespace PakeGui.Win;

/// <summary>
/// Logdatei neben der EXE (Fallback: <c>%LOCALAPPDATA%\PakeGUIWin</c>).
/// Die Schreiblogik liegt in <see cref="AppLogFile"/> und ist dort getestet.
/// </summary>
internal static class AppLog
{
    private static readonly AppLogFile Writer = new(
        AppContext.BaseDirectory,
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PakeGUIWin"));

    public static string LogFilePath => Writer.FilePath;

    public static bool UsedFallback => Writer.UsedFallback;

    public static void Initialize()
        => Writer.Initialize(Application.ProductVersion, Application.ExecutablePath);

    public static void Info(string message) => Writer.Info(message);

    public static void Warn(string message) => Writer.Warn(message);

    public static void Error(string message, Exception? exception = null) => Writer.Error(message, exception);
}
