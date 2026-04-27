# Cyber Drift Runner Production Art Pipeline

This project no longer treats generated geometry as the shipping art source.

## Required import targets

- `Assets/Art/Characters` — player mesh, skins, boss mesh
- `Assets/Art/Drones` — drone variants and boss drone attachments
- `Assets/Art/Environment` — district kits, skyline modules, props, signage, vehicles
- `Assets/Art/UI` — icons, logos, panel ornaments, button glyphs
- `Assets/Art/VFX` — particles, masks, distortion textures, trail textures
- `Assets/Audio/Music` — menu, gameplay, boss loops
- `Assets/Audio/SFX` — lane, jump, slide, hack, hit, revive, reward, UI clips

## Rules

- Shipping-critical visuals must come from authored prefabs/materials, not runtime-created fallback objects.
- Scene-wide look is controlled by:
  - `DistrictPresentationLibrary`
  - `UiVisualTheme`
  - `AudioStyleProfile`
- Imported art/audio intake is controlled by:
  - `VisualAssetCatalog`
  - `ProductionAssetImportProcessor`
- Runtime fallback audio remains only when authored clips are missing.
- `RuntimeVisualStyleController` is fallback-only and should not be used as the primary shipping look.

## Prefab expectations

- One prefab family per district with shared materials and collider conventions.
- LODs on skyline/large prop meshes.
- Colliders only on gameplay-relevant objects.
- Emissive materials should expose a main accent color and mobile-safe emission.

## Scene ownership

- `PresentationDirector` owns district lighting, atmosphere, shader globals, and post-process profile application.
- `UIFlowController` owns authored canvas/panel visibility for menu, HUD, revive, pause, and game-over transitions.
- `RunFlowController` remains the only owner of death/revive/game-over state.

## Intake workflow

1. Import models, textures, and audio into the `Assets/Art/*` and `Assets/Audio/*` trees.
2. Let `ProductionAssetImportProcessor` apply mobile-safe defaults.
3. Bind imported prefabs/materials in `VisualAssetCatalog`.
4. Bind imported clips in `AudioStyleProfile`.
5. Run `Cyber Drift Runner/Validate Production Asset Catalog`.
6. Only after validation passes, rerun `Cyber Drift Runner/Create Demo Scenes and Prefabs`.
7. Run `Cyber Drift Runner/Validate Production Readiness` and clear every reported scene, audio, and presentation dependency.

## Failure policy

- If `VisualAssetCatalog.AllowGeneratedFallbacks` is disabled, scene generation now aborts when required prefabs, materials, or district chunk families are missing.
- `LevelChunkGenerator`, `PresentationDirector`, and `UIFlowController` log explicit configuration errors instead of silently degrading into broken visuals.
- `Validate Production Readiness` checks the actual `MainMenu`, `GameScene`, and `GameOver` scenes for missing authored runtime bindings.
- Do not use generated fallback art as a shipping path once authored assets are being integrated.
