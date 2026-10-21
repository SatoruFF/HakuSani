using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace HakuSani.Levels;

/// <summary>
/// Глава II. Следы, которых ещё нет.
/// Past: an old railway crossing and a fog-drowned maze of walls. Future: the walls are long gone —
/// only low rubble lines remain, and the floor is dust. Where Sani walks, glowing paw prints appear
/// in Haku's fog. A fallen pillar blocks Sani; Haku topples the still-standing pillar the other way.
/// At the end Haku finds their toy — and for a moment, both see home.
/// </summary>
public partial class Chapter2 : LevelBase
{
    protected override int Chapter => 2;
    protected override (string past, string future) MusicTracks => ("fog", "future");
    protected override string StepSound(Cat c) => c.Era == Era.Past ? "step_grass" : "step_stone";
    protected override float FogPast => 0.13f;
    protected override float FogFuture => 0.02f;
    protected override (Vector3 pos, float yaw) StartHaku => (new Vector3(-0.5f, 0, 3f), 0);
    protected override (Vector3 pos, float yaw) StartSani => (new Vector3(0.5f, 0, 3f), 0);

    const int Cols = 9, Rows = 8;
    const float Cell = 2.4f, X0 = -10.8f, Z0 = -5f;
    bool[,] _h = new bool[Rows + 1, Cols];   // horizontal wall at the top edge of row r (z = Z0 - r*Cell)
    bool[,] _v = new bool[Rows, Cols + 1];   // vertical wall at the left edge of column c
    List<(int r, int c)> _path;
    Vector3 _blockPos;
    Node3D _pastPillar, _futureRubble;
    bool _pillarDown, _sawBlock, _toyTaken;
    Footprints _prints;

    Vector3 CellCenter(int r, int c) => new(X0 + (c + 0.5f) * Cell, 0, Z0 - (r + 0.5f) * Cell);

    protected override void BuildWorld()
    {
        Ground(new Vector3(0, 0, -12), new Vector2(40, 50), Mats.DirtPast, Mats.Paving);
        B.Box(W, Era.Future, new Vector3(0, 0.01f, Z0 - Rows * Cell / 2), new Vector3(Cols * Cell, 0.02f, Rows * Cell), Mats.DustFloor, 0, false);

        BuildCrossing();
        GenerateMaze();
        BuildMaze();
        BuildPillar();
        BuildEnd();

        _prints = new Footprints
        {
            ShowIn = Era.Past,
            Color = new Color(0.45f, 0.75f, 1f),
            IsDust = p => p.Z < Z0 + 0.5f && p.Z > Z0 - Rows * Cell - 3f && Mathf.Abs(p.X) < Cols * Cell / 2 + 0.5f,
        };
        W.AddChild(_prints);

        Skyline(Era.Future, new Rect2(-12.5f, -32, 25, 38), true, 21);
        Skyline(Era.Past, new Rect2(-12.5f, -32, 25, 38), false, 22);
        Vines(new Vector3(-12.2f, 0, 5.5f), new Vector3(-12.2f, 0, -31.5f), 3f, 23);
        Vines(new Vector3(12.2f, 0, -31.5f), new Vector3(12.2f, 0, 5.5f), 3f, 24);
        // outer boundary so nobody wanders off into the void
        Wall(new Vector3(-12.5f, 0, 6), new Vector3(-12.5f, 0, -32), 3, 0.5f, Mats.StoneOld, Mats.StoneNew);
        Wall(new Vector3(12.5f, 0, 6), new Vector3(12.5f, 0, -32), 3, 0.5f, Mats.StoneOld, Mats.StoneNew);
        Wall(new Vector3(-12.5f, 0, 6), new Vector3(12.5f, 0, 6), 3, 0.5f, Mats.StoneOld, Mats.StoneNew);
        Wall(new Vector3(-12.5f, 0, -32), new Vector3(12.5f, 0, -32), 3, 0.5f, Mats.StoneOld, Mats.StoneNew);
    }

    public override void _Ready()
    {
        base._Ready();
        _prints.Hook(Sani);
    }

    void BuildCrossing()
    {
        // rails along X at z = 0.5 .. 1.5
        foreach (var z in new[] { 0.6f, 1.4f })
        {
            B.Box(W, Era.Past, new Vector3(0, 0.06f, z), new Vector3(25, 0.08f, 0.07f), Mats.Flat(new Color(0.45f, 0.28f, 0.18f), 0.9f), 0, false);
            B.Box(W, Era.Future, new Vector3(0, 0.06f, z), new Vector3(25, 0.08f, 0.07f), Mats.Metal, 0, false);
        }
        for (float x = -12; x <= 12; x += 0.7f)
        {
            B.Box(W, Era.Past, new Vector3(x, 0.02f, 1f), new Vector3(0.22f, 0.05f, 1.3f), Mats.WoodOld, 0, false);
            B.Box(W, Era.Future, new Vector3(x, 0.02f, 1f), new Vector3(0.22f, 0.05f, 1.3f), Mats.StoneNew, 0, false);
        }
        B.Scatter(W, Era.Past, new Rect2(-12, -1, 24, 6), 90, 31, new[] { "Grass_Wispy_Tall", "Grass_Common_Tall", "Fern_1", "Plant_1" });
        // crossing sign + signal lamp
        B.Cylinder(W, Era.Past, new Vector3(-1.8f, 0.9f, 2.2f), 0.04f, 1.8f, Mats.WoodOld, false);
        B.Box(W, Era.Past, new Vector3(-1.8f, 1.7f, 2.2f), new Vector3(0.9f, 0.12f, 0.03f), Mats.WoodPlank, 35, false);
        B.Box(W, Era.Past, new Vector3(-1.8f, 1.7f, 2.2f), new Vector3(0.9f, 0.12f, 0.03f), Mats.WoodPlank, -35, false);
        B.Cylinder(W, Era.Future, new Vector3(-1.8f, 1.1f, 2.2f), 0.05f, 2.2f, Mats.Metal, false);
        var red = Mats.Emissive(new Color(1f, 0.2f, 0.15f), 4f);
        var lamp = B.Box(W, Era.Future, new Vector3(-1.8f, 2.1f, 2.25f), new Vector3(0.18f, 0.18f, 0.08f), red, 0, false);
        var l = B.Lamp(W, Era.Future, new Vector3(-1.8f, 2.1f, 2.5f), new Color(1f, 0.25f, 0.2f), 2f, 5f);
        var blink = CreateTween().SetLoops();
        blink.TweenProperty(l, "light_energy", 0f, 0.5f);
        blink.TweenProperty(l, "light_energy", 2f, 0.5f);
        B.Motes(W, Era.Past, new Vector3(0, 1.2f, -12), new Vector3(12, 1.2f, 16), new Color(1f, 0.9f, 0.7f), 200);
        B.Motes(W, Era.Future, new Vector3(0, 1.2f, -12), new Vector3(12, 1.2f, 16), new Color(0.6f, 0.75f, 1f), 120);
    }

    void GenerateMaze()
    {
        for (int r = 0; r <= Rows; r++) for (int c = 0; c < Cols; c++) _h[r, c] = true;
        for (int r = 0; r < Rows; r++) for (int c = 0; c <= Cols; c++) _v[r, c] = true;
        var rng = new Random(7);
        var seen = new bool[Rows, Cols];
        var stack = new Stack<(int, int)>();
        stack.Push((0, Cols / 2));
        seen[0, Cols / 2] = true;
        while (stack.Count > 0)
        {
            var (r, c) = stack.Peek();
            var opts = new List<(int, int, int)>();
            if (r > 0 && !seen[r - 1, c]) opts.Add((r - 1, c, 0));
            if (r < Rows - 1 && !seen[r + 1, c]) opts.Add((r + 1, c, 1));
            if (c > 0 && !seen[r, c - 1]) opts.Add((r, c - 1, 2));
            if (c < Cols - 1 && !seen[r, c + 1]) opts.Add((r, c + 1, 3));
            if (opts.Count == 0) { stack.Pop(); continue; }
            var (nr, nc, d) = opts[rng.Next(opts.Count)];
            if (d == 0) _h[r, c] = false;
            if (d == 1) _h[r + 1, c] = false;
            if (d == 2) _v[r, c] = false;
            if (d == 3) _v[r, c + 1] = false;
            seen[nr, nc] = true;
            stack.Push((nr, nc));
        }
        _h[0, Cols / 2] = false;          // entrance
        _h[Rows, Cols / 2] = false;       // exit

        // BFS for the solution path
        var prev = new Dictionary<(int, int), (int, int)>();
        var q = new Queue<(int, int)>();
        q.Enqueue((0, Cols / 2));
        prev[(0, Cols / 2)] = (-1, -1);
        while (q.Count > 0)
        {
            var (r, c) = q.Dequeue();
            void Try(int nr, int nc, bool open)
            {
                if (!open || nr < 0 || nc < 0 || nr >= Rows || nc >= Cols || prev.ContainsKey((nr, nc))) return;
                prev[(nr, nc)] = (r, c);
                q.Enqueue((nr, nc));
            }
            Try(r - 1, c, !_h[r, c]);
            Try(r + 1, c, r + 1 <= Rows && !_h[r + 1, c]);
            Try(r, c - 1, !_v[r, c]);
            Try(r, c + 1, !_v[r, c + 1]);
        }
        _path = new List<(int, int)>();
        var cur = (Rows - 1, Cols / 2);
        while (cur.Item1 >= 0) { _path.Add(cur); cur = prev[cur]; }
        _path.Reverse();
    }

    void BuildMaze()
    {
        float hgt = 1.8f, thick = 0.3f;
        for (int r = 0; r <= Rows; r++)
            for (int c = 0; c < Cols; c++)
                if (_h[r, c])
                {
                    var p = new Vector3(X0 + (c + 0.5f) * Cell, 0, Z0 - r * Cell);
                    B.Box(W, Era.Past, p + Vector3.Up * hgt / 2, new Vector3(Cell + thick, hgt, thick), (r + c) % 3 == 0 ? Mats.MossyStone : Mats.StoneOld);
                    B.Box(W, Era.Future, p + Vector3.Up * 0.12f, new Vector3(Cell * 0.9f, 0.24f, thick * 1.3f), Mats.Rubble);
                }
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c <= Cols; c++)
                if (_v[r, c])
                {
                    var p = new Vector3(X0 + c * Cell, 0, Z0 - (r + 0.5f) * Cell);
                    B.Box(W, Era.Past, p + Vector3.Up * hgt / 2, new Vector3(thick, hgt, Cell + thick), (r * 2 + c) % 3 == 0 ? Mats.MossyStone : Mats.StoneOld);
                    B.Box(W, Era.Future, p + Vector3.Up * 0.12f, new Vector3(thick * 1.3f, 0.24f, Cell * 0.9f), Mats.Rubble);
                }
        // ivy on some past maze walls
        var vr = new RandomNumberGenerator { Seed = 77 };
        for (int r = 1; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                if (_h[r, c] && vr.Randf() < 0.35f)
                    Kit.Med(W, Era.Past, "Prop_Vine6", new Vector3(X0 + (c + 0.5f) * Cell, hgt - 0.25f, Z0 - r * Cell), vr.Randf() < 0.5f ? 0 : 180, 0.8f, Kit.RuinTint);
        // overgrowth inside the past maze, a few dead trees
        B.Scatter(W, Era.Past, new Rect2(X0, Z0 - Rows * Cell, Cols * Cell, Rows * Cell), 160, 41, new[] { "Grass_Wispy_Short", "Grass_Common_Short", "Clover_2", "Fern_1", "Mushroom_Common" });
        B.Prop(W, Era.Future, "DeadTree_2", CellCenter(2, 1), 30, 0.6f, 0.2f);
        B.Prop(W, Era.Future, "DeadTree_4", CellCenter(5, 7), 130, 0.6f, 0.2f);
        B.Prop(W, Era.Past, "TwistedTree_1", CellCenter(3, 8), 30, 0.7f, 0.25f);
        B.Prop(W, Era.Past, "DeadTree_1", CellCenter(6, 1), 30, 0.7f, 0.25f);
    }

    void BuildPillar()
    {
        int k = Math.Max(2, _path.Count / 3);
        var a = _path[k];
        var b = _path[k + 1];
        _blockPos = (CellCenter(a.r, a.c) + CellCenter(b.r, b.c)) / 2;
        bool alongZ = a.c == b.c;   // passage crosses a horizontal wall line
        // future: fallen pillar + debris blocking the passage
        _futureRubble = new Node3D { Position = _blockPos };
        W.AddChild(_futureRubble);
        B.Slab(_futureRubble, Era.Future, new Vector3(0, 0.45f, 0), new Vector3(alongZ ? 2.3f : 0.6f, 0.9f, alongZ ? 0.6f : 2.3f), Mats.StoneNew, new Vector3(0, 0, alongZ ? 8 : 0));
        B.Box(_futureRubble, Era.Future, new Vector3(0.3f, 1.05f, 0.2f), new Vector3(0.6f, 0.4f, 0.6f), Mats.Rubble, 25);
        // past: the pillar still stands at the corner of that passage
        var corner = _blockPos + (alongZ ? new Vector3(Cell / 2 - 0.25f, 0, 0) : new Vector3(0, 0, Cell / 2 - 0.25f));
        _pastPillar = new Node3D { Position = corner };
        W.AddChild(_pastPillar);
        var col = B.Cylinder(_pastPillar, Era.Past, new Vector3(0, 1.3f, 0), 0.22f, 2.6f, Mats.StoneOld);
        B.Box(_pastPillar, Era.Past, new Vector3(0, 2.65f, 0), new Vector3(0.6f, 0.15f, 0.6f), Mats.StoneOld, 0, false);
        _pastPillar.RotationDegrees = alongZ ? new Vector3(0, 0, -6) : new Vector3(6, 0, 0);
        var it = Interactable.Create(W, corner + Vector3.Up * 0.3f, Era.Past, "Толкнуть колонну", c => TopplePillar(alongZ), 0.9f);
    }

    async void TopplePillar(bool alongZ)
    {
        _pillarDown = true;
        B.Shake(_pastPillar, 0.03f, 0.5f);
        await Wait(0.5f);
        Audio.Sfx("stone", 0);
        // falls outward, away from the corridor, onto the wall line
        B.Move(_pastPillar, null, alongZ ? new Vector3(0, 0, -86) : new Vector3(86, 0, 0), 1.1f, Tween.TransitionType.Bounce, Tween.EaseType.Out);
        foreach (var c in _pastPillar.GetChildren()) if (c is Node3D n) B.Disable(n);
        await Wait(1.3f);
        B.Puff(W, Era.Past, _pastPillar.GlobalPosition, 1.6f, 36);
        Haku.Cam.Shake(0.025f, 0.35f);
        B.Ripple(W, Era.Future, _blockPos, 1.8f);
        // decades later the debris lies in the wall line instead of across the path
        var dir = alongZ ? new Vector3(Cell * 0.8f, 0, 0) : new Vector3(0, 0, Cell * 0.8f);
        B.Move(_futureRubble, _blockPos + dir + Vector3.Down * 0.3f, null, 0.8f);
        foreach (var c in _futureRubble.GetChildren()) if (c is Node3D n) B.Disable(n);
        _ = View.Narrate("Один не всегда видит решение — но может создать его для другого.", 4f);
        Hint(Era.Future, "Путь свободен. Веди Хаку дальше");
        Hint(Era.Past, "Иди по светящимся следам Сани");
    }

    void BuildEnd()
    {
        var end = new Vector3(0, 0, Z0 - Rows * Cell - 2.2f);
        // the toy on a stone in the past
        B.Box(W, Era.Past, end + new Vector3(0, 0.15f, -0.8f), new Vector3(0.6f, 0.3f, 0.6f), Mats.MossyStone);
        var toy = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.06f, Height = 0.1f }, MaterialOverride = Mats.Flat(new Color(0.85f, 0.75f, 0.6f)),
            Layers = L.VisPast, Position = end + new Vector3(0, 0.36f, -0.8f), Scale = new Vector3(1, 0.8f, 1.5f),
        };
        W.AddChild(toy);
        var glint = B.Lamp(W, Era.Past, end + new Vector3(0, 0.8f, -0.6f), new Color(1f, 0.85f, 0.6f), 1.5f, 3f);
        Interactable.Create(W, end + new Vector3(0, 0.3f, -0.8f), Era.Past, "Поднять игрушку", c =>
        {
            if (Flat(Sani.GlobalPosition - end) > 3f)
            {
                _ = View.Narrate("Сани ещё в пути. Дождись её.", 2.5f);
                return;
            }
            toy.Visible = false;
            _toyTaken = true;
            Vision();
        }, 0.9f, false);
        B.GlowDisc(W, end, new Color(1f, 0.95f, 0.8f), 1.4f);
        Checkpoint(Era.Both, end, 2f, end, 0);
        B.Box(W, Era.Future, end + new Vector3(0, 0.15f, -0.8f), new Vector3(0.7f, 0.3f, 0.7f), Mats.StoneNew);
    }

    static float Flat(Vector3 v) => new Vector2(v.X, v.Z).Length();

    async void Vision()
    {
        Running = false;
        SetControl(false);
        // a quick glimpse of home on both screens
        var home = Home.Build(W, new Vector3(300, 0, 0), L.VisCine, HomeLook.Sunny, false);
        var cams = new List<Camera3D>();
        foreach (var p in View.P)
        {
            var cam = new Camera3D { CullMask = L.VisCine, Fov = 55, Environment = EnvFactory.Make(Era.Past, 0) };
            p.Vp.AddChild(cam);
            cam.GlobalPosition = new Vector3(300, 1.3f, 2.2f);
            cam.LookAt(new Vector3(300, 0.4f, -1.2f), Vector3.Up);
            cams.Add(cam);
        }
        await ToSignal(View.Fade(1, 0.6f, Colors.White), Tween.SignalName.Finished);
        foreach (var c in cams) c.Current = true;
        View.Fade(0, 0.8f);
        _ = View.Narrate("На мгновение туман исчезает: знакомая комната, солнце на полу, место, где они спали вместе.", 4.5f);
        foreach (var c in cams)
        {
            var t = c.CreateTween();
            t.TweenProperty(c, "position", c.Position + new Vector3(0, -0.3f, -0.9f), 5f);
        }
        await Wait(4.8f);
        await ToSignal(View.Fade(1, 0.5f, Colors.White), Tween.SignalName.Finished);
        foreach (var c in cams) c.QueueFree();
        home.QueueFree();
        View.Fade(0, 0.8f, Colors.Black);
        await View.Narrate("Дом существует. Нужно только найти дорогу.", 3f);
        Complete();
    }

    protected override async Task Intro()
    {
        await View.Title(Game.ChapterTitles[2], "", 2f);
        await View.Narrate("Старый железнодорожный переход. За ним — густой туман и одинаковые улицы.", 4f);
        Hint(Era.Past, "Хаку: в тумане не видно дороги. Жди следов Сани");
        Hint(Era.Future, "Сани: твои шаги по пыли видны в прошлом. Проложи Хаку путь через лабиринт");
    }

    protected override void Tick(float dt)
    {
        if (!_sawBlock && !_pillarDown && Sani.GlobalPosition.DistanceTo(_blockPos) < 2.2f)
        {
            _sawBlock = true;
            _ = View.Narrate("Следы обрываются: путь Сани завален упавшей колонной.", 3.5f);
            Hint(Era.Past, "В прошлом колонна ещё стоит — найди её там, где обрываются следы");
            Hint(Era.Future, "Завал! Подожди, пока Хаку что-нибудь изменит");
        }
    }
}
