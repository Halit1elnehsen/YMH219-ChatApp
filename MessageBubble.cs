using System.Drawing.Drawing2D;

namespace YMH219_ChatApp;

internal sealed class MessageBubble : Control
{
    private readonly ChatMessage _message;
    private readonly bool _isOwn;
    private readonly Font _senderFont = new("Segoe UI", 8.5f, FontStyle.Bold);
    private readonly Font _bodyFont = new("Segoe UI", 10.5f);

    public MessageBubble(ChatMessage message, bool isOwn)
    {
        _message = message;
        _isOwn = isOwn;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        ForeColor = Color.FromArgb(37, 62, 66);
        AccessibleRole = AccessibleRole.StaticText;
        AccessibleName = $"{(isOwn ? "Sen" : message.User)}: {message.Text}";
        AccessibleDescription = message.Timestamp.ToString("HH:mm:ss");
        UpdateHeight();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateHeight();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var maxWidth = Math.Max(170, (int)(ClientSize.Width * .76f));
        var textWidth = Math.Max(110, maxWidth - 32);
        var body = TextRenderer.MeasureText(_message.Text, _bodyFont, new Size(textWidth, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        var bubbleWidth = Math.Min(maxWidth, Math.Max(184, body.Width + 32));
        var bubbleHeight = body.Height + 47;
        var left = _isOwn ? ClientSize.Width - bubbleWidth - 8 : 8;
        var bounds = new Rectangle(Math.Max(4, left), 2, bubbleWidth, Math.Min(ClientSize.Height - 4, bubbleHeight));
        var fillColor = _isOwn ? Color.FromArgb(211, 237, 229) : Color.FromArgb(255, 255, 255);
        var borderColor = _isOwn ? Color.FromArgb(172, 211, 199) : Color.FromArgb(222, 231, 227);
        using var shadow = new SolidBrush(Color.FromArgb(18, 31, 69, 68));
        using var shadowPath = Rounded(new Rectangle(bounds.X, bounds.Y + 2, bounds.Width, bounds.Height), 13);
        g.FillPath(shadow, shadowPath);
        using var fill = new SolidBrush(fillColor);
        using var path = Rounded(bounds, 13);
        g.FillPath(fill, path);
        using var border = new Pen(borderColor);
        g.DrawPath(border, path);

        var name = _isOwn ? "SEN" : _message.User;
        var nameColor = _isOwn ? Color.FromArgb(35, 111, 94) : Color.FromArgb(46, 99, 112);
        var contentRect = new Rectangle(bounds.X + 15, bounds.Y + 9, bounds.Width - 30, bounds.Height - 17);
        TextRenderer.DrawText(g, name, _senderFont, contentRect, nameColor, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        var timeRect = new Rectangle(contentRect.X, contentRect.Y, contentRect.Width, 18);
        TextRenderer.DrawText(g, _message.Timestamp.ToString("HH:mm"), new Font("Segoe UI", 8), timeRect, Color.FromArgb(103, 127, 126), TextFormatFlags.Right | TextFormatFlags.Top | TextFormatFlags.SingleLine);
        var bodyRect = new Rectangle(contentRect.X, contentRect.Y + 22, contentRect.Width, contentRect.Height - 22);
        TextRenderer.DrawText(g, _message.Text, _bodyFont, bodyRect, Color.FromArgb(38, 59, 64), TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
    }

    private void UpdateHeight()
    {
        if (Width <= 0) return;
        var maxWidth = Math.Max(170, (int)(Width * .76f));
        var body = TextRenderer.MeasureText(_message.Text, _bodyFont, new Size(Math.Max(100, maxWidth - 32), int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        Height = Math.Max(80, body.Height + 59);
        Invalidate();
    }

    private static GraphicsPath Rounded(Rectangle rect, int radius)
    {
        var r = Math.Clamp(radius, 1, Math.Min(rect.Width, rect.Height) / 2);
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, r * 2, r * 2, 180, 90);
        path.AddArc(rect.Right - r * 2, rect.Y, r * 2, r * 2, 270, 90);
        path.AddArc(rect.Right - r * 2, rect.Bottom - r * 2, r * 2, r * 2, 0, 90);
        path.AddArc(rect.X, rect.Bottom - r * 2, r * 2, r * 2, 90, 90);
        path.CloseFigure();
        return path;
    }
}
