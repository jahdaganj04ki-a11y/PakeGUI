namespace PakeGui.Win;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        Application.ThreadException += (_, e) => ShowError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => ShowError(e.ExceptionObject as Exception);

        Application.Run(new MainForm());
    }

    private static void ShowError(Exception? exception)
    {
        MessageBox.Show(
            exception?.ToString() ?? "Unbekannter Fehler.",
            "Pake GUI – unerwarteter Fehler",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
