namespace PakeGui.Win;

/// <summary>Kleiner Eingabedialog (z. B. fuer einen eigenen Pake-Befehl).</summary>
internal sealed class PromptForm : Form
{
    private readonly TextBox _input = new();

    private PromptForm(string title, string label, string value)
    {
        Text = title;
        Font = Theme.Body;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(520, 122);
        BackColor = Theme.Surface;

        var caption = new Label
        {
            Text = label,
            AutoSize = true,
            Location = new Point(16, 14),
            ForeColor = Theme.Text,
        };

        _input.Text = value;
        _input.SetBounds(16, 38, 488, 24);
        _input.Font = Theme.Body;
        _input.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        var ok = Theme.PrimaryButton("OK", 90);
        ok.Location = new Point(414, 78);
        ok.DialogResult = DialogResult.OK;

        var cancel = Theme.SecondaryButton("Abbrechen", 100);
        cancel.Location = new Point(304, 78);
        cancel.DialogResult = DialogResult.Cancel;

        Controls.AddRange([caption, _input, ok, cancel]);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    public static string? Ask(IWin32Window? owner, string title, string label, string value)
    {
        using var dialog = new PromptForm(title, label, value);
        return dialog.ShowDialog(owner) == DialogResult.OK ? dialog._input.Text.Trim() : null;
    }
}
