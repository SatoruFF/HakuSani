using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace HakuSani.Levels;

/// <summary>
/// Глава I. Чужой двор.
/// Yard A: Haku tears loose boards off an old door (past) → the handle-less door in Sani's era rots and falls.
/// Yard B: Haku pushes a crate to climb a ruined wall, then knocks down a young post — decades later
/// the chain wrapped around it is gone and Sani's iron gate swings open.
/// </summary>
public partial class Chapter1 : LevelBase
{
    protected override int Chapter => 1;
    protected override (Vector3 pos, float yaw) StartHaku => (new Vector3(-0.5f, 0, 2.2f), 0);
    protected override (Vector3 pos, float yaw) StartSani => (new Vector3(0.5f, 0, 2.2f), 0);

    int _boards = 3;
    bool _glowTouched, _doorDown, _gateOpen;
    Node3D _futureDoorPivot, _futureGate, _chainPost, _futureChain;
    Node3D _pastDoorCollider;
    Interactable _futureDoorPush;

    protected override void BuildWorld()
    {
        Ground(new Vector3(0, 0, -10), new Vector2(40, 60), Mats.GrassPast, Mats.Paving);

        // ---------------- Yard A walls ----------------
        float h = 3f, t = 0.5f;
        Wall(new Vector3(-7.25f, 0, 4.5f), new Vector3(-7.25f, 0, -22), h, t, Mats.MossyStone, Mats.StoneNew);
        Wall(new Vector3(7.25f, 0, 4.5f), new Vector3(7.25f, 0, -22), h, t, Mats.MossyStone, Mats.StoneNew);
        Wall(new Vector3(-7.5f, 0, 4.25f), new Vector3(7.5f, 0, 4.25f), h, t, Mats.MossyStone, Mats.StoneNew);
        Wall(new Vector3(-7.5f, 0, -8), new Vector3(-0.7f, 0, -8), h, t, Mats.StoneOld, Mats.PlasterNew);
        Wall(new Vector3(0.7f, 0, -8), new Vector3(7.5f, 0, -8), h, t, Mats.StoneOld, Mats.PlasterNew);
        B.Box2(W, new Vector3(0, 2.3f, -8), new Vector3(1.4f, 1.4f, t), Mats.StoneOld, Mats.PlasterNew);
        Wall(new Vector3(-7.5f, 0, -22), new Vector3(7.5f, 0, -22), h, t, Mats.MossyStone, Mats.StoneNew);

        BuildDoor();
        BuildYardADressing();
        BuildYardB();

        // outside the walls: an intact lit city for Sani, overgrown ruins for Haku
        Skyline(Era.Future, new Rect2(-7.5f, -22, 15, 26.5f), true, 3);
        Skyline(Era.Past, new Rect2(-7.5f, -22, 15, 26.5f), false, 4);
        Vines(new Vector3(-7f, 0, 4f), new Vector3(-7f, 0, -21.5f), 3f, 1);
        Vines(new Vector3(7f, 0, -21.5f), new Vector3(7f, 0, 4f), 3f, 2);
        Vines(new Vector3(-7.2f, 0, -7.75f), new Vector3(-0.8f, 0, -7.75f), 3f, 3);

        BuildGroundDetail();
        TimeGlow(new Vector3(0, 0, -1.5f), OnGlow);
        Exit(new Vector3(0, 0, -19.5f), 1.3f, OnExit);
    }

    // ---------------- the door ----------------
    void BuildDoor()
    {
        var z = -8f;
        // PAST: upper panel + side pieces of the lower panel, hole in the middle covered by three boards
        B.Box(W, Era.Past, new Vector3(0, 1.08f, z), new Vector3(1.4f, 1.05f, 0.12f), Mats.WoodOld);
        B.Box(W, Era.Past, new Vector3(-0.5f, 0.28f, z), new Vector3(0.4f, 0.56f, 0.12f), Mats.WoodOld);
        B.Box(W, Era.Past, new Vector3(0.5f, 0.28f, z), new Vector3(0.4f, 0.56f, 0.12f), Mats.WoodOld);
        _pastDoorCollider = B.Box(W, Era.Past, new Vector3(0, 0.28f, z + 0.02f), new Vector3(0.6f, 0.56f, 0.04f), Mats.WindowDark, 0, true);
        _pastDoorCollider.Visible = false; // invisible blocker until the boards are gone
        Interactable.Create(W, new Vector3(0, 0.6f, z + 0.35f), Era.Past, "Толкнуть дверь", c =>
        {
            if (_boards > 0) _ = View.Narrate("Дверь не поддаётся. Но доски внизу расшатаны…", 3f);
        }, 0.9f, false).Animation = "Paw";

        var tilt = new[] { 4f, -6f, 3f };
        for (int i = 0; i < 3; i++)
        {
            var board = B.Box(W, Era.Past, new Vector3(0, 0.1f + i * 0.17f, z + 0.1f), new Vector3(1.25f, 0.12f, 0.05f), Mats.WoodPlank);
            board.RotationDegrees = new Vector3(0, 0, tilt[i]);
            int idx = i;
            Interactable.Create(W, new Vector3(0, 0.1f + i * 0.17f, z + 0.3f), Era.Past, "Сорвать доску", c => TearBoard(board, idx), 0.85f);
        }

        // FUTURE: solid door with no handle, hinged at the bottom so it can fall forward
        _futureDoorPivot = new Node3D { Position = new Vector3(0, 0, z - 0.08f) };
        W.AddChild(_futureDoorPivot);
        B.Box(_futureDoorPivot, Era.Future, new Vector3(0, 0.8f, 0), new Vector3(1.4f, 1.6f, 0.14f), Mats.Metal);
        B.Box(_futureDoorPivot, Era.Future, new Vector3(0, 0.8f, 0.08f), new Vector3(1.1f, 1.3f, 0.02f), Mats.StoneNew, 0, false);
        _futureDoorPush = Interactable.Create(W, new Vector3(0, 0.6f, z + 0.35f), Era.Future, "Толкнуть дверь", c =>
        {
            if (!_doorDown) _ = View.Narrate("Ни ручки, ни замка. С этой стороны её не открыть.", 3f);
        }, 0.9f, false);
    }

    void TearBoard(Node3D board, int idx)
    {
        B.Disable(board);
        var side = idx % 2 == 0 ? 1 : -1;
        var tw = B.Move(board, board.Position + new Vector3(side * 0.6f, -0.05f, 0.55f), new Vector3(90, side * 40, side * 70), 0.6f, Tween.TransitionType.Back, Tween.EaseType.Out);
        _boards--;
        Audio.Sfx("wood", -2);
        B.Puff(W, Era.Past, board.GlobalPosition + new Vector3(0, -0.1f, 0.3f), 0.6f, 14);
        B.Ripple(W, Era.Future, new Vector3(0, 0, -8.4f), 1.3f);
        // future door cracks a bit more with every board
        B.Shake(_futureDoorPivot, 0.02f + (3 - _boards) * 0.015f, 0.5f);
        if (_boards == 0) BoardsGone();
    }

    async void BoardsGone()
    {
        B.Disable(_pastDoorCollider);
        Hint(Era.Past, "Под досками дыра — пролезь в соседний двор");
        await Wait(0.8f);
        B.Shake(_futureDoorPivot, 0.06f, 1.0f);
        await Wait(1.1f);
        var tw = B.Move(_futureDoorPivot, null, new Vector3(-88, 0, 0), 0.9f, Tween.TransitionType.Bounce, Tween.EaseType.Out);
        Audio.Sfx("wood_heavy", 0);
        await Wait(0.35f);
        B.Puff(W, Era.Future, new Vector3(0, 0, -9.0f), 2.2f, 40);
        Sani.Cam.Shake(0.03f, 0.45f);
        _doorDown = true;
        _futureDoorPush.Enabled = false;
        Hint(Era.Future, "Дверь упала! Проход открыт");
        _ = View.Narrate("Впервые коты понимают, что могут влиять на мир друг друга.", 4f);
        await Wait(1.2f);
        foreach (var c in _futureDoorPivot.GetChildren())
            if (c is StaticBody3D sb) { sb.CollisionLayer = L.PhysFuture; }
        Checkpoint(Era.Both, new Vector3(0, 0, -9.5f), 1.2f, new Vector3(0, 0, -9.5f), 0);
    }

    // ---------------- ground detail ----------------
    void BuildGroundDetail()
    {
        var rng = new RandomNumberGenerator { Seed = 1201 };
        // PAST: a trodden dirt path from where they wake to the old door, stepping stones half-sunk in grass
        for (float z = 3.2f; z > -21f; z -= 0.9f)
        {
            if (z < -7.6f && z > -8.4f) continue;
            float x = Mathf.Sin(z * 0.35f) * 0.5f + (z < -15 ? 0.4f : 0);
            B.DirtDecal(W, Era.Past, new Vector3(x, 0, z), rng.RandfRange(1.0f, 1.5f), rng.RandfRange(0, 360));
            if (rng.Randf() < 0.45f)
                B.Prop(W, Era.Past, rng.Randf() < 0.5f ? "RockPath_Round_Small_1" : "RockPath_Round_Small_2", new Vector3(x + rng.RandfRange(-0.3f, 0.3f), 0.0f, z), rng.RandfRange(0, 360), 0.45f, 0, false);
        }
        foreach (var (x, z) in new[] { (-3.5f, 1.8f), (3.2f, -4.6f), (-1.8f, -12.2f), (5.4f, -17.5f), (-5.4f, -16.4f), (2.4f, -20.4f) })
            B.Prop(W, Era.Past, "Pebble_Round_" + rng.RandiRange(1, 5), new Vector3(x, 0, z), rng.RandfRange(0, 360), 0.8f, 0, false);

        // FUTURE: rain puddles that mirror the lit windows and lamps
        var puddle = new StandardMaterial3D { AlbedoColor = new Color(0.08f, 0.1f, 0.16f), Roughness = 0.03f, Metallic = 0.3f, MetallicSpecular = 0.8f };
        foreach (var (x, z, sx, sz) in new[] { (-1.6f, 1.2f, 1.4f, 0.9f), (2.2f, -3.4f, 1.1f, 1.6f), (-3.6f, -5.6f, 0.9f, 0.7f), (1.2f, -11.8f, 1.6f, 1.0f), (-2.8f, -18.2f, 1.2f, 1.4f), (3.4f, -20.4f, 0.9f, 0.8f) })
        {
            var mi = new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0.5f, BottomRadius = 0.5f, Height = 0.004f, RadialSegments = 24 },
                MaterialOverride = puddle, Layers = L.VisFuture, Position = new Vector3(x, 0.004f, z), Scale = new Vector3(sx, 1, sz),
                RotationDegrees = new Vector3(0, x * 37, 0), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            W.AddChild(mi);
        }
    }

    // ---------------- yard A dressing ----------------
    void BuildYardADressing()
    {
        // PAST: abandoned and overgrown
        B.Prop(W, Era.Past, "CommonTree_3", new Vector3(-5.2f, 0, 2.6f), 30, 0.9f, 0.3f);
        B.Prop(W, Era.Past, "TwistedTree_2", new Vector3(5.4f, 0, -5.8f), 120, 0.8f, 0.3f);
        B.Prop(W, Era.Past, "DeadTree_3", new Vector3(4.8f, 0, 2.8f), 200, 0.7f, 0.25f);
        B.Prop(W, Era.Past, "Bush_Common", new Vector3(-6.2f, 0, -6.6f), 0, 1.1f);
        B.Prop(W, Era.Past, "Bush_Common_Flowers", new Vector3(6.3f, 0, 0.4f), 80, 1f);
        B.Prop(W, Era.Past, "Rock_Medium_2", new Vector3(-3.4f, 0, -6.4f), 40, 0.7f);
        B.Prop(W, Era.Past, "Plant_7_Big", new Vector3(-1.6f, 0, -7.4f), 10, 0.8f);
        B.Prop(W, Era.Past, "Fern_1", new Vector3(1.5f, 0, -7.3f), 60, 1.2f);
        B.Scatter(W, Era.Past, new Rect2(-7, -8, 14, 12), 140, 11);
        B.House(W, Era.Past, new Vector3(-5.2f, 0, -3.2f), new Vector3(3.2f, 2.6f, 3.4f), Mats.PlasterOld, Mats.RoofOld, Mats.WindowDark, 90, 2, 1);
        // abandoned wagon, crates, broken fence
        Kit.Med(W, Era.Past, "Prop_Wagon", new Vector3(4.3f, 0, -1.2f), 200, 1, Kit.RuinTint, true).RotationDegrees = new Vector3(0, 200, -6);
        Kit.Med(W, Era.Past, "Prop_Crate", new Vector3(-2.6f, 0, -6.6f), 15, 0.75f, Kit.RuinTint, true);
        Kit.Med(W, Era.Past, "Prop_Crate", new Vector3(-2.1f, 0.81f, -6.7f), 40, 0.6f, Kit.RuinTint, true);
        for (int i = 0; i < 3; i++)
            Kit.Med(W, Era.Past, "Prop_WoodenFence_Single", new Vector3(-6.6f + i * 0.4f, 0, 0.8f + i * 2f), 90 + i * 12, 1, Kit.RuinTint, true).RotationDegrees += new Vector3(0, 0, i == 1 ? 25 : 0);
        B.Motes(W, Era.Past, new Vector3(0, 1.5f, -2), new Vector3(7, 1.5f, 6), new Color(1f, 0.85f, 0.5f), 160);

        // FUTURE: intact, lit, eerily empty
        B.House(W, Era.Future, new Vector3(-5.2f, 0, -3.2f), new Vector3(3.2f, 3.6f, 3.4f), Mats.PlasterNew, Mats.RoofNew, Mats.WindowLit, 90, 2, 2);
        B.Prop(W, Era.Future, "CommonTree_1", new Vector3(5.4f, 0, -5.8f), 0, 0.45f, 0.15f);
        B.Box(W, Era.Future, new Vector3(5.4f, 0.2f, -5.8f), new Vector3(1.2f, 0.4f, 1.2f), Mats.StoneNew);
        for (int i = 0; i < 2; i++)
        {
            var x = i == 0 ? -2.8f : 2.8f;
            B.Cylinder(W, Era.Future, new Vector3(x, 1.1f, -6.8f), 0.05f, 2.2f, Mats.Metal);
            B.Box(W, Era.Future, new Vector3(x, 2.25f, -6.8f), new Vector3(0.25f, 0.2f, 0.25f), Mats.WindowLit, 0, false);
            B.Lamp(W, Era.Future, new Vector3(x, 2.1f, -6.6f), new Color(1f, 0.8f, 0.55f), 2.5f, 7f, false);
        }
        B.Box(W, Era.Future, new Vector3(4.2f, 0.22f, 1.5f), new Vector3(1.6f, 0.06f, 0.5f), Mats.WoodPlank);
        B.Box(W, Era.Future, new Vector3(4.2f, 0.1f, 1.5f), new Vector3(1.4f, 0.2f, 0.1f), Mats.Metal, 0, false);
        // tidy planters and bollards
        foreach (var (x, z) in new[] { (-5.8f, 2.4f), (5.8f, -3.2f), (-2.2f, -6.9f), (2.2f, -6.9f) })
        {
            Kit.City(W, Era.Future, "Prop_Planter_Single", new Vector3(x, 0, z), 0, 0.6f, null, true);
            B.Prop(W, Era.Future, "Bush_Common_Flowers", new Vector3(x, 0.36f, z), x * 40, 0.55f);
        }
        foreach (var x in new[] { -1.1f, 1.1f })
            Kit.City(W, Era.Future, "Prop_Bollard", new Vector3(x, 0, -7.0f), 0, 0.8f, null, true);
        B.Motes(W, Era.Future, new Vector3(0, 1.5f, -2), new Vector3(7, 1.5f, 6), new Color(0.55f, 0.75f, 1f), 100);
    }

    // ---------------- yard B ----------------
    void BuildYardB()
    {
        // PAST: ruined wall across the yard (too high to jump), crate to climb it, young post
        Wall(new Vector3(-7, 0, -15), new Vector3(7, 0, -15), 1.5f, 0.4f, Mats.StoneOld, null, Era.Past);
        var crate = PushBlock.Create(W, Era.Past, new Vector3(2.5f, 0.4f, -11.4f), new Vector3(0.8f, 0.8f, 0.8f), Mats.WoodPlank);
        B.Prop(W, Era.Past, "CommonTree_4", new Vector3(-5.6f, 0, -11), 50, 0.85f, 0.3f);
        B.Prop(W, Era.Past, "TwistedTree_4", new Vector3(5.5f, 0, -19.5f), 160, 0.8f, 0.3f);
        B.Prop(W, Era.Past, "Rock_Medium_1", new Vector3(-4.5f, 0, -13.8f), 0, 0.6f);
        B.Scatter(W, Era.Past, new Rect2(-7, -22, 14, 14), 130, 21);
        Checkpoint(Era.Past, new Vector3(0, 0, -16.5f), 2f, new Vector3(0, 0, -16.5f), 0);

        // the post: in the past a young wooden post Haku can knock over
        _chainPost = B.Box(W, Era.Past, new Vector3(1.6f, 0.45f, -16.2f), new Vector3(0.14f, 0.9f, 0.14f), Mats.WoodOld);
        Interactable.Create(W, new Vector3(1.6f, 0.3f, -16.2f), Era.Past, "Повалить столбик", c => KnockPost(), 0.8f);

        // FUTURE: iron fence across the yard; the gate is chained to a heavy post
        for (float x = -7f; x <= 7.01f; x += 0.25f)
        {
            if (Mathf.Abs(x) < 0.85f) continue;
            B.Cylinder(W, Era.Future, new Vector3(x, 1.1f, -15), 0.025f, 2.2f, Mats.Metal, false);
        }
        B.Box(W, Era.Future, new Vector3(-3.9f, 1.1f, -15), new Vector3(6.2f, 2.2f, 0.1f), Mats.Metal, 0, true).Visible = false;
        B.Box(W, Era.Future, new Vector3(3.9f, 1.1f, -15), new Vector3(6.2f, 2.2f, 0.1f), Mats.Metal, 0, true).Visible = false;
        B.Box(W, Era.Future, new Vector3(0, 2.15f, -15), new Vector3(14f, 0.08f, 0.08f), Mats.Metal, 0, false);
        B.Box(W, Era.Future, new Vector3(0, 0.15f, -15), new Vector3(14f, 0.08f, 0.08f), Mats.Metal, 0, false);
        _futureGate = new Node3D { Position = new Vector3(-0.8f, 0, -15) };
        W.AddChild(_futureGate);
        for (float x = 0.1f; x < 1.6f; x += 0.2f)
            B.Cylinder(_futureGate, Era.Future, new Vector3(x, 1f, 0), 0.03f, 2f, Mats.Metal, false);
        B.Box(_futureGate, Era.Future, new Vector3(0.8f, 1f, 0), new Vector3(1.6f, 2f, 0.08f), Mats.Metal).GetChild<MeshInstance3D>(1).Visible = false;
        B.Box(_futureGate, Era.Future, new Vector3(0.8f, 1.9f, 0), new Vector3(1.6f, 0.08f, 0.08f), Mats.Metal, 0, false);
        B.Box(_futureGate, Era.Future, new Vector3(0.8f, 0.2f, 0), new Vector3(1.6f, 0.08f, 0.08f), Mats.Metal, 0, false);
        // the stone post with the chain, standing where Haku's young post is
        var post = B.Box(W, Era.Future, new Vector3(1.6f, 0.55f, -16.2f), new Vector3(0.35f, 1.1f, 0.35f), Mats.StoneNew);
        _futureChain = new Node3D();
        W.AddChild(_futureChain);
        for (int i = 0; i < 8; i++)
        {
            var pz = Mathf.Lerp(-15.05f, -16.05f, i / 7f);
            var px = Mathf.Lerp(0.8f, 1.5f, i / 7f);
            B.Box(_futureChain, Era.Future, new Vector3(px, 0.9f + 0.05f * (i % 2), pz), new Vector3(0.08f, 0.05f, 0.12f), Mats.Metal, 30 * (i % 2), false);
        }
        Interactable.Create(W, new Vector3(0, 0.6f, -14.7f), Era.Future, "Толкнуть калитку", c =>
        {
            if (!_gateOpen) _ = View.Narrate("Калитка прикована цепью к каменному столбу.", 3f);
        }, 0.9f, false);
        _postFuture = post;

        B.House(W, Era.Future, new Vector3(5.2f, 0, -19.2f), new Vector3(3.2f, 3.8f, 3f), Mats.PlasterNew, Mats.RoofNew, Mats.WindowLit, -90, 2, 2);
        B.House(W, Era.Past, new Vector3(-5.2f, 0, -19.2f), new Vector3(3.2f, 2.2f, 3f), Mats.PlasterOld, Mats.RoofOld, Mats.WindowDark, 90, 2, 1);
        B.House(W, Era.Future, new Vector3(-5.2f, 0, -19.2f), new Vector3(3.2f, 3.8f, 3f), Mats.PlasterNew, Mats.RoofNew, Mats.WindowLit, 90, 2, 2);
        B.Box(W, Era.Future, new Vector3(0, 0.02f, -11.5f), new Vector3(1.2f, 0.04f, 6f), Mats.StoneNew, 0, false);
        Kit.Med(W, Era.Past, "Prop_Crate", new Vector3(-3.5f, 0, -12.8f), 30, 0.7f, Kit.RuinTint, true);
        Kit.Med(W, Era.Past, "Prop_Brick2", new Vector3(-1.2f, 0, -14.5f), 10, 1, Kit.RuinTint, true);
        Kit.Med(W, Era.Past, "Prop_Brick3", new Vector3(3.6f, 0, -14.6f), 70, 1, Kit.RuinTint, true);
        Vines(new Vector3(-6.8f, 0, -15.2f), new Vector3(6.8f, 0, -15.2f), 1.5f, 9);
        foreach (var (x, z) in new[] { (-4.5f, -12.5f), (4.5f, -12.5f) })
        {
            Kit.City(W, Era.Future, "Prop_Planter_Single", new Vector3(x, 0, z), 0, 0.6f, null, true);
            B.Prop(W, Era.Future, "Bush_Common", new Vector3(x, 0.36f, z), 0, 0.5f);
        }
        B.Lamp(W, Era.Future, new Vector3(0, 2.3f, -18f), new Color(0.7f, 0.85f, 1f), 2f, 8f);
        B.Motes(W, Era.Past, new Vector3(0, 1.5f, -15), new Vector3(7, 1.5f, 7), new Color(1f, 0.85f, 0.5f), 140);
        B.Motes(W, Era.Future, new Vector3(0, 1.5f, -15), new Vector3(7, 1.5f, 7), new Color(0.55f, 0.75f, 1f), 90);
    }

    Node3D _postFuture;

    async void KnockPost()
    {
        B.Disable(_chainPost);
        Audio.Sfx("wood", -2);
        B.Puff(W, Era.Past, _chainPost.GlobalPosition + new Vector3(0.4f, -0.4f, 0), 0.8f);
        B.Move(_chainPost, _chainPost.Position + new Vector3(0.3f, -0.38f, 0), new Vector3(0, 0, -85), 0.6f, Tween.TransitionType.Bounce, Tween.EaseType.Out);
        await Wait(0.9f);
        // decades later: no post, so no chain, so the gate is free
        B.Ripple(W, Era.Future, new Vector3(1.6f, 0, -16.2f), 1.4f);
        B.Shake(_postFuture, 0.05f, 0.6f);
        await Wait(0.6f);
        var fade = _postFuture.CreateTween().SetParallel();
        fade.TweenProperty(_postFuture, "scale", new Vector3(1, 0.01f, 1), 0.8f);
        fade.TweenProperty(_futureChain, "position", new Vector3(0, -0.85f, 0), 0.6f).SetTrans(Tween.TransitionType.Bounce);
        B.Disable(_postFuture);
        await Wait(0.8f);
        _gateOpen = true;
        B.Ripple(W, Era.Future, new Vector3(0, 0, -15f), 1.8f);
        Audio.Sfx("metal", -4);
        Audio.Sfx("door", -4);
        B.Move(_futureGate, null, new Vector3(0, 100, 0), 1.4f, Tween.TransitionType.Elastic, Tween.EaseType.Out);
        foreach (var c in _futureGate.GetChildren())
            if (c is StaticBody3D sb) B.Disable(sb);
        Hint(Era.Future, "Калитка открылась — иди к сиянию");
        Hint(Era.Past, "Иди к сиянию у дальней стены");
    }

    // ---------------- flow ----------------
    protected override async Task Intro()
    {
        Haku.ForceAnim("Sleep");
        Sani.ForceAnim("Sleep");
        await View.Title(Game.ChapterTitles[1], "", 2f);
        await View.Narrate("Когда Хаку просыпается, вокруг уже нет дома. Заросший двор, высокие стены…", 4f);
        Haku.ForceAnim(null);
        await View.Narrate("Сани просыпается в том же дворе — но совершенно ином. Свет в окнах, и ни души.", 4f);
        Sani.ForceAnim(null);
        Hint(Era.Past, "Хаку: найди светящееся пятно на земле");
        Hint(Era.Future, "Сани: найди светящееся пятно на земле");
    }

    void OnGlow()
    {
        if (_glowTouched) return;
        _glowTouched = true;
        _ = View.Narrate("Силуэт — словно сквозь тонкое стекло. Они рядом, но между ними годы.", 4.5f);
        Hint(Era.Past, "Осмотри старую дверь в северной стене");
        Hint(Era.Future, "Осмотри дверь в северной стене");
    }

    async void OnExit()
    {
        await View.Narrate("Так начинается их путешествие.", 2.5f);
        Complete();
    }
}
