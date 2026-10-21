using System;
using Godot;

namespace HakuSani;

/// <summary>Level-building helpers. Everything is placed per era via render/physics layers.</summary>
public static class B
{
    const string NatureKit = "res://assets/Stylized Nature MegaKit[Standard]/glTF/";

    public static void SetLayers(Node n, uint layers, bool shadows = true)
    {
        if (n is VisualInstance3D vi) vi.Layers = layers;
        if (!shadows && n is GeometryInstance3D gi) gi.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        foreach (var c in n.GetChildren()) SetLayers(c, layers, shadows);
    }

    /// <summary>A box in one era. Returns the body (or a plain Node3D when not colliding).</summary>
    public static Node3D Box(Node parent, Era era, Vector3 pos, Vector3 size, Material mat, float rotY = 0, bool collide = true)
    {
        Node3D root = collide
            ? new StaticBody3D { CollisionLayer = L.Phys(era), CollisionMask = 0 }
            : new Node3D();
        root.Position = pos;
        root.Rotation = new Vector3(0, Mathf.DegToRad(rotY), 0);
        if (collide)
            root.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        if (era == Era.Both)
        {
            root.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, MaterialOverride = mat, Layers = L.VisPast });
            root.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, MaterialOverride = mat, Layers = L.VisFuture });
        }
        else
            root.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, MaterialOverride = mat, Layers = L.Vis(era) });
        parent.AddChild(root);
        return root;
    }

    /// <summary>Same box geometry in both eras with different looks; collides for both cats.</summary>
    public static Node3D Box2(Node parent, Vector3 pos, Vector3 size, Material past, Material future, float rotY = 0, bool collide = true)
    {
        var a = Box(parent, Era.Past, pos, size, past, rotY, collide);
        Box(parent, Era.Future, pos, size, future, rotY, collide);
        return a;
    }

    /// <summary>Rotated box with full euler rotation (degrees) — ramps, fallen beams, roots.</summary>
    public static Node3D Slab(Node parent, Era era, Vector3 pos, Vector3 size, Material mat, Vector3 rotDeg, bool collide = true)
    {
        var n = Box(parent, era, pos, size, mat, 0, collide);
        n.RotationDegrees = rotDeg;
        return n;
    }

    public static Node3D Cylinder(Node parent, Era era, Vector3 pos, float radius, float height, Material mat, bool collide = true)
    {
        Node3D root = collide ? new StaticBody3D { CollisionLayer = L.Phys(era), CollisionMask = 0 } : new Node3D();
        root.Position = pos;
        if (collide) root.AddChild(new CollisionShape3D { Shape = new CylinderShape3D { Radius = radius, Height = height } });
        root.AddChild(new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = height, RadialSegments = 20 },
            MaterialOverride = mat, Layers = L.Vis(era),
        });
        parent.AddChild(root);
        return root;
    }

    /// <summary>Per-asset base scale so the kit reads right next to a 0.45 m kitten.</summary>
    static float BaseScale(string name)
    {
        if (name.StartsWith("Grass")) return 0.22f;
        if (name.StartsWith("Clover")) return 0.2f;
        if (name.StartsWith("Flower")) return 0.13f;
        if (name.StartsWith("Fern")) return 0.07f;
        if (name == "Plant_1_Big") return 0.12f;
        if (name.StartsWith("Plant_1")) return 0.25f;
        if (name.StartsWith("Plant_7")) return 0.6f;
        if (name.StartsWith("Mushroom")) return 0.35f;
        if (name.StartsWith("Bush")) return 0.45f;
        if (name.StartsWith("Rock_")) return 0.35f;
        if (name.StartsWith("CommonTree") || name.StartsWith("Pine")) return 0.75f;
        if (name.StartsWith("DeadTree")) return 0.45f;
        if (name.StartsWith("TwistedTree")) return 0.38f;
        if (name.StartsWith("Petal")) return 0.5f;
        return 1f;
    }

    /// <summary>MegaKit nature prop. Optional trunk collider radius.</summary>
    public static Node3D Prop(Node parent, Era era, string name, Vector3 pos, float rotY = 0, float scale = 1, float collideRadius = 0, bool shadows = true)
    {
        var scene = GD.Load<PackedScene>(NatureKit + name + ".gltf");
        var inst = scene.Instantiate<Node3D>();
        inst.Position = pos;
        inst.RotationDegrees = new Vector3(0, rotY, 0);
        inst.Scale = Vector3.One * scale * BaseScale(name);
        SetLayers(inst, L.Vis(era), shadows);
        parent.AddChild(inst);
        if (inst.IsInsideTree())
        {
            if (name.StartsWith("Rock_")) Kit.AddBoundsCollider(inst, era, 0.75f);        // big rocks are solid
            else if (name.StartsWith("Bush")) Kit.AddBoundsCollider(inst, era, 0.6f);     // so are bushes
        }
        if (collideRadius > 0)
        {
            var body = new StaticBody3D { CollisionLayer = L.Phys(era), CollisionMask = 0, Position = pos + Vector3.Up * 1.5f };
            body.AddChild(new CollisionShape3D { Shape = new CylinderShape3D { Radius = collideRadius, Height = 3f } });
            parent.AddChild(body);
        }
        return inst;
    }

    static readonly string[] Grasses = { "Grass_Common_Short", "Grass_Wispy_Short", "Grass_Common_Tall", "Grass_Wispy_Tall", "Clover_1", "Fern_1", "Plant_1", "Flower_3_Group", "Flower_4_Group" };

    /// <summary>Scatter small vegetation in a rect (non-colliding).</summary>
    public static void Scatter(Node parent, Era era, Rect2 area, int count, int seed, string[] kinds = null, float scaleMin = 0.7f, float scaleMax = 1.3f)
    {
        kinds ??= Grasses;
        var rng = new RandomNumberGenerator { Seed = (ulong)seed };
        for (int i = 0; i < count; i++)
        {
            var p = new Vector3(rng.RandfRange(area.Position.X, area.End.X), 0, rng.RandfRange(area.Position.Y, area.End.Y));
            Prop(parent, era, kinds[rng.RandiRange(0, kinds.Length - 1)], p, rng.RandfRange(0, 360), rng.RandfRange(scaleMin, scaleMax), 0, false);
        }
    }

    static int _houseSeed;

    /// <summary>Modular building from the Medieval Village kit. Lit windows = intact & lived-in, otherwise a ruin.</summary>
    public static Node3D House(Node parent, Era era, Vector3 pos, Vector3 size, Material wall, Material roof, Material window, float rotY = 0, int windowsPerRow = 3, int floors = 2)
    {
        int nx = Mathf.Max(2, Mathf.RoundToInt(size.X / 2));
        int nz = Mathf.Max(2, Mathf.RoundToInt(size.Z / 2));
        bool lit = window == Mats.WindowLit;
        return Kit.House(parent, era, pos, nx, nz, floors, rotY, !lit, lit, ++_houseSeed);
    }

    /// <summary>Floor marker visible to both players (unshaded, shared layer).</summary>
    public static MeshInstance3D GlowDisc(Node parent, Vector3 pos, Color color, float radius = 0.7f, uint layers = L.VisShared)
    {
        var mat = new ShaderMaterial { Shader = Shaders.Glow };
        mat.SetShaderParameter("color", color);
        var mi = new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(radius * 2, radius * 2) },
            MaterialOverride = mat,
            Position = pos + Vector3.Up * 0.03f,
            Layers = layers,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        parent.AddChild(mi);
        return mi;
    }

    public static OmniLight3D Lamp(Node parent, Era era, Vector3 pos, Color color, float energy = 1.5f, float range = 6f, bool shadow = false)
    {
        var l = new OmniLight3D
        {
            Position = pos, LightColor = color, LightEnergy = energy, OmniRange = range,
            LightCullMask = L.Vis(era), ShadowEnabled = shadow,
        };
        parent.AddChild(l);
        return l;
    }

    /// <summary>Floating dust / fireflies for atmosphere.</summary>
    public static GpuParticles3D Motes(Node parent, Era era, Vector3 center, Vector3 extents, Color color, int amount = 120)
    {
        var pm = new ParticleProcessMaterial
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
            EmissionBoxExtents = extents,
            Gravity = new Vector3(0, 0.02f, 0),
            InitialVelocityMin = 0.02f, InitialVelocityMax = 0.12f,
            Direction = Vector3.Up, Spread = 180,
            TurbulenceEnabled = true, TurbulenceNoiseStrength = 0.4f,
            ScaleMin = 0.4f, ScaleMax = 1f,
        };
        var curve = new Curve();
        curve.AddPoint(new Vector2(0, 0)); curve.AddPoint(new Vector2(0.2f, 1)); curve.AddPoint(new Vector2(0.8f, 1)); curve.AddPoint(new Vector2(1, 0));
        pm.ScaleCurve = new CurveTexture { Curve = curve };
        var mat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BlendMode = BaseMaterial3D.BlendModeEnum.Add,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
            AlbedoColor = color,
            VertexColorUseAsAlbedo = false,
        };
        var p = new GpuParticles3D
        {
            Amount = amount, Lifetime = 8, Position = center, ProcessMaterial = pm, Preprocess = 8,
            DrawPass1 = new SphereMesh { Radius = 0.015f, Height = 0.03f, RadialSegments = 6, Rings = 3, Material = mat },
            VisibilityAabb = new Aabb(-extents - Vector3.One, extents * 2 + Vector3.One * 2),
            Layers = L.Vis(era), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        parent.AddChild(p);
        return p;
    }

    static ImageTexture _dirt;

    /// <summary>Soft-edged dirt patch projected onto the ground (paths, trodden earth).</summary>
    public static Decal DirtDecal(Node parent, Era era, Vector3 pos, float size, float rotY)
    {
        if (_dirt == null)
        {
            const int N = 128;
            var img = Image.CreateEmpty(N, N, false, Image.Format.Rgba8);
            var noise = new FastNoiseLite { Frequency = 0.06f, Seed = 9, FractalOctaves = 3 };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x - N / 2f) / (N / 2f), dy = (y - N / 2f) / (N / 2f);
                    float r = Mathf.Sqrt(dx * dx + dy * dy) + noise.GetNoise2D(x, y) * 0.35f;
                    float a = Mathf.Clamp((1f - r) * 2.2f, 0, 1) * 0.85f;
                    float shade = 0.85f + noise.GetNoise2D(x * 3, y * 3) * 0.25f;
                    img.SetPixel(x, y, new Color(0.42f * shade, 0.33f * shade, 0.22f * shade, a));
                }
            img.GenerateMipmaps();
            _dirt = ImageTexture.CreateFromImage(img);
        }
        var d = new Decal
        {
            Size = new Vector3(size, 0.3f, size), TextureAlbedo = _dirt, CullMask = L.Vis(era),
            Position = pos, RotationDegrees = new Vector3(0, rotY, 0), UpperFade = 0.3f, LowerFade = 0.3f,
        };
        parent.AddChild(d);
        return d;
    }

    /// <summary>One-shot dust puff (landings, falling doors, cleared rubble).</summary>
    public static void Puff(Node parent, Era era, Vector3 pos, float size = 1f, int amount = 24)
    {
        var pm = new ParticleProcessMaterial
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Ring,
            EmissionRingAxis = Vector3.Up, EmissionRingRadius = 0.12f * size, EmissionRingInnerRadius = 0.02f, EmissionRingHeight = 0.02f,
            Direction = new Vector3(0, 0.3f, 0), Spread = 80, InitialVelocityMin = 0.3f * size, InitialVelocityMax = 0.9f * size,
            Gravity = new Vector3(0, 0.25f, 0), DampingMin = 2, DampingMax = 3,
            ScaleMin = 0.6f * size, ScaleMax = 1.4f * size,
        };
        var curve = new Curve();
        curve.AddPoint(new Vector2(0, 0.3f)); curve.AddPoint(new Vector2(0.3f, 1)); curve.AddPoint(new Vector2(1, 0));
        pm.ScaleCurve = new CurveTexture { Curve = curve };
        var mat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
            AlbedoColor = era == Era.Past ? new Color(0.85f, 0.75f, 0.6f, 0.35f) : new Color(0.7f, 0.75f, 0.85f, 0.35f),
        };
        var p = new GpuParticles3D
        {
            Amount = amount, Lifetime = 1.2f, OneShot = true, Explosiveness = 0.9f, Emitting = true,
            ProcessMaterial = pm, DrawPass1 = new QuadMesh { Size = new Vector2(0.12f, 0.12f), Material = mat },
            Layers = L.Vis(era), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            VisibilityAabb = new Aabb(new Vector3(-2, -1, -2), new Vector3(4, 3, 4)),
        };
        parent.AddChild(p);
        p.GlobalPosition = pos + Vector3.Up * 0.03f;
        p.Finished += p.QueueFree;
    }

    /// <summary>Time ripple on the ground of one era, plus a brief light flash.</summary>
    public static void Ripple(Node parent, Era era, Vector3 pos, float radius = 1.2f, Color? color = null)
    {
        var c = color ?? (era == Era.Past ? new Color(1f, 0.8f, 0.45f) : new Color(0.55f, 0.75f, 1f));
        var mat = new ShaderMaterial { Shader = Shaders.Ripple };
        mat.SetShaderParameter("color", c);
        var mi = new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(radius * 2, radius * 2) }, MaterialOverride = mat,
            Layers = L.Vis(era), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        parent.AddChild(mi);
        mi.GlobalPosition = pos + Vector3.Up * 0.04f;
        var light = new OmniLight3D { LightColor = c, LightEnergy = 2.5f, OmniRange = radius * 2.5f, LightCullMask = L.Vis(era) };
        parent.AddChild(light);
        light.GlobalPosition = pos + Vector3.Up * 0.6f;
        var t = mi.CreateTween().SetParallel();
        t.TweenMethod(Callable.From<float>(v => mat.SetShaderParameter("t", v)), 0f, 1f, 1.1f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
        t.TweenProperty(light, "light_energy", 0f, 1.1f);
        t.Chain().TweenCallback(Callable.From(() => { mi.QueueFree(); light.QueueFree(); }));
    }

    /// <summary>Animate a node to a new transform (used for doors falling, beams, etc.).</summary>
    public static Tween Move(Node3D n, Vector3? pos = null, Vector3? rotDeg = null, float time = 0.8f, Tween.TransitionType trans = Tween.TransitionType.Cubic, Tween.EaseType ease = Tween.EaseType.InOut)
    {
        var t = n.CreateTween().SetParallel();
        if (pos.HasValue) t.TweenProperty(n, "position", pos.Value, time).SetTrans(trans).SetEase(ease);
        if (rotDeg.HasValue) t.TweenProperty(n, "rotation_degrees", rotDeg.Value, time).SetTrans(trans).SetEase(ease);
        return t;
    }

    public static void Shake(Node3D n, float amount = 0.04f, float time = 0.6f)
    {
        var start = n.Position;
        var t = n.CreateTween();
        int steps = (int)(time / 0.05f);
        var rng = new RandomNumberGenerator();
        for (int i = 0; i < steps; i++)
            t.TweenProperty(n, "position", start + new Vector3(rng.RandfRange(-amount, amount), 0, rng.RandfRange(-amount, amount)), 0.05f);
        t.TweenProperty(n, "position", start, 0.05f);
    }

    public static void Disable(Node3D body)
    {
        if (body is CollisionObject3D co) { co.CollisionLayer = 0; }
        foreach (var c in body.GetChildren())
            if (c is CollisionShape3D cs) cs.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
    }

    public static Label3D Text3D(Node parent, Era era, Vector3 pos, string text, Color color, float size = 0.25f)
    {
        var l = new Label3D
        {
            Text = text, Position = pos, Modulate = color, PixelSize = size / 64f, FontSize = 64,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, Layers = era == Era.Both ? L.VisShared : L.Vis(era),
            NoDepthTest = false, OutlineSize = 8,
        };
        parent.AddChild(l);
        return l;
    }
}
