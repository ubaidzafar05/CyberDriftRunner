# Cyber Drift Runner

Unity 2022.3+ mobile endless runner scaffold generated from code.

## Open and run
1. Open `/Users/azt/Desktop/Python/CyberDriftRunner` in Unity Hub with Unity 2022.3 or newer.
2. Wait for the project to import.
3. In Unity, run `Cyber Drift Runner/Create Demo Scenes and Prefabs` from the top menu.
4. Open `Assets/Scenes/MainMenu.unity` and press Play.

## What the bootstrap creates
- `MainMenu`, `GameScene`, and `GameOver` scenes
- Primitive-based prefabs for the player, drones, obstacles, projectile, and power-ups
- A playable HUD and mobile-style hack button
- Core serialized wiring for the generated scripts
- Skin shop, revive overlay, procedural audio, and mobile performance managers

## Controls
- Swipe left/right: change lane
- Swipe up: jump
- Swipe down: slide
- Tap: shoot nearest drone
- Hold hack button: hacking slow motion

## Notes
- The bootstrap uses primitive meshes and built-in UI so the project is immediately runnable without external assets.
- `GameManager` persists across scenes and stores the last run summary for the game-over screen.
- Audio is generated procedurally at runtime so the project has working music and SFX without imported clips.
- Ads and IAP run through project-local interfaces with mock services by default. Replace `MockAdService` and `MockStoreService` once the Unity monetization packages are installed.

## Monetization and shop
- Rewarded ad revive is offered once per run.
- Interstitials are triggered on the game-over flow.
- Skins can unlock via credits or premium product IDs.
- Currency earned in runs is banked into persistent progression on game over.

## Mobile polish
- `MobilePerformanceManager` targets 60 FPS, disables vsync, and prevents sleep on mobile.
- `PlayerVfxController` adds hack trails and burst particles for jump, shoot, hit, revive, and power-up beats.
- `AudioManager` swaps between menu and gameplay loops automatically by scene.
