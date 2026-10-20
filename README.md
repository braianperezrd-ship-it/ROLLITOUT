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
- **Crush:** roll fast over an enemy no bigger than your ball and you flatten it (dashing doesn't crush, it crashes into them): you keep your momentum, it can't move
  or shoot for a moment, and a lethal crush leaves a splat. Bigger enemies (Orc, and Slime until you take Big Ball) still bounce you back.
- **Speed = damage:** crash damage scales with how fast you hit (x1 at base speed, up to x2.5), so dash hits land hardest.
- **Bank shots:** dash into a wall and you bounce off it, keeping your momentum (+15% speed, capped) and banking a charge (up to 3,
  shown as orange pips next to the dash bar). Your next enemy hit within 2 s spends them for +25% damage each.
- **Dodge** projectiles (the only thing that hurts you). Each enemy attacks its own way, and its wind-up shows which:
  Skeleton and Small Slime fire one bolt, Goblin a quick 3-shot volley (thick line), Orc a wide 5-shot cone, Archer a tight
  3-shot spread, Slime a ring of 8 in every direction (swelling ring), and Shaman a slow seeker that curves after you briefly.
- **Pick 1 of 3 upgrade cards** after each round with the mouse or keys `1` `2` `3`.
- **Pause:** gamepad Start, `Esc` or `P` during a round opens the pause menu (Resume / Restart / Quit). Navigate with the stick, d-pad or W/S
  and pick with A / `Enter` or the mouse; Start, `Esc` or B closes it.
- **Restart** after game over with `R` / `Enter` / gamepad A or the button.

Game code is in `Assets/_Project/Scripts/Runtime/` (namespace `RollItOut`). Tuning values live at the top of each
script (`PlayerBallController`, `PlayerCollisionDamage` (crush thresholds), `PlayerHealth`, `EnemyDefs`, `RoundManager.Composition`).

Upgrade cards are data in `CardDefs` (`UpgradeManager.cs`): name, category, rarity, text, stack limit and icon. Stat cards
are read in `PlayerUpgradeState`; on-hit, on-kill and trail cards run in `PlayerCardEffects`. Rewards are weighted by
rarity (Common 6 : Rare 3 : Epic 1) and never repeat a card within one offer.

There are 47 cards. The design doc's five XP / elite utility cards (Magnet, Greed, Vacuum, Lucky, Treasure Hunter) wait
for those systems; their slots hold Regenerate (healing), Deflect, Steamroller (crush), Wrecking Ball (Collision epic) and
Overdrive (Movement epic). Four Dash cards build on the dash: Quick Dash (-15% recharge, max 3), Double Dash (store 2
dashes), Phase Dash (no damage for 0.5 s after a dash) and Reflect Dash (for 0.5 s after a dash, a projectile that hits you is sent back for 15 damage to the
enemy it hits), plus Slingshot (wall bounces gain +30% speed up to 2.4x max speed and restart the dash burst), and Rebound (Collision rare: a plain roll at 85%+ of max speed bounces off walls like a dash and banks a charge) and Pinball (Collision rare: a crash shoots you at max speed into the nearest other enemy within 6 units, up to 4 in a row). Four Health cards: First Aid (common, heal 2 hearts on pick, only
offered when hurt), Vampire (heal 10% of a heart per enemy death), Last Stand (at 1 heart: +30% damage, +15% speed) and
Giant Heart (epic, +2 max hearts and full heal). Stacking stat cards have caps (Speed 5, Heavy Ball 5, Big Ball 4, Wound 4) so long runs stay
controllable. Trail zones of one kind (Dust Trail, Rolling Fire, Blood Trail, Aftershock) don't stack on the same enemy,
so a trail deals the damage per second it says.
