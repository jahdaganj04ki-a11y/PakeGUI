using System.Drawing.Drawing2D;

namespace PakeGui.Win;

/// <summary>
/// Unterstrich-Tabs im GitHub-Look. TabControl selbst lässt sich unter Windows
/// nicht vollständig dunkel zeichnen, deshalb übernimmt dieses Steuerelement den
/// Reiterstreifen und schaltet nur noch fertige Seiten ein/aus.
/// </summary>
internal sealed class DarkTabStrip : Control
{
    private readonly List<string> _titles = [];
    private readonly List<Rectangle> _bounds = [];

    private int _hoverIndex = -1;

    public DarkTabStrip()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);

        Dock = DockStyle.Top;
        Height = 42;
        Font = Theme.Body;
        BackColor = Theme.CanvasDefault;
        ForeColor = Theme.FgDefault;
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    public int SelectedIndex { get; private set; }

    public event EventHandler? SelectedIndexChanged;

    public void SetTitles(IReadOnlyList<string> titles)
    {
        _titles.Clear();
        _titles.AddRange(titles);

        if (SelectedIndex >= _titles.Count)
            SelectedIndex = 0;

        Recalculate();
        Invalidate();
    }

    public void Select(int index, bool raiseEvent = true)
    {
        if (index < 0 || index >= _titles.Count || index == SelectedIndex)
            return;

        SelectedIndex = index;
        Invalidate();

        if (raiseEvent)
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Recalculate()
    {
        _bounds.Clear();

        var x = 10;
        foreach (var title in _titles)
        {
            var textSize = TextRenderer.MeasureText(title, Theme.BodyBold);
            var width = textSize.Width + 28;
            _bounds.Add(new Rectangle(x, 0, width, Height));
            x += width;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.None;
        g.Clear(Theme.CanvasDefault);

        if (_bounds.Count != _titles.Count)
            Recalculate();

        for (var i = 0; i < _titles.Count; i++)
        {
            var bounds = _bounds[i];
            var isSelected = i == SelectedIndex;

            if (i == _hoverIndex && !isSelected)
            {
                using var hover = new SolidBrush(Theme.CanvasSubtle);
                g.FillRectangle(hover, bounds);
            }

            var color = isSelected || i == _hoverIndex ? Theme.FgDefault : Theme.FgMuted;
            TextRenderer.DrawText(
                g,
                _titles[i],
                isSelected ? Theme.BodyBold : Theme.Body,
                bounds,
                color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            if (isSelected)
            {
                using var pen = new Pen(Theme.TabUnderline, 2);
                g.DrawLine(pen, bounds.Left + 8, Height - 4, bounds.Right - 8, Height - 4);
            }
        }

        using var border = new Pen(Theme.BorderDefault);
        g.DrawLine(border, 0, Height - 1, Width, Height - 1);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var index = HitTest(e.X);
        if (index == _hoverIndex)
            return;

        _hoverIndex = index;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);

        if (_hoverIndex == -1)
            return;

        _hoverIndex = -1;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        Focus();
        var index = HitTest(e.X);
        if (index >= 0)
            Select(index);
    }

    protected override bool IsInputKey(Keys keyData)
        => keyData is Keys.Left or Keys.Right || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        switch (e.KeyCode)
        {
            case Keys.Left:
                Select(Math.Max(0, SelectedIndex - 1));
                e.Handled = true;
                break;

            case Keys.Right:
                Select(Math.Min(_titles.Count - 1, SelectedIndex + 1));
                e.Handled = true;
                break;
        }
    }

    private int HitTest(int x)
    {
        for (var i = 0; i < _bounds.Count; i++)
        {
            if (_bounds[i].Contains(x, Height / 2))
                return i;
        }

        return -1;
    }
}
