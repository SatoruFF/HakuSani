using System.Threading.Tasks;
using Godot;

namespace HakuSani.Levels;

/// <summary>
/// Глава IV. На разных сторонах времени.
/// A long wooden corridor with a garden window. Haku clears rubble (each pile he clears in the past
/// is gone in Sani's future). Then two gates: Sani holds a glowing plate to keep Haku's gate open,
/// Haku holds his plate to keep Sani's gate open. Finally both touch the familiar door together.
/// </summary>
public partial class Chapter4 : LevelBase
{
    protected override int Chapter => 4;
    protected override string StepSound(Cat c) => "step_wood";
    protected override float FogPast => 0.008f;
    protected override float FogFuture => 0.02f;
    protected override (Vector3 pos, float yaw) StartHaku => (new Vector3(-0.5f, 0, 1.5f), 0);
    protected override (Vector3 pos, float yaw) StartSani => (new Vector3(0.5f, 0, 1.5f), 0);

    const float HalfW = 1.6f, Len = 32f, WallH = 2.6f;
    readonly Node3D[] _pastPiles = new Node3D[3], _futurePiles = new Node3D[3];
    int _cleared;
    Node3D _gateP, _gateF;
    PressurePlate _plateS, _plateH;
    float _touchHaku = -99, _touchSani = -99, _time;
    bool _finished;

    protected override void BuildWorld()
    {
        // floors: past intact; future has holes near the start
        B.Box(W, Era.Past, new Vector3(0, -0.25f, -Len / 2 + 2), new Vector3(HalfW * 2 + 1, 0.5f, Len + 4), Mats.WoodPlank);
        float[] futureFloor = { 4f, -2.2f, -3.2f, -4.4f, -5.4f, -Len - 2 };
        for (int i = 0; i < futureFloor.Length - 1; i += 2)
        {
            float a = futureFloor[i], b = futureFloor[i + 1];
            B.Box(W, Era.Future, new Vector3(0, -0.25f, (a + b) / 2), new Vector3(HalfW * 2 + 1, 0.5f, a - b), Mats.DustFloor);
        }
        Checkpoint(Era.Future, new Vector3(0, 0, -6.5f), 1.5f, new Vector3(0, 0, -6.5f), 0);

        // walls (east wall has windows to the garden)
        Wall(new Vector3(-HalfW - 0.15f, 0, 4), new Vector3(-HalfW - 0.15f, 0, -Len), WallH, 0.3f, Mats.PlasterNew, Mats.PlasterOld);
        B.Box2(W, new Vector3(-HalfW + 0.02f, 0.45f, -Len / 2 + 2), new Vector3(0.06f, 0.9f, Len + 4), Mats.WoodOld, Mats.WoodOld, 0, false);
        for (float z = 4; z > -Len; z -= 4)
        {
            Wall(new Vector3(HalfW + 0.15f, 0, z), new Vector3(HalfW + 0.15f, 0, z - 1.6f), WallH, 0.3f, Mats.PlasterNew, Mats.PlasterOld);
            B.Box2(W, new Vector3(HalfW + 0.15f, 0.45f, z - 2.8f), new Vector3(0.3f, 0.9f, 2.4f), Mats.WoodOld, Mats.WoodOld);
            B.Box2(W, new Vector3(HalfW + 0.15f, WallH - 0.3f, z - 2.8f), new Vector3(0.3f, 0.6f, 2.4f), Mats.WoodOld, Mats.WoodOld);
            B.Box(W, Era.Past, new Vector3(HalfW + 0.15f, 1.4f, z - 2.8f), new Vector3(0.3f, 1.1f, 2.4f), Mats.WindowLit, 0, true).GetChild<MeshInstance3D>(1).Visible = false;
            B.Box(W, Era.Future, new Vector3(HalfW + 0.15f, 1.4f, z - 2.8f), new Vector3(0.3f, 1.1f, 2.4f), Mats.WindowDark, 0, true).GetChild<MeshInstance3D>(1).Visible = false;
            B.Lamp(W, Era.Past, new Vector3(0, WallH - 0.3f, z - 2f), new Color(1f, 0.8f, 0.55f), 1.2f, 5f);
            B.Lamp(W, Era.Future, new Vector3(0.8f, 2.0f, z - 2.8f), new Color(0.55f, 0.7f, 1f), 1.4f, 4.5f);
        }
        Wall(new Vector3(-HalfW, 0, 4.2f), new Vector3(HalfW, 0, 4.2f), WallH, 0.3f, Mats.WoodOld, Mats.WoodOld);
        // ceiling: intact in the past, partly collapsed in the future (sky shows through)
        B.Box(W, Era.Past, new Vector3(0, WallH + 0.1f, -Len / 2 + 2), new Vector3(HalfW * 2 + 0.6f, 0.2f, Len + 4), Mats.PlasterNew, 0, false);
        for (float bz = 3; bz > -Len; bz -= 2.5f) B.Box(W, Era.Past, new Vector3(0, WallH - 0.05f, bz), new Vector3(HalfW * 2, 0.15f, 0.18f), Mats.WoodOld, 0, false);
        B.Box(W, Era.Future, new Vector3(0, WallH + 0.1f, -Len * 0.75f), new Vector3(HalfW * 2 + 0.6f, 0.2f, Len * 0.5f), Mats.WoodOld, 0, false);
        B.Slab(W, Era.Future, new Vector3(0.6f, 0.5f, -9f), new Vector3(0.25f, 0.25f, 3f), Mats.WoodOld, new Vector3(18, 12, 0), false);

        BuildGarden();
        BuildRubble();
        BuildGates();
        BuildDoor();
        B.Motes(W, Era.Past, new Vector3(0, 1.3f, -Len / 2), new Vector3(HalfW, 1.2f, Len / 2), new Color(1f, 0.85f, 0.55f), 140);
        B.Motes(W, Era.Future, new Vector3(0, 1.3f, -Len / 2), new Vector3(HalfW, 1.2f, Len / 2), new Color(0.7f, 0.8f, 1f), 160);
    }

    void BuildGarden()
    {
        B.Box(W, Era.Past, new Vector3(9, -0.1f, -14), new Vector3(14, 0.2f, 40), Mats.GrassPast, 0, false);
        B.Box(W, Era.Future, new Vector3(9, -0.1f, -14), new Vector3(14, 0.2f, 40), Mats.DirtPast, 0, false);
        for (int i = 0; i < 6; i++)
        {
            var p = new Vector3(5 + (i % 2) * 4, 0, 2 - i * 6);
            B.Prop(W, Era.Past, i % 2 == 0 ? "CommonTree_2" : "Bush_Common_Flowers", p, i * 50, i % 2 == 0 ? 0.8f : 1.2f);
            B.Prop(W, Era.Future, i % 3 == 0 ? "DeadTree_5" : "TwistedTree_1", p, i * 70, 1.0f);
        }
        B.House(W, Era.Past, new Vector3(10.5f, 0, -13), new Vector3(6, 3, 6), Mats.PlasterNew, Mats.RoofNew, Mats.WindowLit, -90, 3, 2);
        B.House(W, Era.Future, new Vector3(10.5f, 0, -13), new Vector3(6, 3, 6), Mats.PlasterOld, Mats.RoofOld, Mats.WindowDark, -90, 3, 2);
        for (int i = 0; i < 8; i++)
            Kit.Med(W, Era.Past, "Prop_WoodenFence_Single", new Vector3(13.5f, 0, 3 - i * 2.1f), 90);
        B.Scatter(W, Era.Past, new Rect2(3, -32, 10, 36), 120, 71, new[] { "Flower_3_Group", "Flower_4_Group", "Grass_Common_Short", "Clover_1" });
        B.Scatter(W, Era.Future, new Rect2(3, -32, 10, 36), 120, 72, new[] { "Grass_Wispy_Tall", "Fern_1", "Plant_7" });
    }

    void BuildRubble()
    {
        float z = -7.5f;
        for (int i = 0; i < 3; i++)
        {
            float x = -HalfW + (i + 0.5f) * (HalfW * 2 / 3);
            int idx = i;
            var p = new Node3D { Position = new Vector3(x, 0, z) };
            W.AddChild(p);
            B.Box(p, Era.Past, new Vector3(0, 0.6f, 0), new Vector3(1.05f, 1.2f, 0.9f), Mats.Rubble, 8 * (i - 1));
            B.Slab(p, Era.Past, new Vector3(0.1f, 1.25f, 0.1f), new Vector3(1.1f, 0.12f, 0.15f), Mats.WoodOld, new Vector3(0, 30, 15), false);
            _pastPiles[i] = p;
            var f = new Node3D { Position = new Vector3(x, 0, z) };
            W.AddChild(f);
            B.Box(f, Era.Future, new Vector3(0, 0.75f, 0), new Vector3(1.07f, 1.5f, 1.1f), Mats.MossyStone, -6 * (i - 1));
            _futurePiles[i] = f;
            Interactable.Create(W, new Vector3(x, 0.4f, z + 0.6f), Era.Past, "Разобрать завал", c => ClearPile(idx), 0.8f);
        }
        Interactable.Create(W, new Vector3(0, 0.4f, z + 0.7f), Era.Future, "Осмотреть завал", c =>
        {
            if (_cleared == 0) _ = View.Narrate("Обломки срослись с мхом — здесь не пройти. Если бы кто-то разобрал их давно…", 3.5f);
        }, 1.2f, false);
    }

    async void ClearPile(int i)
    {
        var p = _pastPiles[i];
        foreach (var c in p.GetChildren()) if (c is Node3D n) B.Disable(n);
        B.Move(p, p.Position + new Vector3(0, -1.45f, 0.3f), new Vector3(0, 20, 0), 0.7f, Tween.TransitionType.Back, Tween.EaseType.In);
        _cleared++;
        Audio.Sfx("stone", -2);
        await Wait(0.9f);
        var f = _futurePiles[i];
        B.Shake(f, 0.03f, 0.4f);
        await Wait(0.4f);
        B.Move(f, f.Position + Vector3.Down * 1.6f, null, 0.8f);
        foreach (var c in f.GetChildren()) if (c is Node3D n) B.Disable(n);
        if (_cleared == 1)
        {
            Hint(Era.Future, "В завале появился проход!");
            Hint(Era.Past, "Расчисти себе путь и иди дальше");
        }
    }

    void BuildGates()
    {
        // Gate P (past) at z=-14 — held open by Sani's glowing plate (future, z=-12)
        _gateP = MakeGate(Era.Past, -14f, Mats.WoodPlank);
        // Gate F (future) at z=-18 — held open by Haku's plate (past, z=-16)
        _gateF = MakeGate(Era.Future, -18f, Mats.Metal);
        _plateS = PressurePlate.Create(W, Era.Future, new Vector3(0.9f, 0, -12f), new Color(0.45f, 0.75f, 1f), () => new Node3D[] { Sani }, 0.4f);
        _plateH = PressurePlate.Create(W, Era.Past, new Vector3(-0.9f, 0, -16f), new Color(1f, 0.6f, 0.75f), () => new Node3D[] { Haku }, 0.4f);
        B.GlowDisc(W, new Vector3(0.9f, 0, -12f), new Color(0.45f, 0.75f, 1f), 0.6f, L.VisFuture);
        B.GlowDisc(W, new Vector3(-0.9f, 0, -16f), new Color(1f, 0.6f, 0.75f), 0.6f, L.VisPast);
        _plateS.Changed = on => SetGate(_gateP, on);
        _plateH.Changed = on => SetGate(_gateF, on);
        Checkpoint(Era.Past, new Vector3(0, 0, -15.5f), 1f, new Vector3(0, 0, -15.3f), 0);
        Checkpoint(Era.Both, new Vector3(0, 0, -20f), 1.5f, new Vector3(0, 0, -20f), 0);
    }

    Node3D MakeGate(Era era, float z, Material mat)
    {
        var g = new Node3D { Position = new Vector3(0, 0, z) };
        W.AddChild(g);
        for (float x = -HalfW + 0.15f; x < HalfW; x += 0.28f)
            B.Box(g, era, new Vector3(x, WallH / 2, 0), new Vector3(0.08f, WallH, 0.08f), mat, 0, false);
        B.Box(g, era, new Vector3(0, WallH / 2, 0), new Vector3(HalfW * 2, WallH, 0.12f), mat).GetChild<MeshInstance3D>(1).Visible = false;
        B.Box(g, era, new Vector3(0, 0.15f, 0), new Vector3(HalfW * 2, 0.08f, 0.1f), mat, 0, false);
        return g;
    }

    void SetGate(Node3D g, bool open)
    {
        Audio.Sfx("metal", -8);
        var target = new Vector3(0, open ? WallH - 0.15f : 0, g.Position.Z);
        var t = g.CreateTween();
        t.TweenProperty(g, "position", target, open ? 0.7f : 0.5f).SetTrans(Tween.TransitionType.Sine);
    }

    void BuildDoor()
    {
        float z = -28f;
        Wall(new Vector3(-HalfW, 0, z), new Vector3(-0.55f, 0, z), WallH, 0.3f, Mats.WoodOld, Mats.WoodOld);
        Wall(new Vector3(0.55f, 0, z), new Vector3(HalfW, 0, z), WallH, 0.3f, Mats.WoodOld, Mats.WoodOld);
        B.Box2(W, new Vector3(0, 2.25f, z), new Vector3(1.1f, 0.7f, 0.3f), Mats.WoodOld, Mats.WoodOld);
        B.Box(W, Era.Past, new Vector3(0, 0.95f, z), new Vector3(1.1f, 1.9f, 0.1f), Mats.Flat(new Color(0.85f, 0.82f, 0.75f)));
        B.Box(W, Era.Future, new Vector3(0, 0.95f, z), new Vector3(1.1f, 1.9f, 0.1f), Mats.Flat(new Color(0.45f, 0.42f, 0.38f)));
        foreach (var era in new[] { Era.Past, Era.Future })
            B.Box(W, era, new Vector3(0.38f, 0.95f, z + 0.08f), new Vector3(0.12f, 0.05f, 0.05f), Mats.Metal, 0, false);
        B.Lamp(W, Era.Past, new Vector3(0, 1.4f, z + 1.2f), new Color(1f, 0.85f, 0.6f), 1.2f, 3f);
        B.Lamp(W, Era.Future, new Vector3(0, 1.4f, z + 1.2f), new Color(0.6f, 0.75f, 1f), 1.2f, 3f);
        B.GlowDisc(W, new Vector3(0, 0, z + 0.7f), new Color(1f, 0.95f, 0.85f), 0.9f);
        Interactable.Create(W, new Vector3(0, 0.4f, z + 0.5f), Era.Past, "Коснуться двери лапой", c => Touch(true), 1.0f, false);
        Interactable.Create(W, new Vector3(0, 0.4f, z + 0.5f), Era.Future, "Коснуться двери лапой", c => Touch(false), 1.0f, false);
    }

    void Touch(bool haku)
    {
        if (haku) _touchHaku = _time; else _touchSani = _time;
        Audio.Sfx("door", -8);
        (haku ? Haku : Sani).ShowGhost(3);
        (haku ? Sani : Haku).ShowGhost(3);
        if (Mathf.Abs(_touchHaku - _touchSani) < 3f) Finish();
        else _ = View.Narrate("Дверь отзывается теплом… но лишь с одной стороны. Нужно коснуться вместе.", 3f);
    }

    async void Finish()
    {
        if (_finished) return;
        _finished = true;
        Running = false;
        SetControl(false);
        await View.Narrate("Впервые за всё путешествие мир не меняется вслед за их действиями. На мгновение всё замирает.", 4.5f);
        Complete();
    }

    protected override async Task Intro()
    {
        Haku.SetGhostAlways(true);
        Sani.SetGhostAlways(true);
        await View.Title(Game.ChapterTitles[4], "", 2f);
        await View.Narrate("Длинный коридор, деревянные стены, окно с видом на сад. Знакомое и чужое одновременно.", 4f);
        Hint(Era.Past, "Хаку: проход завален. Разбери обломки");
        Hint(Era.Future, "Сани: перепрыгни провалы в полу");
    }

    protected override void Tick(float dt)
    {
        _time += dt;
        if (_cleared > 0 && Sani.GlobalPosition.Z < -9f && Sani.GlobalPosition.Z > -13.5f && !_plateS.Pressed && Haku.GlobalPosition.Z > -14f)
        {
            Hint(Era.Future, "Встань на светящуюся плиту — она удержит ворота Хаку");
            Hint(Era.Past, "Ворота откроются, если Сани встанет на свою плиту");
        }
        if (Haku.GlobalPosition.Z < -14.3f && Sani.GlobalPosition.Z > -18f)
        {
            Hint(Era.Past, "Встань на розовую плиту — она держит ворота Сани");
            Hint(Era.Future, "Хаку держит ворота! Беги через них");
        }
        if (Haku.GlobalPosition.Z < -18.5f && Sani.GlobalPosition.Z < -18.5f)
        {
            Hint(Era.Past, "Подойди к двери и коснись её вместе с Сани");
            Hint(Era.Future, "Подойди к двери и коснись её вместе с Хаку");
        }
    }
}
