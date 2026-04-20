using Godot;

/// The hallway — the starting room.
/// Contains: apple item on the table, door to kitchen (left), door to bedroom (right).
public partial class HallwayScene : RoomBase
{
    protected override int   RoomWidth  => 3000;
    protected override float FloorY     => 885f;

    // Richer wallpaper tint for the hallway
    protected override Color WallColor  => new Color(0.20f, 0.12f, 0.08f);

    protected override void SetupRoom()
    {
        // ── Spawn points ──────────────────────────────────────────────────────
        SpawnPoints["DefaultSpawn"]      = new Vector2(960f,  SpawnY);
        SpawnPoints["SpawnFromKitchen"]  = new Vector2(300f,  SpawnY);
        SpawnPoints["SpawnFromBedroom"]  = new Vector2(2700f, SpawnY);
        SpawnPoints["SpawnFromLivingRoom"] = new Vector2(2000f, SpawnY);

        // ── Orb intro dialogue after a short delay ────────────────────────────
        var timer = new Timer();
        timer.WaitTime = 1.2f;
        timer.OneShot = true;
        AddChild(timer);
        timer.Timeout += OrbIntro;
        timer.Start();

        // ── Table ─────────────────────────────────────────────────────────────
        float tableX = 1125f, tableY = FloorY - 90f;
        AddColorRect(new Rect2(tableX - 60, tableY, 120, 15), new Color(0.4f, 0.25f, 0.1f));       // top
        AddColorRect(new Rect2(tableX - 57, tableY + 15, 9, 75), new Color(0.32f, 0.18f, 0.07f));  // left leg
        AddColorRect(new Rect2(tableX + 48, tableY + 15, 9, 75), new Color(0.32f, 0.18f, 0.07f));  // right leg

        // ── Apple item — placed at floor level so the player hitbox always reaches it
        var apple = new ItemData("apple", "Apple", "A fresh green apple.", new Color(0.15f, 0.7f, 0.15f));
        AddItem(apple, new Vector2(tableX, FloorY - 21f));

        // ── Key item (near bedroom door) ──────────────────────────────────────
        var key = new ItemData("key", "Key", "An old brass key.", new Color(0.85f, 0.65f, 0.1f));
        AddItem(key, new Vector2(2250f, FloorY - 21f));

        // ── Note item (on the floor) ──────────────────────────────────────────
        var note = new ItemData("note", "Note", "A crumpled piece of paper.", new Color(0.9f, 0.88f, 0.7f));
        AddItem(note, new Vector2(1440f, FloorY - 21f));

        // ── Door to Kitchen (left) ────────────────────────────────────────────
        AddDoor(
            new Rect2(60f, FloorY - 345f, 150f, 345f),
            "Enter Kitchen",
            "res://scenes/kitchen.tscn",
            "SpawnFromHallway"
        );

        // ── Door to Bedroom (right) ───────────────────────────────────────────
        AddDoor(
            new Rect2(2790f, FloorY - 345f, 150f, 345f),
            "Enter Bedroom",
            "res://scenes/bedroom.tscn",
            "SpawnFromHallway"
        );

        // ── Door to Living Room (center-right) ───────────────────────────────
        AddDoor(
            new Rect2(2100f, FloorY - 345f, 150f, 345f),
            "Enter Living Room",
            "res://scenes/living_room.tscn",
            "SpawnFromHallway"
        );

        // ── Ambient wall lamps ────────────────────────────────────────────────
        AddLamp(new Vector2(600f,  300f));
        AddLamp(new Vector2(1800f, 300f));
        AddLamp(new Vector2(2700f, 300f));
    }

    private void OrbIntro()
    {
        // Show only once per session
        if (GameManager.OrbIntroShown) return;
        if (QuestManager.Instance.IsQuestActive("HelpMother") ||
            QuestManager.Instance.IsQuestComplete("HelpMother"))
        {
            GameManager.OrbIntroShown = true;
            return;
        }

        GameManager.OrbIntroShown = true;

        DialogueManager.Instance.StartDialogue(GameDialogues.OrbIntro());
    }

    private void AddLamp(Vector2 pos)
    {
        AddColorRect(new Rect2(pos.X - 9,  pos.Y,      18, 30), new Color(0.6f, 0.45f, 0.2f),        -6); // bracket
        AddColorRect(new Rect2(pos.X - 15, pos.Y + 30, 30, 24), new Color(0.9f, 0.75f, 0.3f),        -5); // lamp
        AddColorRect(new Rect2(pos.X - 60, pos.Y + 54, 120, 120), new Color(0.9f, 0.75f, 0.3f, 0.06f), -7); // glow
    }
}
