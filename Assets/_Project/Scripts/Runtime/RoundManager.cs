using System.Collections.Generic;
using UnityEngine;

namespace RollItOut
{
    public enum GameState { Playing, Clearing, Reward, GameOver }

    /// <summary>Spawns rounds, tracks living enemies (including split Slimes), runs the reward and game-over flow.</summary>
    public class RoundManager : MonoBehaviour
    {
        public const float ClearDelay = 0.7f;
        public const float RespawnInvulnerability = 2f;
        const float SelectFlashTime = 0.35f;
        /// <summary>Base Composition is multiplied by this, ramping from StartCountMultiplier in round 1 by CountMultiplierStep per round up to
        /// MaxCountMultiplier, so early rounds feel sparse and the screen fills up as the run goes on.</summary>
        public const float StartCountMultiplier = 1.5f, CountMultiplierStep = 0.5f, MaxCountMultiplier = 4f;
        /// <summary>Flat extra on top of the ramp (+15% enemies every round).</summary>
        public const float EnemyQuantityBoost = 1.15f;
        public static float EnemyCountMultiplier(int round) { return EnemyQuantityBoost * Mathf.Min(MaxCountMultiplier, StartCountMultiplier + CountMultiplierStep * (round - 1)); }
        /// <summary>Global enemy HP factor (40% less than EnemyDefs). XP still comes from the base HP, so kills give the same XP.</summary>
        public const float EnemyHpMultiplier = 0.6f;
        /// <summary>Share of each round spawned in a ring just off-screen (they converge within seconds); the rest spawn in a wider
        /// ring and trickle in over the next 10-15s, so the screen keeps getting company instead of emptying after the first wave.
        /// Spawning anywhere on the 233x127 map left far enemies half a minute away.</summary>
        const float NearSpawnShare = 0.7f;
        const float NearSpawnMin = 14f, NearSpawnMax = 22f, FarSpawnMax = 40f, MinSpawnDistance = 8f;

        public Vector2 ArenaHalf = new Vector2(116.875f, 63.75f);
        public Transform EnemyParent, ProjectileParent, OrbParent;
        public Transform player;
        public PlayerHealth health;
        public PlayerBallController ball;
        public UpgradeManager upgrades;
        public PlayerCardEffects effects;
        public PlayerUpgradeState PlayerUpgrades => upgrades.state;

        public int Round { get; private set; }
        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        public int XpNeeded => XpForLevel(Level);
        /// <summary>Card picks waiting to be shown (level-ups, and anything else that grants a card via GrantCard).</summary>
        public int PendingCards { get; private set; }
        public string RewardTitle { get; private set; } = "CHOOSE AN UPGRADE";
        public GameState State { get; private set; } = GameState.Playing;
        public List<UpgradeType> Offered { get; private set; } = new List<UpgradeType>();
        public int Selected { get; private set; } = -1;
        public int Highlight;
        public string Banner { get; private set; } = "";
        public string BannerSub { get; private set; } = "";
        public float BannerTime { get; private set; }
        public int AliveCount => alive.Count;
        public IReadOnlyList<EnemyController> Alive => alive;

        readonly List<EnemyController> alive = new List<EnemyController>();
        float hpScale = 1f, speedScale = 1f, projScale = 1f, cooldownScale = 1f;
        float clearTimer, selectAt;

        void Start()
        {
            health.Died += OnPlayerDied;
            StartRound(1);
        }

        void OnDestroy()
        {
            if (health != null) health.Died -= OnPlayerDied;
        }

        void OnPlayerDied()
        {
            var cp = Checkpoint.Current;
            if (cp != null)
            {
                Respawn(cp);
                return;
            }
            State = GameState.GameOver;
            Fx.Burst(player.position, new Color(1f, 0.8f, 0.2f), 20, 8f, 0.25f, 0.7f);
            Time.timeScale = 0f;
        }

        void Respawn(Checkpoint cp)
        {
            Fx.Burst(player.position, new Color(1f, 0.8f, 0.2f), 20, 8f, 0.25f, 0.7f);
            CameraShake.Shake(0.3f, 0.3f);
            cp.Consume();
            ClearProjectiles(); // shots in flight would land the moment the blink ends
            ball.Teleport(cp.transform.position);
            health.Revive(RespawnInvulnerability);
            Fx.Burst(cp.transform.position, new Color(0.35f, 1f, 0.55f), 24, 7f, 0.2f, 0.6f);
            ShowBanner("RESPAWNED", Checkpoint.Next == null ? "No checkpoints left - next fall is final!" : "Checkpoint used up - reach the next one", 2f);
        }

        /// <summary>Checkpoint completed: queue a card pick (shares the level-up flow), then carry on with the same round.</summary>
        public void OfferCheckpointReward(int number)
        {
            GrantCard("CHECKPOINT " + number + "/" + Checkpoint.Count + " COMPLETE");
        }

        /// <summary>XP needed to go from level n to n+1: 8, 13, 20, 29, 40, 53... Round 1 (16 XP) is worth about one level.</summary>
        public static int XpForLevel(int n) { return 8 + 4 * (n - 1) + (n - 1) * (n - 1); }

        static int XpFor(EnemyDef d) { return Mathf.Max(1, Mathf.RoundToInt(d.hp / 10f)); }

        public void AddXp(int amount)
        {
            Xp += amount;
            while (Xp >= XpNeeded)
            {
                Xp -= XpNeeded;
                Level++;
                GrantCard("LEVEL UP!  LV " + Level);
            }
        }

        /// <summary>Queues one card pick; shown right away (or after the round clears if that's already under way).</summary>
        public void GrantCard(string title = "CHOOSE AN UPGRADE")
        {
            PendingCards++;
            if (RewardTitle != title && State != GameState.Reward) RewardTitle = title;
        }

        void EnterReward()
        {
            Offered = upgrades.Roll(3);
            Selected = -1;
            Highlight = 1;
            InputReader.NavStep(); // swallow the stick state held from play
            State = GameState.Reward;
            Time.timeScale = 0f; // pause combat while the cards are open
        }

        public void ShowBanner(string title, string sub, float seconds)
        {
            Banner = title;
            BannerSub = sub;
            BannerTime = seconds;
        }

        // ---- round flow ----

        void StartRound(int n)
        {
            Round = n;
            State = GameState.Playing;
            Selected = -1;

            // Pressure ramps one or two variables at a time: counts early, then HP + speed from round 6, projectiles from round 4.
            hpScale = EnemyHpMultiplier * (n > 5 ? 1f + 0.1f * (n - 5) : 1f);
            speedScale = n > 5 ? Mathf.Min(1.25f, 1f + 0.02f * (n - 5)) : 1f;
            projScale = n > 4 ? Mathf.Min(1.3f, 1f + 0.03f * (n - 4)) : 1f;
            cooldownScale = n > 4 ? Mathf.Max(0.7f, 1f - 0.03f * (n - 4)) : 1f;

            ball.SetVelocity(Vector2.zero); // each round starts at rest
            ball.controlEnabled = true;
            health.OnRoundStart();
            PlayerUpgrades.OnRoundStart();

            Banner = "ROUND " + n;
            BannerSub = n == 1 ? "Every enemy can shoot - watch the telegraphs!"
                      : n == 2 ? "New: Skeleton Archers keep their distance"
                      : n == 5 ? "New: Slimes split when they die"
                      : n == 6 ? "New: Goblin Shamans keep their distance"
                      : "";
            BannerTime = 2.2f;

            foreach (var kv in Composition(n))
                for (int i = 0; i < kv.Value; i++)
                    SpawnEnemy(kv.Key, PickSpawnPoint(Random.value < NearSpawnShare), 0.9f);
        }

        static List<KeyValuePair<EnemyType, int>> Composition(int n)
        {
            var l = new List<KeyValuePair<EnemyType, int>>();
            float mul = EnemyCountMultiplier(n);
            void Add(EnemyType t, int c) { if (c > 0) l.Add(new KeyValuePair<EnemyType, int>(t, Mathf.Max(1, Mathf.RoundToInt(c * mul)))); }
            if (n == 1) { Add(EnemyType.Skeleton, 5); }
            else if (n == 2) { Add(EnemyType.Skeleton, 3); Add(EnemyType.Goblin, 3); Add(EnemyType.Archer, 1); }
            else if (n == 3) { Add(EnemyType.Skeleton, 3); Add(EnemyType.Goblin, 3); Add(EnemyType.Orc, 1); Add(EnemyType.Archer, 2); }
            else if (n == 4) { Add(EnemyType.Skeleton, 3); Add(EnemyType.Goblin, 2); Add(EnemyType.Orc, 1); Add(EnemyType.Archer, 2); }
            else if (n == 5) { Add(EnemyType.Skeleton, 2); Add(EnemyType.Goblin, 2); Add(EnemyType.Orc, 1); Add(EnemyType.Archer, 2); Add(EnemyType.Slime, 2); }
            else
            {
                int x = n - 6;
                Add(EnemyType.Skeleton, 3 + x / 2);
                Add(EnemyType.Goblin, 2 + x / 2);
                Add(EnemyType.Orc, 1 + x / 3);
                Add(EnemyType.Archer, 2 + x / 2);
                Add(EnemyType.Slime, 2 + x / 3);
                Add(EnemyType.Shaman, 1 + x / 2);
            }
            return l;
        }

        Vector2 PickSpawnPoint(bool near)
        {
            Vector2 p = Vector2.zero;
            Vector2 min = -ArenaHalf + Vector2.one * 1.2f, max = ArenaHalf - Vector2.one * 1.2f;
            float rMin = near ? NearSpawnMin : NearSpawnMax, rMax = near ? NearSpawnMax : FarSpawnMax;
            for (int i = 0; i < 30; i++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f);
                p = (Vector2)player.position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(rMin, rMax);
                if (p.x > min.x && p.x < max.x && p.y > min.y && p.y < max.y) return p;
            }
            for (int i = 0; i < 30; i++)
            {
                p = new Vector2(Random.Range(-ArenaHalf.x + 1.2f, ArenaHalf.x - 1.2f), Random.Range(-ArenaHalf.y + 1.2f, ArenaHalf.y - 1.2f));
                if (Vector2.Distance(p, player.position) > MinSpawnDistance) break;
            }
            return p;
        }

        public EnemyController SpawnEnemy(EnemyType type, Vector2 pos, float spawnDelay)
        {
            var def = EnemyDefs.Get(type);
            var go = new GameObject(def.name);
            go.transform.SetParent(EnemyParent, false);
            go.transform.position = pos;
            var e = go.AddComponent<EnemyController>();
            e.Init(def, this, player, hpScale, speedScale, projScale, cooldownScale, spawnDelay);
            alive.Add(e);
            return e;
        }

        public void OnEnemyDied(EnemyController e)
        {
            alive.Remove(e);
            XpOrb.Spawn(OrbParent, e.transform.position, XpFor(e.Def), this, player); // XP only counts once the ball picks the orb up
            if (effects != null) effects.OnEnemyKilled(e); // may kill more enemies (Explosive Impact)
            if (State == GameState.Playing && alive.Count == 0) BeginClear();
        }

        void BeginClear()
        {
            State = GameState.Clearing;
            clearTimer = ClearDelay;
            ClearProjectiles(); // nothing may hurt the player between clear and the reward screen
            if (PlayerUpgrades.Has(UpgradeType.Regenerate)) health.Heal(1);
        }

        /// <summary>Also clears card damage zones, which live under the projectile parent.</summary>
        void ClearProjectiles()
        {
            for (int i = ProjectileParent.childCount - 1; i >= 0; i--) Destroy(ProjectileParent.GetChild(i).gameObject);
        }

        // ---- reward flow ----

        void Update()
        {
            switch (State)
            {
                case GameState.Playing:
                    if (BannerTime > 0f) BannerTime -= Time.deltaTime;
                    if (PendingCards > 0) EnterReward();
                    break;
                case GameState.Clearing:
                    clearTimer -= Time.deltaTime;
                    if (clearTimer <= 0f)
                    {
                        XpOrb.CollectAll(); // whatever didn't reach the ball yet still counts before the cards / next round
                        if (PendingCards > 0) EnterReward();
                        else StartRound(Round + 1);
                    }
                    break;
                case GameState.Reward:
                    if (Selected < 0)
                    {
                        for (int i = 0; i < Offered.Count; i++)
                            if (InputReader.DigitPressed(i)) Select(i);
                        Highlight = Mathf.Clamp(Highlight + InputReader.NavStep(), 0, Offered.Count - 1);
                        if (InputReader.SubmitPressed()) Select(Highlight);
                    }
                    else if (Time.unscaledTime >= selectAt)
                    {
                        upgrades.Apply(Offered[Selected]);
                        health.OnCardTaken(Offered[Selected]);
                        PendingCards--;
                        if (PendingCards > 0) { EnterReward(); break; }
                        RewardTitle = "CHOOSE AN UPGRADE";
                        Time.timeScale = 1f;
                        if (alive.Count == 0)
                        {
                            ClearProjectiles();
                            StartRound(Round + 1);
                        }
                        else State = GameState.Playing;
                    }
                    break;
                case GameState.GameOver:
                    if (InputReader.ConfirmPressed()) Restart();
                    break;
            }
        }

        public float SelectProgress => Selected < 0 ? 0f : 1f - Mathf.Clamp01((selectAt - Time.unscaledTime) / SelectFlashTime);

        public void Select(int index)
        {
            if (State != GameState.Reward || Selected >= 0 || index < 0 || index >= Offered.Count) return;
            Selected = index;
            selectAt = Time.unscaledTime + SelectFlashTime;
        }

        public void Restart()
        {
            GameBootstrap.Restart();
        }
    }
}
