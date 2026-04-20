# Memory

## Toolchain
- Godot: 4.6.2.stable.mono
- Godot.NET.Sdk: 4.6.2 (from /Applications/Godot_mono.app)
- dotnet: 9.0.115
- TargetFramework: net9.0
- Python: 3.12 available at python3.12

## Sprite assets
- Hero spritesheet: `sprites/hero_spritesheet.png` — 1274×880 RGBA. Three character views side by side. frameWidth = 1274/3 ≈ 424px. Front view at x=0, side view at x=424, back at x=848. White background removed with PIL threshold >230.
- Mother sprite: `sprites/mother_sprite.png` — processed from mother_sprite.jpg (2480×3508), white background removed with PIL threshold >220.
- PIL BG removal pattern: `white_mask = (r > T) & (g > T) & (b > T); data[white_mask, 3] = 0`

## HUD visibility pattern
- All autoload HUDs start hidden (`_hudLayer.Visible = false` in _Ready())
- RoomBase._Ready() shows all HUDs after SpawnPlayer() via Instance?.SetHUDVisible(true)
- Main menu never calls SetHUDVisible, so hotbar stays hidden there automatically

## Headless capture quirks
- `--script test/TestTask.cs` bypasses `run/main_scene`; must call ChangeSceneToFile() in _Initialize()
- `--write-movie` requires explicit `.png` extension (e.g. `screenshots/frame.png`, not `screenshots/frame`)

## Architecture decisions
- Game is 2D side-scrolling with CharacterBody2D player
- All room geometry created at runtime (not in .tscn files) via RoomBase helper methods
- Autoloads create their own CanvasLayer UI in _Ready() — no external scene files needed for HUD
- Scene transitions: SceneTransitionManager.GoToScene() → fade-out, ChangeSceneToFile(), fade-in
- Player spawn: GameManager.TargetSpawnPoint string, consumed by RoomBase._Ready()
- Interaction: InteractableBase (Area2D) BodyEntered/Exited → GameManager.SetNearestInteractable

## Image generation
- Gemini API key available but both keys are on free tier with quota = 0 for gemini-3.1-flash-image
- Reference.png not generated; proceeding without visual target

## Post-MVP bug fixes applied (2026-04-20)
- Sprite seam: inset RegionRect by 1px on interior edges; LinearWithMipmaps bleeds across atlas boundaries
- Quest dialogue double-trigger: DialogueManager.JustFinished flag (true for 1 frame after close); PlayerController.HandleInteractInput checks it
- Out-of-bounds: RoomBase.SpawnGeometry always calls AddWallCollider (removed WallThick > 0 guard)
- Apple pickup: lowered to floor level (FloorY-21) + hitbox radius 55px
- GameDialogues.cs centralises all dialogue; scripts call static methods
- SettingsManager: Resolution + keybinding persistence; ApplyFullscreen restores window size on un-fullscreen
- MainMenuUI: _refreshing flag prevents slider/toggle callbacks from firing during RefreshSettingsControls

## Known issues / quirks applied
- SetScript() quirk: temp parent pattern used in all scene builders
- Camera2D lerp swoop: _initialized guard applied in PlayerController._Ready()
- Camera2D.ResetSmoothing() called after positioning to prevent frame-0 swoop
