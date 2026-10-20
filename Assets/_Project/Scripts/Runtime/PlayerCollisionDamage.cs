using UnityEngine;

namespace RollItOut
{
    /// <summary>Ball-to-enemy damage, knockback and recoil. Enemy contact never hurts the player.</summary>
    [RequireComponent(typeof(PlayerBallController))]
    public class PlayerCollisionDamage : MonoBehaviour
    {
        public const float HitCooldown = 0.25f;
        /// <summary>Below this speed a touch is just pushing, not a crash.</summary>
        public const float MinImpactSpeed = 2f;

        PlayerBallController ctrl;
        PlayerUpgradeState up;

        void Awake()
        {
            ctrl = GetComponent<PlayerBallController>();
            up = GetComponent<PlayerUpgradeState>();
        }

        void OnCollisionEnter2D(Collision2D c) { TryHit(c); }
        void OnCollisionStay2D(Collision2D c) { TryHit(c); }

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
            e.TakeHit(up.CollisionDamage, dir, impact, up.WoundDuration);

            // Small controlled bounce: reverse a fraction of the impact, keep some sideways motion.
            float heavy = Mathf.Clamp01((e.Def.knockbackResistance - 1f) / 4f);
            float recoil = Mathf.Clamp(impact * 0.3f, 1.5f, 3.5f) * Mathf.Lerp(1f, 1.6f, heavy);
            Vector2 sideways = pre - dir * Vector2.Dot(pre, dir);
            ctrl.SetVelocity(-dir * recoil + sideways * 0.8f);

            if (impact > 7.5f) CameraShake.Shake(0.12f, 0.12f);
        }
    }
}
