using System;
using System.Collections.Generic;
using Godot;

namespace HakuSani;

/// <summary>Floor plate pressed while a cat or a block rests on it.</summary>
public partial class PressurePlate : Node3D
{
    public Func<IEnumerable<Node3D>> Candidates;
    public float Radius = 0.45f;
    public bool Pressed { get; private set; }
    public Action<bool> Changed;
    MeshInstance3D _top;
    Material _off, _on;

    public static PressurePlate Create(Node parent, Era era, Vector3 pos, Color color, Func<IEnumerable<Node3D>> candidates, float radius = 0.45f)
    {
        var p = new PressurePlate { Position = pos, Candidates = candidates, Radius = radius };
        parent.AddChild(p);
        p._off = Mats.Flat(color.Darkened(0.5f), 0.5f);
        p._on = Mats.Emissive(color, 2.5f);
        p._top = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius + 0.05f, Height = 0.06f },
            MaterialOverride = p._off, Layers = L.Vis(era), Position = Vector3.Up * 0.03f,
        };
        p.AddChild(p._top);
        return p;
    }

    public override void _PhysicsProcess(double delta)
    {
        bool now = false;
        foreach (var n in Candidates())
        {
            if (n == null || !IsInstanceValid(n)) continue;
            var d = n.GlobalPosition - GlobalPosition;
            if (d.Y > -0.3f && d.Y < 0.6f && new Vector2(d.X, d.Z).Length() < Radius + 0.1f) { now = true; break; }
        }
        if (now == Pressed) return;
        Pressed = now;
        _top.MaterialOverride = now ? _on : _off;
        _top.Position = Vector3.Up * (now ? 0.0f : 0.03f);
        Audio.Sfx("latch", -8);
        Changed?.Invoke(now);
    }
}
