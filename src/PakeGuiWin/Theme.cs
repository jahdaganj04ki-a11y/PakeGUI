using PakeGui.Core;

namespace PakeGui.Win;

/// <summary>
/// Oberflächenfarben nach dem GitHub-Codespace-Theme „GitHub Dark Default“.
/// Die Werte selbst liegen in <see cref="GitHubDarkPalette"/> (getestet),
/// hier kommen Namen und Helfer für die WinForms-Steuerelemente dazu.
/// </summary>
internal static class Theme
{
    // --- Primer-Tokens: GitHub Dark Default -------------------------------
    public static readonly Color CanvasDefault = GitHubDarkPalette.CanvasDefault;
    public static readonly Color CanvasSubtle = GitHubDarkPalette.CanvasSubtle;
    public static readonly Color CanvasInset = GitHubDarkPalette.CanvasInset;

    public static readonly Color BorderDefault = GitHubDarkPalette.BorderDefault;
    public static readonly Color BorderMuted = GitHubDarkPalette.BorderMuted;

    public static readonly Color FgDefault = GitHubDarkPalette.FgDefault;
    public static readonly Color FgMuted = GitHubDarkPalette.FgMuted;
    public static readonly Color FgAccent = GitHubDarkPalette.FgAccent;

    public static readonly Color BtnPrimary = GitHubDarkPalette.BtnPrimary;
    public static readonly Color BtnPrimaryHover = GitHubDarkPalette.BtnPrimaryHover;
    public static readonly Color BtnDefault = GitHubDarkPalette.BtnDefault;
    public static readonly Color BtnDefaultHover = GitHubDarkPalette.BtnDefaultHover;

    public static readonly Color FgSuccess = GitHubDarkPalette.FgSuccess;
    public static readonly Color FgAttention = GitHubDarkPalette.FgAttention;
    public static readonly Color FgDanger = GitHubDarkPalette.FgDanger;
    public static readonly Color TabUnderline = GitHubDarkPalette.TabUnderline;

    // --- Namen, die die Oberfläche verwendet ------------------------------
    public static readonly Color Surface = CanvasDefault;
    public static readonly Color Card = CanvasSubtle;
    public static readonly Color Inset = CanvasInset;
    public static readonly Color Header = CanvasSubtle;
    public static readonly Color HeaderSubtitle = FgMuted;
    public static readonly Color Link = FgAccent;
    public static readonly Color Accent = FgAccent;
    public static readonly Color AccentHover = GitHubDarkPalette.AccentEmphasis;
    public static readonly Color Text = FgDefault;
    public static readonly Color Muted = FgMuted;
    public static readonly Color Border = BorderDefault;
    public static readonly Color LogBackground = CanvasInset;
    public static readonly Color LogText = GitHubDarkPalette.LogText;
    public static readonly Color Warning = FgAttention;
    public static readonly Color Danger = FgDanger;
    public static readonly Color Success = FgSuccess;

    public static readonly Font Body = new("Segoe UI", 9F);
    public static readonly Font BodyBold = new("Segoe UI Semibold", 9F, FontStyle.Bold);
    public static readonly Font Small = new("Segoe UI", 8.25F);
    public static readonly Font Title = new("Segoe UI Semibold", 15F, FontStyle.Bold);
    public static readonly Font Subtitle = new("Segoe UI", 9F);
    public static readonly Font Mono = new("Consolas", 9F);

    // --- Steuerelemente ---------------------------------------------------

    public static Button PrimaryButton(string text, int width = 132)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = BtnPrimary,
            ForeColor = Color.White,
            Font = BodyBold,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = BtnPrimaryHover;
        button.FlatAppearance.MouseDownBackColor = BtnPrimaryHover;
        return button;
    }

    public static Button SecondaryButton(string text, int width = 132)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = BtnDefault,
            ForeColor = FgDefault,
            Font = Body,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
        };
        button.FlatAppearance.BorderColor = BorderDefault;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = BtnDefaultHover;
        button.FlatAppearance.MouseDownBackColor = BtnDefaultHover;
        return button;
    }

    /// <summary>Kleine Schaltfläche "..." rechts neben einem Textfeld.</summary>
    public static Button BrowseButton(string tooltip, int width = 34)
    {
        var button = new Button
        {
            Text = "...",
            Width = width,
            Height = 24,
            FlatStyle = FlatStyle.Flat,
            BackColor = BtnDefault,
            ForeColor = FgDefault,
            Font = Body,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
        };
        button.FlatAppearance.BorderColor = BorderDefault;
        button.FlatAppearance.MouseOverBackColor = BtnDefaultHover;

        var tips = new ToolTip();
        tips.SetToolTip(button, tooltip);
        button.Disposed += (_, _) => tips.Dispose();

        return button;
    }

    public static void StyleTextBox(TextBox box, bool inset = false)
    {
        box.BackColor = inset ? CanvasInset : CanvasDefault;
        box.ForeColor = FgDefault;
        box.BorderStyle = BorderStyle.FixedSingle;
    }

    public static void StyleCombo(ComboBox combo)
    {
        combo.FlatStyle = FlatStyle.Flat;
        combo.BackColor = CanvasDefault;
        combo.ForeColor = FgDefault;
    }

    public static void StyleSpin(NumericUpDown spin)
    {
        spin.BackColor = CanvasDefault;
        spin.ForeColor = FgDefault;
        spin.BorderStyle = BorderStyle.FixedSingle;
    }

    public static void StyleCheck(CheckBox check)
    {
        check.BackColor = Color.Transparent;
        check.ForeColor = FgDefault;
    }

    public static void StyleGroupBox(GroupBox group)
    {
        group.BackColor = CanvasSubtle;
        group.ForeColor = FgDefault;
        group.FlatStyle = FlatStyle.Flat;
    }

    public static void StyleContextMenu(ContextMenuStrip menu)
    {
        menu.BackColor = CanvasSubtle;
        menu.ForeColor = FgDefault;
        menu.Renderer = new DarkToolStripRenderer();
    }
}
