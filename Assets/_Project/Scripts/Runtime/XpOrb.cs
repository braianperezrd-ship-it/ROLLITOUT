using System.Collections.Generic;
using UnityEngine;

namespace RollItOut
{
    /// <summary>
    /// XP dropped by a dead enemy. It pops out, sits on the floor and has to be rolled over to count;
    /// once the ball comes within MagnetRadius it homes in and can't be lost. When the round is cleared
    /// every orb left on the map flies to the ball, so no XP is wasted between rounds.
    /// </summary>
    public class XpOrb : MonoBehaviour
    {
        public const float MagnetRadius = 3f;
        public const float CollectRadius = 0.55f;
        const float PopDrag = 6f, HomeSpeed = 7f, HomeAccel = 40f;

        public static readonly List<XpOrb> All = new List<XpOrb>();
        static readonly Color OrbColor = new Color(0.35f, 0.95f, 0.5f);

        public int Value;
        RoundManager round;
        Transform player;
        Vector2 vel;
        bool homing;
        float speed, bob;
        SpriteRenderer gem, glow;

        public static void Spawn(Transform parent, Vector2 pos, int value, RoundManager round, Transform player)
        {
            var go = new GameObject("XpOrb");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var o = go.AddComponent<XpOrb>();
            o.Value = value;
            o.round = round;
            o.player = player;
            float ang = Random.Range(0f, Mathf.PI * 2f);
            o.vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(1.5f, 4f);
            o.bob = Random.value * 10f;
            float size = Mathf.Min(0.6f, 0.26f + 0.05f * Mathf.Sqrt(value)); // tougher enemies drop visibly bigger orbs
            o.glow = SpriteLib.Add(go.transform, "Glow", SpriteLib.Circle, new Color(OrbColor.r, OrbColor.g, OrbColor.b, 0.25f), -3, Vector3.zero, Vector3.one * size * 2.2f);
            o.gem = SpriteLib.Add(go.transform, "Gem", SpriteLib.Diamond, OrbColor, -2, Vector3.zero, Vector3.one * size);
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Update()
        {
            if (player == null) return;
            float dt = Time.deltaTime;
            Vector2 pos = transform.position;
            Vector2 to = (Vector2)player.position - pos;
            float dist = to.magnitude;

            if (!homing && (dist < MagnetRadius || (round != null && round.State == GameState.Clearing)))
            {
                homing = true;
                speed = HomeSpeed;
            }

            if (homing)
            {
                speed += HomeAccel * dt; // always ends up faster than the ball, so it can't be outrun
                if (round != null && round.State == GameState.Clearing) speed = Mathf.Max(speed, 25f);
                if (dist <= CollectRadius || dist <= speed * dt) { Collect(); return; }
                pos += to / dist * speed * dt;
            }
            else
            {
                pos += vel * dt;
                vel *= Mathf.Exp(-PopDrag * dt);
                if (round != null) pos = new Vector2(Mathf.Clamp(pos.x, -round.ArenaHalf.x + 0.5f, round.ArenaHalf.x - 0.5f),
                                                     Mathf.Clamp(pos.y, -round.ArenaHalf.y + 0.5f, round.ArenaHalf.y - 0.5f));
            }
            transform.position = pos;

            bob += dt;
            gem.transform.localRotation = Quaternion.Euler(0f, 0f, bob * 120f);
            float pulse = 1f + Mathf.Sin(bob * 5f) * 0.12f;
            glow.transform.localScale = Vector3.one * gem.transform.localScale.x * 2.2f * pulse;
        }

        public void Collect()
        {
            if (Value <= 0) return;
            int v = Value;
            Value = 0;
            Fx.Burst(transform.position, OrbColor, 4, 2.5f, 0.1f, 0.25f);
            Destroy(gameObject);
            if (round != null) round.AddXp(v);
        }

        /// <summary>Gives the player every orb still lying around (end of round).</summary>
        public static void CollectAll()
        {
            for (int i = All.Count - 1; i >= 0; i--)
                if (i < All.Count) All[i].Collect();
        }
    }
}
