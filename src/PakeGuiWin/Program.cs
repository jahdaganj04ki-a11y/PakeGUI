namespace PakeGui.Win;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Zuerst das Log öffnen: so landet auch ein Startfehler in der Datei.
        AppLog.Initialize();

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Report("UI-Thread", e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Report("Hintergrund-Thread", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Report("Task", e.Exception);
            e.SetObserved();
        };

        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
            AppLog.Info("Programm regulär beendet.");
        }
        catch (Exception ex)
        {
            Report("Start", ex);
        }
    }

    private static void Report(string context, Exception? exception)
    {
        AppLog.Error($"{context}: {exception?.GetType().Name}: {exception?.Message}", exception);

        MessageBox.Show(
            (exception?.Message ?? "Unbekannter Fehler.") +
            Environment.NewLine + Environment.NewLine +
            "Details (inklusive Aufrufliste) stehen in der Logdatei:" + Environment.NewLine +
            AppLog.LogFilePath,
            "Pake GUI – unerwarteter Fehler",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
