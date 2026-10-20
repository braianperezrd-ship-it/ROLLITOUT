using System.Collections.Generic;
using UnityEngine;

namespace RollItOut
{
    /// <summary>
    /// Ball-to-enemy damage. Enemies no bigger than the ball get crushed: the ball rolls over them, they go flat,
    /// and a lethal crush leaves a splat (crushable size: PlayerUpgradeState.CrushSizeRatio). Bigger enemies still take
    /// a crash with knockback and recoil, unless Wrecking Ball ploughs through them at full speed.
    /// Crash damage scales with impact speed, and bounces off the arena walls bank speed and bonus damage.
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
        public const float CrushDamageMultiplier = 1.5f;
        /// <summary>Wrecking Ball: fraction of max speed needed to plough through an enemy too big to crush.</summary>
        public const float WreckSpeedFraction = 0.9f;
        /// <summary>Fraction of speed kept after rolling over an enemy (a small bump, not a stop).</summary>
        public const float CrushSpeedKeep = 0.9f;
        /// <summary>Crash damage scales with impact speed: x1 at base speed, clamped to this range.</summary>
        public const float MinSpeedDamage = 0.6f, MaxSpeedDamage = 2.5f;
        /// <summary>Below this speed the ball just rolls along a wall instead of bouncing off it.</summary>
        public const float MinWallBounceSpeed = 3f;
        /// <summary>Speed multiplier per wall bounce, capped at a multiple of max speed.</summary>
        public const float WallSpeedGain = 1.15f, WallSpeedCap = 1.8f;
        /// <summary>Each bounce banks a charge: +damage on the next enemy hit, if it lands within the window.</summary>
        public const int MaxWallCharges = 3;
        public const float WallChargeDamage = 0.25f, WallChargeWindow = 2f;
        const float WallBounceCooldown = 0.1f;

        float nextWallBounceTime;

        PlayerBallController ctrl;
        PlayerUpgradeState up;
        PlayerCardEffects effects;
        CircleCollider2D col;
        readonly List<EnemyController> passingOver = new List<EnemyController>();

        void Awake()
        {
            ctrl = GetComponent<PlayerBallController>();
            up = GetComponent<PlayerUpgradeState>();
            effects = GetComponent<PlayerCardEffects>();
            col = GetComponent<CircleCollider2D>();
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            if (c.collider.GetComponent<ArenaWall>() != null) { WallBounce(c); return; }
            TryHit(c);
        }
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

        /// <summary>
        /// Fast bounces off the arena walls keep the ball's speed (and add a bit), and bank a charge that boosts
        /// the next enemy hit. Bank shots off a wall are the way to land the biggest crashes.
        /// </summary>
        void WallBounce(Collision2D c)
        {
            if (Time.time < nextWallBounceTime || c.contactCount == 0) return;
            Vector2 pre = ctrl.PreStepVelocity;
            float speed = pre.magnitude;
            if (speed < MinWallBounceSpeed) return;

            Vector2 n = c.GetContact(0).normal;
            if (Vector2.Dot(pre, n) > 0f) n = -n; // make it face against the incoming roll
            if (Vector2.Dot(pre.normalized, n) > -0.3f) return; // grazing along the wall, not hitting it

            nextWallBounceTime = Time.time + WallBounceCooldown;
            ctrl.WallBounce(pre, n, WallSpeedGain, up.MaxSpeed * WallSpeedCap);

            up.wallCharges = up.ActiveWallCharges < MaxWallCharges ? up.ActiveWallCharges + 1 : MaxWallCharges;
            up.wallChargeUntil = Time.time + WallChargeWindow;

            Vector2 at = c.GetContact(0).point;
            float k = up.wallCharges / (float)MaxWallCharges;
            Fx.Burst(at, Color.Lerp(new Color(1f, 0.9f, 0.6f), new Color(1f, 0.45f, 0.15f), k), 6 + 3 * up.wallCharges, 6f, 0.14f, 0.3f);
            CameraShake.Shake(0.06f + 0.03f * up.wallCharges, 0.06f + 0.02f * up.wallCharges);
            ctrl.Squash();
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
            float bonus = 1f;
            if (up.Has(UpgradeType.Impact) && up.impactArmed)
            {
                bonus += 0.5f;
                Fx.Burst(e.transform.position, new Color(1f, 0.9f, 0.5f), 10, 7f, 0.2f, 0.35f);
            }
            bonus += up.ChainBonus;
            up.impactArmed = false;
            int charges = up.ActiveWallCharges;
            if (charges > 0)
            {
                bonus += WallChargeDamage * charges;
                up.wallCharges = 0;
                Fx.Burst(e.transform.position, new Color(1f, 0.45f, 0.15f), 6 + 4 * charges, 8f, 0.2f, 0.35f);
                CameraShake.Shake(0.1f + 0.04f * charges, 0.1f + 0.03f * charges);
            }
            bonus *= Mathf.Clamp(impact / PlayerUpgradeState.BaseSpeed, MinSpeedDamage, MaxSpeedDamage);
            if (up.WoundDuration > 0f) e.ApplyWound(up.WoundDuration, up.WoundDamagePerSecond);

            if (impact >= MinCrushSpeed && e.Def.size <= up.SizeScale * up.CrushSizeRatio)
            {
                Crush(e, pre, up.CollisionDamage * bonus * CrushDamageMultiplier * up.CrushDamageMultiplier);
                if (effects != null) effects.OnEnemyHit(e, impact);
                return;
            }

            if (up.Has(UpgradeType.WreckingBall) && impact >= up.MaxSpeed * WreckSpeedFraction)
            {
                // Plough straight through: full knockback for the enemy, no bounce for the ball.
                e.TakeHit(up.CollisionDamage * bonus, dir, impact, 1f);
                PassThrough(e);
                ctrl.SetVelocity(pre * CrushSpeedKeep);
                HitStop.Freeze(0.04f);
                CameraShake.Shake(0.16f, 0.14f);
                if (effects != null) effects.OnEnemyHit(e, impact);
                return;
            }

            e.TakeHit(up.CollisionDamage * bonus, dir, impact, 1f);

            // Small controlled bounce: reverse a fraction of the impact, keep some sideways motion.
            float heavy = Mathf.Clamp01((e.Def.knockbackResistance - 1f) / 4f);
            float recoil = Mathf.Clamp(impact * 0.3f, 1.5f, 3.5f) * Mathf.Lerp(1f, 1.6f, heavy);
            if (up.Has(UpgradeType.Ricochet)) recoil *= 1.3f;
            Vector2 sideways = pre - dir * Vector2.Dot(pre, dir);
            ctrl.SetVelocity(-dir * recoil + sideways * 0.8f);

            if (impact > 7.5f) CameraShake.Shake(0.12f, 0.12f);
            if (effects != null) effects.OnEnemyHit(e, impact);
        }

        void Crush(EnemyController e, Vector2 pre, float damage)
        {
            // Let the ball roll straight over the enemy and keep (most of) its momentum.
            PassThrough(e);
            ctrl.SetVelocity(pre * CrushSpeedKeep);

            bool killed = e.Crush(damage, pre, EnemyController.FlatDuration * up.CrushFlatMultiplier);

            HitStop.Freeze(killed ? 0.08f : 0.045f);
            CameraShake.Shake(killed ? 0.2f : 0.12f, killed ? 0.16f : 0.1f);
            ctrl.Squash();
        }

        /// <summary>Turns off ball-enemy collision until the ball has fully rolled clear (see FixedUpdate).</summary>
        void PassThrough(EnemyController e)
        {
            Physics2D.IgnoreCollision(col, e.Collider, true);
            if (!passingOver.Contains(e)) passingOver.Add(e);
        }
    }
}
