using Godot;

namespace HakuSani;

/// <summary>Third-person orbit camera that lives inside a player's SubViewport.</summary>
public partial class CatCamera : Node3D
{
    public Cat Target;
    public float Yaw, Pitch = -0.32f;
    public float Distance = 2.3f;
    public Camera3D Camera;
    SpringArm3D _arm;
    float _idleCam;

    public static CatCamera Create(Cat target, Environment env)
    {
        var c = new CatCamera { Target = target, Name = target.Name + "Cam" };
        c._arm = new SpringArm3D
        {
            SpringLength = c.Distance,
            CollisionMask = L.Phys(target.Era),
            Shape = new SphereShape3D { Radius = 0.12f },
            Margin = 0.05f,
        };
        c.AddChild(c._arm);
        c.Camera = new Camera3D
        {
            Fov = 62,
            Near = 0.03f,
            Far = 300,
            CullMask = L.CameraMask(target.Era) | 1,
            Environment = env,
            Current = true,
        };
        c._arm.AddChild(c.Camera);
        target.Cam = c;
        return c;
    }

    public override void _Ready()
    {
        _arm.AddExcludedObject(Target.GetRid());
        SnapBehind();
    }

    public void SnapBehind()
    {
        if (Target == null) return;
        Yaw = Target.Rotation.Y;
        GlobalPosition = Target.GlobalPosition + Vector3.Up * 0.35f;
        Apply();
    }

    float _shake, _shakeT;
    Vector2 _jitter;
    public void Shake(float amount, float time)
    {
        _shake = Mathf.Max(_shake, amount);
        _shakeT = Mathf.Max(_shakeT, time);
    }

    public void AddMouse(Vector2 rel)
    {
        Yaw -= rel.X * Game.I.MouseSensitivity;
        Pitch = Mathf.Clamp(Pitch - rel.Y * Game.I.MouseSensitivity, -1.2f, 0.35f);
        _idleCam = 0;
    }

    public override void _Process(double delta)
    {
        if (Target == null) return;
        float dt = (float)delta;
        int slot = Target.InputSlot;
        if (slot > -2 && Target.ControlEnabled)
        {
            var c = Inputs.Cam(slot);
            if (c.LengthSquared() > 0.01f)
            {
                Yaw -= c.X * 2.6f * dt;
                Pitch = Mathf.Clamp(Pitch - c.Y * 1.8f * dt, -1.2f, 0.35f);
                _idleCam = 0;
            }
        }
        // gentle auto-follow when moving and the player isn't steering the camera
        _idleCam += dt;
        var hv = new Vector3(Target.Velocity.X, 0, Target.Velocity.Z);
        if (_idleCam > 1.5f && hv.Length() > 0.5f)
            Yaw = Mathf.LerpAngle(Yaw, Target.Rotation.Y, 0.6f * dt);

        // look slightly ahead of a moving cat; widen the view when running
        var hv2 = new Vector3(Target.Velocity.X, 0, Target.Velocity.Z);
        var want = Target.GlobalPosition + Vector3.Up * 0.35f + hv2 * 0.12f;
        GlobalPosition = GlobalPosition.Lerp(want, 1f - Mathf.Exp(-8f * dt));
        float speedK = Mathf.Clamp(hv2.Length() / Cat.RunSpeed, 0, 1);
        Camera.Fov = Mathf.Lerp(Camera.Fov, 60 + 8 * speedK, 1f - Mathf.Exp(-3f * dt));
        Distance = Mathf.Lerp(Distance, 2.1f + 0.5f * speedK, 1f - Mathf.Exp(-2f * dt));
        _jitter = Vector2.Zero;
        if (_shakeT > 0)
        {
            _shakeT -= dt;
            _jitter = new Vector2((float)GD.RandRange(-1, 1), (float)GD.RandRange(-1, 1)) * _shake * 4f;
            if (_shakeT <= 0) _shake = 0;
        }
        Apply();
    }

    void Apply()
    {
        Rotation = new Vector3(Pitch + _jitter.Y, Yaw + _jitter.X, 0);
        _arm.SpringLength = Distance;
    }
}
