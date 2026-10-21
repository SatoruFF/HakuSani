using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Environment = Godot.Environment;

namespace HakuSani.Levels;

/// <summary>
/// Common chapter scaffolding: two eras (environment + sun each), both cats, split-screen view,
/// HUD prompts, checkpoints, kill zones, exit zone and pause.
/// </summary>
public abstract partial class LevelBase : Node3D
{
    public Cat Haku, Sani;
    public GameView View;
    protected Node3D W;
    protected Environment EnvPast, EnvFuture;
    protected DirectionalLight3D SunPast, SunFuture;
    protected PauseMenu Pause;
    protected bool Running;

    protected abstract int Chapter { get; }
    protected virtual float FogPast => 0.012f;
    protected virtual float FogFuture => 0.01f;
    protected virtual bool Night => false;
    protected virtual (Vector3 pos, float yaw) StartHaku => (new Vector3(-0.6f, 0, 0), 0);
    protected virtual (Vector3 pos, float yaw) StartSani => (new Vector3(0.6f, 0, 0), 0);

    protected abstract void BuildWorld();
    protected virtual (string past, string future) MusicTracks => ("past", "future");
    protected virtual string StepSound(Cat c) => c.Era == Era.Past ? "step_grass" : "step_stone";
    protected virtual async Task Intro() => await View.Title(Game.ChapterTitles[Chapter], "", 2f);
    protected virtual void Tick(float dt) { }

    readonly List<(Aabb box, Era era)> _killZones = new();
    readonly List<(Vector3 pos, float r, Era era, Vector3 spawn, float yaw)> _checkpoints = new();
    Vector3 _exitPos;
    float _exitR;
    Action _onExit;
    bool _exitDone;
    int _active;     // solo: 0 = Haku, 1 = Sani
    float _fpsT;

    public override void _Ready()
    {
        W = new Node3D { Name = "World" };
        AddChild(W);
        EnvPast = EnvFactory.Make(Era.Past, FogPast, Night);
        EnvFuture = EnvFactory.Make(Era.Future, FogFuture, Night);
        SunPast = EnvFactory.Sun(this, Era.Past, Night);
        SunFuture = EnvFactory.Sun(this, Era.Future, Night);

        BuildWorld();

        Haku = Cat.Create(Era.Past);
        Sani = Cat.Create(Era.Future);
        W.AddChild(Haku);
        W.AddChild(Sani);
        Haku.Teleport(StartHaku.pos, StartHaku.yaw);
        Sani.Teleport(StartSani.pos, StartSani.yaw);
        Haku.SetCheckpoint(StartHaku.pos, StartHaku.yaw);
        Sani.SetCheckpoint(StartSani.pos, StartSani.yaw);

        Haku.Stepped += (c, _) => Audio.Sfx(StepSound(c), -16, 0.12f);
        Sani.Stepped += (c, _) => Audio.Sfx(StepSound(c), -16, 0.12f);
        Audio.I.Music(MusicTracks.past, MusicTracks.future);

        View = new GameView();
        AddChild(View);
        View.P[0].Vp.AddChild(CatCamera.Create(Haku, EnvPast));
        View.P[1].Vp.AddChild(CatCamera.Create(Sani, EnvFuture));
        ApplyLayout();

        Pause = new PauseMenu { Restart = () => Game.I.StartChapter(Chapter) };
        View.AddChild(Pause);

        SetControl(false);
        Callable.From(Begin).CallDeferred();
    }

    async void Begin()
    {
        Haku.Cam.SnapBehind();
        Sani.Cam.SnapBehind();
        View.Fade(0, 1.5f);
        await Intro();
        SetControl(true);
        Input.MouseMode = Input.MouseModeEnum.Captured;
        Running = true;
        if (Game.I.DebugScript == "solve")
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            GD.Print($"SOLVE chapter {Chapter}");
            await DebugSolve();
        }
    }

    // ---------- automated walkthrough (dev only) ----------
    protected virtual Task DebugSolve() => Task.CompletedTask;

    protected void Shot(string n) => Game.I.Shot($"ch{Chapter}_{n}");

    /// <summary>Teleport the cat in front of the nearest matching interactable and use it.</summary>
    protected async Task DUse(Cat c, Vector3 near, string contains = null)
    {
        Interactable best = null; float bd = 1e9f;
        foreach (var it in Interactable.All)
        {
            if (!it.Enabled || !L.Matches(it.Who, c.Era)) continue;
            if (contains != null && !it.Prompt.Contains(contains)) continue;
            float d = it.GlobalPosition.DistanceTo(near);
            if (d < bd) { bd = d; best = it; }
        }
        if (best == null) { GD.PrintErr($"DUse: nothing near {near} for {c.Name}"); return; }
        GD.Print($"DUse {c.Name}: {best.Prompt}");
        best.Trigger(c);
        await Wait(0.6f);
    }

    protected async Task DPut(Cat c, Vector3 p, float yaw = 0)
    {
        c.Teleport(p, yaw);
        await Wait(0.4f);
    }

    protected void SetControl(bool on)
    {
        Haku.ControlEnabled = on;
        Sani.ControlEnabled = on;
    }

    void ApplyLayout()
    {
        bool coop = Game.I.Mode == PlayMode.Coop;
        Haku.IsActive = coop || _active == 0;
        Sani.IsActive = coop || _active == 1;
        View.Layout(coop, _active);
        Audio.I.Focus(coop ? 0.5f : _active);
    }

    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouseMotion mm && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            var cat = Game.I.Mode == PlayMode.Coop ? Haku : (_active == 0 ? Haku : Sani);
            cat.Cam?.AddMouse(mm.Relative);
        }
        else if (e is InputEventMouseButton mb && mb.Pressed && Running && !GetTree().Paused)
            Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("pause") && !GetTree().Paused)
        {
            Pause.Open();
            GetViewport().SetInputAsHandled();
        }
        else if (e.IsActionPressed("switch_cat") && Game.I.Mode == PlayMode.Solo && Running)
        {
            _active = 1 - _active;
            ApplyLayout();
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _fpsT += dt;
        if (Game.I.ShotDir != null && _fpsT > 3) { _fpsT = 0; GD.Print($"FPS {Engine.GetFramesPerSecond()}"); }
        UpdatePrompt(0, Haku);
        UpdatePrompt(1, Sani);
        if (Running) Tick(dt);

        foreach (var cat in new[] { Haku, Sani })
        {
            foreach (var (box, era) in _killZones)
                if (L.Matches(era, cat.Era) && box.HasPoint(cat.GlobalPosition + Vector3.Up * 0.1f))
                    cat.Respawn();
            foreach (var cp in _checkpoints)
                if (L.Matches(cp.era, cat.Era) && cat.GlobalPosition.DistanceTo(cp.pos) < cp.r)
                    cat.SetCheckpoint(cp.spawn, cp.yaw);
        }

        if (_onExit != null && !_exitDone && Running &&
            Flat(Haku.GlobalPosition - _exitPos) < _exitR && Flat(Sani.GlobalPosition - _exitPos) < _exitR)
        {
            _exitDone = true;
            _onExit();
        }
    }

    static float Flat(Vector3 v) => new Vector2(v.X, v.Z).Length();

    void UpdatePrompt(int i, Cat c)
    {
        if (c.Focus == null || !c.ControlEnabled) { View.SetPrompt(i, ""); return; }
        string key = Game.I.Mode == PlayMode.Solo ? "E" : (i == 0 ? "E" : ".");
        View.SetPrompt(i, $"[{key}]  {c.Focus.Prompt}");
    }

    // ---------- helpers for chapters ----------

    protected async Task Wait(float s) => await ToSignal(GetTree().CreateTimer(s), SceneTreeTimer.SignalName.Timeout);

    protected void Hint(Era who, string text)
    {
        if (who != Era.Future) View.SetHint(0, text);
        if (who != Era.Past) View.SetHint(1, text);
    }

    protected void KillZone(Era era, Vector3 min, Vector3 max) => _killZones.Add((new Aabb(min, max - min), era));

    protected void Checkpoint(Era era, Vector3 pos, float r, float yaw = 0) => _checkpoints.Add((pos, r, era, pos, yaw));
    protected void Checkpoint(Era era, Vector3 pos, float r, Vector3 spawn, float yaw) => _checkpoints.Add((pos, r, era, spawn, yaw));

    /// <summary>Glowing spot: touching it lets both cats see each other's silhouette for a while.</summary>
    protected Interactable TimeGlow(Vector3 pos, Action onTouch = null, float seconds = 6f)
    {
        B.GlowDisc(W, pos, new Color(0.85f, 0.75f, 1f), 0.6f);
        return Interactable.Create(W, pos + Vector3.Up * 0.1f, Era.Both, "Коснуться свечения", c =>
        {
            Haku.ShowGhost(seconds);
            Sani.ShowGhost(seconds);
            Audio.Sfx("glow", -6);
            onTouch?.Invoke();
        }, 0.75f, false);
    }

    protected void Exit(Vector3 pos, float r, Action onBoth)
    {
        _exitPos = pos;
        _exitR = r;
        _onExit = onBoth;
        B.GlowDisc(W, pos, new Color(1f, 0.95f, 0.8f), r);
        B.Motes(W, Era.Past, pos + Vector3.Up, new Vector3(r, 1, r), new Color(1f, 0.85f, 0.5f), 40);
        B.Motes(W, Era.Future, pos + Vector3.Up, new Vector3(r, 1, r), new Color(0.6f, 0.8f, 1f), 40);
    }

    protected void Ground(Vector3 center, Vector2 size, Material past, Material future)
    {
        B.Box2(W, center + Vector3.Down * 0.25f, new Vector3(size.X, 0.5f, size.Y), past, future);
    }

    /// <summary>Wall segment between two ground points, present in both eras with era-specific looks.</summary>
    protected Node3D Wall(Vector3 a, Vector3 b, float h, float thick, Material past, Material future, Era era = Era.Both)
    {
        var mid = (a + b) / 2 + Vector3.Up * h / 2;
        var d = b - a;
        float len = d.Length();
        float rot = Mathf.RadToDeg(Mathf.Atan2(-d.Z, d.X));
        if (era == Era.Both) return B.Box2(W, mid, new Vector3(len, h, thick), past, future, rot);
        return B.Box(W, era, mid, new Vector3(len, h, thick), era == Era.Past ? past : future, rot);
    }

    /// <summary>Ring of big buildings outside the play area (modern lit city or medieval houses).</summary>
    protected void Skyline(Era era, Rect2 inner, bool modern, int seed = 3, float gap = 4f)
    {
        var rng = new RandomNumberGenerator { Seed = (ulong)seed };
        string[] city = { "Building_Small_1", "Building_Medium_2_001", "Building_Large_2" };
        void Row(Vector3 from, Vector3 dir, float len, float face)
        {
            float t = 0;
            while (t < len)
            {
                var p = from + dir * t;
                if (modern) { Kit.City(W, era, city[rng.RandiRange(0, 2)], p, face + rng.RandiRange(-1, 1) * 2, 1); t += rng.RandfRange(14, 20); }
                else { Kit.House(W, era, p, rng.RandiRange(2, 4), rng.RandiRange(2, 3), rng.RandiRange(2, 3), face, era == Era.Future, era == Era.Past, rng.RandiRange(1, 999)); t += rng.RandfRange(8, 11); }
            }
        }
        float x0 = inner.Position.X - gap, x1 = inner.End.X + gap, z0 = inner.Position.Y - gap, z1 = inner.End.Y + gap;
        float off = modern ? 8 : 3;
        Row(new Vector3(x0 - off, 0, z0), Vector3.Back, z1 - z0, 90);      // west side, facing east (+X)
        Row(new Vector3(x1 + off, 0, z1), Vector3.Forward, z1 - z0, -90);  // east side
        Row(new Vector3(x1, 0, z0 - off), Vector3.Left, x1 - x0, 0);       // north side, facing south (+Z)
        Row(new Vector3(x0, 0, z1 + off), Vector3.Right, x1 - x0, 180);    // south side
    }

    /// <summary>Ivy hanging over the top of a wall segment (past ruins).</summary>
    protected void Vines(Vector3 a, Vector3 b, float h, int seed = 5)
    {
        var rng = new RandomNumberGenerator { Seed = (ulong)seed };
        var d = b - a; float len = d.Length(); var dir = d / len;
        float rot = Mathf.RadToDeg(Mathf.Atan2(dir.X, dir.Z)) - 90;
        for (float t = 1f; t < len - 0.5f; t += rng.RandfRange(2.2f, 4f))
            Kit.Med(W, Era.Past, rng.Randf() < 0.5f ? "Prop_Vine9" : "Prop_Vine6", a + dir * t + Vector3.Up * (h - 0.3f), rot + (rng.Randf() < 0.5f ? 0 : 180), rng.RandfRange(0.8f, 1.2f), Kit.RuinTint);
    }

    protected async void Complete()
    {
        Running = false;
        SetControl(false);
        Audio.Sfx("chime", -6);
        Audio.I.FadeOut();
        View.SetHint(0, ""); View.SetHint(1, "");
        await ToSignal(View.Fade(1, 1.4f), Tween.SignalName.Finished);
        Game.I.StartChapter(Chapter + 1);
    }
}
