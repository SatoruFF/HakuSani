using System.Threading.Tasks;
using Godot;
using Environment = Godot.Environment;

namespace HakuSani.Levels;

/// <summary>
/// Пролог (in-engine intro): an ordinary evening, the cats fall asleep; a storm and a swirl of light lift
/// them into a time vortex where Haku is pulled toward the golden past and Sani toward the blue future.
/// </summary>
public partial class Prologue : Cinematic
{
    Node3D _home, _void;
    static readonly Vector3 VoidPos = new(0, 60, 0);

    protected override async Task Run()
    {
        _home = Home.Build(this, Vector3.Zero, L.VisCine, HomeLook.Night);
        Audio.I.Music("menu");
        Cam.Environment = Env(true);
        Place(Haku, new Vector3(-0.24f, 0.12f, -1.15f), 60);
        Place(Sani, new Vector3(0.24f, 0.12f, -1.25f), -110);
        Haku.ForceAnim("Idle");
        Sani.ForceAnim("Idle");
        Cam.Position = new Vector3(1.6f, 1.4f, 2.0f);
        Cam.LookAt(new Vector3(0, 0.3f, -1.2f), Vector3.Up);
        var dolly = Cam.CreateTween();
        dolly.TweenProperty(Cam, "position", new Vector3(0.6f, 0.8f, 0.4f), 15f).SetTrans(Tween.TransitionType.Sine);
        dolly.Parallel().TweenMethod(Callable.From<float>(_ => Cam.LookAt(new Vector3(0, 0.25f, -1.2f), Vector3.Up)), 0f, 1f, 15f);

        View.Fade(0, 2f);
        await View.Title(Game.ChapterTitles[0], "", 1.5f);
        await Say("Обычный вечер. Хаку и Сани устраиваются спать в своём привычном месте.", 4f);
        Haku.ForceAnim("Sleep");
        await Wait(0.6f);
        Sani.ForceAnim("Sleep");
        await Say("За окном шумит ветер, в комнате тихо.", 3f);
        await Say("Они сворачиваются рядом и засыпают.", 3f);
        if (Skipped) return;

        await Storm();
        if (Skipped) return;
        await Vortex();
        if (Skipped) return;
        await ToSignal(View.Fade(1, 0.6f, Colors.White), Tween.SignalName.Finished);
        Game.I.StartChapter(1);
    }

    /// <summary>Lightning outside, lights flicker, motes of gold and blue start circling the bed.</summary>
    async Task Storm()
    {
        var flash = new OmniLight3D { LightColor = new Color(0.75f, 0.85f, 1f), LightEnergy = 0, OmniRange = 9, LightCullMask = L.VisCine, Position = new Vector3(0, 2f, -2.2f) };
        _home.AddChild(flash);
        var lights = new System.Collections.Generic.List<Light3D>();
        foreach (var n in _home.GetChildren()) if (n is Light3D l && l != flash) lights.Add(l);

        var swirl = Swirl(new Vector3(0, 0.4f, -1.2f), 0.9f);
        _ = Say("Но этой ночью что-то меняется.", 3f);
        for (int i = 0; i < 4 && !Skipped; i++)
        {
            var t = flash.CreateTween();
            t.TweenProperty(flash, "light_energy", 6f, 0.05f);
            t.TweenProperty(flash, "light_energy", 0.5f, 0.08f);
            t.TweenProperty(flash, "light_energy", 4f, 0.05f);
            t.TweenProperty(flash, "light_energy", 0f, 0.4f);
            Audio.Sfx("wood_heavy", -10, 0.3f);
            foreach (var l in lights)
            {
                var f = l.CreateTween();
                f.TweenProperty(l, "light_energy", 0.05f, 0.06f);
                f.TweenProperty(l, "light_energy", l.LightEnergy, 0.25f);
            }
            await Wait(i == 0 ? 1.2f : 0.7f);
        }
        swirl.Amount = 360;
        Audio.Sfx("glow", -2);

        // the cats wake inside the glow and are lifted off the cushion
        Haku.SetGhostAlways(true, 0.28f);
        Sani.SetGhostAlways(true, 0.28f);
        Haku.ForceAnim("Fall", 0.4f);
        Sani.ForceAnim("Fall", 0.4f);
        var up = CreateTween().SetParallel();
        up.TweenProperty(Haku, "position", new Vector3(-0.35f, 0.9f, -1.1f), 3f).SetTrans(Tween.TransitionType.Sine);
        up.TweenProperty(Sani, "position", new Vector3(0.35f, 1.0f, -1.3f), 3f).SetTrans(Tween.TransitionType.Sine);
        up.TweenProperty(Haku, "rotation:y", Haku.Rotation.Y + 1.5f, 3f);
        up.TweenProperty(Sani, "rotation:y", Sani.Rotation.Y - 1.5f, 3f);
        var cam = Cam.CreateTween();
        cam.TweenProperty(Cam, "position", new Vector3(0.2f, 1.0f, 0.9f), 3f).SetTrans(Tween.TransitionType.Sine);
        cam.Parallel().TweenMethod(Callable.From<float>(_ => Cam.LookAt(new Vector3(0, 0.8f, -1.2f), Vector3.Up)), 0f, 1f, 3f);
        await Say("Комнату заливает светом. Их подхватывает вихрь.", 3f);
        await ToSignal(View.Fade(1, 0.4f, Colors.White), Tween.SignalName.Finished);
    }

    /// <summary>The time vortex: gold past on the left, blue future on the right; the cats are pulled apart.</summary>
    async Task Vortex()
    {
        _home.Visible = false;
        _void = new Node3D { Position = VoidPos };
        AddChild(_void);
        Cam.Environment = VoidEnv();

        var mat = new ShaderMaterial { Shader = Shaders.Vortex };
        var backdrop = new MeshInstance3D { Mesh = new QuadMesh { Size = new Vector2(16, 16) }, MaterialOverride = mat, Layers = L.VisCine, Position = new Vector3(0, 0, -4), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
        _void.AddChild(backdrop);
        _void.AddChild(new OmniLight3D { LightColor = new Color(1f, 0.7f, 0.35f), LightEnergy = 3f, OmniRange = 6, LightCullMask = L.VisCine, Position = new Vector3(-1.5f, 0.5f, 0.6f) });
        _void.AddChild(new OmniLight3D { LightColor = new Color(0.35f, 0.6f, 1f), LightEnergy = 3f, OmniRange = 6, LightCullMask = L.VisCine, Position = new Vector3(1.5f, 0.5f, 0.6f) });
        _void.AddChild(new OmniLight3D { LightColor = new Color(1f, 1f, 1f), LightEnergy = 0.8f, OmniRange = 5, LightCullMask = L.VisCine, Position = new Vector3(0, 1.2f, 1.6f) });
        Stream(new Vector3(-2.2f, 0, -3), new Color(1f, 0.75f, 0.35f));
        Stream(new Vector3(2.2f, 0, -3), new Color(0.4f, 0.7f, 1f));

        // leaping toward each other, like in a dream
        Haku.Position = VoidPos + new Vector3(-0.55f, -0.15f, 0);
        Sani.Position = VoidPos + new Vector3(0.55f, -0.12f, 0);
        Haku.Rotation = new Vector3(0, -Mathf.Pi / 2, 0);
        Sani.Rotation = new Vector3(0, Mathf.Pi / 2, 0);
        Haku.ForceAnim("Run", 0.25f);
        Sani.ForceAnim("Run", 0.25f);
        Cam.Position = VoidPos + new Vector3(0, 0.05f, 1.7f);
        Cam.LookAt(VoidPos + new Vector3(0, -0.05f, 0), Vector3.Up);
        Cam.Fov = 45;
        var drift = Cam.CreateTween();
        drift.TweenProperty(Cam, "position", VoidPos + new Vector3(0, 0.12f, 1.25f), 6f).SetTrans(Tween.TransitionType.Sine);

        var close = CreateTween().SetParallel();
        close.TweenProperty(Haku, "position", VoidPos + new Vector3(-0.3f, -0.1f, 0), 3f).SetTrans(Tween.TransitionType.Sine);
        close.TweenProperty(Sani, "position", VoidPos + new Vector3(0.3f, -0.08f, 0), 3f).SetTrans(Tween.TransitionType.Sine);
        View.Fade(0, 0.8f, Colors.White);
        await Say("Они тянутся друг к другу…", 2.5f);
        if (Skipped) return;
        Haku.ForceAnim("Paw", 0.6f);
        Sani.ForceAnim("Paw", 0.6f);
        await Wait(0.8f);

        // the vortex splits: past pulls left, future pulls right
        var pull = CreateTween().SetParallel();
        pull.TweenMethod(Callable.From<float>(v => mat.SetShaderParameter("split", v)), 0f, 0.35f, 3.5f);
        pull.TweenProperty(Haku, "position", VoidPos + new Vector3(-1.4f, 0.0f, -1.2f), 4f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        pull.TweenProperty(Sani, "position", VoidPos + new Vector3(1.4f, 0.05f, -1.2f), 4f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        pull.TweenProperty(Haku, "rotation", new Vector3(0.2f, -Mathf.Pi / 2 + 0.6f, 0.5f), 4f);
        pull.TweenProperty(Sani, "rotation", new Vector3(0.2f, Mathf.Pi / 2 - 0.6f, -0.5f), 4f);
        Haku.ForceAnim("Fall", 0.6f);
        Sani.ForceAnim("Fall", 0.6f);
        Purr.Play(this, 3.5f, -14f);
        await Say("Хаку уносит в тёплое золотое прошлое. Сани — в холодное синее будущее.", 4f);
        await Say("Между ними — годы. Но связь остаётся.", 3f);
    }

    static Environment VoidEnv() => new()
    {
        BackgroundMode = Environment.BGMode.Color,
        BackgroundColor = new Color(0.02f, 0.02f, 0.05f),
        AmbientLightSource = Environment.AmbientSource.Color,
        AmbientLightColor = new Color(0.5f, 0.5f, 0.6f),
        AmbientLightEnergy = 0.4f,
        TonemapMode = Environment.ToneMapper.Filmic,
        GlowEnabled = true, GlowIntensity = 1.1f, GlowBloom = 0.2f, GlowHdrThreshold = 0.7f,
    };

    /// <summary>Gold and blue sparks orbiting a point (the swirl over the bed).</summary>
    GpuParticles3D Swirl(Vector3 at, float radius)
    {
        var pm = new ParticleProcessMaterial
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Ring,
            EmissionRingAxis = Vector3.Up, EmissionRingRadius = radius, EmissionRingInnerRadius = radius * 0.3f, EmissionRingHeight = 0.6f,
            OrbitVelocityMin = 0.25f, OrbitVelocityMax = 0.5f,
            Gravity = new Vector3(0, 0.15f, 0),
            ScaleMin = 0.6f, ScaleMax = 1.4f,
            ColorInitialRamp = Ramp(),
        };
        var p = new GpuParticles3D
        {
            Amount = 180, Lifetime = 3, Position = at, ProcessMaterial = pm, Preprocess = 1,
            DrawPass1 = new SphereMesh { Radius = 0.008f, Height = 0.016f, RadialSegments = 6, Rings = 3, Material = SparkMat() },
            Layers = L.VisCine, VisibilityAabb = new Aabb(new Vector3(-2, -1, -2), new Vector3(4, 3, 4)),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        _home.AddChild(p);
        return p;
    }

    /// <summary>Sparks streaming out of the vortex toward the camera.</summary>
    void Stream(Vector3 from, Color c)
    {
        var pm = new ParticleProcessMaterial
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere, EmissionSphereRadius = 1.2f,
            Direction = new Vector3(0, 0, 1), Spread = 25, InitialVelocityMin = 1.5f, InitialVelocityMax = 3.5f,
            Gravity = Vector3.Zero, ScaleMin = 0.5f, ScaleMax = 1.5f,
            Color = c,
        };
        var p = new GpuParticles3D
        {
            Amount = 220, Lifetime = 2.5f, Position = from, ProcessMaterial = pm, Preprocess = 2,
            DrawPass1 = new BoxMesh { Size = new Vector3(0.01f, 0.01f, 0.12f), Material = SparkMat() },
            Layers = L.VisCine, VisibilityAabb = new Aabb(new Vector3(-4, -4, -2), new Vector3(8, 8, 12)),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        _void.AddChild(p);
    }

    static StandardMaterial3D SparkMat() => new()
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        BlendMode = BaseMaterial3D.BlendModeEnum.Add,
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        VertexColorUseAsAlbedo = true,
        AlbedoColor = new Color(0.9f, 0.9f, 0.9f),
    };

    static GradientTexture1D Ramp()
    {
        var g = new Gradient();
        g.SetColor(0, new Color(1f, 0.75f, 0.35f));
        g.SetColor(1, new Color(0.4f, 0.7f, 1f));
        return new GradientTexture1D { Gradient = g };
    }

    protected override void OnSkip() => Game.I.StartChapter(1);
}
