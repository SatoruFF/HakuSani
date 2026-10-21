using Godot;

namespace HakuSani;

/// <summary>
/// Two input slots. Slot 0 = Haku (WASD + mouse), slot 1 = Sani (arrows).
/// Gamepads: with two pads pad0→slot0, pad1→slot1; with one pad it goes to slot 1 (keyboard stays on slot 0).
/// In solo mode the active cat reads both slots (slot -1).
/// </summary>
public static class Inputs
{
    static readonly string[] Actions = { "left", "right", "fwd", "back", "jump", "run", "use", "cam_left", "cam_right", "cam_up", "cam_down" };

    public static void Setup()
    {
        var pads = Input.GetConnectedJoypads();
        int padFor0 = pads.Count >= 2 ? pads[0] : -1;
        int padFor1 = pads.Count >= 2 ? pads[1] : pads.Count == 1 ? pads[0] : -1;

        foreach (var a in Actions)
            for (int s = 0; s < 2; s++)
            {
                var name = $"p{s}_{a}";
                if (InputMap.HasAction(name)) InputMap.EraseAction(name);
                InputMap.AddAction(name, 0.25f);
            }

        Keys(0, "left", Key.A); Keys(0, "right", Key.D); Keys(0, "fwd", Key.W); Keys(0, "back", Key.S);
        Keys(0, "jump", Key.Space); Keys(0, "run", Key.Shift); Keys(0, "use", Key.E);
        Keys(0, "cam_left", Key.Q); Keys(0, "cam_right", Key.R);

        Keys(1, "left", Key.Left); Keys(1, "right", Key.Right); Keys(1, "fwd", Key.Up); Keys(1, "back", Key.Down);
        Keys(1, "jump", Key.Enter); Keys(1, "jump", Key.KpEnter); Keys(1, "run", Key.Slash); Keys(1, "use", Key.Apostrophe);
        Keys(1, "use", Key.Period); Keys(1, "cam_left", Key.Bracketleft); Keys(1, "cam_right", Key.Bracketright);

        if (padFor0 >= 0) Pad(0, padFor0);
        if (padFor1 >= 0) Pad(1, padFor1);

        Ensure("pause", new InputEventKey { PhysicalKeycode = Key.Escape }, new InputEventJoypadButton { ButtonIndex = JoyButton.Start, Device = -1 });
        Ensure("switch_cat", new InputEventKey { PhysicalKeycode = Key.Tab }, new InputEventJoypadButton { ButtonIndex = JoyButton.Y, Device = -1 });
        Ensure("skip", new InputEventKey { PhysicalKeycode = Key.Space }, new InputEventKey { PhysicalKeycode = Key.Enter },
            new InputEventKey { PhysicalKeycode = Key.Escape }, new InputEventJoypadButton { ButtonIndex = JoyButton.A, Device = -1 });
    }

    static void Ensure(string name, params InputEvent[] evs)
    {
        if (InputMap.HasAction(name)) InputMap.EraseAction(name);
        InputMap.AddAction(name);
        foreach (var e in evs) InputMap.ActionAddEvent(name, e);
    }

    static void Keys(int slot, string a, Key k) =>
        InputMap.ActionAddEvent($"p{slot}_{a}", new InputEventKey { PhysicalKeycode = k });

    static void Pad(int slot, int dev)
    {
        void Axis(string a, JoyAxis ax, float v) =>
            InputMap.ActionAddEvent($"p{slot}_{a}", new InputEventJoypadMotion { Device = dev, Axis = ax, AxisValue = v });
        void Btn(string a, JoyButton b) =>
            InputMap.ActionAddEvent($"p{slot}_{a}", new InputEventJoypadButton { Device = dev, ButtonIndex = b });
        Axis("left", JoyAxis.LeftX, -1); Axis("right", JoyAxis.LeftX, 1);
        Axis("fwd", JoyAxis.LeftY, -1); Axis("back", JoyAxis.LeftY, 1);
        Axis("cam_left", JoyAxis.RightX, -1); Axis("cam_right", JoyAxis.RightX, 1);
        Axis("cam_up", JoyAxis.RightY, -1); Axis("cam_down", JoyAxis.RightY, 1);
        Axis("run", JoyAxis.TriggerRight, 1);
        Btn("jump", JoyButton.A); Btn("use", JoyButton.X); Btn("run", JoyButton.B);
    }

    public static Vector2 Move(int slot)
    {
        if (slot < 0) return (Move(0) + Move(1)).LimitLength(1f);
        return Input.GetVector($"p{slot}_left", $"p{slot}_right", $"p{slot}_fwd", $"p{slot}_back");
    }

    public static Vector2 Cam(int slot)
    {
        if (slot < 0) return Cam(0) + Cam(1);
        return Input.GetVector($"p{slot}_cam_left", $"p{slot}_cam_right", $"p{slot}_cam_up", $"p{slot}_cam_down");
    }

    public static bool Pressed(int slot, string a) =>
        slot < 0 ? Pressed(0, a) || Pressed(1, a) : Input.IsActionJustPressed($"p{slot}_{a}");

    public static bool Held(int slot, string a) =>
        slot < 0 ? Held(0, a) || Held(1, a) : Input.IsActionPressed($"p{slot}_{a}");
}
