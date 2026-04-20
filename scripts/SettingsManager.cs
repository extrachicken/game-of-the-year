using Godot;
using System.Collections.Generic;

/// Autoload singleton — persists settings (volume, display, keybindings) via ConfigFile.
public partial class SettingsManager : Node
{
    public static SettingsManager Instance { get; private set; } = null!;

    // ─── Audio ────────────────────────────────────────────────────────────────

    private float _masterVolume = 1f;
    private float _musicVolume  = 0.8f;
    private float _sfxVolume    = 1f;

    public float MasterVolume
    {
        get => _masterVolume;
        set { _masterVolume = Mathf.Clamp(value, 0f, 1f); ApplyMasterVolume(); }
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set { _musicVolume = Mathf.Clamp(value, 0f, 1f); ApplyMusicVolume(); }
    }

    public float SfxVolume
    {
        get => _sfxVolume;
        set { _sfxVolume = Mathf.Clamp(value, 0f, 1f); ApplySfxVolume(); }
    }

    // ─── Display ──────────────────────────────────────────────────────────────

    private bool     _fullscreen  = false;
    private Vector2I _resolution  = new(1920, 1080);

    public bool Fullscreen
    {
        get => _fullscreen;
        set { _fullscreen = value; ApplyFullscreen(); }
    }

    public Vector2I Resolution
    {
        get => _resolution;
        set { _resolution = value; ApplyResolution(); }
    }

    // ─── Input ────────────────────────────────────────────────────────────────

    private float _mouseSensitivity = 1f;

    public float MouseSensitivity
    {
        get => _mouseSensitivity;
        set => _mouseSensitivity = Mathf.Clamp(value, 0.1f, 5f);
    }

    // action → physical keycode (int).  Only overridden actions are stored.
    private Dictionary<string, int> _keyBindings = new();

    /// Rebind an action to a single key (erases all previous events for that action).
    public void SetBinding(string action, Key physicalKeycode)
    {
        _keyBindings[action] = (int)physicalKeycode;
        ApplyBinding(action, physicalKeycode);
    }

    /// Returns the first physical-keycode bound to an action, or Key.None.
    public Key GetBinding(string action)
    {
        if (_keyBindings.TryGetValue(action, out int code))
            return (Key)code;

        // Fall back to whatever is currently in InputMap (project defaults)
        foreach (var ev in InputMap.ActionGetEvents(action))
        {
            if (ev is InputEventKey key)
                return key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode;
        }
        return Key.None;
    }

    // ─── Persistence ─────────────────────────────────────────────────────────

    private const string SavePath = "user://settings.cfg";

    public override void _Ready()
    {
        Instance = this;
        LoadSettings();
    }

    public void SaveSettings()
    {
        var cfg = new ConfigFile();
        cfg.SetValue("audio", "master_volume",    _masterVolume);
        cfg.SetValue("audio", "music_volume",     _musicVolume);
        cfg.SetValue("audio", "sfx_volume",       _sfxVolume);
        cfg.SetValue("display", "fullscreen",     _fullscreen);
        cfg.SetValue("display", "resolution_x",   _resolution.X);
        cfg.SetValue("display", "resolution_y",   _resolution.Y);
        cfg.SetValue("input",  "mouse_sensitivity", _mouseSensitivity);
        foreach (var kv in _keyBindings)
            cfg.SetValue("bindings", kv.Key, kv.Value);
        cfg.Save(SavePath);
    }

    private void LoadSettings()
    {
        var cfg = new ConfigFile();
        if (cfg.Load(SavePath) != Error.Ok)
        {
            ApplyAll();
            return;
        }

        _masterVolume      = (float)cfg.GetValue("audio", "master_volume",    1f);
        _musicVolume       = (float)cfg.GetValue("audio", "music_volume",     0.8f);
        _sfxVolume         = (float)cfg.GetValue("audio", "sfx_volume",       1f);
        _fullscreen        = (bool)cfg.GetValue("display", "fullscreen",      false);
        int rx             = (int)cfg.GetValue("display", "resolution_x",     1920);
        int ry             = (int)cfg.GetValue("display", "resolution_y",     1080);
        _resolution        = new Vector2I(rx, ry);
        _mouseSensitivity  = (float)cfg.GetValue("input", "mouse_sensitivity", 1f);

        // Load keybindings
        foreach (string action in new[] { "move_left", "move_right", "run", "interact" })
        {
            var saved = cfg.GetValue("bindings", action, Variant.From(0));
            int code = saved.AsInt32();
            if (code != 0)
            {
                _keyBindings[action] = code;
                ApplyBinding(action, (Key)code);
            }
        }

        ApplyAll();
    }

    // ─── Apply helpers ────────────────────────────────────────────────────────

    private void ApplyAll()
    {
        ApplyMasterVolume();
        ApplyMusicVolume();
        ApplySfxVolume();
        ApplyFullscreen();
        // Resolution is applied inside ApplyFullscreen
    }

    private void ApplyMasterVolume()
    {
        if (AudioServer.GetBusCount() > 0)
            AudioServer.SetBusVolumeDb(0, Mathf.LinearToDb(_masterVolume));
    }

    private void ApplyMusicVolume()
    {
        int bus = AudioServer.GetBusIndex("Music");
        if (bus >= 0)
            AudioServer.SetBusVolumeDb(bus, Mathf.LinearToDb(_musicVolume));
    }

    private void ApplySfxVolume()
    {
        int bus = AudioServer.GetBusIndex("SFX");
        if (bus >= 0)
            AudioServer.SetBusVolumeDb(bus, Mathf.LinearToDb(_sfxVolume));
    }

    private void ApplyFullscreen()
    {
        if (_fullscreen)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        }
        else
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            DisplayServer.WindowSetSize(_resolution);
            DisplayServer.WindowSetPosition(DisplayServer.ScreenGetPosition() +
                (DisplayServer.ScreenGetSize() - _resolution) / 2);
        }
    }

    private void ApplyResolution()
    {
        if (!_fullscreen)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            DisplayServer.WindowSetSize(_resolution);
            DisplayServer.WindowSetPosition(DisplayServer.ScreenGetPosition() +
                (DisplayServer.ScreenGetSize() - _resolution) / 2);
        }
    }

    private void ApplyBinding(string action, Key physicalKeycode)
    {
        if (!InputMap.HasAction(action)) return;
        InputMap.ActionEraseEvents(action);
        var ev = new InputEventKey();
        ev.PhysicalKeycode = physicalKeycode;
        InputMap.ActionAddEvent(action, ev);
    }
}
