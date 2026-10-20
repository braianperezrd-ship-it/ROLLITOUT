using System.Collections.Generic;
using UnityEngine;

namespace RollItOut
{
    /// <summary>
    /// A pad on the floor. Rolling over it makes it the respawn point; dying with an active checkpoint
    /// brings the ball back there at full health instead of ending the run. Each checkpoint saves you once.
    /// </summary>
    public class Checkpoint : MonoBehaviour
    {
        public const float TouchRadius = 0.9f;
        /// <summary>Distance between checkpoints; the count follows the arena size, so a bigger map gets more of them.</summary>
        const float Spacing = 25f;

        static readonly Color IdleColor = new Color(0.45f, 0.5f, 0.6f, 0.8f);
        static readonly Color ActiveColor = new Color(0.35f, 1f, 0.55f);
        static readonly Color SpentColor = new Color(0.25f, 0.25f, 0.3f, 0.6f);

        public static readonly List<Checkpoint> All = new List<Checkpoint>();
        /// <summary>Last checkpoint touched that hasn't been used up yet, or null.</summary>
        public static Checkpoint Current { get; private set; }

        public bool Spent { get; private set; }
        public Transform player;
        public RoundManager round;

        SpriteRenderer pad, ring, gem;
        float pulse;

        /// <summary>Lays checkpoints out on an even grid that scales with the arena (two side by side on the small one).</summary>
        public static void BuildAll(Transform parent, Vector2 arenaHalf, Transform player, RoundManager round)
        {
            Current = null;
            int cols = Mathf.Max(2, Mathf.RoundToInt(arenaHalf.x * 2f / Spacing));
            int rows = Mathf.Max(1, Mathf.RoundToInt(arenaHalf.y * 2f / Spacing));
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    float x = -arenaHalf.x + (c + 0.5f) * arenaHalf.x * 2f / cols;
                    float y = -arenaHalf.y + (r + 0.5f) * arenaHalf.y * 2f / rows;
                    var go = new GameObject("Checkpoint");
                    go.transform.SetParent(parent, false);
                    go.transform.localPosition = new Vector2(x, y);
                    var cp = go.AddComponent<Checkpoint>();
                    cp.player = player;
                    cp.round = round;
                }
        }

        void Awake()
        {
            All.Add(this);
            pad = SpriteLib.Add(transform, "Pad", SpriteLib.Circle, new Color(0.15f, 0.17f, 0.22f, 0.8f), -8, Vector3.zero, Vector3.one * 1.6f);
            ring = SpriteLib.Add(transform, "Ring", SpriteLib.Ring, IdleColor, -7, Vector3.zero, Vector3.one * 1.6f);
            gem = SpriteLib.Add(transform, "Gem", SpriteLib.Diamond, IdleColor, -6, Vector3.zero, Vector3.one * 0.5f);
        }

        void OnDestroy()
        {
            All.Remove(this);
            if (Current == this) Current = null;
        }

        void Update()
        {
            if (Spent || player == null) return;
            if (Current != this && Vector2.Distance(player.position, transform.position) < TouchRadius) Activate();

            if (Current == this)
            {
                pulse += Time.deltaTime;
                float s = 1.6f + Mathf.Sin(pulse * 4f) * 0.1f;
                ring.transform.localScale = Vector3.one * s;
                gem.transform.localRotation = Quaternion.Euler(0f, 0f, pulse * 90f);
            }
        }

        void Activate()
        {
            if (Current != null) Current.SetColor(IdleColor);
            Current = this;
            pulse = 0f;
            SetColor(ActiveColor);
            Fx.Burst(transform.position, ActiveColor, 14, 5f, 0.16f, 0.45f);
            if (round != null) round.ShowBanner("CHECKPOINT", "You'll respawn here if you roll out", 1.4f);
        }

        /// <summary>Called when the player respawns here: the checkpoint is used up.</summary>
        public void Consume()
        {
            Spent = true;
            if (Current == this) Current = null;
            SetColor(SpentColor);
            ring.transform.localScale = Vector3.one * 1.6f;
            gem.transform.localRotation = Quaternion.identity;
            pad.color = new Color(0.12f, 0.12f, 0.15f, 0.6f);
        }

        void SetColor(Color c)
        {
            ring.color = c;
            gem.color = c;
            if (c != ActiveColor) ring.transform.localScale = Vector3.one * 1.6f;
        }
    }
}
