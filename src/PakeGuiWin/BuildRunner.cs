using System.Diagnostics;
using System.Text;

namespace PakeGui.Win;

/// <summary>
/// Fuehrt das erzeugte Build-Skript aus und meldet Ausgaben zeilenweise.
/// Der Aufruf laeuft ueber cmd.exe (Windows) bzw. sh, damit auch
/// npx.cmd / pake.cmd zuverlaessig starten und Abbrueche den Kindprozess treffen.
/// </summary>
internal sealed class BuildRunner : IDisposable
{
    private Process? _process;
    private string? _scriptPath;

    public event EventHandler<string>? OutputReceived;
    public event EventHandler<int>? Completed;

    public bool IsRunning => _process is { HasExited: false };

    public void Start(string scriptContent, string title)
    {
        if (IsRunning)
            throw new InvalidOperationException("Es laeuft bereits ein Build.");

        var (shell, extraArgs, extension) = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe", "/c call", ".cmd")
            : ("/bin/sh", string.Empty, ".sh");

        _scriptPath = Path.Combine(Path.GetTempPath(), $"pakegui-{Guid.NewGuid():N}{extension}");
        File.WriteAllText(_scriptPath, scriptContent, new UTF8Encoding(false));

        var info = new ProcessStartInfo
        {
            FileName = shell,
            Arguments = OperatingSystem.IsWindows()
                ? $"/c call \"{_scriptPath}\""
                : $"\"{_scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        if (!OperatingSystem.IsWindows() && extraArgs.Length > 0)
            info.Arguments = extraArgs + " " + info.Arguments;

        OutputReceived?.Invoke(this, $"[PakeGUI] Start: {title}");
        OutputReceived?.Invoke(this, $"[PakeGUI] Skript: {_scriptPath}");

        _process = new Process { StartInfo = info, EnableRaisingEvents = true };
        _process.OutputDataReceived += (_, e) => Emit(e.Data);
        _process.ErrorDataReceived += (_, e) => Emit(e.Data);
        _process.Exited += (_, _) => OnExited();

        if (!_process.Start())
            throw new InvalidOperationException("Der Build-Prozess konnte nicht gestartet werden.");

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    public void Cancel()
    {
        var process = _process;
        if (process is null || process.HasExited)
            return;

        try
        {
            process.Kill(entireProcessTree: true);
            Emit("Abbruch angefordert – Build wurde beendet.");
        }
        catch (Exception ex)
        {
            Emit($"Abbruch fehlgeschlagen: {ex.Message}");
        }
    }

    private void OnExited()
    {
        var process = _process;
        if (process is null)
            return;

        int exitCode;
        try
        {
            // Restliche Ausgaben abwarten, damit das Protokoll vollstaendig ist.
            process.WaitForExit(5000);
            exitCode = process.ExitCode;
        }
        catch (Exception)
        {
            exitCode = -1;
        }

        CleanupScript();
        Completed?.Invoke(this, exitCode);
    }

    private void Emit(string? line)
    {
        if (!string.IsNullOrEmpty(line))
            OutputReceived?.Invoke(this, line);
    }

    private void CleanupScript()
    {
        try
        {
            if (_scriptPath is not null && File.Exists(_scriptPath))
                File.Delete(_scriptPath);
        }
        catch (IOException)
        {
            // Temporäre Datei darf liegen bleiben.
        }
        catch (UnauthorizedAccessException)
        {
        }
        finally
        {
            _scriptPath = null;
        }
    }

    public void Dispose()
    {
        Cancel();
        _process?.Dispose();
        _process = null;
        CleanupScript();
    }
}
