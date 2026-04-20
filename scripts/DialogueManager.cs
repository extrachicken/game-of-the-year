using Godot;
using System;
using System.Collections.Generic;

/// A single line in a dialogue sequence.
public struct DialogueLine
{
    public string Speaker;
    public string Text;

    public DialogueLine(string speaker, string text)
    {
        Speaker = speaker;
        Text    = text;
    }
}

/// Autoload singleton — manages dialogue display.
/// Call StartDialogue() to queue lines; player advances with E or left-click.
public partial class DialogueManager : Node
{
    public static DialogueManager Instance { get; private set; } = null!;

    private Queue<DialogueLine> _lineQueue = new();
    private bool _isShowing = false;
    private bool _justFinished = false;
    private Action? _onComplete;

    /// True for exactly one frame after a dialogue closes — lets callers ignore
    /// the same E press that dismissed the final line.
    public bool JustFinished => _justFinished;

    // UI elements
    private CanvasLayer _layer = null!;
    private Panel _panel = null!;
    private Label _speakerLabel = null!;
    private Label _textLabel = null!;
    private Label _advanceHint = null!;

    public bool IsShowing => _isShowing;

    public override void _Process(double delta)
    {
        if (_justFinished) _justFinished = false;
    }

    public override void _Ready()
    {
        Instance = this;
        BuildDialogueUI();
    }

    public override void _Input(InputEvent @event)
    {
        if (!_isShowing) return;

        bool advance = Input.IsActionJustPressed("interact")
                    || (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left);

        if (advance)
        {
            AdvanceLine();
            GetViewport().SetInputAsHandled();
        }
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    public void StartDialogue(List<DialogueLine> lines, Action? onComplete = null)
    {
        _lineQueue.Clear();
        foreach (var line in lines)
            _lineQueue.Enqueue(line);
        _onComplete = onComplete;
        _isShowing = true;
        _panel.Visible = true;
        ShowNextLine();
    }

    public void StopDialogue()
    {
        _isShowing = false;
        _panel.Visible = false;
        _lineQueue.Clear();
    }

    public void SetHUDVisible(bool visible) => _layer.Visible = visible;

    // ─── Internal ─────────────────────────────────────────────────────────────

    private void ShowNextLine()
    {
        if (_lineQueue.Count == 0)
        {
            FinishDialogue();
            return;
        }
        var line = _lineQueue.Dequeue();
        _speakerLabel.Text = line.Speaker;
        _textLabel.Text = line.Text;
        _advanceHint.Text = _lineQueue.Count > 0 ? "[E] Continue" : "[E] Close";
    }

    private void AdvanceLine()
    {
        if (_lineQueue.Count == 0)
        {
            FinishDialogue();
            return;
        }
        ShowNextLine();
    }

    private void FinishDialogue()
    {
        _isShowing = false;
        _justFinished = true;
        _panel.Visible = false;
        _onComplete?.Invoke();
        _onComplete = null;
    }

    // ─── UI Building ─────────────────────────────────────────────────────────

    private void BuildDialogueUI()
    {
        _layer = new CanvasLayer();
        _layer.Layer = 15;
        AddChild(_layer);

        _panel = new Panel();
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.04f, 0.04f, 0.07f, 0.94f);
        style.CornerRadiusTopLeft = style.CornerRadiusTopRight = 10;
        style.CornerRadiusBottomLeft = style.CornerRadiusBottomRight = 10;
        style.BorderColor = new Color(0.5f, 0.42f, 0.12f, 0.9f);
        style.BorderWidthBottom = style.BorderWidthTop = style.BorderWidthLeft = style.BorderWidthRight = 1;
        _panel.AddThemeStyleboxOverride("panel", style);

        // Bottom-wide: left/right/bottom anchored, top offset defines height
        _panel.AnchorLeft   = 0f;
        _panel.AnchorRight  = 1f;
        _panel.AnchorTop    = 1f;
        _panel.AnchorBottom = 1f;
        _panel.OffsetLeft   = 40f;
        _panel.OffsetRight  = -40f;
        _panel.OffsetTop    = -155f;
        _panel.OffsetBottom = -15f;
        _panel.Visible = false;
        _layer.AddChild(_panel);

        var vbox = new VBoxContainer();
        // Fill the panel using anchors
        vbox.AnchorLeft   = 0f;
        vbox.AnchorRight  = 1f;
        vbox.AnchorTop    = 0f;
        vbox.AnchorBottom = 1f;
        vbox.OffsetLeft   = 20f;
        vbox.OffsetRight  = -20f;
        vbox.OffsetTop    = 12f;
        vbox.OffsetBottom = -12f;
        _panel.AddChild(vbox);

        // Speaker name row
        _speakerLabel = new Label();
        _speakerLabel.Text = "";
        _speakerLabel.AddThemeColorOverride("font_color", new Color(1f, 0.88f, 0.25f));
        _speakerLabel.AddThemeFontSizeOverride("font_size", 15);
        vbox.AddChild(_speakerLabel);

        // Dialogue text
        _textLabel = new Label();
        _textLabel.Text = "";
        _textLabel.AddThemeColorOverride("font_color", new Color(0.94f, 0.94f, 0.96f));
        _textLabel.AddThemeFontSizeOverride("font_size", 14);
        _textLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _textLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        vbox.AddChild(_textLabel);

        // Advance hint (bottom-right)
        _advanceHint = new Label();
        _advanceHint.Text = "[E] Continue";
        _advanceHint.HorizontalAlignment = HorizontalAlignment.Right;
        _advanceHint.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        _advanceHint.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(_advanceHint);
    }
}
