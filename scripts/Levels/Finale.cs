using System.Threading.Tasks;
using Godot;

namespace HakuSani.Levels;

/// <summary>Финал. Дом: the room flickers through the years, the silhouettes merge, they go to sleep together.</summary>
public partial class Finale : Cinematic
{
    Node3D _past, _future, _night;

    protected override async Task Run()
    {
        _past = Home.Build(this, Vector3.Zero, L.VisCine, HomeLook.Past, true, true);
        _future = Home.Build(this, Vector3.Zero, L.VisCine, HomeLook.Future, false, true);
        _night = Home.Build(this, Vector3.Zero, L.VisCine, HomeLook.Night, false, true);
        _future.Visible = false;
        _night.Visible = false;
        _past.GetNode<Node3D>("DoorPivot").RotationDegrees = new Vector3(0, 100, 0);
        _future.GetNode<Node3D>("DoorPivot").RotationDegrees = new Vector3(0, 100, 0);
        Cam.Environment = Env(false);
        Audio.I.Music("finale");

        Place(Haku, new Vector3(-0.5f, 0, 3.4f), 0);
        Place(Sani, new Vector3(0.5f, 0, 3.4f), 0);
        Haku.ForceAnim("Idle");
        Sani.ForceAnim("Idle");
        Haku.SetGhostAlways(true);
        Sani.SetGhostAlways(true);
        Cam.Position = new Vector3(0.3f, 1.3f, -0.6f);
        Cam.LookAt(new Vector3(0, 0.6f, 2.5f), Vector3.Up);

        View.Fade(0, 1.5f);
        await View.Title(Game.ChapterTitles[5], "", 1.5f);
        await Say("В комнате становится тихо.", 2.5f);

        // years flicker past
        var say = Say("Пыль поднимается с пола и медленно оседает. Свет за окном меняется, словно кто-то быстро перелистывает годы.", 5f);
        float step = 0.45f;
        for (int i = 0; i < 12 && !Skipped; i++)
        {
            bool f = i % 2 == 0;
            _past.Visible = !f;
            _future.Visible = f;
            Haku.Visible = !f || i > 8;
            Sani.Visible = f || i > 8;
            await Wait(step);
            step = Mathf.Max(0.12f, step * 0.82f);
        }
        await say;
        _past.Visible = false;
        _future.Visible = false;
        _night.Visible = true;
        Cam.Environment = Env(true);
        Haku.Visible = Sani.Visible = true;

        // silhouettes drift together until they coincide… then separate again as two real cats
        var t = CreateTween().SetParallel();
        t.TweenProperty(Haku, "position", new Vector3(-0.15f, 0, 3.2f), 2.5f).SetTrans(Tween.TransitionType.Sine);
        t.TweenProperty(Sani, "position", new Vector3(0.15f, 0, 3.2f), 2.5f).SetTrans(Tween.TransitionType.Sine);
        var line = Say("Их силуэты мерцают — пока наконец не совпадают. Время больше не разделяет их.", 4f);
        await ToSignal(t, Tween.SignalName.Finished);
        Haku.ShowGhost(1.5f); Sani.ShowGhost(1.5f);
        Haku.SetGhostAlways(false);
        Sani.SetGhostAlways(false);
        await line;
        var door = _night.GetNode<Node3D>("DoorPivot");
        var dt = door.CreateTween();
        dt.TweenProperty(door, "rotation_degrees", new Vector3(0, 100, 0), 1.6f).SetTrans(Tween.TransitionType.Sine);
        Audio.Sfx("door", -4);
        await Say("Дверь открывается.", 2f);

        // Haku goes first, Sani follows
        var camMove = Cam.CreateTween();
        camMove.TweenProperty(Cam, "position", new Vector3(1.8f, 1.3f, 1.2f), 6f).SetTrans(Tween.TransitionType.Sine);
        camMove.Parallel().TweenMethod(Callable.From<float>(_ => Cam.LookAt(new Vector3(0, 0.3f, -0.6f), Vector3.Up)), 0f, 1f, 6f);
        _ = Say("Хаку первым заходит внутрь. Сани следует за ним.", 3.5f);
        var h = WalkTo(Haku, new Vector3(-0.1f, 0, 1.6f));
        await Wait(0.9f);
        var s2 = WalkTo(Sani, new Vector3(0.1f, 0, 1.9f));
        await h;
        h = WalkTo(Haku, new Vector3(-0.24f, 0.12f, -1.1f));
        await s2;
        await WalkTo(Sani, new Vector3(0.24f, 0.12f, -1.25f));
        await h;
        Place(Haku, Haku.Position, 60);
        Place(Sani, Sani.Position, -110);
        Haku.ForceAnim("Sleep");
        await Wait(0.4f);
        Sani.ForceAnim("Sleep");
        Purr.Play(this, 6f, -10f);
        await Say("Они забираются на своё место, сворачиваются рядом и засыпают.", 3.5f);

        // camera pulls back; two fresh paw prints remain on the floor
        foreach (var (x, z) in new[] { (-0.12f, 1.2f), (0.12f, 1.35f) })
        {
            var mat = new ShaderMaterial { Shader = Shaders.Footprint };
            mat.SetShaderParameter("tex", Footprints.PawTexture());
            mat.SetShaderParameter("color", x < 0 ? new Color(1f, 0.6f, 0.8f) : new Color(0.5f, 0.75f, 1f));
            mat.SetShaderParameter("life", 1f);
            AddChild(new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(0.12f, 0.12f) }, MaterialOverride = mat, Layers = L.VisCine, Position = new Vector3(x, 0.01f, z) });
        }
        var back = Cam.CreateTween();
        back.TweenProperty(Cam, "position", new Vector3(0.4f, 2.3f, 2.1f), 7f).SetTrans(Tween.TransitionType.Sine);
        back.Parallel().TweenMethod(Callable.From<float>(_ => Cam.LookAt(new Vector3(0, 0.2f, -0.2f), Vector3.Up)), 0f, 1f, 7f);
        await Say("История заканчивается там же, где началась.", 3f);
        await Say("Только теперь они снова вместе.", 3f);
        if (Skipped) return;
        await View.Title("Конец", "Haku & Sani: Time Crossing", 3f);
        await ToSignal(View.Fade(1, 1.5f), Tween.SignalName.Finished);
        Game.I.ShowMenu();
    }

    protected override void OnSkip() => Game.I.ShowMenu();
}
