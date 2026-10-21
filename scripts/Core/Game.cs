using Godot;

namespace HakuSani;

public enum PlayMode { Solo, Coop }

/// <summary>Autoload: global state, save data and scene flow.</summary>
public partial class Game : Node
{
    public static Game I { get; private set; }

    public PlayMode Mode = PlayMode.Coop;
    public int Unlocked = 1;          // highest chapter reachable from the menu
    public float MouseSensitivity = 0.0035f;

    // ---- settings (persisted) ----
    public float MasterVolume = 0.9f, MusicVolume = 0.7f, SfxVolume = 0.85f;
    public int Quality = 1;            // 0 low, 1 medium, 2 high
    public bool Fullscreen;
    public bool SeenControls;
    public event System.Action SettingsChanged;

    /// <summary>Demo build: prologue + chapter I only (export feature "demo" or --demo).</summary>
    public bool IsDemo => OS.HasFeature("demo") || _demoArg;
    bool _demoArg;

    public static readonly string[] ChapterTitles =
    {
        "Пролог. Обычный вечер",
        "Глава I. Чужой двор",
        "Глава II. Следы, которых ещё нет",
        "Глава III. Город, который помнит",
        "Глава IV. На разных сторонах времени",
        "Финал. Дом",
    };

    const string SavePath = "user://save.cfg";
    Node _current;

    public override void _EnterTree()
    {
        I = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Ready()
    {
        AddChild(new Audio());
        Inputs.Setup();
        Input.JoyConnectionChanged += (_, _) => Inputs.Setup();
        Load();
        DebugArgs();
    }

    /// <summary>Dev helpers: --chapter=N --mode=solo|coop --shot=path.png --shot-time=sec --script=name</summary>
    public string DebugScript, ShotDir;

    public async void Shot(string name)
    {
        if (ShotDir == null) return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        GetViewport().GetTexture().GetImage().SavePng($"{ShotDir}/{name}.png");
        GD.Print("SHOT ", name, " fps=", Engine.GetFramesPerSecond());
    }
    async void DebugArgs()
    {
        string shot = null; float shotTime = 6; int chapter = -1;
        foreach (var a in OS.GetCmdlineUserArgs())
        {
            var kv = a.TrimStart('-').Split('=', 2);
            var v = kv.Length > 1 ? kv[1] : "";
            switch (kv[0])
            {
                case "chapter": chapter = int.Parse(v); break;
                case "mode": Mode = v == "solo" ? PlayMode.Solo : PlayMode.Coop; break;
                case "shot": shot = v; break;
                case "shot-time": shotTime = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); break;
                case "script": DebugScript = v; break;
                case "shotdir": ShotDir = v; break;
                case "demo": _demoArg = true; break;
                case "timescale": Engine.TimeScale = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); break;
            }
        }
        if (chapter >= 0)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            StartChapter(chapter);
        }
        if (shot != null)
        {
            await ToSignal(GetTree().CreateTimer(shotTime), SceneTreeTimer.SignalName.Timeout);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            GetViewport().GetTexture().GetImage().SavePng(shot);
            GetTree().Quit();
        }
    }

    void Load()
    {
        var cfg = new ConfigFile();
        if (cfg.Load(SavePath) == Error.Ok)
        {
            Unlocked = (int)cfg.GetValue("progress", "unlocked", 1);
            MasterVolume = (float)cfg.GetValue("settings", "master", MasterVolume);
            MusicVolume = (float)cfg.GetValue("settings", "music", MusicVolume);
            SfxVolume = (float)cfg.GetValue("settings", "sfx", SfxVolume);
            MouseSensitivity = (float)cfg.GetValue("settings", "mouse", MouseSensitivity);
            Quality = (int)cfg.GetValue("settings", "quality", Quality);
            Fullscreen = (bool)cfg.GetValue("settings", "fullscreen", Fullscreen);
            SeenControls = (bool)cfg.GetValue("progress", "seen_controls", false);
        }
        ApplySettings(false);
    }

    /// <summary>Push settings to audio buses / window / listeners, optionally persist.</summary>
    public void ApplySettings(bool save = true)
    {
        Audio.SetBusVolume("Master", MasterVolume);
        Audio.SetBusVolume("Music", MusicVolume);
        Audio.SetBusVolume("SFX", SfxVolume);
        var mode = Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed;
        if (DisplayServer.WindowGetMode() != mode && !OS.HasFeature("headless")) DisplayServer.WindowSetMode(mode);
        SettingsChanged?.Invoke();
        if (save) Save();
    }

    public float RenderScale => Quality switch { 0 => 0.6f, 1 => 0.77f, _ => 1.0f };

    public void Save()
    {
        var cfg = new ConfigFile();
        cfg.SetValue("progress", "unlocked", Unlocked);
        cfg.SetValue("progress", "seen_controls", SeenControls);
        cfg.SetValue("settings", "master", MasterVolume);
        cfg.SetValue("settings", "music", MusicVolume);
        cfg.SetValue("settings", "sfx", SfxVolume);
        cfg.SetValue("settings", "mouse", MouseSensitivity);
        cfg.SetValue("settings", "quality", Quality);
        cfg.SetValue("settings", "fullscreen", Fullscreen);
        cfg.Save(SavePath);
    }

    public void Goto(Node scene)
    {
        GetTree().Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        var old = _current ?? GetTree().CurrentScene;
        GetTree().Root.AddChild(scene);
        GetTree().CurrentScene = scene;
        _current = scene;
        old?.QueueFree();
    }

    public void ShowMenu() => Goto(new MainMenu());

    public void NewGame() => Goto(new Levels.Prologue());

    public void StartChapter(int n)
    {
        if (IsDemo && n >= 2) { Goto(new DemoEnd()); return; }
        if (n > Unlocked) { Unlocked = n; Save(); }
        Goto(n switch
        {
            0 => new Levels.Prologue(),
            1 => new Levels.Chapter1(),
            2 => new Levels.Chapter2(),
            3 => new Levels.Chapter3(),
            4 => new Levels.Chapter4(),
            5 => new Levels.Finale(),
            _ => new MainMenu(),
        });
    }


}
