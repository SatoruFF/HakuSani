using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace HakuSani.Levels;

/// <summary>
/// Глава III. Город, который помнит.
/// Canal: in the past water flows and Haku starts an old raft mechanism; in the future the canal is
/// dry and Sani climbs out on tree roots. Gate: Haku pushes a crate onto a plate — the gate opens and
/// stays open for decades. Tower: Sani climbs roots to a glowing lever that opens Haku's tower door;
/// Haku touches the stopped clockwork and Sani hears his purr through time.
/// </summary>
public partial class Chapter3 : LevelBase
{
    protected override int Chapter => 3;
    protected override string StepSound(Cat c) => "step_stone";
    protected override (Vector3 pos, float yaw) StartHaku => (new Vector3(-0.6f, 0, 2.5f), 0);
    protected override (Vector3 pos, float yaw) StartSani => (new Vector3(0.6f, 0, 2.5f), 0);

    Shuttle _raft;
    Node3D _pastGate, _futureGate, _pastTowerDoor, _gear;
    PushBlock _crate;
    bool _raftOn, _gateOpen, _leverPulled, _done;

    protected override void BuildWorld()
    {
        // plazas are tall blocks so the canal has real walls
        B.Box2(W, new Vector3(0, -1.5f, 0), new Vector3(26, 3, 14), Mats.Paving, Mats.StoneOld);          // south: z 7 .. -7
        B.Box2(W, new Vector3(0, -1.5f, -24), new Vector3(26, 3, 22), Mats.Paving, Mats.StoneOld);        // north: z -13 .. -35
        B.Box2(W, new Vector3(0, -2.75f, -10), new Vector3(26, 0.5f, 6), Mats.StoneOld, Mats.DirtPast);   // canal floor (top y = -2.5)

        Wall(new Vector3(-13, 0, 7), new Vector3(-13, 0, -35), 3.5f, 0.5f, Mats.PlasterNew, Mats.PlasterOld);
        Wall(new Vector3(13, 0, 7), new Vector3(13, 0, -35), 3.5f, 0.5f, Mats.PlasterNew, Mats.PlasterOld);
        Wall(new Vector3(-13, 0, 7), new Vector3(13, 0, 7), 3.5f, 0.5f, Mats.PlasterNew, Mats.PlasterOld);
        Wall(new Vector3(-13, 0, -35), new Vector3(13, 0, -35), 3.5f, 0.5f, Mats.PlasterNew, Mats.PlasterOld);
        B.Box2(W, new Vector3(-13, -1.5f, -10), new Vector3(0.5f, 6, 6), Mats.StoneOld, Mats.StoneOld);
        B.Box2(W, new Vector3(13, -1.5f, -10), new Vector3(0.5f, 6, 6), Mats.StoneOld, Mats.StoneOld);

        BuildCanal();
        BuildGate();
        BuildTower();
        BuildCity();
    }

    void BuildCanal()
    {
        // PAST: water + raft mechanism
        var water = new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(26, 6), SubdivideWidth = 40, SubdivideDepth = 10 },
            MaterialOverride = new ShaderMaterial { Shader = Shaders.Water }, Layers = L.VisPast,
            Position = new Vector3(0, -0.4f, -10),
        };
        W.AddChild(water);
        KillZone(Era.Past, new Vector3(-13, -3, -13), new Vector3(13, -0.3f, -7));
        _raft = Shuttle.Create(W, Era.Past, new Vector3(0, -0.15f, -7.9f), new Vector3(0, -0.15f, -12.1f), new Vector3(1.6f, 0.2f, 1.6f), Mats.WoodPlank);
        _raft.TravelTime = 3.2f;
        // chains guiding the raft
        foreach (var x in new[] { -0.9f, 0.9f })
            B.Box(W, Era.Past, new Vector3(x, 0.1f, -10), new Vector3(0.04f, 0.04f, 6.4f), Mats.Metal, 0, false);
        // mechanism with a gear and a lever
        B.Box(W, Era.Past, new Vector3(2.2f, 0.45f, -5.6f), new Vector3(0.9f, 0.9f, 0.6f), Mats.WoodOld);
        _gear = new Node3D { Position = new Vector3(2.2f, 1.15f, -5.6f) };
        W.AddChild(_gear);
        _gear.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.45f, BottomRadius = 0.45f, Height = 0.1f, RadialSegments = 12 }, MaterialOverride = Mats.Metal, Layers = L.VisPast, RotationDegrees = new Vector3(90, 0, 0) });
        for (int i = 0; i < 12; i++)
        {
            var a = Mathf.Tau * i / 12;
            _gear.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.12f, 0.12f, 0.1f) }, MaterialOverride = Mats.Metal, Layers = L.VisPast, Position = new Vector3(Mathf.Cos(a) * 0.5f, Mathf.Sin(a) * 0.5f, 0) });
        }
        Interactable.Create(W, new Vector3(2.2f, 0.3f, -5.1f), Era.Past, "Запустить механизм", c => StartRaft(), 0.9f);

        // FUTURE: dry canal, broken bridge pieces, roots ramp up the north side
        B.Slab(W, Era.Future, new Vector3(-3.5f, -2.2f, -9.5f), new Vector3(3, 0.3f, 1.4f), Mats.StoneNew, new Vector3(0, 20, 12));
        B.Slab(W, Era.Future, new Vector3(-1.2f, -2.1f, -11.3f), new Vector3(2.2f, 0.3f, 1.2f), Mats.StoneNew, new Vector3(8, -15, -10));
        B.Slab(W, Era.Future, new Vector3(6f, -1.25f, -12.5f), new Vector3(4.9f, 0.4f, 0.95f), Mats.Roots, new Vector3(0, 0, 31));
        B.Slab(W, Era.Future, new Vector3(6.3f, -1.15f, -12.2f), new Vector3(4.5f, 0.25f, 0.35f), Mats.Roots, new Vector3(0, 8, 29), false);
        B.Prop(W, Era.Future, "TwistedTree_3", new Vector3(9.3f, 0, -14.4f), 200, 1.2f, 0.4f);
        B.Prop(W, Era.Future, "Grass_Wispy_Tall", new Vector3(2, -2.5f, -9), 0, 1f);
        B.Scatter(W, Era.Future, new Rect2(-12, -12.5f, 24, 5), 50, 51, new[] { "Grass_Wispy_Short", "Fern_1", "Plant_1" });
        Checkpoint(Era.Future, new Vector3(0, -2.5f, -10), 6f, new Vector3(3.5f, -2.5f, -11.5f), -Mathf.Pi / 2);
        Checkpoint(Era.Both, new Vector3(0, 0, -15), 2.5f, new Vector3(0, 0, -15), 0);
    }

    void StartRaft()
    {
        if (_raftOn) return;
        _raftOn = true;
        Audio.Sfx("door", -2);
        _raft.Run();
        var t = _gear.CreateTween().SetLoops();
        t.TweenProperty(_gear, "rotation:z", Mathf.Tau, 4f).AsRelative();
        Hint(Era.Past, "Плот поплыл! Переберись на другой берег");
    }

    void BuildGate()
    {
        float z = -19;
        Wall(new Vector3(-13, 0, z), new Vector3(-1.1f, 0, z), 3.2f, 0.6f, Mats.PlasterNew, Mats.PlasterOld);
        Wall(new Vector3(1.1f, 0, z), new Vector3(13, 0, z), 3.2f, 0.6f, Mats.PlasterNew, Mats.PlasterOld);
        B.Box2(W, new Vector3(0, 2.6f, z), new Vector3(2.2f, 1.2f, 0.6f), Mats.PlasterNew, Mats.PlasterOld);

        _pastGate = new Node3D { Position = new Vector3(0, 0, z) };
        W.AddChild(_pastGate);
        for (float x = -0.9f; x <= 0.91f; x += 0.3f)
            B.Box(_pastGate, Era.Past, new Vector3(x, 1f, 0), new Vector3(0.07f, 2f, 0.07f), Mats.Metal, 0, false);
        B.Box(_pastGate, Era.Past, new Vector3(0, 1f, 0), new Vector3(2.2f, 2f, 0.1f), Mats.Metal).GetChild<MeshInstance3D>(1).Visible = false;
        B.Box(_pastGate, Era.Past, new Vector3(0, 1.9f, 0), new Vector3(2.2f, 0.1f, 0.1f), Mats.Metal, 0, false);

        _futureGate = new Node3D { Position = new Vector3(0, 0, z) };
        W.AddChild(_futureGate);
        B.Box(_futureGate, Era.Future, new Vector3(0, 1f, 0), new Vector3(2.2f, 2f, 0.14f), Mats.Flat(new Color(0.45f, 0.3f, 0.22f), 0.9f));
        B.Prop(W, Era.Future, "Plant_7_Big", new Vector3(1.3f, 0, z + 0.5f), 0, 0.8f);
        Interactable.Create(W, new Vector3(0, 0.5f, z + 0.4f), Era.Future, "Толкнуть ворота", c =>
        {
            if (!_gateOpen) _ = View.Narrate("Ворота проржавели насквозь. Только механизм в прошлом мог их поднять.", 3.5f);
        }, 0.9f, false);

        // the crate and the plate (past, south of the gate)
        _crate = PushBlock.Create(W, Era.Past, new Vector3(-4, 0.4f, -15.5f), new Vector3(0.8f, 0.8f, 0.8f), Mats.WoodPlank);
        var plate = PressurePlate.Create(W, Era.Past, new Vector3(-4, 0, -17.5f), new Color(1f, 0.75f, 0.35f), () => new Node3D[] { _crate }, 0.45f);
        plate.Changed = on => { if (on) OpenGate(); };
        // a chain runs from the plate to the gate to make the link readable
        B.Box(W, Era.Past, new Vector3(-2.2f, 0.03f, -17.8f), new Vector3(3.4f, 0.03f, 0.05f), Mats.Metal, 0, false);
    }

    async void OpenGate()
    {
        if (_gateOpen) return;
        _gateOpen = true;
        Audio.Sfx("metal", -2);
        B.Move(_pastGate, new Vector3(0, 2.1f, -19), null, 1.6f, Tween.TransitionType.Sine);
        foreach (var c in _pastGate.GetChildren()) if (c is Node3D n) B.Disable(n);
        await Wait(1.2f);
        B.Ripple(W, Era.Future, new Vector3(0, 0, -19), 2f);
        // and it stays up for decades: the rusted gate in the future is simply not there
        B.Shake(_futureGate, 0.05f, 0.5f);
        await Wait(0.5f);
        B.Move(_futureGate, new Vector3(0, 2.15f, -19), null, 1.2f);
        foreach (var c in _futureGate.GetChildren()) if (c is Node3D n) B.Disable(n);
        _ = View.Narrate("Механизм в прошлом — открытые ворота спустя десятилетия.", 3.5f);
        Hint(Era.Past, "Иди к башне с часами");
        Hint(Era.Future, "Ворота открыты. Взберись по корням в башню");
    }

    void BuildTower()
    {
        float zs = -27, ze = -33, x0 = -3, x1 = 3, H = 8;
        var wallP = Mats.StoneNew; var wallF = Mats.MossyStone;
        // south face with a door hole
        B.Box2(W, new Vector3(-1.9f, H / 2, zs), new Vector3(2.2f, H, 0.4f), wallP, wallF);
        B.Box2(W, new Vector3(1.9f, H / 2, zs), new Vector3(2.2f, H, 0.4f), wallP, wallF);
        B.Box2(W, new Vector3(0, (H + 1.7f) / 2, zs), new Vector3(1.6f, H - 1.7f, 0.4f), wallP, wallF);
        // west & north
        B.Box2(W, new Vector3(x0, H / 2, (zs + ze) / 2), new Vector3(0.4f, H, 6.4f), wallP, wallF);
        B.Box2(W, new Vector3(0, H / 2, ze), new Vector3(6.4f, H, 0.4f), wallP, wallF);
        // east face: past solid, future has a window hole at y 2.2..3.6 between z -28.8 and -30.4
        B.Box(W, Era.Past, new Vector3(x1, H / 2, (zs + ze) / 2), new Vector3(0.4f, H, 6.4f), wallP);
        B.Box(W, Era.Future, new Vector3(x1, 1.1f, (zs + ze) / 2), new Vector3(0.4f, 2.2f, 6.4f), wallF);
        B.Box(W, Era.Future, new Vector3(x1, 2.9f, -27.9f), new Vector3(0.4f, 1.4f, 1.8f), wallF);
        B.Box(W, Era.Future, new Vector3(x1, 2.9f, -31.7f), new Vector3(0.4f, 1.4f, 2.6f), wallF);
        B.Box(W, Era.Future, new Vector3(x1, (H + 3.6f) / 2, (zs + ze) / 2), new Vector3(0.4f, H - 3.6f, 6.4f), wallF);
        B.Box2(W, new Vector3(0, H + 0.4f, (zs + ze) / 2), new Vector3(7, 0.8f, 7), Mats.RoofNew, Mats.RoofOld);
        // upper floor inside (z -29 .. -33)
        B.Box2(W, new Vector3(0, 2.1f, -31f), new Vector3(5.6f, 0.2f, 3.6f), Mats.WoodPlank, Mats.WoodOld);

        // clock face, stopped
        var face = new Vector3(0, 5.8f, zs + 0.22f);
        foreach (var era in new[] { Era.Past, Era.Future })
        {
            var disc = new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 1.3f, BottomRadius = 1.3f, Height = 0.06f },
                MaterialOverride = era == Era.Past ? Mats.Emissive(new Color(1f, 0.92f, 0.75f), 0.8f) : Mats.Flat(new Color(0.6f, 0.6f, 0.55f)),
                Layers = L.Vis(era), Position = face, RotationDegrees = new Vector3(90, 0, 0),
            };
            W.AddChild(disc);
            B.Slab(W, era, face + new Vector3(0, 0.35f, 0.05f), new Vector3(0.07f, 0.8f, 0.03f), Mats.Flat(UiKit.Ink), new Vector3(0, 0, 0), false);
            B.Slab(W, era, face + new Vector3(0.3f, 0.12f, 0.06f), new Vector3(0.65f, 0.06f, 0.03f), Mats.Flat(UiKit.Ink), new Vector3(0, 0, 25), false);
        }

        // PAST: closed door that opens from the time echo; clockwork inside
        _pastTowerDoor = new Node3D { Position = new Vector3(-0.8f, 0, zs) };
        W.AddChild(_pastTowerDoor);
        B.Box(_pastTowerDoor, Era.Past, new Vector3(0.8f, 0.85f, 0), new Vector3(1.6f, 1.7f, 0.15f), Mats.WoodPlank);
        Interactable.Create(W, new Vector3(0, 0.5f, zs + 0.5f), Era.Past, "Толкнуть дверь башни", c =>
        {
            if (!_leverPulled) _ = View.Narrate("Заперто изнутри. Наверху, в будущем, что-то тихо светится…", 3.5f);
        }, 0.9f, false);
        var clock = new Node3D { Position = new Vector3(0, 0, -30.2f) };
        W.AddChild(clock);
        B.Box(clock, Era.Past, new Vector3(0, 0.4f, 0), new Vector3(1.2f, 0.8f, 1.2f), Mats.WoodOld);
        var big = new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.6f, BottomRadius = 0.6f, Height = 0.12f, RadialSegments = 16 }, MaterialOverride = Mats.Metal, Layers = L.VisPast, Position = new Vector3(0, 1.3f, 0), RotationDegrees = new Vector3(90, 0, 0) };
        clock.AddChild(big);
        B.GlowDisc(clock, new Vector3(0, 0, 0.9f), new Color(1f, 0.85f, 0.55f), 0.55f, L.VisPast);
        B.Lamp(clock, Era.Past, new Vector3(0, 2f, 1f), new Color(1f, 0.8f, 0.5f), 1.4f, 5f);
        var clockIt = Interactable.Create(clock, new Vector3(0, 0.4f, 0.9f), Era.Past, "Коснуться часового механизма", c => TouchClock(big), 1.0f, false);

        // FUTURE: door choked with rubble; roots climb to the east window; glowing echo lever upstairs
        B.Box(W, Era.Future, new Vector3(0, 0.8f, zs + 0.2f), new Vector3(1.8f, 1.6f, 0.8f), Mats.Rubble, 5);
        B.Box(W, Era.Future, new Vector3(0.4f, 0.3f, zs + 0.8f), new Vector3(1.0f, 0.6f, 0.8f), Mats.Rubble, -15);
        B.Box(W, Era.Future, new Vector3(5.3f, 0.375f, -24.8f), new Vector3(1.1f, 0.75f, 1.1f), Mats.Roots, 12);
        B.Box(W, Era.Future, new Vector3(5.3f, 0.75f, -26.4f), new Vector3(1.0f, 1.5f, 1.0f), Mats.Roots, -8);
        B.Box(W, Era.Future, new Vector3(4.3f, 1.1f, -28.2f), new Vector3(1.2f, 2.2f, 1.2f), Mats.Roots, 20);
        B.Box(W, Era.Future, new Vector3(3.6f, 2.15f, -29.6f), new Vector3(1.0f, 0.1f, 1.4f), Mats.Roots, 0);
        B.Prop(W, Era.Future, "TwistedTree_5", new Vector3(6.2f, 0, -27.5f), 70, 1.4f, 0.5f);
        B.Prop(W, Era.Future, "CommonTree_2", new Vector3(-6.0f, 0, -30.5f), 10, 1.2f, 0.4f);
        var lever = new Node3D { Position = new Vector3(0, 2.2f, -31.8f) };
        W.AddChild(lever);
        B.Box(lever, Era.Future, new Vector3(0, 0.25f, 0), new Vector3(0.3f, 0.5f, 0.3f), Mats.Metal);
        var handle = B.Box(lever, Era.Future, new Vector3(0, 0.6f, 0), new Vector3(0.06f, 0.5f, 0.06f), Mats.Neon, 0, false);
        handle.RotationDegrees = new Vector3(30, 0, 0);
        B.GlowDisc(lever, new Vector3(0, 0, 0.6f), new Color(0.6f, 0.85f, 1f), 0.5f, L.VisFuture);
        B.Lamp(lever, Era.Future, new Vector3(0, 1f, 0.6f), new Color(0.5f, 0.75f, 1f), 1.5f, 4f);
        Interactable.Create(lever, new Vector3(0, 0.3f, 0.5f), Era.Future, "Потянуть светящийся рычаг", c => PullLever(handle), 0.9f);
        Checkpoint(Era.Future, new Vector3(0, 2.2f, -31f), 1.8f, new Vector3(1.5f, 2.25f, -30.5f), Mathf.Pi / 2);
    }

    async void PullLever(Node3D handle)
    {
        _leverPulled = true;
        Audio.Sfx("latch", -2);
        B.Move(handle, null, new Vector3(-30, 0, 0), 0.4f);
        Haku.ShowGhost(4); Sani.ShowGhost(4);
        B.Ripple(W, Era.Past, new Vector3(0, 0, -27.5f), 1.6f);
        await Wait(0.8f);
        B.Move(_pastTowerDoor, null, new Vector3(0, 105, 0), 1.3f);
        foreach (var c in _pastTowerDoor.GetChildren()) if (c is Node3D n) B.Disable(n);
        _ = View.Narrate("Светящийся рычаг откликается сквозь время — дверь башни в прошлом открывается.", 3.5f);
        Hint(Era.Past, "Дверь башни открыта! Коснись часового механизма");
        Hint(Era.Future, "Дождись, пока Хаку доберётся до механизма");
    }

    async void TouchClock(Node3D gear)
    {
        if (_done) return;
        if (!_leverPulled) return;
        _done = true;
        Running = false;
        SetControl(false);
        var t = gear.CreateTween();
        t.TweenProperty(gear, "rotation:y", Mathf.Tau, 3f).AsRelative();
        Haku.SetGhostAlways(true);
        Sani.SetGhostAlways(true);
        Purr.Play(this, 4.5f);
        await View.Narrate("Когда Хаку касается механизма, Сани впервые слышит его голос — тихое мурлыканье сквозь время.", 4.5f);
        await View.Narrate("Их связь становится сильнее, когда они приближаются к дому.", 3.5f);
        Complete();
    }

    void BuildCity()
    {
        // PAST: the city is alive — intact houses, lit windows, lamps
        var housesP = new (Vector3 p, Vector3 s, float r)[]
        {
            (new Vector3(-9.5f, 0, 3), new Vector3(4, 3.4f, 4), 90), (new Vector3(9.5f, 0, 2.5f), new Vector3(4, 3.4f, 4.5f), -90),
            (new Vector3(-9.5f, 0, -24), new Vector3(4, 3.4f, 6), 90), (new Vector3(9.5f, 0, -32), new Vector3(4, 3.4f, 4), -90),
            (new Vector3(-9.5f, 0, -31.5f), new Vector3(4, 3.4f, 5), 90),
        };
        foreach (var (p, s, r) in housesP)
        {
            B.House(W, Era.Past, p, s, Mats.PlasterNew, Mats.RoofNew, Mats.WindowLit, r, 3, 2);
            // FUTURE: same houses, broken down and overgrown
            B.House(W, Era.Future, p, new Vector3(s.X, s.Y * 0.55f, s.Z), Mats.PlasterOld, Mats.RoofOld, Mats.WindowDark, r, 3, 1);
            B.Prop(W, Era.Future, "CommonTree_5", p + new Vector3(0, s.Y * 0.4f, 0), r, 0.9f, 0);
        }
        foreach (var lp in new[] { new Vector3(-4, 0, -2), new Vector3(4, 0, -2), new Vector3(-4, 0, -22), new Vector3(4, 0, -22) })
        {
            B.Cylinder(W, Era.Past, lp + Vector3.Up * 1.1f, 0.05f, 2.2f, Mats.Metal);
            B.Box(W, Era.Past, lp + Vector3.Up * 2.25f, new Vector3(0.25f, 0.2f, 0.25f), Mats.WindowLit, 0, false);
            B.Lamp(W, Era.Past, lp + Vector3.Up * 2.1f, new Color(1f, 0.8f, 0.5f), 1.5f, 6f);
            B.Cylinder(W, Era.Future, lp + new Vector3(0.3f, 0.1f, 0), 0.05f, 2.2f, Mats.Metal, false).RotationDegrees = new Vector3(0, 0, 85);
        }
        Skyline(Era.Past, new Rect2(-13, -35, 26, 42), false, 31, 1f);
        Skyline(Era.Future, new Rect2(-13, -35, 26, 42), false, 31, 1f);
        // lively past: wagon, crates, fences; the future keeps only rotten crates
        Kit.Med(W, Era.Past, "Prop_Wagon", new Vector3(-7, 0, -3.5f), 75, 1, null, true);
        foreach (var (x, z, r) in new[] { (6.5f, 4.5f, 10f), (7.2f, 4.0f, 40f), (-6.5f, -28f, 0f), (7f, -21f, 25f) })
        {
            Kit.Med(W, Era.Past, "Prop_Crate", new Vector3(x, 0, z), r, 0.75f, null, true);
            Kit.Med(W, Era.Future, "Prop_Crate", new Vector3(x, 0, z), r + 8, 0.75f, Kit.RuinTint, true).RotationDegrees = new Vector3(8, r + 8, 12);
        }
        for (int i = 0; i < 5; i++)
            Kit.Med(W, Era.Past, "Prop_MetalFence_Ornament", new Vector3(-11 + i * 2, 0, -6.7f), 0, 0.35f, null, true);
        for (int i = 0; i < 5; i++)
            Kit.Med(W, Era.Past, "Prop_MetalFence_Ornament", new Vector3(3 + i * 2, 0, -6.7f), 0, 0.35f, null, true);
        for (float z = 5; z > -34; z -= 3.2f)
            foreach (var x in new[] { -12.7f, 12.7f })
                Kit.Med(W, Era.Future, "Prop_Vine9", new Vector3(x, 3.2f, z), x < 0 ? 90 : -90, 1f, Kit.RuinTint);
        B.Scatter(W, Era.Future, new Rect2(-12, -34, 24, 20), 140, 61);
        B.Scatter(W, Era.Future, new Rect2(-12, -6, 24, 12), 70, 62);
        B.Prop(W, Era.Future, "CommonTree_3", new Vector3(-7f, 0, -15f), 40, 1.3f, 0.4f);
        B.Prop(W, Era.Future, "Pine_3", new Vector3(8f, 0, 4.5f), 0, 0.9f, 0.3f);
        B.Prop(W, Era.Past, "Bush_Common_Flowers", new Vector3(-6f, 0, 4f), 0, 0.9f);
        B.Prop(W, Era.Past, "Bush_Common_Flowers", new Vector3(6f, 0, -24f), 90, 0.9f);
        B.Motes(W, Era.Past, new Vector3(0, 1.5f, -14), new Vector3(12, 1.5f, 20), new Color(1f, 0.85f, 0.5f), 160);
        B.Motes(W, Era.Future, new Vector3(0, 1.5f, -14), new Vector3(12, 1.5f, 20), new Color(0.6f, 0.8f, 1f), 160);
    }

    protected override async Task Intro()
    {
        await View.Title(Game.ChapterTitles[3], "", 2f);
        await View.Narrate("В прошлом город полон жизни: работают механизмы, вода течёт по каналам.", 3.8f);
        await View.Narrate("В будущем всё остановилось. Мосты обрушились, каналы высохли.", 3.5f);
        Hint(Era.Past, "Хаку: канал не переплыть. Найди механизм на берегу");
        Hint(Era.Future, "Сани: спустись в высохший канал и найди путь наверх");
    }

    protected override void Tick(float dt)
    {
        if (_raftOn && !_gateOpen && Haku.GlobalPosition.Z < -14f && Haku.Focus == null)
            Hint(Era.Past, "Ворота заперты. Может, ящик прижмёт плиту механизма?");
    }
}
