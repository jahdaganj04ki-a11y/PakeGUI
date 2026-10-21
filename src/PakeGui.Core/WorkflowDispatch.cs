using System.Text;
using System.Text.Json;

namespace PakeGui.Core;

/// <summary>
/// Erzeugt aus den Einstellungen die Eingaben des Online-Workflows
/// "Build App With Pake CLI" (workflow_dispatch) – als JSON und als gh-Befehl.
/// </summary>
public static class WorkflowDispatch
{
    public const string Repository = "jahdaganj04ki-a11y/PakeGUIWin";
    public const string WorkflowFile = "pake-cli.yaml";

    /// <summary>Die 14 Workflow-Eingaben in der Reihenfolge des YAML.</summary>
    public static Dictionary<string, string> ToInputs(PakeSettings s)
    {
        ArgumentNullException.ThrowIfNull(s);

        return new Dictionary<string, string>
        {
            ["platform"] = s.Platform,
            ["url"] = s.Url.Trim(),
            ["name"] = s.Name.Trim(),
            ["icon"] = s.Icon.Trim(),
            ["width"] = s.Width.ToString(),
            ["height"] = s.Height.ToString(),
            ["min_width"] = s.MinWidth > 0 ? s.MinWidth.ToString() : string.Empty,
            ["min_height"] = s.MinHeight > 0 ? s.MinHeight.ToString() : string.Empty,
            ["app_version"] = s.AppVersion.Trim(),
            ["fullscreen"] = Bool(s.Fullscreen),
            ["hide_title_bar"] = Bool(s.HideTitleBar),
            ["new_window"] = Bool(s.NewWindow),
            ["multi_arch"] = Bool(s.MultiArch),
            ["targets"] = s.Targets.Trim(),
        };
    }

    /// <summary>Payload fuer die GitHub-API (POST /repos/{owner}/{repo}/actions/workflows/{file}/dispatches).</summary>
    public static string ToDispatchJson(PakeSettings s, string gitRef = "main")
    {
        var payload = new Dictionary<string, object>
        {
            ["ref"] = gitRef,
            ["inputs"] = ToInputs(s),
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
    }

    /// <summary>Fertiger gh-Befehl zum Starten des Workflows.</summary>
    public static string ToGhCommand(PakeSettings s, string gitRef = "main")
    {
        var sb = new StringBuilder();
        sb.Append($"gh workflow run {WorkflowFile} --repo {Repository} --ref {gitRef}");

        foreach (var (key, value) in ToInputs(s))
            sb.Append($" -f {key}={Quote(value)}");

        return sb.ToString();
    }

    private static string Bool(bool value) => value ? "true" : "false";

    private static string Quote(string value)
        => value.Any(char.IsWhiteSpace) || value.Contains('"')
            ? "\"" + value.Replace("\"", "\\\"") + "\""
            : value;
}
