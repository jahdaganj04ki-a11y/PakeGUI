namespace PakeGui.Core;

/// <summary>Eine garantiert gültige Aufteilung für einen <c>SplitContainer</c>.</summary>
public readonly record struct SplitLayoutResult(int Panel1MinSize, int Panel2MinSize, int SplitterDistance);

/// <summary>
/// Berechnet Mindestbreiten und Splitterposition so, dass sie immer zulässig sind.
///
/// Hintergrund: Windows Forms wirft
/// "SplitterDistance must be between Panel1MinSize and Width - Panel2MinSize",
/// sobald eine Mindestbreite größer ist als die aktuelle Breite des Steuerelements.
/// Genau das passiert, wenn man <c>Panel1MinSize</c> im Objekt-Initialisierer setzt –
/// dort ist der Container noch 150 px breit.
/// </summary>
public static class SplitLayout
{
    public const int DefaultPanel1Min = 460;
    public const int DefaultPanel2Min = 360;
    public const double DefaultRatio = 0.58;

    public static SplitLayoutResult Compute(
        int width,
        int splitterWidth,
        int panel1Min = DefaultPanel1Min,
        int panel2Min = DefaultPanel2Min,
        double ratio = DefaultRatio)
    {
        panel1Min = Math.Max(0, panel1Min);
        panel2Min = Math.Max(0, panel2Min);
        splitterWidth = Math.Max(0, splitterWidth);

        if (width <= splitterWidth)
            return new SplitLayoutResult(0, 0, 0);

        var available = width - splitterWidth;

        // Mindestbreiten so kappen, dass beide zusammen hineinpassen.
        var min2 = Math.Min(panel2Min, available);
        var min1 = Math.Min(panel1Min, available - min2);

        var maxDistance = available - min2; // immer >= min1
        var wanted = (int)Math.Round(available * Math.Clamp(ratio, 0d, 1d));
        var distance = Math.Clamp(wanted, min1, maxDistance);

        return new SplitLayoutResult(min1, min2, distance);
    }
}
