using Godot;

namespace HakuSani;

public partial class MainMenu : Control
{
    VBoxContainer _main, _chapters;
    Button _modeBtn;

    public override void _Ready()
    {
        UiKit.Full(this);
        Audio.I.Music("menu");
        Audio.I.Focus(0);
        Audio.I.Ambient(0);
        var bg = new ColorRect { Color = UiKit.Cream };
        UiKit.Full(bg);
        AddChild(bg);

        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        UiKit.Full(row);
        row.AddThemeConstantOverride("separation", 80);
        AddChild(row);

        var logo = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://assets/ui/logo.jpeg"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(560, 840),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        row.AddChild(logo);

        var right = new VBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        right.AddThemeConstantOverride("separation", 18);
        row.AddChild(right);
        var t = UiKit.Label(Game.I.IsDemo ? "Time Crossing · демо" : "Time Crossing", 44, UiKit.Ink, true, 0);
        t.LabelSettings.ShadowSize = 0;
        t.AutowrapMode = TextServer.AutowrapMode.Off;
        right.AddChild(t);
        var sub = UiKit.Label("Двое котов. Два времени. Один путь домой.", 22, UiKit.Ink, false, 0);
        sub.LabelSettings.ShadowSize = 0;
        right.AddChild(sub);
        right.AddChild(new Control { CustomMinimumSize = new Vector2(0, 20) });

        _main = new VBoxContainer();
        _main.AddThemeConstantOverride("separation", 14);
        right.AddChild(_main);
        _modeBtn = UiKit.Button("", ToggleMode);
        _main.AddChild(_modeBtn);
        UpdateMode();
        var newGame = UiKit.Button("Новая игра", () => Game.I.NewGame());
        _main.AddChild(newGame);
        if (Game.I.Unlocked > 1 && !Game.I.IsDemo)
            _main.AddChild(UiKit.Button($"Продолжить: {Game.ChapterTitles[Game.I.Unlocked].Split('.')[0]}", () => Game.I.StartChapter(Game.I.Unlocked)));
        _main.AddChild(UiKit.Button("Выбор главы", () => { _main.Visible = false; _chapters.Visible = true; (_chapters.GetChild(0) as Control)?.GrabFocus(); }));
        _main.AddChild(UiKit.Button("Настройки", () => AddChild(new SettingsMenu { Closed = () => newGame.GrabFocus() })));
        _main.AddChild(UiKit.Button("Выход", () => GetTree().Quit()));

        _chapters = new VBoxContainer { Visible = false };
        _chapters.AddThemeConstantOverride("separation", 10);
        right.AddChild(_chapters);
        for (int i = 0; i < Game.ChapterTitles.Length; i++)
        {
            int n = i;
            var b = UiKit.Button(Game.ChapterTitles[i], () => { if (n == 0) Game.I.NewGame(); else Game.I.StartChapter(n); });
            b.Disabled = i > Game.I.Unlocked || (Game.I.IsDemo && i > 1);
            b.CustomMinimumSize = new Vector2(520, 50);
            _chapters.AddChild(b);
        }
        _chapters.AddChild(UiKit.Button("Назад", () => { _chapters.Visible = false; _main.Visible = true; newGame.GrabFocus(); }));

        var help = UiKit.Label("Вдвоём: экран делится пополам (клавиатура WASD + стрелки или два геймпада).\nОдному: Tab / Y на геймпаде — переключиться между котами.", 18, UiKit.Ink, false, 0);
        help.LabelSettings.ShadowSize = 0;
        right.AddChild(help);

        newGame.GrabFocus();
    }

    void ToggleMode()
    {
        Game.I.Mode = Game.I.Mode == PlayMode.Coop ? PlayMode.Solo : PlayMode.Coop;
        UpdateMode();
    }

    void UpdateMode() => _modeBtn.Text = Game.I.Mode == PlayMode.Coop ? "Режим: вдвоём (split-screen)" : "Режим: один игрок";
}
