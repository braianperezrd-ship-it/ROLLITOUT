using System.Collections.Generic;
using UnityEngine;

namespace RollItOut
{
    /// <summary>
    /// One of four numbered pads that must be completed in order. Rolling onto the next one in the
    /// sequence completes it: it pays out a card reward and becomes the respawn point. Pads further
    /// along stay locked until the ones before them are done. Dying with a completed checkpoint brings
    /// the ball back there at full health; each checkpoint saves you once.
    /// </summary>
    public class Checkpoint : MonoBehaviour
    {
        public const int Count = 4;
        public const float TouchRadius = 0.9f;
        const float LockedWarnCooldown = 2.5f;

        static readonly Color LockedColor = new Color(0.45f, 0.45f, 0.5f, 0.7f);
        static readonly Color NextColor = new Color(1f, 0.85f, 0.3f);
        static readonly Color DoneColor = new Color(0.35f, 1f, 0.55f);
        static readonly Color SpentColor = new Color(0.25f, 0.25f, 0.3f, 0.6f);

        /// <summary>In sequence order: All[0] is checkpoint 1.</summary>
        public static readonly List<Checkpoint> All = new List<Checkpoint>();
        /// <summary>How many checkpoints have been completed; All[Completed] is the one to reach next.</summary>
        public static int Completed { get; private set; }
        public static Checkpoint Next => Completed < All.Count ? All[Completed] : null;
        /// <summary>Respawn point: the last completed checkpoint, unless it has already saved you.</summary>
        public static Checkpoint Current { get; private set; }

        public int Index;
        public bool Done { get; private set; }
        public bool Spent { get; private set; }
        public Transform player;
        public RoundManager round;

        SpriteRenderer pad, ring, gem;
        float pulse;
        static float lockedWarnUntil;

        /// <summary>Lays the four pads out as a zigzag across the arena, so the route crosses the whole map whatever its size.</summary>
        public static void BuildAll(Transform parent, Vector2 arenaHalf, Transform player, RoundManager round)
        {
            All.Clear();
            Completed = 0;
            Current = null;
            lockedWarnUntil = 0f;
            for (int i = 0; i < Count; i++)
            {
                float x = -arenaHalf.x + (i + 0.5f) * arenaHalf.x * 2f / Count;
                float y = (i % 2 == 0 ? 0.5f : -0.5f) * arenaHalf.y;
                var go = new GameObject("Checkpoint" + (i + 1));
                go.transform.SetParent(parent, false);
                go.transform.localPosition = new Vector2(x, y);
                var cp = go.AddComponent<Checkpoint>();
                cp.Index = i;
                cp.player = player;
                cp.round = round;
                All.Add(cp);
            }
            foreach (var cp in All) cp.Refresh();
        }

        void Awake()
        {
            pad = SpriteLib.Add(transform, "Pad", SpriteLib.Circle, new Color(0.15f, 0.17f, 0.22f, 0.8f), -8, Vector3.zero, Vector3.one * 1.6f);
            ring = SpriteLib.Add(transform, "Ring", SpriteLib.Ring, LockedColor, -7, Vector3.zero, Vector3.one * 1.6f);
            gem = SpriteLib.Add(transform, "Gem", SpriteLib.Diamond, LockedColor, -6, Vector3.zero, Vector3.one * 0.5f);
        }

        void OnDestroy()
        {
            All.Remove(this);
            if (Current == this) Current = null;
        }

        void Update()
        {
            if (player == null || Done) return;
            bool next = Index == Completed;
            if (next)
            {
                pulse += Time.deltaTime;
                ring.transform.localScale = Vector3.one * (1.6f + Mathf.Sin(pulse * 4f) * 0.12f);
                gem.transform.localRotation = Quaternion.Euler(0f, 0f, pulse * 90f);
            }

            if (round == null || round.State != GameState.Playing) return;
            if (Vector2.Distance(player.position, transform.position) >= TouchRadius) return;
            if (next) Complete();
            else if (Time.time >= lockedWarnUntil)
            {
                lockedWarnUntil = Time.time + LockedWarnCooldown;
                round.ShowBanner("LOCKED", "Complete checkpoint " + (Completed + 1) + " first", 1.4f);
            }
        }

        void Complete()
        {
            Done = true;
            Completed++;
            Current = this;
            foreach (var cp in All) cp.Refresh();
            Fx.Burst(transform.position, DoneColor, 20, 6f, 0.18f, 0.5f);
            CameraShake.Shake(0.15f, 0.2f);
            round.OfferCheckpointReward(Index + 1);
        }

        /// <summary>Called when the player respawns here: the checkpoint is used up.</summary>
        public void Consume()
        {
            Spent = true;
            if (Current == this) Current = null;
            Refresh();
        }

        void Refresh()
        {
            Color c = Spent ? SpentColor : Done ? DoneColor : Index == Completed ? NextColor : LockedColor;
            ring.color = c;
            gem.color = c;
            pad.color = Spent ? new Color(0.12f, 0.12f, 0.15f, 0.6f) : new Color(0.15f, 0.17f, 0.22f, 0.8f);
            if (Index != Completed || Done)
            {
                ring.transform.localScale = Vector3.one * 1.6f;
                gem.transform.localRotation = Quaternion.identity;
            }
        }
    }
}
