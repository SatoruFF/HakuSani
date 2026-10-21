using System;
using System.Collections.Generic;
using Godot;

namespace HakuSani;

/// <summary>A spot a cat can use with the "use" button. Shows a prompt in that player's HUD.</summary>
public partial class Interactable : Node3D
{
    public static readonly List<Interactable> All = new();

    public string Prompt = "Осмотреть";
    public Era Who = Era.Both;
    public float Radius = 0.8f;
    public bool Enabled = true;
    public bool OneShot;
    public bool FaceTarget = true;
    public string Animation = "Paw";
    public Action<Cat> OnUse;

    public static Interactable Create(Node parent, Vector3 pos, Era who, string prompt, Action<Cat> onUse, float radius = 0.8f, bool oneShot = true)
    {
        var it = new Interactable { Who = who, Prompt = prompt, OnUse = onUse, Radius = radius, OneShot = oneShot, Position = pos };
        parent.AddChild(it);
        return it;
    }

    public override void _EnterTree() => All.Add(this);
    public override void _ExitTree() => All.Remove(this);

    public void Trigger(Cat c)
    {
        if (OneShot) Enabled = false;
        Audio.Sfx("paw", -12);
        OnUse?.Invoke(c);
    }
}
