using PakeGui.Core;
using Xunit;

namespace PakeGui.Core.Tests;

public class AppLogFileTests : IDisposable
{
    private readonly string _root;

    public AppLogFileTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "pakegui-log-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }
        catch (IOException)
        {
            // Aufräumen ist optional.
        }
    }

    [Fact]
    public void Writes_next_to_the_executable_with_header()
    {
        var log = new AppLogFile(_root);

        log.Initialize("1.2.3", "/pfad/PakeGUIWin.exe");
        log.Info("Build gestartet");

        Assert.Equal(_root, log.DirectoryPath);
        Assert.False(log.UsedFallback);
        Assert.True(File.Exists(log.FilePath));

        var text = File.ReadAllText(log.FilePath);
        Assert.Contains("PAKE GUI START", text);
        Assert.Contains("Version: 1.2.3", text);
        Assert.Contains("Build gestartet", text);
        Assert.Matches(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} \[INFO \]", text);
    }

    [Fact]
    public void Falls_back_when_program_directory_is_not_usable()
    {
        // Eine Datei kann nicht als Ordner verwendet werden – unabhängig vom OS.
        var blocked = Path.Combine(_root, "blocked");
        File.WriteAllText(blocked, "kein Ordner");
        var fallback = Path.Combine(_root, "fallback");

        var log = new AppLogFile(blocked, fallback);
        log.Info("Hallo Fallback");

        Assert.True(log.UsedFallback);
        Assert.Equal(fallback, log.DirectoryPath);
        Assert.True(File.Exists(log.FilePath));
        Assert.Contains("Hallo Fallback", File.ReadAllText(log.FilePath));
    }

    [Fact]
    public void Rotates_when_the_file_exceeds_the_limit()
    {
        var log = new AppLogFile(_root, maxBytes: 200);
        log.Info("erste Zeile");
        var path = log.FilePath;

        // Datei künstlich über die Grenze bringen.
        File.AppendAllText(path, new string('x', 400));

        var second = new AppLogFile(_root, maxBytes: 200);
        second.Initialize("1.0.0", null);

        Assert.True(File.Exists(second.BackupFilePath), "Backup fehlt");

        var backup = File.ReadAllText(second.BackupFilePath);
        Assert.Contains("erste Zeile", backup);
        Assert.Contains("xxxx", backup);

        var fresh = File.ReadAllText(second.FilePath);
        Assert.Contains("PAKE GUI START", fresh);
        Assert.DoesNotContain("xxxx", fresh);
        Assert.True(fresh.Contains("Version: 1.0.0"), "Neue Datei enthält den Startblock nicht");
    }

    [Fact]
    public void Writes_from_many_threads_without_losing_lines()
    {
        var log = new AppLogFile(_root);
        log.Info("Start");

        Parallel.For(0, 8, thread =>
        {
            for (var i = 0; i < 50; i++)
                log.Info($"T{thread}-{i}");
        });

        var lines = File.ReadAllLines(log.FilePath).Where(l => l.Contains("] T")).ToList();
        Assert.Equal(400, lines.Count);
    }

    [Fact]
    public void Errors_include_the_stack_trace()
    {
        var log = new AppLogFile(_root);

        try
        {
            throw new InvalidOperationException("Testfehler");
        }
        catch (Exception ex)
        {
            log.Error("Beim Bauen", ex);
        }

        var text = File.ReadAllText(log.FilePath);
        Assert.Contains("[ERROR]", text);
        Assert.Contains("Beim Bauen", text);
        Assert.Contains("InvalidOperationException", text);
        Assert.Contains("Testfehler", text);
    }

    [Fact]
    public void Never_throws_even_with_unwritable_base_and_fallback()
    {
        var blocked = Path.Combine(_root, "blocked2");
        File.WriteAllText(blocked, "kein Ordner");

        var log = new AppLogFile(blocked, blocked);

        var exception = Record.Exception(() =>
        {
            log.Initialize(null, null);
            log.Error("egal", new Exception("x"));
        });

        Assert.Null(exception);
        Assert.True(File.Exists(log.FilePath));
    }
}
