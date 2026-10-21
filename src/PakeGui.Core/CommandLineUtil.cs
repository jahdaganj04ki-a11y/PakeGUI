using System.Text;

namespace PakeGui.Core;

/// <summary>Zerlegt eine Argumentzeile unter Beachtung von "..." und '...'.</summary>
public static class CommandLineSplitter
{
    public static IReadOnlyList<string> Split(string? commandLine)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(commandLine))
            return result;

        var current = new StringBuilder();
        var hasContent = false;
        var quoteChar = '\0';

        foreach (var c in commandLine)
        {
            if (quoteChar != '\0')
            {
                if (c == quoteChar)
                    quoteChar = '\0';
                else
                    current.Append(c);
                continue;
            }

            if (c is '"' or '\'')
            {
                quoteChar = c;
                hasContent = true;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (hasContent)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    hasContent = false;
                }
                continue;
            }

            current.Append(c);
            hasContent = true;
        }

        if (hasContent)
            result.Add(current.ToString());

        return result;
    }
}

/// <summary>Quotet Argumente fuer cmd.exe bzw. POSIX-Shells.</summary>
public static class CommandLineJoin
{
    private const string CmdSpecials = " &|<>^()%!,;=";

    /// <summary>Fuegt Argumente zu einer Windows-cmd-Befehlszeile zusammen.</summary>
    public static string Join(IEnumerable<string> arguments, bool quoteAll = false)
        => string.Join(" ", arguments.Select(a => QuoteForCmd(a, quoteAll)));

    public static string QuoteForCmd(string argument, bool force = false)
    {
        if (argument.Length == 0)
            return "\"\"";

        // Prozentzeichen wuerde in einer .cmd-Datei expandiert.
        var escaped = argument.Replace("%", "%%");
        escaped = escaped.Replace("\"", "\\\"");

        var needsQuotes = force
            || escaped.Any(c => char.IsWhiteSpace(c) || CmdSpecials.Contains(c));

        return needsQuotes ? $"\"{escaped}\"" : escaped;
    }

    /// <summary>Fuegt Argumente zu einer POSIX-Befehlszeile zusammen (Verifikation auf Linux/macOS).</summary>
    public static string JoinPosix(IEnumerable<string> arguments)
        => string.Join(" ", arguments.Select(QuoteForPosix));

    public static string QuoteForPosix(string argument)
    {
        if (argument.Length == 0)
            return "''";

        if (argument.All(c => char.IsLetterOrDigit(c) || "-_./:=@+,".Contains(c)))
            return argument;

        return "'" + argument.Replace("'", "'\\''") + "'";
    }
}
