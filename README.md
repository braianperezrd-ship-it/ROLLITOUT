# ROLL IT OUT

A 2D game made in Unity.

- **Unity:** 6.3 LTS (`6000.3.x`), see `ProjectSettings/ProjectVersion.txt`
- **Render pipeline:** URP with the 2D Renderer
- **Input:** Input System package

## Getting started

1. Install [Git LFS](https://git-lfs.com) once per machine: `git lfs install`
2. Clone the repo: `git clone https://github.com/braianperezrd-ship-it/rollitout.git`
3. In **Unity Hub**, click **Add > Add project from disk** and pick the cloned `rollitout` folder.
   If Hub says the exact editor version isn't installed, pick any installed Unity 6.3 LTS patch.
4. Open the project. On the first open, `Assets/_Project/Editor/ProjectBootstrap.cs` creates the
   URP 2D pipeline asset and `Assets/_Project/Scenes/Main.unity`, then assigns them.
   Commit what it generates (`.meta` files, `Settings/`, `Scenes/Main.unity`, `ProjectSettings/`, `Packages/packages-lock.json`).

You can re-run the setup any time from **Tools > ROLL IT OUT > Run Project Setup**.

## Folder layout

All game content lives under `Assets/_Project/` so it stays separate from imported packages and store assets.

```
Assets/_Project/
  Art/        Sprites, Animations, Tilemaps
  Audio/      Music, SFX
  Editor/     Editor-only scripts
  Fonts/
  Materials/
  Prefabs/
  Scenes/
  Scripts/    Runtime game code
  Settings/   URP and other project assets
  UI/
```

Images, audio, fonts and other binaries are stored with Git LFS (see `.gitattributes`).

## Playing the prototype

Open `Assets/_Project/Scenes/Main.unity` (or any scene) and press **Play**. `GameBootstrap` builds the arena,
player, enemies and UI at runtime, so there is no scene wiring or art to import.

- **Move:** WASD / arrow keys / gamepad left stick. There is no attack button: crash into enemies.
- **Dodge** projectiles (the only thing that hurts you). **Pick 1 of 3 upgrade cards** after each round with the mouse or keys `1` `2` `3`.
- **Restart** after game over with `R` / `Enter` / gamepad A or the button.

Game code is in `Assets/_Project/Scripts/Runtime/` (namespace `RollItOut`). Tuning values live at the top of each
script (`PlayerBallController`, `PlayerCollisionDamage`, `PlayerHealth`, `EnemyDefs`, `RoundManager.Composition`).
