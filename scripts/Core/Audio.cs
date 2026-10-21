using System.Collections.Generic;
using Godot;

namespace HakuSani;

/// <summary>
/// Music + sound effects. Music runs two layers (past / future) that crossfade with the focused cat;
/// in split-screen both play at equal weight. Missing files are skipped silently.
/// Files: res://assets/audio/music/*.ogg|mp3, res://assets/audio/sfx/*.ogg (Kenney CC0 packs).
/// </summary>
public partial class Audio : Node
{
    public static Audio I { get; private set; }

    readonly AudioStreamPlayer[] _layers = new AudioStreamPlayer[2];   // 0 = past, 1 = future
    string[] _tracks = new string[2];
    float _focus = 0.5f, _focusTarget = 0.5f, _master = 1f, _masterTarget = 1f;
    readonly Dictionary<string, List<AudioStream>> _sfx = new();
    readonly List<AudioStreamPlayer> _pool = new();
    const float MusicDb = -8f;

    public static void SetBusVolume(string bus, float lin)
    {
        int i = AudioServer.GetBusIndex(bus);
        if (i < 0) return;
        AudioServer.SetBusVolumeDb(i, lin < 0.001f ? -80 : Mathf.LinearToDb(lin));
    }

    static void EnsureBus(string name)
    {
        if (AudioServer.GetBusIndex(name) >= 0) return;
        AudioServer.AddBus();
        int i = AudioServer.BusCount - 1;
        AudioServer.SetBusName(i, name);
        AudioServer.SetBusSend(i, "Master");
    }

    public override void _EnterTree()
    {
        I = this;
        EnsureBus("Music");
        EnsureBus("SFX");
        ProcessMode = ProcessModeEnum.Always;
        for (int i = 0; i < 2; i++)
        {
            _layers[i] = new AudioStreamPlayer { VolumeDb = -80, Bus = "Music" };
            AddChild(_layers[i]);
        }
        _wind = new AudioStreamPlayer { Stream = new AudioStreamGenerator { MixRate = 22050, BufferLength = 0.25f }, VolumeDb = -80, Bus = "SFX" };
        AddChild(_wind);
        _wind.Play();
        _windPb = (AudioStreamGeneratorPlayback)_wind.GetStreamPlayback();
        for (int i = 0; i < 12; i++)
        {
            var p = new AudioStreamPlayer { Bus = "SFX" };
            AddChild(p);
            _pool.Add(p);
        }
    }

    static AudioStream LoadStream(string path)
    {
        foreach (var ext in new[] { ".ogg", ".mp3", ".wav" })
            if (ResourceLoader.Exists(path + ext)) return GD.Load<AudioStream>(path + ext);
        return null;
    }

    /// <summary>Set music. Same track for both eras when future is null.</summary>
    public void Music(string past, string future = null)
    {
        future ??= past;
        var names = new[] { past, future };
        for (int i = 0; i < 2; i++)
        {
            if (_tracks[i] == names[i] && _layers[i].Playing) continue;
            _tracks[i] = names[i];
            var s = names[i] == null ? null : LoadStream("res://assets/audio/music/" + names[i]);
            if (s is AudioStreamOggVorbis ogg) ogg.Loop = true;
            if (s is AudioStreamMP3 mp3) mp3.Loop = true;
            _layers[i].Stream = s;
            if (names[i] != null && s == null) GD.PushWarning($"[audio] music missing: {names[i]}");
            if (s != null) _layers[i].Play(); else _layers[i].Stop();
        }
        // identical track on both layers: keep them in sync by silencing the second
        if (past == future) _layers[1].Stop();
        _master = 0; _masterTarget = 1;
    }

    public void FadeOut() => _masterTarget = 0;

    // ---------------- procedural wind ambience ----------------
    AudioStreamPlayer _wind;
    AudioStreamGeneratorPlayback _windPb;
    float _windLevel, _windTarget;
    double _wt;
    float _brownL, _brownR, _lpL, _lpR;
    readonly RandomNumberGenerator _rng = new();

    /// <summary>0 = silent, 1 = gusty. Levels set this; menus/cinematics turn it off.</summary>
    public void Ambient(float level) => _windTarget = level;

    void FillWind(float dt)
    {
        _windLevel = Mathf.MoveToward(_windLevel, _windTarget, dt * 0.3f);
        _wind.VolumeDb = _windLevel < 0.01f ? -80 : Mathf.LinearToDb(_windLevel) - 14f;
        if (_windPb == null) return;
        int frames = _windPb.GetFramesAvailable();
        for (int i = 0; i < frames; i++)
        {
            _wt += 1 / 22050.0;
            // slow gusts: two incommensurate sines
            float gust = 0.55f + 0.3f * Mathf.Sin((float)_wt * 0.37f) + 0.15f * Mathf.Sin((float)_wt * 1.13f + 1.7f);
            _brownL = Mathf.Clamp(_brownL + (_rng.Randf() * 2 - 1) * 0.02f, -1, 1) * 0.998f;
            _brownR = Mathf.Clamp(_brownR + (_rng.Randf() * 2 - 1) * 0.02f, -1, 1) * 0.998f;
            float k = 0.04f + 0.08f * gust;                      // gusts open the filter → brighter whoosh
            _lpL += (_brownL - _lpL) * k;
            _lpR += (_brownR - _lpR) * k;
            _windPb.PushFrame(new Vector2(_lpL, _lpR) * gust * 3f);
        }
    }

    /// <summary>0 = only past layer, 1 = only future layer, 0.5 = both.</summary>
    public void Focus(float f) => _focusTarget = f;

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        FillWind(dt);
        _focus = Mathf.MoveToward(_focus, _focusTarget, dt * 0.8f);
        _master = Mathf.MoveToward(_master, _masterTarget, dt * 0.5f);
        bool shared = _tracks[0] == _tracks[1];
        float wPast = shared ? 1 : Mathf.Cos(_focus * Mathf.Pi / 2);
        float wFut = shared ? 0 : Mathf.Sin(_focus * Mathf.Pi / 2);
        _layers[0].VolumeDb = Db(wPast * _master);
        _layers[1].VolumeDb = Db(wFut * _master);
    }

    static float Db(float lin) => lin < 0.001f ? -80 : Mathf.LinearToDb(lin) + MusicDb;

    // ---------------- SFX ----------------

    /// <summary>Kenney file-name prefixes per logical sound.</summary>
    static readonly Dictionary<string, string[]> Map = new()
    {
        ["step_grass"] = new[] { "footstep_grass_00" },
        ["step_stone"] = new[] { "footstep_concrete_00" },
        ["step_wood"] = new[] { "footstep_wood_00" },
        ["wood"] = new[] { "impactWood_medium_00", "impactPlank_medium_00" },
        ["wood_heavy"] = new[] { "impactWood_heavy_00" },
        ["stone"] = new[] { "impactMining_00", "impactPunch_heavy_00" },
        ["metal"] = new[] { "impactMetal_medium_00", "impactMetal_heavy_00" },
        ["latch"] = new[] { "metalLatch", "metalClick" },
        ["door"] = new[] { "doorOpen_", "creak" },
        ["paw"] = new[] { "cloth", "impactSoft_medium_00" },
        ["scrape"] = new[] { "impactPlank_medium_00", "creak" },
        ["glow"] = new[] { "glass_00", "confirmation_00" },
        ["ui"] = new[] { "click_00", "select_00" },
        ["chime"] = new[] { "confirmation_00", "maximize_00" },
    };

    List<AudioStream> Variants(string name)
    {
        if (_sfx.TryGetValue(name, out var list)) return list;
        list = new List<AudioStream>();
        var dir = DirAccess.Open("res://assets/audio/sfx");
        if (dir != null && Map.TryGetValue(name, out var prefixes))
            foreach (var f in dir.GetFiles())
            {
                var file = f.Replace(".import", "").Replace(".remap", "");
                if (!file.EndsWith(".ogg")) continue;
                foreach (var pre in prefixes)
                    if (file.StartsWith(pre))
                    {
                        var s = GD.Load<AudioStream>("res://assets/audio/sfx/" + file);
                        if (s != null && !list.Contains(s)) list.Add(s);
                        break;
                    }
            }
        _sfx[name] = list;
        if (OS.IsDebugBuild()) GD.Print($"[audio] sfx {name}: {list.Count} variants");
        return list;
    }

    public static void Sfx(string name, float volumeDb = -4f, float pitchJitter = 0.08f)
    {
        if (I == null) return;
        var v = I.Variants(name);
        if (v.Count == 0) return;
        AudioStreamPlayer p = null;
        foreach (var x in I._pool) if (!x.Playing) { p = x; break; }
        p ??= I._pool[0];
        p.Stream = v[(int)(GD.Randi() % (uint)v.Count)];
        p.VolumeDb = volumeDb;
        p.PitchScale = 1f + (float)GD.RandRange(-pitchJitter, pitchJitter);
        p.Play();
    }
}
