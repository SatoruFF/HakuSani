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
            Unlocked = (int)cfg.GetValue("progress", "unlocked", 1);
    }

    public void Save()
    {
        var cfg = new ConfigFile();
        cfg.SetValue("progress", "unlocked", Unlocked);
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
