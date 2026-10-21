using Godot;

namespace HakuSani;

/// <summary>Platform that shuttles between two points once started (e.g. raft across a canal).</summary>
public partial class Shuttle : AnimatableBody3D
{
    public Vector3 A, B;
    public float TravelTime = 3f, Pause = 1.2f;

    public static Shuttle Create(Node parent, Era era, Vector3 a, Vector3 b, Vector3 size, Material mat)
    {
        var s = new Shuttle { A = a, B = b, Position = a, CollisionLayer = L.Phys(era), CollisionMask = 0, SyncToPhysics = true };
        s.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        s.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, MaterialOverride = mat, Layers = L.Vis(era) });
        parent.AddChild(s);
        return s;
    }

    public void Run()
    {
        var t = CreateTween().SetLoops().SetProcessMode(Tween.TweenProcessMode.Physics);
        t.TweenInterval(Pause);
        t.TweenProperty(this, "position", B, TravelTime).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        t.TweenInterval(Pause);
        t.TweenProperty(this, "position", A, TravelTime).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }
}
