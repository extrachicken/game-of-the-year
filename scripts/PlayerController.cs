using Godot;

/// Player character — CharacterBody2D.
/// Handles WASD movement, gravity, sprite animation, and interaction dispatch.
public partial class PlayerController : CharacterBody2D
{
    [Export] public float WalkSpeed = 210f;
    [Export] public float RunSpeed  = 350f;
    [Export] public float Gravity   = 900f;

    // Sprite sheet layout: 3 views side by side (front, side-right, back).
    // Each view occupies exactly 1/3 of the image width.
    private Sprite2D    _sprite      = null!;
    private Camera2D    _camera      = null!;
    private Area2D      _interactArea = null!;

    private int  _spriteWidth;   // full texture width
    private int  _spriteHeight;  // full texture height
    private int  _frameWidth;    // width of one character view

    private bool  _facingRight = true;
    private bool  _initialized = false; // camera lerp guard (see quirks.md)
    private float _baseOffsetY = 0f;    // sprite feet-at-origin offset
    private float _walkPhase   = 0f;    // drives walk-bob sine wave

    public override void _Ready()
    {
        AddToGroup("player");

        _sprite       = GetNode<Sprite2D>("Sprite2D");
        _camera       = GetNode<Camera2D>("Camera2D");
        _interactArea = GetNode<Area2D>("InteractionArea");

        // Measure sprite sheet and set quality / scale for 1080p
        if (_sprite.Texture != null)
        {
            _spriteWidth  = _sprite.Texture.GetWidth();
            _spriteHeight = _sprite.Texture.GetHeight();
            _frameWidth   = _spriteWidth / 3;

            _sprite.TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmaps;
            float scale = 240f / _spriteHeight; // ~240px tall at 1080p
            _sprite.Scale = new Vector2(scale, scale);
            _baseOffsetY = -_spriteHeight / 2f + 72f; // feet at origin
            _sprite.Offset = new Vector2(0, _baseOffsetY);

            _sprite.RegionEnabled = true;
            SetIdlePose(); // start with front view
        }

        // Snap camera to player on first frame (prevents lerp swoop from origin)
        _camera.GlobalPosition = GlobalPosition;
        _initialized = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        var vel = Velocity;

        // Gravity
        if (!IsOnFloor())
            vel.Y += Gravity * dt;
        else if (vel.Y > 0)
            vel.Y = 0f;

        // Block movement while dialogue is open
        if (DialogueManager.Instance.IsShowing)
        {
            vel.X = Mathf.MoveToward(vel.X, 0f, WalkSpeed * 6f * dt);
            Velocity = vel;
            MoveAndSlide();
            UpdateSpriteAnimation(0f, dt);
            return;
        }

        // Horizontal input
        float dir = Input.GetAxis("move_left", "move_right");
        float speed = Input.IsActionPressed("run") ? RunSpeed : WalkSpeed;
        float targetX = dir * speed;

        // Smooth acceleration / deceleration
        vel.X = Mathf.MoveToward(vel.X, targetX, speed * 8f * dt);

        Velocity = vel;
        MoveAndSlide();

        UpdateSpriteAnimation(dir, dt);
        HandleInventoryInput();
        HandleInteractInput();
    }

    // ─── Sprite Animation ─────────────────────────────────────────────────────

    private void SetIdlePose()
    {
        // Front view = first column; inset 1px on right edge to stop seam bleed
        _sprite.RegionRect = new Rect2(0, 0, _frameWidth - 1, _spriteHeight);
        _sprite.FlipH = false;
    }

    private void SetWalkPose(bool movingRight)
    {
        // Side view = second column; inset 1px on both interior edges to stop seam bleed
        _sprite.RegionRect = new Rect2(_frameWidth + 1, 0, _frameWidth - 2, _spriteHeight);
        _sprite.FlipH = !movingRight;
    }

    private void UpdateSpriteAnimation(float direction, float dt)
    {
        if (Mathf.Abs(direction) > 0.05f)
        {
            bool right = direction > 0f;
            if (_facingRight != right)
                _facingRight = right;
            SetWalkPose(_facingRight);

            // Walk-bob: subtle up/down displacement creates a footstep rhythm
            _walkPhase += dt * 10f;
            float bob = Mathf.Sin(_walkPhase * 2f) * 2.5f;
            _sprite.Offset = new Vector2(0, _baseOffsetY + bob);
        }
        else
        {
            SetIdlePose();
            _sprite.Offset = new Vector2(0, _baseOffsetY);
        }
    }

    // ─── Input ────────────────────────────────────────────────────────────────

    private void HandleInventoryInput()
    {
        if (Input.IsActionJustPressed("inventory_next")) InventoryManager.Instance.SelectNext();
        if (Input.IsActionJustPressed("inventory_prev")) InventoryManager.Instance.SelectPrev();
        if (Input.IsActionJustPressed("slot_1")) InventoryManager.Instance.SelectSlot(0);
        if (Input.IsActionJustPressed("slot_2")) InventoryManager.Instance.SelectSlot(1);
        if (Input.IsActionJustPressed("slot_3")) InventoryManager.Instance.SelectSlot(2);
        if (Input.IsActionJustPressed("slot_4")) InventoryManager.Instance.SelectSlot(3);
        if (Input.IsActionJustPressed("slot_5")) InventoryManager.Instance.SelectSlot(4);
        if (Input.IsActionJustPressed("slot_6")) InventoryManager.Instance.SelectSlot(5);
    }

    private void HandleInteractInput()
    {
        if (!Input.IsActionJustPressed("interact")) return;
        if (DialogueManager.Instance.IsShowing) return;  // DialogueManager handles E itself
        if (DialogueManager.Instance.JustFinished) return; // same E press that closed dialogue

        var interactable = GameManager.NearestInteractable;
        interactable?.Interact(this);
    }
}
