# Game Plan: The House

## Game Description

Third-person 2D side-scrolling indoor exploration game. Player starts in a hallway, explores a house with multiple rooms connected by doors, interacts with an AI companion orb and a mother NPC, completes a simple fetch quest, and manages a small inventory.

## Risk Tasks

### 1. Sprite animation from design-sheet source
- **Why isolated:** Hero sprite sheet is a CHARACTER DESIGN SHEET (3 views: front/side/back) rather than a multi-frame animation sheet. Region extraction must be pixel-accurate; wrong frame = wrong character view.
- **Approach:** Divide sheet width by 3 → frameWidth per view. Use Sprite2D.RegionRect to select front (idle) vs side (walk). Flip horizontally for left movement.
- **Verify:** Player shows front view when idle, side view when walking right, mirrored side view when walking left. No magenta/blank region visible.

### 2. Camera2D lerp from-origin swoop
- **Why isolated:** Camera lerp initialised before first `_Process()` tick swoops from (0,0). `_initialized` flag must snap on frame 1.
- **Approach:** In `PlayerController._Ready()`, snap `Camera2D.GlobalPosition = GlobalPosition` before physics runs.
- **Verify:** No visible camera swoop when entering any room.

## Main Build

- [x] Project scaffold: project.godot, TheHouse.csproj, .gitignore
- [x] Autoloads: SettingsManager, GameManager, InventoryManager, QuestManager, DialogueManager, SceneTransitionManager
- [x] Player scene: CharacterBody2D + sprite + camera + interact area
- [x] Orb companion scene: Node2D with OrbFollower
- [x] RoomBase: shared room geometry, player/orb spawning, door/item helpers
- [x] Room scenes: HallwayScene, KitchenScene, BedroomScene, LivingRoomScene
- [x] InteractableBase + DoorInteraction + ItemPickup + MotherNPCController
- [x] Main menu scene: title, Play/Settings/Quit buttons
- [x] Settings panel: volume sliders, fullscreen toggle, sensitivity
- [x] Inventory HUD: 6-slot hotbar, mouse wheel / 1-6 keys
- [x] Quest tracker UI: top-right panel, shows active/complete quest
- [x] Dialogue box UI: bottom panel, speaker + text, E/click to advance
- [x] Interact prompt: GameManager CanvasLayer label shows "[E] Action" when near interactable
- [x] Scene builders: BuildPlayer, BuildOrb, BuildHallway, BuildKitchen, BuildBedroom, BuildLivingRoom, BuildMainMenu
- [x] dotnet build + godot --headless --import
- [x] Run scene builders in order
- [x] godot --headless --quit verification
- [x] HUD visibility: hotbar/prompt hidden on main menu, shown in game rooms
- [x] Hero sprite white background removed (PIL threshold >230)

## Bug Fixes (post-MVP)

- [x] Sprite region seam fixed — inset RegionRect 1px at interior edges to stop LinearWithMipmaps bleed
- [x] Walk-bob animation — sine-wave offset drives subtle step rhythm
- [x] Apple pickup — lowered to FloorY-21 so player capsule always overlaps; hitbox radius 36→55
- [x] Out-of-bounds walking — RoomBase now always adds boundary wall colliders at room edges
- [x] GameDialogues.cs — all dialogue content centralised; MotherNPCController + HallwayScene use it
- [x] Quest dialogue continue bug — DialogueManager.JustFinished flag blocks same-frame re-trigger
- [x] Key rebinding UI in settings (move_left/right, run, interact) + Reset to defaults
- [x] Resolution presets in settings (720p / 900p / 1080p / 1440p) + auto-center on apply
- [x] Fullscreen fix — refresh guard prevents double-fire; ApplyFullscreen restores saved resolution on un-fullscreen

## Verify

- Player walks left/right with correct sprite flip; no seam line visible on direction change
- Walk animation shows subtle up/down bob
- Apple can be picked up from the table
- Player cannot walk past left/right room edges in any room
- All dialogue text is served from GameDialogues.cs
- Pressing E on the last quest-dialogue line does NOT immediately trigger the "no item yet" dialogue
- Rebind keys in settings; new keys work in-game; survives quit-reopen
- Resolution change resizes window correctly; fullscreen toggle works
