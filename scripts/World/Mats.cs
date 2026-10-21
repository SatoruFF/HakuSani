using System.Collections.Generic;
using Godot;

namespace HakuSani;

/// <summary>Shared stylized materials. Textures come from the Stylized Nature MegaKit, applied triplanar.</summary>
public static class Mats
{
    const string Tex = "res://assets/Stylized Nature MegaKit[Standard]/glTF/";
    static readonly Dictionary<string, Material> Cache = new();

    static StandardMaterial3D Tri(string tex, Color tint, float scale, float rough = 0.9f)
    {
        var m = new StandardMaterial3D
        {
            AlbedoColor = tint,
            Roughness = rough,
            Uv1Triplanar = true,
            Uv1WorldTriplanar = true,
            Uv1Scale = Vector3.One * scale,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
        };
        if (tex != null) m.AlbedoTexture = GD.Load<Texture2D>(Tex + tex);
        return m;
    }

    static Material Get(string key, System.Func<Material> make)
    {
        if (!Cache.TryGetValue(key, out var m)) Cache[key] = m = make();
        return m;
    }

    /// <summary>Kit texture set (BaseColor + Normal + Roughness/ORM), triplanar in world space.</summary>
    static Material Pbr(string set, Color tint, float scale)
    {
        string dir = set.StartsWith("city:") ? "res://assets/city/" : "res://assets/medieval/";
        string n = set.Contains(':') ? set[(set.IndexOf(':') + 1)..] : set;
        string Tex(string suffix) => $"{dir}T_{n}_{suffix}.png";
        BaseMaterial3D m = ResourceLoader.Exists(Tex("ORM")) ? new OrmMaterial3D { OrmTexture = GD.Load<Texture2D>(Tex("ORM")) } : new StandardMaterial3D();
        m.AlbedoTexture = GD.Load<Texture2D>(Tex("BaseColor"));
        m.AlbedoColor = tint;
        if (ResourceLoader.Exists(Tex("Normal"))) { m.NormalEnabled = true; m.NormalTexture = GD.Load<Texture2D>(Tex("Normal")); }
        if (m is StandardMaterial3D sm)
        {
            if (ResourceLoader.Exists(Tex("Roughness"))) { sm.RoughnessTexture = GD.Load<Texture2D>(Tex("Roughness")); sm.RoughnessTextureChannel = BaseMaterial3D.TextureChannel.Red; }
            else sm.Roughness = 0.85f;
        }
        m.Uv1Triplanar = true;
        m.Uv1WorldTriplanar = true;
        m.Uv1Scale = Vector3.One * scale;
        m.TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic;
        return m;
    }

    static NoiseTexture2D Noise(float freq, Color a, Color b, int seed = 1)
    {
        var g = new Gradient();
        g.SetColor(0, a);
        g.SetColor(1, b);
        return new NoiseTexture2D
        {
            Width = 512, Height = 512, Seamless = true,
            Noise = new FastNoiseLite { Frequency = freq, Seed = seed, FractalOctaves = 4 },
            ColorRamp = g,
        };
    }

    static StandardMaterial3D NoiseMat(float freq, Color a, Color b, float scale, int seed = 1) => new()
    {
        AlbedoTexture = Noise(freq, a, b, seed),
        Uv1Triplanar = true,
        Uv1WorldTriplanar = true,
        Uv1Scale = Vector3.One * scale,
        Roughness = 0.95f,
    };

    // ---- Past (Haku): old, warm, overgrown ----
    public static Material GrassPast => Get("grassP", () => NoiseMat(0.02f, new Color(0.30f, 0.38f, 0.16f), new Color(0.52f, 0.55f, 0.24f), 0.12f));
    public static Material StoneOld => Get("stoneOld", () => Pbr("UnevenBrick", new Color(0.95f, 0.88f, 0.78f), 0.5f));
    public static Material MossyStone => Get("mossy", () => Pbr("UnevenBrick", new Color(0.72f, 0.82f, 0.62f), 0.5f));
    public static Material PlasterOld => Get("plasterOld", () => Pbr("Plaster", new Color(0.85f, 0.76f, 0.62f), 0.5f));
    public static Material WoodOld => Get("woodOld", () => Pbr("WoodTrim", new Color(0.75f, 0.62f, 0.5f), 0.7f));
    public static Material WoodPlank => Get("woodPlank", () => Pbr("WoodTrim", new Color(1f, 0.85f, 0.68f), 0.9f));
    public static Material RoofOld => Get("roofOld", () => Pbr("RoundTiles", new Color(0.9f, 0.8f, 0.7f), 0.5f));
    public static Material WindowDark => Get("winDark", () => new StandardMaterial3D { AlbedoColor = new Color(0.08f, 0.07f, 0.06f), Roughness = 0.4f });
    public static Material DirtPast => Get("dirtP", () => Pbr("city:Dirt", new Color(0.95f, 0.88f, 0.75f), 0.35f));
    public static Material Rubble => Get("rubble", () => Pbr("RockTrim", new Color(0.85f, 0.82f, 0.78f), 0.8f));

    // ---- Future (Sani): intact, clean, cool ----
    public static Material Paving => Get("paving", () => Pbr("Brick", new Color(0.82f, 0.86f, 0.95f), 0.6f));
    public static Material StoneNew => Get("stoneNew", () => Pbr("city:Concrete", new Color(0.9f, 0.93f, 1f), 0.4f));
    public static Material PlasterNew => Get("plasterNew", () => Pbr("Plaster", new Color(0.95f, 0.97f, 1f), 0.5f));
    public static Material RoofNew => Get("roofNew", () => Pbr("city:RoofSlate", new Color(0.85f, 0.9f, 1f), 0.5f));
    public static Material Metal => Get("metal", () => new StandardMaterial3D { AlbedoColor = new Color(0.32f, 0.36f, 0.42f), Metallic = 0.7f, Roughness = 0.35f });
    public static Material WindowLit => Get("winLit", () => new StandardMaterial3D
    {
        AlbedoColor = new Color(1f, 0.85f, 0.55f),
        EmissionEnabled = true, Emission = new Color(1f, 0.78f, 0.45f), EmissionEnergyMultiplier = 2.2f,
    });
    public static Material Neon => Get("neon", () => new StandardMaterial3D
    {
        AlbedoColor = new Color(0.5f, 0.8f, 1f),
        EmissionEnabled = true, Emission = new Color(0.4f, 0.75f, 1f), EmissionEnergyMultiplier = 3f,
    });
    public static Material DustFloor => Get("dust", () => Pbr("city:Concrete", new Color(0.78f, 0.76f, 0.74f), 0.4f));
    public static Material Roots => Get("roots", () => Tri("Bark_TwistedTree.png", new Color(0.55f, 0.45f, 0.38f), 1.2f));

    // ---- Shared ----
    public static Material Fabric(Color c) => new StandardMaterial3D { AlbedoColor = c, Roughness = 1f };
    public static Material Flat(Color c, float rough = 0.8f) => new StandardMaterial3D { AlbedoColor = c, Roughness = rough };
    public static Material Emissive(Color c, float energy = 2f) => new StandardMaterial3D
    {
        AlbedoColor = c, EmissionEnabled = true, Emission = c, EmissionEnergyMultiplier = energy,
    };
}
