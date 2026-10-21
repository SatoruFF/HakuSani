using Godot;

namespace HakuSani;

public partial class PauseMenu : Control
{
    public System.Action Restart;
    VBoxContainer _box;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.WhenPaused;
        UiKit.Full(this);
        var dim = new ColorRect { Color = new Color(0.02f, 0.04f, 0.08f, 0.7f) };
        UiKit.Full(dim);
        AddChild(dim);
        var center = new CenterContainer();
        UiKit.Full(center);
        AddChild(center);
        _box = new VBoxContainer();
        _box.AddThemeConstantOverride("separation", 16);
        center.AddChild(_box);
        _box.AddChild(UiKit.Label("Пауза", 56, UiKit.Cream, true));
        _box.AddChild(UiKit.Button("Продолжить", Close));
        _box.AddChild(UiKit.Button("Начать главу заново", () => { GetTree().Paused = false; Restart?.Invoke(); }));
        _box.AddChild(UiKit.Button("Настройки", () => AddChild(new SettingsMenu { Closed = () => (_box.GetChild(1) as Control)?.GrabFocus() })));
        _box.AddChild(UiKit.Button("Управление", () => AddChild(new ControlsCard { Closed = () => (_box.GetChild(1) as Control)?.GrabFocus() })));
        _box.AddChild(UiKit.Button("Главное меню", () => Game.I.ShowMenu()));
        Visible = false;
    }

    public void Open()
    {
        Visible = true;
        GetTree().Paused = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        (_box.GetChild(1) as Control)?.GrabFocus();
    }

    public void Close()
    {
        Visible = false;
        GetTree().Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (Visible && e.IsActionPressed("pause"))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }
}
