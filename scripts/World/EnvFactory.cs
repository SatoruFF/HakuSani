using Godot;

namespace HakuSani;

public static class EnvFactory
{
    /// <summary>Warm golden afternoon for the past, cool blue dusk for the future.</summary>
    public static Environment Make(Era era, float fog = 0.012f, bool night = false)
    {
        var sky = new ProceduralSkyMaterial();
        if (night)
        {
            sky.SkyTopColor = new Color(0.02f, 0.04f, 0.12f);
            sky.SkyHorizonColor = new Color(0.10f, 0.14f, 0.30f);
            sky.GroundHorizonColor = new Color(0.08f, 0.1f, 0.2f);
            sky.GroundBottomColor = new Color(0.02f, 0.02f, 0.05f);
            sky.SunAngleMax = 6;
        }
        else if (era == Era.Past)
        {
            sky.SkyTopColor = new Color(0.42f, 0.55f, 0.75f);
            sky.SkyHorizonColor = new Color(0.98f, 0.78f, 0.52f);
            sky.GroundHorizonColor = new Color(0.85f, 0.68f, 0.48f);
            sky.GroundBottomColor = new Color(0.25f, 0.2f, 0.15f);
            sky.SunAngleMax = 30;
        }
        else
        {
            sky.SkyTopColor = new Color(0.06f, 0.10f, 0.28f);
            sky.SkyHorizonColor = new Color(0.36f, 0.48f, 0.80f);
            sky.GroundHorizonColor = new Color(0.25f, 0.32f, 0.55f);
            sky.GroundBottomColor = new Color(0.06f, 0.08f, 0.14f);
            sky.SunAngleMax = 20;
        }

        var env = new Environment
        {
            BackgroundMode = Environment.BGMode.Sky,
            Sky = new Sky { SkyMaterial = sky },
            AmbientLightSource = Environment.AmbientSource.Sky,
            AmbientLightSkyContribution = 0.85f,
            AmbientLightEnergy = night ? 0.5f : era == Era.Past ? 1f : 1.35f,
            TonemapMode = Environment.ToneMapper.Filmic,
            TonemapExposure = era == Era.Past ? 1.0f : 1.1f,
            GlowEnabled = true,
            GlowIntensity = 0.7f,
            GlowBloom = 0.08f,
            GlowHdrThreshold = 0.9f,
            FogEnabled = fog > 0,
            FogLightColor = era == Era.Past ? new Color(0.95f, 0.78f, 0.55f) : new Color(0.38f, 0.5f, 0.85f),
            FogDensity = fog,
            FogSkyAffect = 0.4f,
            FogAerialPerspective = 0.3f,
            AdjustmentEnabled = true,
            AdjustmentSaturation = era == Era.Past ? 1.1f : 1.05f,
            AdjustmentContrast = 1.05f,
            SsaoEnabled = true,
            SsaoRadius = 0.6f,
            SsaoIntensity = 1.4f,
        };
        return env;
    }

    public static DirectionalLight3D Sun(Node parent, Era era, bool night = false)
    {
        var l = new DirectionalLight3D
        {
            LightColor = night ? new Color(0.6f, 0.7f, 1f) : era == Era.Past ? new Color(1f, 0.86f, 0.66f) : new Color(0.62f, 0.72f, 1f),
            LightEnergy = night ? 0.35f : era == Era.Past ? 1.35f : 1.0f,
            ShadowEnabled = true,
            DirectionalShadowMaxDistance = 35,
            DirectionalShadowMode = DirectionalLight3D.ShadowMode.Parallel2Splits,
            LightCullMask = L.Vis(era),
            ShadowCasterMask = L.Vis(era),
            ShadowBlur = 1.5f,
        };
        l.RotationDegrees = era == Era.Past ? new Vector3(-38, -35, 0) : new Vector3(-30, 140, 0);
        parent.AddChild(l);
        return l;
    }
}
