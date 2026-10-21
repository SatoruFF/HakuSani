using Godot;

namespace HakuSani;

/// <summary>Controls overview shown the first time a chapter starts (and from the pause menu).</summary>
public partial class ControlsCard : Control
{
    float _t;
    public System.Action Closed;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        UiKit.Full(this);
        var dim = new ColorRect { Color = new Color(0.02f, 0.04f, 0.08f, 0.75f) };
        UiKit.Full(dim);
        AddChild(dim);
        var center = new CenterContainer();
        UiKit.Full(center);
        AddChild(center);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 18);
        center.AddChild(box);
        box.AddChild(UiKit.Label("Управление", 48, UiKit.Cream, true));

        bool coop = Game.I.Mode == PlayMode.Coop;
        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 60);
        box.AddChild(row);
        row.AddChild(Column(coop ? "Хаку (слева)" : "Клавиатура", new Color(1f, 0.55f, 0.75f),
            "WASD — идти", "Shift — бежать", "Пробел — прыжок", "E — действие", "Мышь, Q / R — камера"));
        if (coop)
            row.AddChild(Column("Сани (справа)", new Color(0.45f, 0.7f, 1f),
                "Стрелки — идти", "/ — бежать", "Enter — прыжок", ". — действие", "[ ] — камера"));
        row.AddChild(Column("Геймпад", UiKit.Cream,
            "Стик — идти", "B / RT — бежать", "A — прыжок", "X — действие", "Правый стик — камера"));

        var tips = coop
            ? "Вы в разных временах: что Хаку меняет в прошлом, Сани видит в будущем. Касайтесь свечения, чтобы увидеть друг друга."
            : "Tab (Y на геймпаде) — переключиться между котами. Что Хаку меняет в прошлом, Сани видит в будущем.";
        var tip = UiKit.Label(tips, 22, UiKit.Cream);
        tip.CustomMinimumSize = new Vector2(900, 0);
        box.AddChild(tip);
        box.AddChild(UiKit.Label("Нажмите любую кнопку, чтобы продолжить", 20, new Color(1, 1, 1, 0.6f)));
        foreach (var c in box.GetChildren())
            if (c is Label l && l != tip) l.AutowrapMode = TextServer.AutowrapMode.Off;
    }

    static VBoxContainer Column(string title, Color c, params string[] lines)
    {
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 6);
        var t = UiKit.Label(title, 28, c);
        t.AutowrapMode = TextServer.AutowrapMode.Off;
        t.HorizontalAlignment = HorizontalAlignment.Left;
        col.AddChild(t);
        foreach (var l in lines)
        {
            var x = UiKit.Label(l, 22, Colors.White, false, 4);
            x.HorizontalAlignment = HorizontalAlignment.Left;
            x.AutowrapMode = TextServer.AutowrapMode.Off;
            col.AddChild(x);
        }
        return col;
    }

    public override void _Process(double delta) => _t += (float)delta;

    public override void _Input(InputEvent e)
    {
        if (_t < 0.8f) return;
        bool press = e is InputEventKey { Pressed: true } || e is InputEventJoypadButton { Pressed: true } || e is InputEventMouseButton { Pressed: true };
        if (!press) return;
        GetViewport().SetInputAsHandled();
        Closed?.Invoke();
        QueueFree();
    }
}
