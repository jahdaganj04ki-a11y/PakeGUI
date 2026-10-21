namespace PakeGui.Win;

/// <summary>Farben, Schriften und kleine Control-Fabriken fuer ein einheitliches Aussehen.</summary>
internal static class Theme
{
    public static readonly Color Surface = Color.FromArgb(246, 247, 249);
    public static readonly Color Card = Color.White;
    public static readonly Color Header = Color.FromArgb(23, 37, 84);
    public static readonly Color HeaderAccent = Color.FromArgb(96, 165, 250);
    public static readonly Color Accent = Color.FromArgb(37, 99, 235);
    public static readonly Color AccentHover = Color.FromArgb(29, 78, 216);
    public static readonly Color Text = Color.FromArgb(31, 41, 55);
    public static readonly Color Muted = Color.FromArgb(107, 114, 128);
    public static readonly Color Border = Color.FromArgb(214, 219, 227);
    public static readonly Color LogBackground = Color.FromArgb(15, 23, 42);
    public static readonly Color LogText = Color.FromArgb(209, 213, 219);
    public static readonly Color Warning = Color.FromArgb(180, 83, 9);
    public static readonly Color Danger = Color.FromArgb(185, 28, 28);
    public static readonly Color Success = Color.FromArgb(21, 128, 61);

    public static readonly Font Body = new("Segoe UI", 9F);
    public static readonly Font BodyBold = new("Segoe UI Semibold", 9F, FontStyle.Bold);
    public static readonly Font Small = new("Segoe UI", 8.25F);
    public static readonly Font Title = new("Segoe UI Semibold", 15F, FontStyle.Bold);
    public static readonly Font Subtitle = new("Segoe UI", 9F);
    public static readonly Font Mono = new("Consolas", 9F);

    public static Button PrimaryButton(string text, int width = 132)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = Accent,
            ForeColor = Color.White,
            Font = BodyBold,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = AccentHover;
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
            BackColor = Card,
            ForeColor = Text,
            Font = Body,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
        };
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.BorderSize = 1;
        return button;
    }

    /// <summary>Kleine Schaltflaeche "..." rechts neben einem Textfeld.</summary>
    public static Button BrowseButton(string tooltip, int width = 34)
    {
        var button = new Button
        {
            Text = "...",
            Width = width,
            Height = 24,
            FlatStyle = FlatStyle.Flat,
            BackColor = Card,
            ForeColor = Text,
            Font = Body,
        };
        button.FlatAppearance.BorderColor = Border;
        new ToolTip().SetToolTip(button, tooltip);
        return button;
    }
}
