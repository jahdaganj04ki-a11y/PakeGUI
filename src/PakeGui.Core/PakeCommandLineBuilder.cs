using System.Diagnostics;
using System.Text;

namespace PakeGui.Core;

/// <summary>Fertiger Aufruf der Pake-CLI (Programm + Argumente).</summary>
public sealed class PakeCommandLine
{
    public PakeCommandLine(string executable, IReadOnlyList<string> arguments, IReadOnlyList<string> warnings)
    {
        Executable = executable;
        Arguments = arguments;
        Warnings = warnings;
    }

    public string Executable { get; }
    public IReadOnlyList<string> Arguments { get; }
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>Anzeige-/Kopierform der Befehlszeile (cmd-Quoting).</summary>
    public string Display => CommandLineJoin.QuoteForCmd(Executable) + " " + CommandLineJoin.Join(Arguments);
}

/// <summary>Baut aus den Einstellungen den konkreten CLI-Aufruf.</summary>
public static class PakeCommandLineBuilder
{
    public const string NpxPackage = "pake-cli@latest";

    public static PakeCommandLine Build(PakeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var built = PakeArgumentBuilder.Build(settings);
        var warnings = new List<string>(built.Warnings);

        string executable;
        var prefix = new List<string>();

        switch (settings.RunnerMode)
        {
            case PakeRunnerMode.Custom when !string.IsNullOrWhiteSpace(settings.CustomRunnerPath):
            {
                var parts = CommandLineSplitter.Split(settings.CustomRunnerPath);
                if (parts.Count == 0)
                {
                    executable = "pake";
                    break;
                }

                executable = parts[0];
                prefix.AddRange(parts.Skip(1));
                break;
            }

            case PakeRunnerMode.Global:
                executable = PathSearch.FindOnPath("pake") ?? "pake";
                if (PathSearch.FindOnPath("pake") is null)
                    warnings.Add("pake wurde nicht im PATH gefunden – bitte 'npm install -g pake-cli' ausfuehren.");
                break;

            case PakeRunnerMode.Npx:
                executable = "npx";
                prefix.AddRange(["--yes", NpxPackage]);
                break;

            default:
            {
                var global = PathSearch.FindOnPath("pake");
                if (global is not null)
                {
                    executable = global;
                }
                else
                {
                    executable = "npx";
                    prefix.AddRange(["--yes", NpxPackage]);
                }

                break;
            }
        }

        var args = new List<string>(prefix);
        args.AddRange(built.Arguments);

        return new PakeCommandLine(executable, args, warnings);
    }

    /// <summary>Startskript fuer cmd.exe (Windows) bzw. sh (andere Systeme).</summary>
    public static string CreateScript(PakeCommandLine command, string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sb = new StringBuilder();
        var dir = string.IsNullOrWhiteSpace(workingDirectory) ? Environment.CurrentDirectory : workingDirectory;

        if (OperatingSystem.IsWindows())
        {
            sb.AppendLine("@echo off");
            sb.AppendLine("chcp 65001 >nul");
            sb.AppendLine("title Pake GUI Build");
            sb.AppendLine($"cd /d {CommandLineJoin.QuoteForCmd(dir)}");
            sb.AppendLine($"echo [PakeGUI] {EscapeEcho(command.Display)}");
            sb.AppendLine(CommandLineJoin.QuoteForCmd(command.Executable) + " " + CommandLineJoin.Join(command.Arguments));
        }
        else
        {
            sb.AppendLine("#!/bin/sh");
            sb.AppendLine($"cd {CommandLineJoin.QuoteForPosix(dir)} || exit 1");
            sb.AppendLine("echo \"[PakeGUI] " +
                          CommandLineJoin.JoinPosix([command.Executable, .. command.Arguments])
                              .Replace("'", string.Empty) + "\"");
            sb.AppendLine(CommandLineJoin.JoinPosix([command.Executable, .. command.Arguments]));
        }

        return sb.ToString();
    }

    /// <summary>Maskiert Text fuer "echo" in einer .cmd-Datei (nur Prozent und Zirkumflex).</summary>
    private static string EscapeEcho(string text)
        => text.Replace("%", "%%").Replace("^", "^^");
}

/// <summary>Sucht Programme im PATH (Windows inkl. PATHEXT).</summary>
public static class PathSearch
{
    public static string? FindOnPath(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        // Absolute oder relative Pfadangabe?
        if (name.Contains(Path.DirectorySeparatorChar) || name.Contains('/') || name.Contains('\\'))
            return File.Exists(name) ? name : null;

        var extensions = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD")
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
            : [string.Empty];

        var paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var directory in paths)
        {
            foreach (var extension in extensions)
            {
                try
                {
                    var candidate = Path.Combine(directory.Trim('"'), name + extension.ToLowerInvariant());
                    if (File.Exists(candidate))
                        return candidate;

                    candidate = Path.Combine(directory.Trim('"'), name + extension);
                    if (File.Exists(candidate))
                        return candidate;
                }
                catch (ArgumentException)
                {
                    // Ungueltiger PATH-Eintrag ignorieren.
                }
            }
        }

        return null;
    }
}
