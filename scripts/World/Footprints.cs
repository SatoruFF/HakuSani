using System;
using System.Collections.Generic;
using Godot;

namespace HakuSani;

/// <summary>Spawns glowing paw prints in the OTHER era where a cat walks over a "dust" area.</summary>
public partial class Footprints : Node3D
{
    public Era ShowIn = Era.Past;
    public Color Color = new(0.45f, 0.75f, 1f);
    public Func<Vector3, bool> IsDust = _ => true;
    public float Life = 90f;
    static ImageTexture _tex;
    readonly List<(MeshInstance3D mi, ShaderMaterial mat, float born)> _prints = new();
    bool _left;
    float _time;

    public void Hook(Cat c) => c.Stepped += OnStep;

    public static ImageTexture PawTexture()
    {
        if (_tex != null) return _tex;
        var img = Image.CreateEmpty(64, 64, false, Image.Format.Rgba8);
        void Blob(float cx, float cy, float rx, float ry)
        {
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = (x - cx) / rx, dy = (y - cy) / ry;
                    float d = dx * dx + dy * dy;
                    if (d >= 1f) continue;
                    float a = Mathf.Clamp((1f - d) * 3f, 0, 1);
                    float old = img.GetPixel(x, y).A;
                    img.SetPixel(x, y, new Color(1, 1, 1, Mathf.Max(old, a)));
                }
        }
        Blob(32, 40, 13, 11);
        Blob(16, 22, 6, 7); Blob(26, 14, 6, 7); Blob(38, 14, 6, 7); Blob(48, 22, 6, 7);
        _tex = ImageTexture.CreateFromImage(img);
        return _tex;
    }

    void OnStep(Cat c, Vector3 pos)
    {
        if (!IsDust(pos)) return;
        _left = !_left;
        var right = c.GlobalTransform.Basis.X;
        var p = pos + right * (_left ? -0.05f : 0.05f) + Vector3.Up * 0.02f;
        var mat = new ShaderMaterial { Shader = Shaders.Footprint };
        mat.SetShaderParameter("tex", PawTexture());
        mat.SetShaderParameter("color", Color);
        var mi = new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(0.1f, 0.1f) },
            MaterialOverride = mat, Layers = L.Vis(ShowIn),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        AddChild(mi);
        mi.GlobalPosition = p;
        mi.Rotation = new Vector3(0, c.Rotation.Y, 0);
        _prints.Add((mi, mat, _time));
        if (_prints.Count > 400) { _prints[0].mi.QueueFree(); _prints.RemoveAt(0); }
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        for (int i = _prints.Count - 1; i >= 0; i--)
        {
            var (mi, mat, born) = _prints[i];
            float age = _time - born;
            float fade = Mathf.Clamp(1f - (age - Life * 0.7f) / (Life * 0.3f), 0, 1);
            mat.SetShaderParameter("life", Mathf.Min(1f, age * 3f) * fade);
            if (age > Life) { mi.QueueFree(); _prints.RemoveAt(i); }
        }
    }
}
