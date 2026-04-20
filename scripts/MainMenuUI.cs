using Godot;
using System.Collections.Generic;

/// Main menu scene controller.
/// Settings panel includes: audio, display (fullscreen + resolution), and key rebinding.
public partial class MainMenuUI : Control
{
    // Settings controls
    private Control      _settingsPanel     = null!;
    private HSlider      _masterSlider      = null!;
    private HSlider      _musicSlider       = null!;
    private HSlider      _sfxSlider         = null!;
    private HSlider      _sensitivitySlider = null!;
    private CheckButton  _fullscreenBtn     = null!;
    private OptionButton _resolutionOption  = null!;

    // Keybinding rows: action → current-key display button
    private Dictionary<string, Button> _bindButtons = new();

    private bool   _settingsOpen   = false;
    private bool   _refreshing     = false; // suppresses slider/toggle callbacks during RefreshSettingsControls
    private bool   _waitingForKey  = false;
    private string _rebindAction   = "";
    private Button? _rebindTarget  = null;

    // Preset resolutions — (width, height, label)
    private static readonly (int W, int H, string Label)[] Resolutions = new[]
    {
        (1280,  720,  "1280 × 720"),
        (1600,  900,  "1600 × 900"),
        (1920, 1080, "1920 × 1080"),
        (2560, 1440, "2560 × 1440"),
    };

    // Rebindable actions
    private static readonly (string Action, string Label)[] BindableActions = new[]
    {
        ("move_left",  "Move Left"),
        ("move_right", "Move Right"),
        ("run",        "Run"),
        ("interact",   "Interact"),
    };

    // Colors
    private static readonly Color PanelBg     = new(0.05f, 0.04f, 0.07f, 0.97f);
    private static readonly Color ButtonBg    = new(0.14f, 0.10f, 0.18f, 1f);
    private static readonly Color ButtonHover = new(0.24f, 0.18f, 0.30f, 1f);
    private static readonly Color Gold        = new(0.95f, 0.82f, 0.2f);
    private static readonly Color Rebinding   = new(0.8f, 0.4f, 0.1f, 1f);

    public override void _Ready()
    {
        AnchorLeft = 0; AnchorRight = 1; AnchorTop = 0; AnchorBottom = 1;

        var bg = new ColorRect();
        bg.Color = new Color(0.04f, 0.03f, 0.06f);
        bg.AnchorLeft = 0; bg.AnchorRight = 1; bg.AnchorTop = 0; bg.AnchorBottom = 1;
        AddChild(bg);

        var stripe = new ColorRect();
        stripe.Color = new Color(0.5f, 0.4f, 0.1f, 0.06f);
        stripe.Position = new Vector2(750, 0);
        stripe.Size = new Vector2(420, 1080);
        AddChild(stripe);

        var title = new Label();
        title.Text = "THE  HOUSE";
        title.Position = new Vector2(0, 240);
        title.Size = new Vector2(1920, 100);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeColorOverride("font_color", Gold);
        title.AddThemeFontSizeOverride("font_size", 80);
        AddChild(title);

        var subtitle = new Label();
        subtitle.Text = "A story in rooms";
        subtitle.Position = new Vector2(0, 348);
        subtitle.Size = new Vector2(1920, 40);
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.AddThemeColorOverride("font_color", new Color(0.7f, 0.65f, 0.55f));
        subtitle.AddThemeFontSizeOverride("font_size", 26);
        AddChild(subtitle);

        var div = new ColorRect();
        div.Color = new Color(0.6f, 0.5f, 0.15f, 0.5f);
        div.Position = new Vector2(795, 408);
        div.Size = new Vector2(330, 2);
        AddChild(div);

        float btnCenterX = 960f, btnY = 460f, btnSpacing = 100f;
        MakeButton("  Play",     new Vector2(btnCenterX, btnY),                OnPlay);
        MakeButton("  Settings", new Vector2(btnCenterX, btnY + btnSpacing),   OnSettings);
        MakeButton("  Quit",     new Vector2(btnCenterX, btnY + btnSpacing*2), OnQuit);

        var ver = new Label();
        ver.Text = "v0.1 MVP";
        ver.SetAnchorsPreset(LayoutPreset.BottomRight);
        ver.Position = new Vector2(-140, -44);
        ver.Size = new Vector2(130, 34);
        ver.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.4f));
        ver.AddThemeFontSizeOverride("font_size", 16);
        AddChild(ver);

        _settingsPanel = BuildSettingsPanel();
        _settingsPanel.Visible = false;
        AddChild(_settingsPanel);
    }

    // ─── Input — key rebinding capture ───────────────────────────────────────

    public override void _Input(InputEvent @event)
    {
        if (!_waitingForKey) return;
        if (@event is not InputEventKey keyEvent) return;
        if (!keyEvent.Pressed || keyEvent.Echo) return;

        GetViewport().SetInputAsHandled();

        if (keyEvent.PhysicalKeycode == Key.Escape)
        {
            CancelRebind();
            return;
        }

        var keycode = keyEvent.PhysicalKeycode != Key.None
            ? keyEvent.PhysicalKeycode
            : keyEvent.Keycode;

        SettingsManager.Instance.SetBinding(_rebindAction, keycode);
        SettingsManager.Instance.SaveSettings();

        if (_rebindTarget != null)
            _rebindTarget.Text = OS.GetKeycodeString(keycode);

        _waitingForKey = false;
        _rebindTarget  = null;
        _rebindAction  = "";
    }

    // ─── Button factory ───────────────────────────────────────────────────────

    private void MakeButton(string text, Vector2 center, System.Action callback)
    {
        const float W = 320f, H = 72f;
        var btn = new Button();
        btn.Text = text;
        btn.Position = new Vector2(center.X - W / 2f, center.Y);
        btn.Size = new Vector2(W, H);
        btn.AddThemeStyleboxOverride("normal",  ButtonStyleBox(ButtonBg));
        btn.AddThemeStyleboxOverride("hover",   ButtonStyleBox(ButtonHover));
        btn.AddThemeStyleboxOverride("pressed", ButtonStyleBox(new Color(0.08f, 0.06f, 0.12f)));
        btn.AddThemeColorOverride("font_color", new Color(0.92f, 0.90f, 0.86f));
        btn.AddThemeFontSizeOverride("font_size", 28);
        btn.Pressed += () => callback();
        AddChild(btn);
    }

    private StyleBoxFlat ButtonStyleBox(Color bg)
    {
        var s = new StyleBoxFlat();
        s.BgColor = bg;
        s.CornerRadiusTopLeft = s.CornerRadiusTopRight = 6;
        s.CornerRadiusBottomLeft = s.CornerRadiusBottomRight = 6;
        s.BorderColor = new Color(0.6f, 0.5f, 0.15f, 0.7f);
        s.BorderWidthBottom = s.BorderWidthTop = s.BorderWidthLeft = s.BorderWidthRight = 1;
        s.ContentMarginLeft = 20;
        return s;
    }

    // ─── Button callbacks ─────────────────────────────────────────────────────

    private void OnPlay()     => SceneTransitionManager.Instance.GoToScene("res://scenes/hallway.tscn", "DefaultSpawn");
    private void OnSettings() { _settingsOpen = !_settingsOpen; _settingsPanel.Visible = _settingsOpen; if (_settingsOpen) RefreshSettingsControls(); }
    private void OnQuit()     => GetTree().Quit();

    // ─── Settings panel ───────────────────────────────────────────────────────

    private Control BuildSettingsPanel()
    {
        var panel = new Panel();
        panel.Position = new Vector2(540f, 60f);
        panel.Size = new Vector2(840f, 960f);

        var style = new StyleBoxFlat();
        style.BgColor = PanelBg;
        style.CornerRadiusTopLeft = style.CornerRadiusTopRight = 12;
        style.CornerRadiusBottomLeft = style.CornerRadiusBottomRight = 12;
        style.BorderColor = new Color(0.6f, 0.5f, 0.15f, 0.8f);
        style.BorderWidthBottom = style.BorderWidthTop = style.BorderWidthLeft = style.BorderWidthRight = 1;
        panel.AddThemeStyleboxOverride("panel", style);

        var scroll = new ScrollContainer();
        scroll.AnchorLeft   = 0; scroll.AnchorRight  = 1;
        scroll.AnchorTop    = 0; scroll.AnchorBottom = 1;
        scroll.OffsetLeft   = 0; scroll.OffsetRight  = 0;
        scroll.OffsetTop    = 0; scroll.OffsetBottom = 0;
        panel.AddChild(scroll);

        var vbox = new VBoxContainer();
        vbox.OffsetLeft = 40; vbox.OffsetTop = 36;
        vbox.CustomMinimumSize = new Vector2(760, 0);
        vbox.AddThemeConstantOverride("separation", 22);
        scroll.AddChild(vbox);

        // Title
        var title = new Label();
        title.Text = "Settings";
        title.AddThemeColorOverride("font_color", Gold);
        title.AddThemeFontSizeOverride("font_size", 32);
        vbox.AddChild(title);

        // ── Audio ─────────────────────────────────────────────────────────────
        AddSectionHeader(vbox, "Audio");
        _masterSlider     = AddSlider(vbox, "Master Volume",     0, 1, 0.01f, OnMasterVolume);
        _musicSlider      = AddSlider(vbox, "Music Volume",      0, 1, 0.01f, OnMusicVolume);
        _sfxSlider        = AddSlider(vbox, "SFX Volume",        0, 1, 0.01f, OnSfxVolume);
        _sensitivitySlider = AddSlider(vbox, "Mouse Sensitivity", 0.1f, 5f, 0.1f, OnSensitivity);

        // ── Display ───────────────────────────────────────────────────────────
        AddSectionHeader(vbox, "Display");

        // Fullscreen row
        var fsRow = new HBoxContainer();
        var fsLabel = new Label();
        fsLabel.Text = "Fullscreen";
        fsLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        fsLabel.AddThemeColorOverride("font_color", Colors.WhiteSmoke);
        fsRow.AddChild(fsLabel);
        _fullscreenBtn = new CheckButton();
        _fullscreenBtn.Toggled += (pressed) =>
        {
            if (_refreshing) return;
            SettingsManager.Instance.Fullscreen = pressed;
            SettingsManager.Instance.SaveSettings();
        };
        fsRow.AddChild(_fullscreenBtn);
        vbox.AddChild(fsRow);

        // Resolution row
        var resRow = new HBoxContainer();
        var resLabel = new Label();
        resLabel.Text = "Resolution  (windowed)";
        resLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        resLabel.AddThemeColorOverride("font_color", Colors.WhiteSmoke);
        resRow.AddChild(resLabel);
        _resolutionOption = new OptionButton();
        _resolutionOption.CustomMinimumSize = new Vector2(200, 0);
        foreach (var (W, H, Lbl) in Resolutions)
            _resolutionOption.AddItem(Lbl);
        _resolutionOption.ItemSelected += (idx) =>
        {
            if (_refreshing) return;
            var (W, H, _) = Resolutions[idx];
            SettingsManager.Instance.Resolution = new Vector2I(W, H);
            SettingsManager.Instance.SaveSettings();
        };
        resRow.AddChild(_resolutionOption);
        vbox.AddChild(resRow);

        // ── Key Bindings ──────────────────────────────────────────────────────
        AddSectionHeader(vbox, "Key Bindings");

        foreach (var (action, label) in BindableActions)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 12);

            var lbl = new Label();
            lbl.Text = label;
            lbl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            lbl.AddThemeColorOverride("font_color", Colors.WhiteSmoke);
            row.AddChild(lbl);

            var bindBtn = new Button();
            bindBtn.CustomMinimumSize = new Vector2(160, 0);
            bindBtn.AddThemeStyleboxOverride("normal", ButtonStyleBox(ButtonBg));
            bindBtn.AddThemeStyleboxOverride("hover",  ButtonStyleBox(ButtonHover));
            bindBtn.AddThemeColorOverride("font_color", Colors.WhiteSmoke);
            bindBtn.AddThemeFontSizeOverride("font_size", 14);

            string act = action; // capture for lambda
            bindBtn.Pressed += () => StartRebind(act, bindBtn);

            _bindButtons[action] = bindBtn;
            row.AddChild(bindBtn);
            vbox.AddChild(row);
        }

        // Reset bindings button
        var resetBtn = new Button();
        resetBtn.Text = "Reset Key Bindings to Default";
        resetBtn.AddThemeStyleboxOverride("normal", ButtonStyleBox(new Color(0.18f, 0.06f, 0.06f)));
        resetBtn.AddThemeStyleboxOverride("hover",  ButtonStyleBox(new Color(0.30f, 0.10f, 0.10f)));
        resetBtn.AddThemeColorOverride("font_color", Colors.WhiteSmoke);
        resetBtn.AddThemeFontSizeOverride("font_size", 13);
        resetBtn.Pressed += OnResetBindings;
        vbox.AddChild(resetBtn);

        // Close
        var closeBtn = new Button();
        closeBtn.Text = "Close";
        closeBtn.AddThemeStyleboxOverride("normal", ButtonStyleBox(ButtonBg));
        closeBtn.AddThemeStyleboxOverride("hover",  ButtonStyleBox(ButtonHover));
        closeBtn.AddThemeColorOverride("font_color", Colors.WhiteSmoke);
        closeBtn.Pressed += () => { CancelRebind(); _settingsPanel.Visible = false; _settingsOpen = false; };
        vbox.AddChild(closeBtn);

        return panel;
    }

    private void AddSectionHeader(VBoxContainer parent, string text)
    {
        var lbl = new Label();
        lbl.Text = text;
        lbl.AddThemeColorOverride("font_color", Gold);
        lbl.AddThemeFontSizeOverride("font_size", 16);
        parent.AddChild(lbl);
    }

    private HSlider AddSlider(VBoxContainer parent, string labelText, float min, float max, float step,
                              System.Action<double> onChange)
    {
        var row = new VBoxContainer();
        row.AddThemeConstantOverride("separation", 4);

        var lbl = new Label();
        lbl.Text = labelText;
        lbl.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        lbl.AddThemeFontSizeOverride("font_size", 13);
        row.AddChild(lbl);

        var slider = new HSlider();
        slider.MinValue = min; slider.MaxValue = max; slider.Step = step;
        slider.CustomMinimumSize = new Vector2(0, 24);
        slider.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        slider.ValueChanged += (val) => { if (!_refreshing) onChange(val); };
        row.AddChild(slider);

        parent.AddChild(row);
        return slider;
    }

    private void OnMasterVolume(double v) { SettingsManager.Instance.MasterVolume = (float)v; SettingsManager.Instance.SaveSettings(); }
    private void OnMusicVolume(double v)  { SettingsManager.Instance.MusicVolume  = (float)v; SettingsManager.Instance.SaveSettings(); }
    private void OnSfxVolume(double v)    { SettingsManager.Instance.SfxVolume    = (float)v; SettingsManager.Instance.SaveSettings(); }
    private void OnSensitivity(double v)  { SettingsManager.Instance.MouseSensitivity = (float)v; SettingsManager.Instance.SaveSettings(); }

    // ─── Rebinding ────────────────────────────────────────────────────────────

    private void StartRebind(string action, Button btn)
    {
        if (_waitingForKey) CancelRebind();
        _waitingForKey = true;
        _rebindAction  = action;
        _rebindTarget  = btn;
        btn.Text = "Press any key…";
        btn.AddThemeStyleboxOverride("normal", ButtonStyleBox(Rebinding));
    }

    private void CancelRebind()
    {
        if (!_waitingForKey) return;
        if (_rebindTarget != null)
        {
            var key = SettingsManager.Instance.GetBinding(_rebindAction);
            _rebindTarget.Text = OS.GetKeycodeString(key);
            _rebindTarget.AddThemeStyleboxOverride("normal", ButtonStyleBox(ButtonBg));
        }
        _waitingForKey = false;
        _rebindTarget  = null;
        _rebindAction  = "";
    }

    private void OnResetBindings()
    {
        // Re-load project defaults by restarting the InputMap actions from project.godot.
        // Simplest approach: clear saved bindings, clear InputMap overrides for each action,
        // then re-add the default keys we know from project.godot.
        var defaults = new Dictionary<string, Key>
        {
            { "move_left",  Key.A },
            { "move_right", Key.D },
            { "run",        Key.Shift },
            { "interact",   Key.E },
        };
        foreach (var (action, key) in defaults)
            SettingsManager.Instance.SetBinding(action, key);
        SettingsManager.Instance.SaveSettings();
        RefreshKeyBindingButtons();
    }

    // ─── Refresh ──────────────────────────────────────────────────────────────

    private void RefreshSettingsControls()
    {
        _refreshing = true;

        var sm = SettingsManager.Instance;
        _masterSlider.Value      = sm.MasterVolume;
        _musicSlider.Value       = sm.MusicVolume;
        _sfxSlider.Value         = sm.SfxVolume;
        _sensitivitySlider.Value = sm.MouseSensitivity;
        _fullscreenBtn.ButtonPressed = sm.Fullscreen;

        // Select matching resolution preset
        var res = sm.Resolution;
        for (int i = 0; i < Resolutions.Length; i++)
        {
            if (Resolutions[i].W == res.X && Resolutions[i].H == res.Y)
            {
                _resolutionOption.Selected = i;
                break;
            }
        }

        RefreshKeyBindingButtons();

        _refreshing = false;
    }

    private void RefreshKeyBindingButtons()
    {
        foreach (var (action, _) in BindableActions)
        {
            if (!_bindButtons.TryGetValue(action, out var btn)) continue;
            var key = SettingsManager.Instance.GetBinding(action);
            btn.Text = key != Key.None ? OS.GetKeycodeString(key) : "---";
            btn.AddThemeStyleboxOverride("normal", ButtonStyleBox(ButtonBg));
        }
    }
}
