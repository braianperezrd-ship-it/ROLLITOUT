using System.Collections.Generic;
using UnityEngine;

namespace RollItOut
{
    /// <summary>
    /// Ball-to-enemy damage. Enemies no bigger than the ball get crushed: the ball rolls over them, they go flat,
    /// and a lethal crush leaves a splat. Bigger enemies still take a crash with knockback and recoil.
    /// Enemy contact never hurts the player.
    /// </summary>
    [RequireComponent(typeof(PlayerBallController))]
    public class PlayerCollisionDamage : MonoBehaviour
    {
        public const float HitCooldown = 0.25f;
        /// <summary>Below this speed a touch is just pushing, not a crash.</summary>
        public const float MinImpactSpeed = 2f;
        /// <summary>Below this speed the ball can't climb over an enemy, it just bumps it.</summary>
        public const float MinCrushSpeed = 3.5f;
        /// <summary>Enemies up to this size relative to the ball's diameter can be rolled over.</summary>
        public const float CrushSizeRatio = 1.05f;
        public const float CrushDamageMultiplier = 1.5f;
        /// <summary>Fraction of speed kept after rolling over an enemy (a small bump, not a stop).</summary>
        public const float CrushSpeedKeep = 0.9f;

        PlayerBallController ctrl;
        PlayerUpgradeState up;
        CircleCollider2D col;
        readonly List<EnemyController> passingOver = new List<EnemyController>();

        void Awake()
        {
            ctrl = GetComponent<PlayerBallController>();
            up = GetComponent<PlayerUpgradeState>();
            col = GetComponent<CircleCollider2D>();
        }

        void OnCollisionEnter2D(Collision2D c) { TryHit(c); }
        void OnCollisionStay2D(Collision2D c) { TryHit(c); }

        void FixedUpdate()
        {
            // Turn collision back on once the ball has fully rolled off an enemy.
            float ballR = col.radius * transform.lossyScale.x;
            for (int i = passingOver.Count - 1; i >= 0; i--)
            {
                var e = passingOver[i];
                if (e == null || e.Dead) { passingOver.RemoveAt(i); continue; }
                float gap = Vector2.Distance(transform.position, e.transform.position) - ballR - e.Def.size * 0.5f;
                if (gap > 0.05f)
                {
                    Physics2D.IgnoreCollision(col, e.Collider, false);
                    passingOver.RemoveAt(i);
                }
            }
        }

        void TryHit(Collision2D c)
        {
            var e = c.collider.GetComponentInParent<EnemyController>();
            if (e == null || e.Dead || !e.Active) return;
            if (Time.time < e.nextCollisionHitTime) return; // per-enemy cooldown: one damage event per window

            Vector2 pre = ctrl.PreStepVelocity;
            float impact = pre.magnitude;
            if (impact < MinImpactSpeed) return;

            Vector2 dir = ((Vector2)e.transform.position - (Vector2)transform.position).normalized;
            if (Vector2.Dot(pre.normalized, dir) < 0.15f) return; // must be rolling into it

            e.nextCollisionHitTime = Time.time + HitCooldown;

            if (impact >= MinCrushSpeed && e.Def.size <= up.SizeScale * CrushSizeRatio)
            {
                Crush(e, pre);
                return;
            }

            e.TakeHit(up.CollisionDamage, dir, impact, up.WoundDuration);

            // Small controlled bounce: reverse a fraction of the impact, keep some sideways motion.
            float heavy = Mathf.Clamp01((e.Def.knockbackResistance - 1f) / 4f);
            float recoil = Mathf.Clamp(impact * 0.3f, 1.5f, 3.5f) * Mathf.Lerp(1f, 1.6f, heavy);
            Vector2 sideways = pre - dir * Vector2.Dot(pre, dir);
            ctrl.SetVelocity(-dir * recoil + sideways * 0.8f);

            if (impact > 7.5f) CameraShake.Shake(0.12f, 0.12f);
        }

        void Crush(EnemyController e, Vector2 pre)
        {
            // Let the ball roll straight over the enemy and keep (most of) its momentum.
            Physics2D.IgnoreCollision(col, e.Collider, true);
            if (!passingOver.Contains(e)) passingOver.Add(e);
            ctrl.SetVelocity(pre * CrushSpeedKeep);

            int damage = Mathf.RoundToInt(up.CollisionDamage * CrushDamageMultiplier);
            bool killed = e.Crush(damage, pre, up.WoundDuration);

            HitStop.Freeze(killed ? 0.08f : 0.045f);
            CameraShake.Shake(killed ? 0.2f : 0.12f, killed ? 0.16f : 0.1f);
            ctrl.Squash();
        }
    }
}
