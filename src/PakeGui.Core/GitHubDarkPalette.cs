using System.Drawing;

namespace PakeGui.Core;

/// <summary>
/// Farbtokens des GitHub-Codespace-Themes „GitHub Dark Default“ (Primer,
/// @primer/primitives 11.x). Die WinForms-Oberfläche bezieht ihre Farben
/// ausschließlich von hier, damit die Palette testbar und dokumentiert bleibt.
/// </summary>
public static class GitHubDarkPalette
{
    // Hintergründe
    public const string CanvasDefaultHex = "#0d1117";
    public const string CanvasSubtleHex = "#151b23";
    public const string CanvasInsetHex = "#010409";

    // Rahmen
    public const string BorderDefaultHex = "#3d444d";
    public const string BorderMutedHex = "#30363d";

    // Text
    public const string FgDefaultHex = "#f0f6fc";
    public const string FgMutedHex = "#9198a1";
    public const string FgAccentHex = "#4493f8";

    // Buttons
    public const string BtnPrimaryHex = "#238636";
    public const string BtnPrimaryHoverHex = "#29903b";
    public const string BtnDefaultHex = "#212830";
    public const string BtnDefaultHoverHex = "#262c36";
    public const string AccentEmphasisHex = "#1f6feb";

    // Status
    public const string FgSuccessHex = "#3fb950";
    public const string FgAttentionHex = "#d29922";
    public const string FgDangerHex = "#f85149";

    // Auszeichnung aktiver Reiter
    public const string TabUnderlineHex = "#f78166";

    /// <summary>Klassische GitHub-Textfarbe für Protokollflächen.</summary>
    public const string LogTextHex = "#e6edf3";

    public static readonly Color CanvasDefault = Parse(CanvasDefaultHex);
    public static readonly Color CanvasSubtle = Parse(CanvasSubtleHex);
    public static readonly Color CanvasInset = Parse(CanvasInsetHex);

    public static readonly Color BorderDefault = Parse(BorderDefaultHex);
    public static readonly Color BorderMuted = Parse(BorderMutedHex);

    public static readonly Color FgDefault = Parse(FgDefaultHex);
    public static readonly Color FgMuted = Parse(FgMutedHex);
    public static readonly Color FgAccent = Parse(FgAccentHex);

    public static readonly Color BtnPrimary = Parse(BtnPrimaryHex);
    public static readonly Color BtnPrimaryHover = Parse(BtnPrimaryHoverHex);
    public static readonly Color BtnDefault = Parse(BtnDefaultHex);
    public static readonly Color BtnDefaultHover = Parse(BtnDefaultHoverHex);
    public static readonly Color AccentEmphasis = Parse(AccentEmphasisHex);

    public static readonly Color FgSuccess = Parse(FgSuccessHex);
    public static readonly Color FgAttention = Parse(FgAttentionHex);
    public static readonly Color FgDanger = Parse(FgDangerHex);

    public static readonly Color TabUnderline = Parse(TabUnderlineHex);
    public static readonly Color LogText = Parse(LogTextHex);

    public static Color Parse(string hex)
    {
        var value = hex.TrimStart('#');
        return Color.FromArgb(
            Convert.ToInt32(value[..2], 16),
            Convert.ToInt32(value.Substring(2, 2), 16),
            Convert.ToInt32(value.Substring(4, 2), 16));
    }

    /// <summary>WCAG-Kontrastverhältnis zweier Farben (1:1 bis 21:1).</summary>
    public static double ContrastRatio(Color foreground, Color background)
    {
        var l1 = RelativeLuminance(foreground);
        var l2 = RelativeLuminance(background);
        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(Color color)
        => 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);

    private static double Channel(byte value)
    {
        var c = value / 255d;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
