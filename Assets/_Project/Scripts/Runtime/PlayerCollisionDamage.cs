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
        PlayerCardEffects effects;

        void Awake()
        {
            ctrl = GetComponent<PlayerBallController>();
            up = GetComponent<PlayerUpgradeState>();
            effects = GetComponent<PlayerCardEffects>();
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
            float bonus = 1f;
            if (up.Has(UpgradeType.Impact) && up.impactArmed)
            {
                bonus += 0.5f;
                Fx.Burst(e.transform.position, new Color(1f, 0.9f, 0.5f), 10, 7f, 0.2f, 0.35f);
            }
            if (up.Has(UpgradeType.ChainImpact) && Time.time < up.chainUntil) bonus += 0.2f;
            up.impactArmed = false;
            if (up.WoundDuration > 0f) e.ApplyWound(up.WoundDuration, up.WoundDamagePerSecond);
            e.TakeHit(up.CollisionDamage * bonus, dir, impact, up.KnockbackMultiplier);

            // Small controlled bounce: reverse a fraction of the impact, keep some sideways motion.
            float heavy = Mathf.Clamp01((e.Def.knockbackResistance - 1f) / 4f);
            float recoil = Mathf.Clamp(impact * 0.3f, 1.5f, 3.5f) * Mathf.Lerp(1f, 1.6f, heavy);
            if (up.Has(UpgradeType.Ricochet)) recoil *= 1.3f;
            Vector2 sideways = pre - dir * Vector2.Dot(pre, dir);
            ctrl.SetVelocity(-dir * recoil + sideways * 0.8f);

            if (impact > 7.5f) CameraShake.Shake(0.12f, 0.12f);
            if (effects != null) effects.OnEnemyHit(e, impact);
        }
    }
}
