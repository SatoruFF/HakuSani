using System.Threading.Tasks;
using Godot;

namespace HakuSani;

/// <summary>Split-screen container: one SubViewport per cat plus HUD overlays.</summary>
public partial class GameView : CanvasLayer
{
    public sealed class Panel
    {
        public Control Root;
        public SubViewport Vp;
        public Label Name, Prompt, Hint;
    }

    public readonly Panel[] P = new Panel[2];
    Control _root;
    ColorRect _fade, _divider;
    Label _subtitle, _title, _subtitle2;
    int _narrateId;

    public override void _Ready()
    {
        _root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        UiKit.Full(_root);
        AddChild(_root);
        for (int i = 0; i < 2; i++) P[i] = MakePanel(i);

        _divider = new ColorRect { Color = new Color(0.05f, 0.05f, 0.08f), MouseFilter = Control.MouseFilterEnum.Ignore };
        _divider.AnchorLeft = 0.5f; _divider.AnchorRight = 0.5f; _divider.AnchorBottom = 1;
        _divider.OffsetLeft = -2; _divider.OffsetRight = 2;
        _root.AddChild(_divider);

        _subtitle = UiKit.Label("", 30, Colors.White);
        _subtitle.AnchorLeft = 0.1f; _subtitle.AnchorRight = 0.9f; _subtitle.AnchorTop = 0.78f; _subtitle.AnchorBottom = 0.95f;
        _subtitle.Modulate = new Color(1, 1, 1, 0);
        _root.AddChild(_subtitle);

        _title = UiKit.Label("", 64, UiKit.Cream, true, 10);
        _title.AnchorLeft = 0; _title.AnchorRight = 1; _title.AnchorTop = 0.35f; _title.AnchorBottom = 0.5f;
        _title.Modulate = new Color(1, 1, 1, 0);
        _root.AddChild(_title);
        _subtitle2 = UiKit.Label("", 28, UiKit.Cream);
        _subtitle2.AnchorLeft = 0; _subtitle2.AnchorRight = 1; _subtitle2.AnchorTop = 0.5f; _subtitle2.AnchorBottom = 0.58f;
        _subtitle2.Modulate = new Color(1, 1, 1, 0);
        _root.AddChild(_subtitle2);

        _fade = new ColorRect { Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Ignore };
        UiKit.Full(_fade);
        _root.AddChild(_fade);
    }

    Panel MakePanel(int i)
    {
        var p = new Panel();
        p.Root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        _root.AddChild(p.Root);
        var svc = new SubViewportContainer { Stretch = true, MouseFilter = Control.MouseFilterEnum.Ignore };
        UiKit.Full(svc);
        p.Root.AddChild(svc);
        p.Vp = new SubViewport
        {
            Msaa3D = Viewport.Msaa.Msaa2X,
            ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled,
            HandleInputLocally = false,
            AudioListenerEnable3D = i == 0,
            PositionalShadowAtlasSize = 2048,
        };
        svc.AddChild(p.Vp);

        var color = i == 0 ? new Color(1f, 0.55f, 0.75f) : new Color(0.45f, 0.7f, 1f);
        p.Name = UiKit.Label(i == 0 ? "Хаку · прошлое" : "Сани · будущее", 24, color);
        p.Name.HorizontalAlignment = HorizontalAlignment.Left;
        p.Name.AutowrapMode = TextServer.AutowrapMode.Off;
        p.Name.Position = new Vector2(24, 16);
        p.Root.AddChild(p.Name);

        p.Hint = UiKit.Label("", 22, UiKit.Cream);
        p.Hint.AnchorLeft = 0.05f; p.Hint.AnchorRight = 0.95f; p.Hint.OffsetTop = 52; p.Hint.OffsetBottom = 110;
        p.Root.AddChild(p.Hint);

        p.Prompt = UiKit.Label("", 26, Colors.White);
        p.Prompt.AnchorLeft = 0; p.Prompt.AnchorRight = 1; p.Prompt.AnchorTop = 0.66f; p.Prompt.AnchorBottom = 0.74f;
        p.Root.AddChild(p.Prompt);
        return p;
    }

    /// <summary>split: both visible side by side. Otherwise only `active` fills the screen.</summary>
    public void Layout(bool split, int active)
    {
        for (int i = 0; i < 2; i++)
        {
            var r = P[i].Root;
            bool vis = split || i == active;
            r.Visible = vis;
            P[i].Vp.RenderTargetUpdateMode = vis ? SubViewport.UpdateMode.Always : SubViewport.UpdateMode.Disabled;
            r.AnchorTop = 0; r.AnchorBottom = 1;
            r.AnchorLeft = split ? i * 0.5f : 0;
            r.AnchorRight = split ? i * 0.5f + 0.5f : 1;
            r.OffsetLeft = r.OffsetRight = r.OffsetTop = r.OffsetBottom = 0;
        }
        _divider.Visible = split;
    }

    public void SetPrompt(int i, string text)
    {
        P[i].Prompt.Text = text ?? "";
    }

    public void SetHint(int i, string text) => P[i].Hint.Text = text ?? "";

    public Tween Fade(float to, float time, Color? color = null)
    {
        if (color.HasValue) _fade.Color = new Color(color.Value, _fade.Color.A);
        var t = CreateTween();
        t.TweenProperty(_fade, "color:a", to, time);
        return t;
    }

    public async Task Narrate(string text, float seconds)
    {
        int id = ++_narrateId;
        _subtitle.Text = text;
        var t = CreateTween();
        t.TweenProperty(_subtitle, "modulate:a", 1f, 0.5f);
        await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
        if (id != _narrateId) return;
        var t2 = CreateTween();
        t2.TweenProperty(_subtitle, "modulate:a", 0f, 0.5f);
        await ToSignal(t2, Tween.SignalName.Finished);
    }

    public async Task Title(string big, string small, float hold = 2.5f)
    {
        _title.Text = big;
        _subtitle2.Text = small;
        var t = CreateTween().SetParallel();
        t.TweenProperty(_title, "modulate:a", 1f, 0.8f);
        t.TweenProperty(_subtitle2, "modulate:a", 1f, 1.2f);
        await ToSignal(GetTree().CreateTimer(0.8f + hold), SceneTreeTimer.SignalName.Timeout);
        var t2 = CreateTween().SetParallel();
        t2.TweenProperty(_title, "modulate:a", 0f, 0.8f);
        t2.TweenProperty(_subtitle2, "modulate:a", 0f, 0.8f);
        await ToSignal(t2, Tween.SignalName.Finished);
    }
}
