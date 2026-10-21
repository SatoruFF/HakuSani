using System;
using Godot;

namespace HakuSani;

/// <summary>Playable cat. Haku = Past era, Sani = Future era.</summary>
public partial class Cat : CharacterBody3D
{
    // WalkSpeed = normal movement (a trot); partial stick tilt slows to a walk. Shift = gallop.
    public const float WalkSpeed = 1.5f, RunSpeed = 3.0f, JumpVelocity = 5.0f, Gravity = 14f;
    // ground speed baked into the gait cycles (tools/build_cats.py prints GAIT_SPEEDS); matching it keeps paws planted
    public const float WalkAnimSpeed = 0.438f, TrotAnimSpeed = 1.133f, RunAnimSpeed = 1.875f;

    public Era Era { get; private set; }
    public string DisplayName => Era == Era.Past ? "Хаку" : "Сани";
    public Color Accent => Era == Era.Past ? new Color(1f, 0.45f, 0.7f) : new Color(0.3f, 0.55f, 1f);

    public CatCamera Cam;
    public bool ControlEnabled = true;
    public bool IsActive = true;          // solo mode: whether this cat receives input
    public bool Frozen;                   // cutscenes: no physics, position driven externally
    public Vector3 Checkpoint;
    public float CheckpointYaw;
    public Action<Cat> OnRespawn;

    Node3D _model, _ghost;
    AnimationPlayer _anim, _ghostAnim;
    string _animName = "";
    float _pawLock;
    float _ghostTimer;
    bool _ghostAlways;
    ShaderMaterial _ghostMat;
    string _forced;                        // cutscene animation override
    float _footstepAcc;

    public Interactable Focus { get; private set; }
    public event Action<Cat, Vector3> Stepped;   // used for footprints

    public static Cat Create(Era era)
    {
        var c = new Cat { Era = era, Name = era == Era.Past ? "Haku" : "Sani" };
        return c;
    }

    public override void _Ready()
    {
        CollisionLayer = L.CatBody(Era);
        CollisionMask = L.Phys(Era);
        FloorSnapLength = 0.15f;
        FloorMaxAngle = Mathf.DegToRad(50);
        AddChild(new CollisionShape3D
        {
            Shape = new CapsuleShape3D { Radius = 0.15f, Height = 0.42f },
            Position = new Vector3(0, 0.21f, 0),
        });

        var path = Era == Era.Past ? "res://assets/cats/haku.glb" : "res://assets/cats/sani.glb";
        var scene = GD.Load<PackedScene>(path);
        _model = scene.Instantiate<Node3D>();
        AddChild(_model);
        _anim = Setup(_model);
        SetVisualLayers(L.Vis(Era));

        _ghost = scene.Instantiate<Node3D>();
        AddChild(_ghost);
        _ghostAnim = Setup(_ghost);
        _ghostMat = new ShaderMaterial { Shader = Shaders.Ghost };
        _ghostMat.SetShaderParameter("tint", Accent);
        _ghostMat.SetShaderParameter("strength", 0f);
        ApplyRecursive(_ghost, mi =>
        {
            mi.Layers = Era == Era.Past ? L.GhostOfHaku : L.GhostOfSani;
            mi.MaterialOverride = _ghostMat;
            mi.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        });
        _ghost.Visible = false;
        Play("Idle");
    }

    static AnimationPlayer Setup(Node3D model)
    {
        var ap = model.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
        if (ap == null) return null;
        foreach (var n in new[] { "Idle", "Walk", "Trot", "Run", "Fall", "Sleep" })
            if (ap.HasAnimation(n)) ap.GetAnimation(n).LoopMode = Animation.LoopModeEnum.Linear;
        return ap;
    }

    static void ApplyRecursive(Node n, Action<MeshInstance3D> f)
    {
        if (n is MeshInstance3D mi) f(mi);
        foreach (var c in n.GetChildren()) ApplyRecursive(c, f);
    }

    public void SetVisualLayers(uint layers) => ApplyRecursive(_model, mi => mi.Layers = layers);

    public void Play(string name, float blend = 0.18f, float speed = 1f)
    {
        if (_anim == null || !_anim.HasAnimation(name)) return;
        if (_animName != name)
        {
            _animName = name;
            _anim.Play(name, blend);
            _ghostAnim?.Play(name, blend);
        }
        _anim.SpeedScale = speed;
        if (_ghostAnim != null) _ghostAnim.SpeedScale = speed;
    }

    /// <summary>Force an animation for cutscenes (null to release).</summary>
    public void ForceAnim(string name, float speed = 1f)
    {
        _forced = name;
        if (name != null) Play(name, 0.4f, speed);
    }

    public void ShowGhost(float seconds) => _ghostTimer = Mathf.Max(_ghostTimer, seconds);
    public void SetGhostAlways(bool on, float strength = 0.55f)
    {
        _ghostAlways = on;
        _ghostAlwaysStrength = strength;
    }
    float _ghostAlwaysStrength = 0.55f;

    public int InputSlot =>
        Game.I.Mode == PlayMode.Coop ? (Era == Era.Past ? 0 : 1) : (IsActive ? -1 : -2);

    public float Yaw => Rotation.Y;

    public void Teleport(Vector3 pos, float yaw)
    {
        GlobalPosition = pos;
        Rotation = new Vector3(0, yaw, 0);
        Velocity = Vector3.Zero;
        Cam?.SnapBehind();
    }

    public void SetCheckpoint(Vector3 pos, float yaw)
    {
        Checkpoint = pos;
        CheckpointYaw = yaw;
    }

    public void Respawn()
    {
        Teleport(Checkpoint, CheckpointYaw);
        OnRespawn?.Invoke(this);
    }

    public override void _Process(double delta)
    {
        // ghost silhouette fades in/out
        _ghostTimer = Mathf.Max(0, _ghostTimer - (float)delta);
        float target = _ghostAlways ? _ghostAlwaysStrength : _ghostTimer > 0 ? Mathf.Min(1f, _ghostTimer) : 0f;
        float cur = (float)_ghostMat.GetShaderParameter("strength");
        cur = Mathf.MoveToward(cur, target, (float)delta * 2f);
        _ghostMat.SetShaderParameter("strength", cur);
        _ghost.Visible = cur > 0.01f;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        if (Frozen) { if (_forced != null) Play(_forced); return; }
        int slot = InputSlot;
        bool canControl = ControlEnabled && slot > -2 && _forced == null;
        var v = Velocity;

        bool onFloor = IsOnFloor();
        if (!onFloor) v.Y -= Gravity * dt;
        // coyote time + jump buffer: forgiving jumps off ledges and slightly early presses
        _sinceFloor = onFloor ? 0 : _sinceFloor + dt;
        _jumpBuffer = Mathf.Max(0, _jumpBuffer - dt);

        Vector3 dir = Vector3.Zero;
        bool run = false;
        float mag = 0;
        _pawLock = Mathf.Max(0, _pawLock - dt);
        if (canControl)
        {
            var m = Inputs.Move(slot);
            mag = Mathf.Clamp(m.Length() * 1.15f, 0, 1);       // analog sticks: partial tilt = slower
            float camYaw = Cam?.Yaw ?? 0;
            dir = m.LengthSquared() > 0.0001f ? new Vector3(m.X, 0, m.Y).Normalized().Rotated(Vector3.Up, camYaw) : Vector3.Zero;
            run = Inputs.Held(slot, "run");
            if (Inputs.Pressed(slot, "jump")) _jumpBuffer = 0.14f;
            if (_jumpBuffer > 0 && _sinceFloor < 0.12f && _pawLock <= 0 && v.Y <= 0.5f)
            {
                v.Y = JumpVelocity;
                _jumpBuffer = 0;
                _sinceFloor = 1;
                _animName = "";
                Play("Jump", 0.06f);
                Audio.Sfx("paw", -14, 0.15f);
            }
            if (Inputs.Pressed(slot, "use") && Focus != null && _pawLock <= 0)
                Use(Focus);
        }
        if (_pawLock > 0) dir = Vector3.Zero;

        float speed = (run ? RunSpeed : WalkSpeed) * mag;
        var horiz = new Vector3(v.X, 0, v.Z);
        var targetVel = dir * speed;
        float accel = onFloor ? (targetVel.LengthSquared() > horiz.LengthSquared() ? 7f : 10f) : 3f;
        horiz = horiz.MoveToward(targetVel, accel * Mathf.Max(speed, WalkSpeed) * dt);
        v.X = horiz.X;
        v.Z = horiz.Z;
        float fallSpeed = -v.Y;
        Velocity = v;
        MoveAndSlide();

        // landing: dust + soft thud
        if (!_wasOnFloor && IsOnFloor() && fallSpeed > 2.5f)
        {
            B.Puff(GetParent(), Era, GlobalPosition, Mathf.Clamp(fallSpeed / 6f, 0.3f, 1f));
            Audio.Sfx(Era == Era.Past ? "step_grass" : "step_stone", -6, 0.1f);
            Cam?.Shake(0.015f * Mathf.Clamp(fallSpeed / 6f, 0, 1), 0.15f);
        }
        _wasOnFloor = IsOnFloor();

        float prevYaw = Rotation.Y;
        if (dir.LengthSquared() > 0.01f)
        {
            float want = Mathf.Atan2(-dir.X, -dir.Z);
            float turnRate = run ? 9f : 12f;
            Rotation = new Vector3(0, Mathf.LerpAngle(Rotation.Y, want, 1f - Mathf.Exp(-turnRate * dt)), 0);
        }
        // lean into turns, proportional to speed
        float yawVel = Mathf.Wrap(Rotation.Y - prevYaw, -Mathf.Pi, Mathf.Pi) / Mathf.Max(dt, 0.0001f);
        float lean = Mathf.Clamp(-yawVel * horiz.Length() * 0.03f, -0.18f, 0.18f);
        _lean = Mathf.Lerp(_lean, lean, 1f - Mathf.Exp(-8f * dt));
        _model.Rotation = new Vector3(0, 0, _lean);
        _ghost.Rotation = _model.Rotation;

        UpdateAnim(horiz.Length(), dt);
        TrackSteps();
        UpdateFocus();

        if (GlobalPosition.Y < -8f) Respawn();
    }

    void UpdateAnim(float hspeed, float dt)
    {
        if (_forced != null) { Play(_forced); return; }
        if (_pawLock > 0) return;
        if (!IsOnFloor())
        {
            if (Velocity.Y < -1f) Play("Fall", 0.25f);
            return;
        }
        if (hspeed < 0.08f) Play("Idle", 0.3f);
        else if (hspeed < 0.75f) Play("Walk", 0.25f, Mathf.Max(0.5f, hspeed / WalkAnimSpeed));
        else if (hspeed < 2.1f) Play("Trot", 0.22f, Mathf.Max(0.7f, hspeed / TrotAnimSpeed));
        else Play("Run", 0.2f, Mathf.Max(0.8f, hspeed / RunAnimSpeed));

    }

    Vector3 _lastPos;
    float _sinceFloor, _jumpBuffer, _lean;
    bool _wasOnFloor = true;
    void TrackSteps()
    {
        var p = GlobalPosition;
        var d = p - _lastPos;
        _lastPos = p;
        d.Y = 0;
        float len = d.Length();
        if (len > 1f || !IsOnFloor()) return;   // teleports / airborne
        _footstepAcc += len;
        if (_footstepAcc > 0.22f)
        {
            _footstepAcc = 0;
            Stepped?.Invoke(this, p);
        }
    }

    void UpdateFocus()
    {
        Interactable best = null;
        float bestD = float.MaxValue;
        var fwd = -GlobalTransform.Basis.Z;
        foreach (var it in Interactable.All)
        {
            if (!it.Enabled || !L.Matches(it.Who, Era) || !it.IsInsideTree()) continue;
            var to = it.GlobalPosition - GlobalPosition;
            to.Y *= 0.5f;
            float d = to.Length();
            if (d > it.Radius) continue;
            float facing = d < 0.3f ? 1f : fwd.Dot(to.Normalized());
            if (facing < -0.2f) continue;
            float score = d - facing * 0.3f;
            if (score < bestD) { bestD = score; best = it; }
        }
        Focus = best;
    }

    async void Use(Interactable it)
    {
        _pawLock = 0.45f;
        if (it.FaceTarget)
        {
            var to = it.GlobalPosition - GlobalPosition;
            Rotation = new Vector3(0, Mathf.Atan2(-to.X, -to.Z), 0);
        }
        _animName = "";
        Play(it.Animation, 0.08f);
        await ToSignal(GetTree().CreateTimer(0.22f), SceneTreeTimer.SignalName.Timeout);
        if (IsInstanceValid(it) && it.Enabled) it.Trigger(this);
    }
}
