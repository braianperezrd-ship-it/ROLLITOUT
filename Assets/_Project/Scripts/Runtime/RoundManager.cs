using System.Collections.Generic;
using UnityEngine;

namespace RollItOut
{
    public enum GameState { Playing, Clearing, Reward, GameOver }

    /// <summary>Spawns rounds, tracks living enemies (including split Slimes), runs the reward and game-over flow.</summary>
    public class RoundManager : MonoBehaviour
    {
        public const float ClearDelay = 0.7f;
        const float SelectFlashTime = 0.35f;

        public Vector2 ArenaHalf = new Vector2(11f, 6f);
        public Transform EnemyParent, ProjectileParent;
        public Transform player;
        public PlayerHealth health;
        public PlayerBallController ball;
        public UpgradeManager upgrades;

        public int Round { get; private set; }
        public GameState State { get; private set; } = GameState.Playing;
        public List<UpgradeType> Offered { get; private set; } = new List<UpgradeType>();
        public int Selected { get; private set; } = -1;
        public int Highlight;
        public string Banner { get; private set; } = "";
        public string BannerSub { get; private set; } = "";
        public float BannerTime { get; private set; }
        public int AliveCount => alive.Count;

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
            State = GameState.GameOver;
            Fx.Burst(player.position, new Color(1f, 0.8f, 0.2f), 20, 8f, 0.25f, 0.7f);
            Time.timeScale = 0f;
        }

        // ---- round flow ----

        void StartRound(int n)
        {
            Round = n;
            State = GameState.Playing;
            Selected = -1;

            // Pressure ramps one or two variables at a time: counts early, then HP + speed from round 6, projectiles from round 4.
            hpScale = n > 5 ? 1f + 0.1f * (n - 5) : 1f;
            speedScale = n > 5 ? Mathf.Min(1.25f, 1f + 0.02f * (n - 5)) : 1f;
            projScale = n > 4 ? Mathf.Min(1.3f, 1f + 0.03f * (n - 4)) : 1f;
            cooldownScale = n > 4 ? Mathf.Max(0.7f, 1f - 0.03f * (n - 4)) : 1f;

            ball.SetVelocity(Vector2.zero); // each round starts at rest
            ball.controlEnabled = true;

            Banner = "ROUND " + n;
            BannerSub = n == 4 ? "New: Skeleton Archers - dodge the shots!"
                      : n == 5 ? "New: Slimes split when they die"
                      : n == 6 ? "New: Goblin Shamans keep their distance"
                      : "";
            BannerTime = 2.2f;

            foreach (var kv in Composition(n))
                for (int i = 0; i < kv.Value; i++)
                    SpawnEnemy(kv.Key, PickSpawnPoint(), 0.9f);
        }

        static List<KeyValuePair<EnemyType, int>> Composition(int n)
        {
            var l = new List<KeyValuePair<EnemyType, int>>();
            void Add(EnemyType t, int c) { if (c > 0) l.Add(new KeyValuePair<EnemyType, int>(t, c)); }
            if (n == 1) { Add(EnemyType.Skeleton, 5); }
            else if (n == 2) { Add(EnemyType.Skeleton, 3); Add(EnemyType.Goblin, 3); }
            else if (n == 3) { Add(EnemyType.Skeleton, 3); Add(EnemyType.Goblin, 3); Add(EnemyType.Orc, 1); }
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

        Vector2 PickSpawnPoint()
        {
            Vector2 p = Vector2.zero;
            for (int i = 0; i < 30; i++)
            {
                p = new Vector2(Random.Range(-ArenaHalf.x + 1.2f, ArenaHalf.x - 1.2f), Random.Range(-ArenaHalf.y + 1.2f, ArenaHalf.y - 1.2f));
                if (Vector2.Distance(p, player.position) > 6f) break;
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
            if (State == GameState.Playing && alive.Count == 0) BeginClear();
        }

        void BeginClear()
        {
            State = GameState.Clearing;
            clearTimer = ClearDelay;
            ClearProjectiles(); // nothing may hurt the player between clear and the reward screen
        }

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
                    break;
                case GameState.Clearing:
                    clearTimer -= Time.deltaTime;
                    if (clearTimer <= 0f)
                    {
                        Offered = upgrades.Roll(3);
                        Selected = -1;
                        Highlight = 1;
                        InputReader.NavStep(); // swallow the stick state held from play
                        State = GameState.Reward;
                        Time.timeScale = 0f; // pause combat while the cards are open
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
                        Time.timeScale = 1f;
                        ClearProjectiles();
                        StartRound(Round + 1);
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
