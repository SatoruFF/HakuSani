using System;
using Godot;

namespace HakuSani;

/// <summary>Heavy crate a cat can shove one step in the direction it faces.</summary>
public partial class PushBlock : StaticBody3D
{
    public Era Era;
    public Vector3 Size;
    public float Step = 1.0f;
    public Action<PushBlock> Moved;
    bool _moving;

    public static PushBlock Create(Node parent, Era era, Vector3 pos, Vector3 size, Material mat, string prompt = "Толкнуть ящик")
    {
        var b = new PushBlock { Era = era, Size = size, Position = pos, CollisionLayer = L.Phys(era), CollisionMask = 0 };
        b.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        if (mat == Mats.WoodPlank)
            Kit.Med(b, era, "Prop_Crate", new Vector3(0, -size.Y / 2, 0), 0, size.X / 1.08f);   // kit crate model, box collider stays
        else
            b.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, MaterialOverride = mat, Layers = L.Vis(era) });
        parent.AddChild(b);
        Interactable.Create(b, Vector3.Zero, era, prompt, c => b.Push(c), Mathf.Max(size.X, size.Z) * 0.5f + 0.55f, false);
        return b;
    }

    void Push(Cat c)
    {
        if (_moving) return;
        var d = GlobalPosition - c.GlobalPosition;
        d.Y = 0;
        var dir = Mathf.Abs(d.X) > Mathf.Abs(d.Z) ? new Vector3(Mathf.Sign(d.X), 0, 0) : new Vector3(0, 0, Mathf.Sign(d.Z));
        var target = GlobalPosition + dir * Step;

        var q = new PhysicsShapeQueryParameters3D
        {
            Shape = new BoxShape3D { Size = Size * 0.9f },
            Transform = new Transform3D(Basis.Identity, target + Vector3.Up * 0.05f),
            CollisionMask = L.Phys(Era),
            Exclude = new Godot.Collections.Array<Rid> { GetRid() },
        };
        var hits = GetWorld3D().DirectSpaceState.IntersectShape(q, 4);
        if (hits.Count > 0) { B.Shake(this, 0.02f, 0.25f); Audio.Sfx("wood", -10); return; }

        _moving = true;
        Audio.Sfx("scrape", -4);
        var t = CreateTween();
        t.TweenProperty(this, "global_position", target, 0.45f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        t.TweenCallback(Callable.From(() => { _moving = false; Moved?.Invoke(this); }));
    }
}
