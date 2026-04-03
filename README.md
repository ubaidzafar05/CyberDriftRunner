# Cyber Drift Runner

Unity 2022.3+ mobile endless runner scaffold generated from code and ready to extend into a full game.

## Architecture
- Scene bootstrap creates the menu, gameplay, and game-over flow.
- Generated prefabs wire the player, drones, obstacles, power-ups, HUD, and revive/shop surfaces.
- Runtime managers handle input, audio, persistence, mobile performance, and monetization hooks.
- Primitive meshes and built-in UI keep the project runnable without external art assets.

## Problem + Solution
### Problem
Mobile runner prototypes often take too long to assemble and stay stuck in a non-playable state until custom art and gameplay wiring are finished.

### Solution
Built a code-generated Unity scaffold that produces a playable baseline immediately, with mobile controls, procedural audio, and progression systems already in place.

## Tech Stack
Unity 2022.3+, C#, URP, Input System, TextMeshPro, UGUI, Unity Ads, Mobile Notifications, Photon networking hooks, Analytics.

## Controls
- Swipe left and right: change lane
- Swipe up: jump
- Swipe down: slide
- Tap: shoot the nearest drone
- Hold hack: slow motion

## Bootstrap
1. Open the project in Unity Hub with Unity 2022.3 or newer.
2. Run `Cyber Drift Runner/Create Demo Scenes and Prefabs`.
3. Open `Assets/Scenes/MainMenu.unity` and press Play.
