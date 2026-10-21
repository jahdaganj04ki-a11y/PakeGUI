using PakeGui.Core;
using Xunit;

namespace PakeGui.Core.Tests;

/// <summary>
/// Regressionstest für den Startfehler "SplitterDistance must be between
/// Panel1MinSize and Width - Panel2MinSize".
/// </summary>
public class SplitLayoutTests
{
    private const int Splitter = 6;

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(50)]
    [InlineData(150)]
    [InlineData(400)]
    [InlineData(600)]
    [InlineData(826)]
    [InlineData(900)]
    [InlineData(1000)]
    [InlineData(1184)]
    [InlineData(1920)]
    [InlineData(3840)]
    public void Layout_is_always_valid(int width)
    {
        var layout = SplitLayout.Compute(width, Splitter);

        Assert.True(layout.Panel1MinSize >= 0);
        Assert.True(layout.Panel2MinSize >= 0);

        if (width <= Splitter)
        {
            Assert.Equal(new SplitLayoutResult(0, 0, 0), layout);
            return;
        }

        var available = width - Splitter;

        // Genau die Bedingungen, die Windows Forms prüft:
        Assert.True(layout.Panel1MinSize + layout.Panel2MinSize <= available);
        Assert.InRange(layout.SplitterDistance, layout.Panel1MinSize, available - layout.Panel2MinSize);
    }

    [Fact]
    public void Wide_window_keeps_the_default_minimums()
    {
        var layout = SplitLayout.Compute(1000, Splitter);

        Assert.Equal(SplitLayout.DefaultPanel1Min, layout.Panel1MinSize);
        Assert.Equal(SplitLayout.DefaultPanel2Min, layout.Panel2MinSize);
        Assert.InRange(layout.SplitterDistance,
            SplitLayout.DefaultPanel1Min,
            1000 - Splitter - SplitLayout.DefaultPanel2Min);
    }

    [Fact]
    public void Narrow_window_shrinks_the_minimums_instead_of_throwing()
    {
        var layout = SplitLayout.Compute(600, Splitter);

        Assert.True(layout.Panel1MinSize < SplitLayout.DefaultPanel1Min);
        Assert.True(layout.Panel1MinSize + layout.Panel2MinSize <= 600 - Splitter);
        Assert.InRange(layout.SplitterDistance, layout.Panel1MinSize, 600 - Splitter - layout.Panel2MinSize);
    }

    [Fact]
    public void Default_window_width_gives_a_usable_left_panel()
    {
        // ClientSize 1200 minus Rahmen, wie in MainForm gesetzt.
        var layout = SplitLayout.Compute(1184, Splitter);

        Assert.Equal(SplitLayout.DefaultPanel1Min, layout.Panel1MinSize);
        Assert.True(layout.SplitterDistance >= 550, $"Aufteilung war {layout.SplitterDistance}px");
        Assert.True(1184 - Splitter - layout.SplitterDistance >= 360);
    }

    [Fact]
    public void Tiny_widths_never_produce_negative_values()
    {
        var layout = SplitLayout.Compute(4, Splitter);

        Assert.Equal(0, layout.Panel1MinSize);
        Assert.Equal(0, layout.Panel2MinSize);
        Assert.Equal(0, layout.SplitterDistance);
    }

    [Fact]
    public void Distance_grows_with_the_window()
    {
        var narrow = SplitLayout.Compute(1200, Splitter).SplitterDistance;
        var wide = SplitLayout.Compute(2400, Splitter).SplitterDistance;

        Assert.True(wide > narrow);
    }
}
