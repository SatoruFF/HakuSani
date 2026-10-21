using Godot;

namespace HakuSani;

public static class UiKit
{
    static Font _body, _title;

    public static Font Body => _body ??= new SystemFont { FontNames = new[] { "Avenir Next", "Helvetica Neue", "Arial", "DejaVu Sans" }, FontWeight = 500 };
    public static Font TitleFont => _title ??= new SystemFont { FontNames = new[] { "Georgia", "Times New Roman", "DejaVu Serif" }, FontWeight = 700 };

    public static readonly Color Ink = new(0.08f, 0.2f, 0.32f);
    public static readonly Color Cream = new(0.95f, 0.89f, 0.82f);

    public static Label Label(string text, int size, Color color, bool title = false, float outline = 6)
    {
        return new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            LabelSettings = new LabelSettings
            {
                Font = title ? TitleFont : Body, FontSize = size, FontColor = color,
                OutlineSize = (int)outline, OutlineColor = new Color(0, 0, 0, 0.65f),
                ShadowColor = new Color(0, 0, 0, 0.35f), ShadowOffset = new Vector2(0, 2), ShadowSize = 4,
            },
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
    }

    public static Button Button(string text, System.Action onPress)
    {
        var b = new Button { Text = text, CustomMinimumSize = new Vector2(380, 58), FocusMode = Control.FocusModeEnum.All };
        b.AddThemeFontOverride("font", Body);
        b.AddThemeFontSizeOverride("font_size", 26);
        var normal = new StyleBoxFlat { BgColor = new Color(Ink, 0.85f), CornerRadiusTopLeft = 14, CornerRadiusTopRight = 14, CornerRadiusBottomLeft = 14, CornerRadiusBottomRight = 14 };
        var hover = (StyleBoxFlat)normal.Duplicate(); hover.BgColor = new Color(0.85f, 0.42f, 0.6f);
        var focus = (StyleBoxFlat)normal.Duplicate(); focus.BgColor = new Color(0.3f, 0.5f, 0.9f);
        b.AddThemeStyleboxOverride("normal", normal);
        b.AddThemeStyleboxOverride("hover", hover);
        b.AddThemeStyleboxOverride("pressed", hover);
        b.AddThemeStyleboxOverride("focus", focus);
        b.AddThemeColorOverride("font_color", Cream);
        b.AddThemeColorOverride("font_hover_color", Colors.White);
        b.Pressed += () => { Audio.Sfx("ui", -10); onPress(); };
        return b;
    }

    public static void Full(Control c)
    {
        c.AnchorLeft = 0; c.AnchorTop = 0; c.AnchorRight = 1; c.AnchorBottom = 1;
        c.OffsetLeft = c.OffsetTop = c.OffsetRight = c.OffsetBottom = 0;
    }
}
