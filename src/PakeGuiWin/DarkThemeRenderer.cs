namespace PakeGui.Win;

/// <summary>Farbtabelle und Renderer für Menü-, Kontext- und Statusleisten im GitHub-Dark-Look.</summary>
internal sealed class DarkColorTable : ProfessionalColorTable
{
    public override Color MenuStripGradientBegin => Theme.CanvasSubtle;
    public override Color MenuStripGradientEnd => Theme.CanvasSubtle;
    public override Color MenuBorder => Theme.BorderDefault;
    public override Color MenuItemBorder => Theme.BorderDefault;
    public override Color MenuItemSelected => Theme.BtnDefaultHover;
    public override Color MenuItemSelectedGradientBegin => Theme.BtnDefaultHover;
    public override Color MenuItemSelectedGradientEnd => Theme.BtnDefaultHover;
    public override Color MenuItemPressedGradientBegin => Theme.BtnDefault;
    public override Color MenuItemPressedGradientEnd => Theme.BtnDefault;
    public override Color ToolStripDropDownBackground => Theme.CanvasSubtle;
    public override Color ToolStripGradientBegin => Theme.CanvasSubtle;
    public override Color ToolStripGradientMiddle => Theme.CanvasSubtle;
    public override Color ToolStripGradientEnd => Theme.CanvasSubtle;
    public override Color ImageMarginGradientBegin => Theme.CanvasSubtle;
    public override Color ImageMarginGradientMiddle => Theme.CanvasSubtle;
    public override Color ImageMarginGradientEnd => Theme.CanvasSubtle;
    public override Color SeparatorDark => Theme.BorderDefault;
    public override Color SeparatorLight => Theme.BorderDefault;
    public override Color ButtonSelectedHighlight => Theme.BtnDefaultHover;
    public override Color ButtonSelectedBorder => Theme.BorderDefault;
    public override Color ButtonSelectedGradientBegin => Theme.BtnDefaultHover;
    public override Color ButtonSelectedGradientMiddle => Theme.BtnDefaultHover;
    public override Color ButtonSelectedGradientEnd => Theme.BtnDefaultHover;
    public override Color ButtonPressedGradientBegin => Theme.BtnDefault;
    public override Color ButtonPressedGradientMiddle => Theme.BtnDefault;
    public override Color ButtonPressedGradientEnd => Theme.BtnDefault;
    public override Color StatusStripGradientBegin => Theme.CanvasSubtle;
    public override Color StatusStripGradientEnd => Theme.CanvasSubtle;
    public override Color GripDark => Theme.BorderDefault;
    public override Color GripLight => Theme.CanvasSubtle;
    public override Color CheckBackground => Theme.CanvasDefault;
    public override Color CheckSelectedBackground => Theme.BtnDefaultHover;
    public override Color CheckPressedBackground => Theme.BtnDefaultHover;
    public override Color OverflowButtonGradientBegin => Theme.BtnDefault;
    public override Color OverflowButtonGradientMiddle => Theme.BtnDefault;
    public override Color OverflowButtonGradientEnd => Theme.BtnDefault;
}

/// <summary>Zeichnet ToolStrip-Elemente mit heller Schrift auf dunklem Grund.</summary>
internal sealed class DarkToolStripRenderer : ToolStripProfessionalRenderer
{
    public DarkToolStripRenderer() : base(new DarkColorTable())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Theme.FgDefault : Theme.FgMuted;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Theme.CanvasSubtle);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        if (e.ToolStrip is ToolStripDropDown)
        {
            using var pen = new Pen(Theme.BorderDefault);
            var bounds = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            e.Graphics.DrawRectangle(pen, bounds);
        }
        else if (e.ToolStrip is StatusStrip or MenuStrip)
        {
            using var pen = new Pen(Theme.BorderDefault);
            var y = e.ToolStrip is MenuStrip ? e.ToolStrip.Height - 1 : 0;
            e.Graphics.DrawLine(pen, 0, y, e.ToolStrip.Width, y);
        }
    }

    protected override void OnRenderStatusStripSizingGrip(ToolStripRenderEventArgs e)
    {
        // Kein heller Griff im dunklen Theme.
    }
}
