using Godot;

namespace HakuSani;

/// <summary>Procedural purr: low rumble amplitude-modulated at ~25 Hz with a slow breathing envelope.</summary>
public partial class Purr : AudioStreamPlayer
{
    AudioStreamGeneratorPlayback _pb;
    double _phase, _t;
    float _len;
    readonly RandomNumberGenerator _rng = new();
    float _lp;

    public static Purr Play(Node parent, float seconds, float volumeDb = -6f)
    {
        var p = new Purr { _len = seconds, VolumeDb = volumeDb, Stream = new AudioStreamGenerator { MixRate = 22050, BufferLength = 0.3f } };
        parent.AddChild(p);
        p.Play();
        p._pb = (AudioStreamGeneratorPlayback)p.GetStreamPlayback();
        return p;
    }

    public override void _Process(double delta)
    {
        if (_pb == null) return;
        int frames = _pb.GetFramesAvailable();
        const double rate = 22050;
        for (int i = 0; i < frames; i++)
        {
            _t += 1 / rate;
            if (_t > _len) { QueueFree(); return; }
            // breathing cycle ~2.2 s: inhale purr slightly higher
            double breath = (_t % 2.2) / 2.2;
            double rateHz = breath < 0.45 ? 27 : 23;
            _phase += rateHz / rate;
            double pulse = Mathf.Pow(Mathf.Max(0, Mathf.Sin((float)(_phase * Mathf.Tau))), 3);
            _lp += ((_rng.Randf() * 2 - 1) - _lp) * 0.08f;          // low-passed noise
            double env = Mathf.SmoothStep(0, 0.4f, (float)_t) * Mathf.SmoothStep(0, 0.6f, (float)(_len - _t));
            double gap = breath > 0.92 ? 0.2 : 1;
            float s = (float)(_lp * pulse * env * gap * 2.2);
            _pb.PushFrame(new Vector2(s, s));
        }
    }
}
