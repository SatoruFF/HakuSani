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
        public PanelContainer PromptBox, HintBox;
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
            Scaling3DMode = Viewport.Scaling3DModeEnum.Fsr,
            Scaling3DScale = Game.I.RenderScale,
        };
        svc.AddChild(p.Vp);

        var color = i == 0 ? new Color(1f, 0.55f, 0.75f) : new Color(0.45f, 0.7f, 1f);
        p.Name = UiKit.Label(i == 0 ? "Хаку · прошлое" : "Сани · будущее", 24, color);
        p.Name.HorizontalAlignment = HorizontalAlignment.Left;
        p.Name.AutowrapMode = TextServer.AutowrapMode.Off;
        p.Name.Position = new Vector2(24, 16);
        p.Root.AddChild(p.Name);

        // objective: soft pill at the top
        p.HintBox = Pill(new Color(0.03f, 0.05f, 0.1f, 0.45f), 18);
        p.HintBox.AnchorLeft = 0.5f; p.HintBox.AnchorRight = 0.5f; p.HintBox.OffsetTop = 54;
        p.HintBox.GrowHorizontal = Control.GrowDirection.Both;
        p.Root.AddChild(p.HintBox);
        p.Hint = UiKit.Label("", 21, UiKit.Cream, false, 0);
        p.Hint.LabelSettings.ShadowSize = 0;
        p.Hint.AutowrapMode = TextServer.AutowrapMode.Off;
        p.HintBox.AddChild(p.Hint);

        // interaction prompt: brighter pill near the cat, accent border in the cat's colour
        p.PromptBox = Pill(new Color(0.05f, 0.06f, 0.1f, 0.72f), 22, color);
        p.PromptBox.AnchorLeft = 0.5f; p.PromptBox.AnchorRight = 0.5f; p.PromptBox.AnchorTop = 0.68f; p.PromptBox.AnchorBottom = 0.68f;
        p.PromptBox.GrowHorizontal = Control.GrowDirection.Both;
        p.Root.AddChild(p.PromptBox);
        p.Prompt = UiKit.Label("", 24, Colors.White, false, 0);
        p.Prompt.LabelSettings.ShadowSize = 0;
        p.Prompt.AutowrapMode = TextServer.AutowrapMode.Off;
        p.PromptBox.AddChild(p.Prompt);
        p.PromptBox.Visible = false;
        p.HintBox.Visible = false;
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

    static PanelContainer Pill(Color bg, int radius, Color? border = null)
    {
        var sb = new StyleBoxFlat
        {
            BgColor = bg, CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius, CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius,
            ContentMarginLeft = 22, ContentMarginRight = 22, ContentMarginTop = 8, ContentMarginBottom = 8,
        };
        if (border.HasValue) { sb.BorderColor = border.Value; sb.BorderWidthBottom = sb.BorderWidthTop = sb.BorderWidthLeft = sb.BorderWidthRight = 2; }
        var pc = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        pc.AddThemeStyleboxOverride("panel", sb);
        return pc;
    }

    public void ApplyQuality()
    {
        foreach (var p in P)
        {
            p.Vp.Scaling3DScale = Game.I.RenderScale;
            p.Vp.Msaa3D = Game.I.Quality == 0 ? Viewport.Msaa.Disabled : Viewport.Msaa.Msaa2X;
        }
    }

    public void SetPrompt(int i, string text)
    {
        text ??= "";
        if (P[i].Prompt.Text == text) return;
        P[i].Prompt.Text = text;
        P[i].PromptBox.Visible = text != "";
        if (text != "")
        {
            P[i].PromptBox.Scale = new Vector2(0.9f, 0.9f);
            P[i].PromptBox.PivotOffset = P[i].PromptBox.Size / 2;
            CreateTween().TweenProperty(P[i].PromptBox, "scale", Vector2.One, 0.15f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }
    }

    public void SetHint(int i, string text)
    {
        text ??= "";
        if (P[i].Hint.Text == text) return;
        P[i].Hint.Text = text;
        P[i].HintBox.Visible = text != "";
        if (text != "")
        {
            P[i].HintBox.Modulate = new Color(1, 1, 1, 0);
            CreateTween().TweenProperty(P[i].HintBox, "modulate:a", 1f, 0.4f);
        }
    }

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
