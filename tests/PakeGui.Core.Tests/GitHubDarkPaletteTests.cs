using System.Drawing;
using PakeGui.Core;
using Xunit;

namespace PakeGui.Core.Tests;

/// <summary>
/// Die Oberfläche soll exakt das Codespace-Theme „GitHub Dark Default“ verwenden.
/// Diese Tests halten die Primer-Tokenwerte fest.
/// </summary>
public class GitHubDarkPaletteTests
{
    [Theory]
    [InlineData("#0d1117", "0D1117")]
    [InlineData("#151b23", "151B23")]
    [InlineData("#010409", "010409")]
    [InlineData("#3d444d", "3D444D")]
    [InlineData("#f0f6fc", "F0F6FC")]
    [InlineData("#9198a1", "9198A1")]
    [InlineData("#4493f8", "4493F8")]
    [InlineData("#238636", "238636")]
    [InlineData("#29903b", "29903B")]
    [InlineData("#212830", "212830")]
    [InlineData("#262c36", "262C36")]
    [InlineData("#3fb950", "3FB950")]
    [InlineData("#d29922", "D29922")]
    [InlineData("#f85149", "F85149")]
    [InlineData("#f78166", "F78166")]
    public void Tokens_match_the_github_values(string hex, string expectedRgb)
    {
        var argb = GitHubDarkPalette.Parse(hex).ToArgb();
        Assert.Equal(255, (argb >> 24) & 0xFF);
        Assert.Equal(expectedRgb, (argb & 0xFFFFFF).ToString("X6"));
    }

    [Fact]
    public void Concrete_tokens_are_wired_to_the_expected_hex_values()
    {
        Assert.Equal(GitHubDarkPalette.Parse("#0d1117"), GitHubDarkPalette.CanvasDefault);
        Assert.Equal(GitHubDarkPalette.Parse("#151b23"), GitHubDarkPalette.CanvasSubtle);
        Assert.Equal(GitHubDarkPalette.Parse("#010409"), GitHubDarkPalette.CanvasInset);
        Assert.Equal(GitHubDarkPalette.Parse("#3d444d"), GitHubDarkPalette.BorderDefault);
        Assert.Equal(GitHubDarkPalette.Parse("#f0f6fc"), GitHubDarkPalette.FgDefault);
        Assert.Equal(GitHubDarkPalette.Parse("#9198a1"), GitHubDarkPalette.FgMuted);
        Assert.Equal(GitHubDarkPalette.Parse("#4493f8"), GitHubDarkPalette.FgAccent);
        Assert.Equal(GitHubDarkPalette.Parse("#238636"), GitHubDarkPalette.BtnPrimary);
        Assert.Equal(GitHubDarkPalette.Parse("#f78166"), GitHubDarkPalette.TabUnderline);
    }

    [Fact]
    public void Text_has_strong_contrast_on_the_canvas()
    {
        var normal = GitHubDarkPalette.ContrastRatio(GitHubDarkPalette.FgDefault, GitHubDarkPalette.CanvasDefault);
        var muted = GitHubDarkPalette.ContrastRatio(GitHubDarkPalette.FgMuted, GitHubDarkPalette.CanvasDefault);
        var accent = GitHubDarkPalette.ContrastRatio(GitHubDarkPalette.FgAccent, GitHubDarkPalette.CanvasDefault);

        Assert.True(normal >= 7, $"Textkontrast zu niedrig: {normal:0.0}:1");
        Assert.True(muted >= 4.5, $"Gedämpfter Text zu blass: {muted:0.0}:1");
        Assert.True(accent >= 4.5, $"Akzenttext zu blass: {accent:0.0}:1");
    }

    [Fact]
    public void White_label_on_the_primary_button_is_readable()
    {
        var ratio = GitHubDarkPalette.ContrastRatio(Color.White, GitHubDarkPalette.BtnPrimary);
        Assert.True(ratio >= 4.5, $"Weiß auf Primärbutton nur {ratio:0.0}:1");
    }

    [Fact]
    public void Status_colors_are_distinguishable_from_the_canvas()
    {
        foreach (var color in new[]
                 {
                     GitHubDarkPalette.FgSuccess,
                     GitHubDarkPalette.FgAttention,
                     GitHubDarkPalette.FgDanger,
                 })
        {
            var ratio = GitHubDarkPalette.ContrastRatio(color, GitHubDarkPalette.CanvasDefault);
            Assert.True(ratio >= 3, $"Statusfarbe {color} zu dunkel: {ratio:0.0}:1");
        }
    }
}
