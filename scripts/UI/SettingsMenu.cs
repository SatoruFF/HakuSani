using System;
using Godot;

namespace HakuSani;

/// <summary>Volume, mouse, graphics and window settings. Used from the main menu and the pause menu.</summary>
public partial class SettingsMenu : Control
{
    public Action Closed;
    Control _first;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        UiKit.Full(this);
        var dim = new ColorRect { Color = new Color(0.02f, 0.04f, 0.08f, 0.82f) };
        UiKit.Full(dim);
        AddChild(dim);
        var center = new CenterContainer();
        UiKit.Full(center);
        AddChild(center);
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(640, 0) };
        box.AddThemeConstantOverride("separation", 14);
        center.AddChild(box);
        box.AddChild(UiKit.Label("Настройки", 52, UiKit.Cream, true));

        var g = Game.I;
        _first = Slider(box, "Громкость", g.MasterVolume, v => g.MasterVolume = v);
        Slider(box, "Музыка", g.MusicVolume, v => g.MusicVolume = v);
        Slider(box, "Звуки", g.SfxVolume, v => g.SfxVolume = v);
        Slider(box, "Чувствительность мыши", Mathf.InverseLerp(0.001f, 0.008f, g.MouseSensitivity), v => g.MouseSensitivity = Mathf.Lerp(0.001f, 0.008f, v));

        var q = Row(box, "Графика");
        var opt = new OptionButton { CustomMinimumSize = new Vector2(300, 44) };
        opt.AddThemeFontOverride("font", UiKit.Body);
        opt.AddThemeFontSizeOverride("font_size", 22);
        foreach (var s in new[] { "Низкая (быстро)", "Средняя", "Высокая" }) opt.AddItem(s);
        opt.Selected = g.Quality;
        opt.ItemSelected += i => { g.Quality = (int)i; g.ApplySettings(); };
        q.AddChild(opt);

        var f = Row(box, "Полный экран");
        var cb = new CheckButton { ButtonPressed = g.Fullscreen };
        cb.Toggled += on => { g.Fullscreen = on; g.ApplySettings(); };
        f.AddChild(cb);

        box.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });
        box.AddChild(UiKit.Button("Готово", Close));
        Callable.From(() => _first?.GrabFocus()).CallDeferred();
    }

    static HBoxContainer Row(VBoxContainer box, string title)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 20);
        var l = UiKit.Label(title, 24, UiKit.Cream, false, 0);
        l.HorizontalAlignment = HorizontalAlignment.Left;
        l.CustomMinimumSize = new Vector2(320, 0);
        row.AddChild(l);
        box.AddChild(row);
        return row;
    }

    static Control Slider(VBoxContainer box, string title, float value, Action<float> set)
    {
        var row = Row(box, title);
        var s = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.01, Value = value, CustomMinimumSize = new Vector2(300, 32), SizeFlagsVertical = SizeFlags.ShrinkCenter };
        s.ValueChanged += v => { set((float)v); Game.I.ApplySettings(false); };
        s.DragEnded += _ => Game.I.Save();
        row.AddChild(s);
        return s;
    }

    void Close()
    {
        Game.I.Save();
        Closed?.Invoke();
        QueueFree();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("pause"))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }
}
