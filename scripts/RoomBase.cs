using Godot;
using System.Collections.Generic;

/// Base class for all room scenes.
/// Handles player/orb spawning, floor/wall geometry, and spawn-point registration.
public abstract partial class RoomBase : Node2D
{
    // ─── Subclass configuration ───────────────────────────────────────────────

    protected virtual int   RoomWidth  { get; } = 1920;
    protected virtual int   RoomHeight { get; } = 1080;
    protected virtual float FloorY     { get; } = 885f;
    protected virtual float WallThick  { get; } = 0f; // how far left/right walls push player inward

    // Background / palette
    protected virtual Color BgColor    { get; } = new Color(0.10f, 0.08f, 0.05f);
    protected virtual Color FloorColor { get; } = new Color(0.22f, 0.14f, 0.07f);
    protected virtual Color CeilColor  { get; } = new Color(0.07f, 0.05f, 0.04f);
    protected virtual Color WallColor  { get; } = new Color(0.16f, 0.10f, 0.07f);

    // Spawn positions — subclass populates in SetupRoom()
    protected Dictionary<string, Vector2> SpawnPoints = new();

    // ─── Lifecycle ───────────────────────────────────────────────────────────

    public override void _Ready()
    {
        SetupRoom();
        SpawnGeometry();
        SpawnPlayer();
        SpawnOrb();
        // Show game HUDs (they start hidden so the main menu is clean)
        InventoryManager.Instance?.SetHUDVisible(true);
        QuestManager.Instance?.SetHUDVisible(true);
        DialogueManager.Instance?.SetHUDVisible(true);
        GameManager.Instance?.SetPromptVisible(true);
    }

    /// Subclass override: add doors, items, NPCs, and register SpawnPoints.
    protected abstract void SetupRoom();

    // ─── Geometry ────────────────────────────────────────────────────────────

    private void SpawnGeometry()
    {
        // Full background rect
        AddColorRect(new Rect2(0, 0, RoomWidth, RoomHeight), BgColor, -10);

        // Floor (visual)
        AddColorRect(new Rect2(0, FloorY, RoomWidth, RoomHeight - FloorY), FloorColor, -9);
        // Floor wood-plank lines
        for (float x = 120; x < RoomWidth; x += 120)
            AddColorRect(new Rect2(x, FloorY + 3, 2, RoomHeight - FloorY - 6),
                         new Color(0f, 0f, 0f, 0.18f), -8);

        // Ceiling (visual)
        AddColorRect(new Rect2(0, 0, RoomWidth, 105), CeilColor, -9);
        // Baseboard trim line above floor
        AddColorRect(new Rect2(0, FloorY - 6, RoomWidth, 6), new Color(0.35f, 0.22f, 0.10f), -8);
        // Ceiling cornice line
        AddColorRect(new Rect2(0, 102, RoomWidth, 6), new Color(0.35f, 0.22f, 0.10f), -8);

        // Wallpaper pattern (vertical stripes)
        for (float x = 0; x < RoomWidth; x += 90)
            AddColorRect(new Rect2(x, 108, 3, FloorY - 114), new Color(0.3f, 0.18f, 0.10f, 0.25f), -8);

        // Floor collision
        AddFloorCollider();

        // Boundary walls — always present so the player can't walk out of bounds
        AddWallCollider(true);
        AddWallCollider(false);
    }

    private void AddFloorCollider()
    {
        var body = new StaticBody2D();
        body.CollisionLayer = 2; // world layer
        body.CollisionMask  = 0;
        var shape = new CollisionShape2D();
        var rect  = new RectangleShape2D();
        rect.Size = new Vector2(RoomWidth + 300, 60);
        shape.Shape = rect;
        shape.Position = new Vector2(RoomWidth / 2f, 30);
        body.AddChild(shape);
        body.Position = new Vector2(-150, FloorY);
        AddChild(body);
    }

    private void AddWallCollider(bool left)
    {
        var body = new StaticBody2D();
        body.CollisionLayer = 2;
        body.CollisionMask  = 0;
        var shape = new CollisionShape2D();
        var rect  = new RectangleShape2D();
        rect.Size = new Vector2(80, RoomHeight * 2);
        shape.Shape = rect;
        body.AddChild(shape);
        // Position wall half-slab outside the room edge so player stops exactly at the boundary
        body.Position = left
            ? new Vector2(-40, RoomHeight / 2f)
            : new Vector2(RoomWidth + 40, RoomHeight / 2f);
        AddChild(body);
    }

    // ─── Door helper ─────────────────────────────────────────────────────────

    /// Add a door at the given world rect. Automatically adds visual and trigger.
    protected void AddDoor(Rect2 doorRect, string label,
                           string targetScene, string targetSpawn)
    {
        // Door visual (frame + dark interior)
        var frame = AddColorRect(doorRect.Grow(9), new Color(0.38f, 0.22f, 0.09f), -7);
        AddColorRect(doorRect, new Color(0.08f, 0.06f, 0.04f), -6);
        // Door handle dot
        bool leftSide = doorRect.Position.X < RoomWidth / 2f;
        float handleX = leftSide
            ? doorRect.Position.X + doorRect.Size.X - 10
            : doorRect.Position.X + 8;
        AddColorRect(new Rect2(handleX, doorRect.Position.Y + doorRect.Size.Y / 2f - 4, 6, 8),
                     new Color(0.7f, 0.55f, 0.1f), -5);

        // Trigger Area2D
        var area = new DoorInteraction();
        area.TargetScene      = targetScene;
        area.TargetSpawnPoint = targetSpawn;
        area.InteractLabel    = label;

        var col = new CollisionShape2D();
        var sh  = new RectangleShape2D();
        sh.Size = new Vector2(doorRect.Size.X + 90, doorRect.Size.Y);
        col.Shape = sh;
        col.Position = doorRect.GetCenter();
        area.AddChild(col);
        AddChild(area);
    }

    // ─── Item helper ─────────────────────────────────────────────────────────

    protected void AddItem(ItemData item, Vector2 worldPos)
    {
        // Skip items already picked up in this session
        if (GameManager.PickedUpItems.Contains(item.ItemId)) return;

        var pickup = new ItemPickup();
        pickup.SetItem(item);
        pickup.Position = worldPos;

        // Trigger shape — large radius so it's reachable from floor level
        var col = new CollisionShape2D();
        var sh  = new CircleShape2D();
        sh.Radius = 55f;
        col.Shape = sh;
        pickup.AddChild(col);
        AddChild(pickup);
    }

    // ─── NPC helper ──────────────────────────────────────────────────────────

    protected MotherNPCController AddMother(Vector2 worldPos)
    {
        var mother = new MotherNPCController();
        mother.Position = worldPos;
        var col = new CollisionShape2D();
        var sh  = new CapsuleShape2D();
        sh.Radius = 40f;
        sh.Height = 80f;
        col.Shape = sh;
        mother.AddChild(col);
        AddChild(mother);
        return mother;
    }

    // ─── Player spawn ─────────────────────────────────────────────────────────

    private void SpawnPlayer()
    {
        // Try to load the player scene
        var playerScene = GD.Load<PackedScene>("res://scenes/player.tscn");
        if (playerScene == null)
        {
            GD.PushError("player.tscn not found — build it first.");
            return;
        }

        var player = playerScene.Instantiate<CharacterBody2D>();
        AddChild(player);

        // Determine spawn position
        string spawnName = GameManager.TargetSpawnPoint;
        if (!SpawnPoints.TryGetValue(spawnName, out Vector2 spawnPos))
            SpawnPoints.TryGetValue("DefaultSpawn", out spawnPos);

        player.GlobalPosition = spawnPos;
        player.Velocity = Vector2.Zero;

        // Apply camera limits for this room
        var camera = player.GetNodeOrNull<Camera2D>("Camera2D");
        if (camera != null)
        {
            camera.LimitLeft   = 0;
            camera.LimitRight  = RoomWidth;
            camera.LimitTop    = 0;
            camera.LimitBottom = RoomHeight;
            camera.GlobalPosition = player.GlobalPosition; // snap (no swoop)
            camera.ResetSmoothing();
        }
    }

    // ─── Orb spawn ────────────────────────────────────────────────────────────

    private void SpawnOrb()
    {
        var orbScene = GD.Load<PackedScene>("res://scenes/orb.tscn");
        if (orbScene == null) return;
        var orb = orbScene.Instantiate<Node2D>();
        AddChild(orb);
    }

    // ─── Util ─────────────────────────────────────────────────────────────────

    protected ColorRect AddColorRect(Rect2 rect, Color color, int zIndex = 0)
    {
        var cr = new ColorRect();
        cr.Color    = color;
        cr.Position = rect.Position;
        cr.Size     = rect.Size;
        cr.ZIndex   = zIndex;
        AddChild(cr);
        return cr;
    }

    /// Spawn Y for the player given the room's floor.
    protected float SpawnY => FloorY - 50f; // spawn slightly above floor; gravity lands the player
}
