using System.Threading.Tasks;
using Godot;
using Environment = Godot.Environment;

namespace HakuSani.Levels;

/// <summary>Base for non-interactive scenes: one camera on the cinematic layer, both cats frozen.</summary>
public abstract partial class Cinematic : Node3D
{
    protected GameView View;
    protected Camera3D Cam;
    protected Cat Haku, Sani;
    protected bool Skipped;
    float _t;

    protected abstract Task Run();
    protected abstract void OnSkip();

    public override void _Ready()
    {
        View = new GameView();
        AddChild(View);
        View.Layout(false, -1);
        Cam = new Camera3D { CullMask = L.VisCine | L.GhostOfHaku | L.GhostOfSani, Fov = 50, Current = true, Near = 0.03f };
        AddChild(Cam);
        Haku = MakeCat(Era.Past);
        Sani = MakeCat(Era.Future);
        Callable.From(() => { _ = Run(); }).CallDeferred();
    }

    Cat MakeCat(Era e)
    {
        var c = Cat.Create(e);
        c.Frozen = true;
        c.ControlEnabled = false;
        AddChild(c);
        c.SetVisualLayers(L.VisCine);
        return c;
    }

    protected Environment Env(bool night)
    {
        var env = EnvFactory.Make(Era.Past, 0, night);
        env.SsaoEnabled = true;
        env.AmbientLightSource = Environment.AmbientSource.Color;
        env.AmbientLightColor = night ? new Color(0.35f, 0.42f, 0.7f) : new Color(0.9f, 0.8f, 0.7f);
        env.AmbientLightEnergy = night ? 0.55f : 0.5f;
        env.TonemapExposure = night ? 1.25f : 1f;
        return env;
    }

    float _shotT;
    int _shotN;
    public override void _Process(double delta)
    {
        _t += (float)delta;
        if (Game.I.ShotDir != null && _t - _shotT > 4f) { _shotT = _t; Game.I.Shot($"cine_{GetType().Name}_{_shotN++:00}"); }
        if (_t > 1f && !Skipped && Input.IsActionJustPressed("skip"))
        {
            Skipped = true;
            OnSkip();
        }
    }

    protected async Task Wait(float s) => await ToSignal(GetTree().CreateTimer(s), SceneTreeTimer.SignalName.Timeout);

    protected async Task Say(string text, float s)
    {
        if (Skipped) return;
        await View.Narrate(text, s);
    }

    protected void Place(Cat c, Vector3 p, float yawDeg)
    {
        c.Position = p;
        c.Rotation = new Vector3(0, Mathf.DegToRad(yawDeg), 0);
    }

    /// <summary>Walk a cat to a point (position tween + walk cycle).</summary>
    protected async Task WalkTo(Cat c, Vector3 p, float speed = 0.5f)
    {
        var d = p - c.Position;
        c.Rotation = new Vector3(0, Mathf.Atan2(-d.X, -d.Z), 0);
        c.ForceAnim("Walk", speed / Cat.WalkAnimSpeed);
        var t = c.CreateTween();
        t.TweenProperty(c, "position", p, d.Length() / speed);
        await ToSignal(t, Tween.SignalName.Finished);
        c.ForceAnim("Idle");
    }
}
