# AGENTS.md

Guide for AI agents working on ROLL IT OUT, a 2D top-down arena game in Unity. The README covers controls and gameplay.

## Engine and packages

- **Unity:** the owner's PC runs **6000.6.5f1** (`open-rollitout.bat` launches that editor). The repo still says
  `6000.3.0f1` in `ProjectSettings/ProjectVersion.txt` and pins older packages in `Packages/manifest.json`
  (URP 17.3, Input System 1.14). The local upgrade was never committed. Don't bump these files unless asked, and
  write code that compiles on both versions.
- **Render pipeline:** URP with the 2D Renderer (`Assets/_Project/Settings/URP-2D.asset` + `Renderer2D.asset`).
- **Input:** Input System package. Gameplay reads input through `InputReader` in `Util.cs` (keyboard + gamepad).
  Use it for new input instead of calling `Keyboard.current` / `Gamepad.current` directly.

## Layout

All game content lives under `Assets/_Project/`:

```
Art/  Audio/  Editor/  Fonts/  Materials/  Prefabs/  Scenes/  Scripts/Runtime/  Settings/  UI/
```

Most of these folders are empty placeholders (`.gitkeep`). There are no prefabs, imported art, tests or `.asmdef`
files: everything compiles into `Assembly-CSharp`.

## How the code works

- All runtime code is in `Assets/_Project/Scripts/Runtime/`, namespace `RollItOut`.
- `GameBootstrap` builds the whole game at runtime (camera, arena, player, managers, HUD) from
  `[RuntimeInitializeOnLoadMethod]`, so any scene works when you press Play. New objects are created in code,
  not wired in a scene or a prefab.
- Sprites are generated procedurally in `SpriteLib` (`Util.cs`). Effects, hit-stop and camera shake also live in `Util.cs`.
- Tuning values are constants at the top of each script: `PlayerBallController`, `PlayerCollisionDamage`
  (crush thresholds), `PlayerHealth`, `EnemyDefs` in `EnemyController.cs` (attacks per enemy), `Projectile`
  (shot kinds), `RoundManager.Composition` + `EnemyCountMultiplier`.
- Upgrade cards are data in `CardDefs` (`UpgradeManager.cs`). Stat cards are read in `PlayerUpgradeState`.
  On-hit, on-kill and trail cards run in `PlayerCardEffects`.
- `Assets/_Project/Editor/ProjectBootstrap.cs` creates the URP asset and `Main.unity` on first open
  (Tools > ROLL IT OUT > Run Project Setup).

## Local-only files (not in git)

- `Assets/_Project/Scenes/Main.unity` and the URP assets in `Settings/` are generated on the owner's PC by `ProjectBootstrap`.
- `Assets/Editor/LocalAutoPlay.cs` auto-enters Play mode on the owner's PC.

Don't commit these unless asked, and don't rely on them existing.

## Conventions

- Match the existing style: plain C#, `static` helpers, short comments explaining *why* a number is what it is.
- `.meta` files are not in git. Unity generates them on the PC, so a new script only needs its `.cs` file.
  Binaries go through Git LFS (`.gitattributes`).
- Keep the README in sync when you change controls, enemies, cards or tuning it describes.
- You can't run Unity in the cloud, so read the code carefully. After a change lands on `main`, the owner pulls on the PC and Unity recompiles.
- Workflow: work on a branch, open a PR and merge it to `main`.
