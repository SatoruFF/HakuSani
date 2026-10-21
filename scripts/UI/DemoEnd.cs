using Godot;

namespace HakuSani;

/// <summary>End card of the demo build: thanks + ask for feedback.</summary>
public partial class DemoEnd : Control
{
    public override void _Ready()
    {
        UiKit.Full(this);
        Audio.I.Music("finale");
        Audio.I.Ambient(0);
        var bg = new ColorRect { Color = UiKit.Cream };
        UiKit.Full(bg);
        AddChild(bg);
        var center = new CenterContainer();
        UiKit.Full(center);
        AddChild(center);
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(900, 0) };
        box.AddThemeConstantOverride("separation", 22);
        center.AddChild(box);

        var logo = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://assets/ui/logo.jpeg"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(0, 360),
        };
        box.AddChild(logo);
        foreach (var (text, size, title) in new[]
        {
            ("Спасибо, что сыграли!", 52, true),
            ("Это конец демо-версии. Дальше Хаку и Сани ждут туман с чужими следами, город, который помнит, и дорога домой.", 24, false),
            ("Нам очень важно ваше мнение: что понравилось, где было непонятно или неудобно, где застряли?", 24, false),
        })
        {
            var l = UiKit.Label(text, size, UiKit.Ink, title, 0);
            l.LabelSettings.ShadowSize = 0;
            box.AddChild(l);
        }
        var menu = UiKit.Button("В главное меню", () => Game.I.ShowMenu());
        box.AddChild(menu);
        menu.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        var t = CreateTween();
        Modulate = new Color(1, 1, 1, 0);
        t.TweenProperty(this, "modulate:a", 1f, 1.2f);
        menu.GrabFocus();
    }
}
