using System;
using UnityEngine;

namespace RollItOut
{
    public enum HitResult { Ignored, Blocked, Damaged }

    /// <summary>
    /// 3 hearts backed by a damage meter (100 units = 1 heart) so Projectile Resistance never rounds away.
    /// Resolution order: invulnerable -> Stone Skin -> resistance -> subtract + i-frames -> death.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        public const int MaxHearts = 3;
        public const float UnitsPerHeart = 100f;
        public const float InvulnerabilityTime = 0.6f;

        public SpriteRenderer body;
        public Color baseColor = Color.white;
        public PlayerUpgradeState upgrades;

        float damageTaken;
        float invulnUntil = -1f;

        public bool Dead { get; private set; }
        public bool Invulnerable => Time.time < invulnUntil;
        public int Hearts => Mathf.Max(0, MaxHearts - Mathf.FloorToInt(damageTaken / UnitsPerHeart + 1e-4f));
        /// <summary>0..1 progress toward losing the current heart (for HUD).</summary>
        public float PartialFraction => Dead ? 0f : (damageTaken % UnitsPerHeart) / UnitsPerHeart;
        public event Action Died;

        public HitResult TakeProjectileHit(float projectileDamage, Vector2 hitPos)
        {
            if (Dead || Invulnerable) return HitResult.Ignored;

            if (upgrades.stoneSkinCharges > 0)
            {
                upgrades.stoneSkinCharges--;
                Fx.Burst(hitPos, new Color(0.75f, 0.78f, 0.82f), 10, 5f, 0.16f, 0.35f);
                CameraShake.Shake(0.08f, 0.12f);
                return HitResult.Blocked;
            }

            damageTaken += UnitsPerHeart * projectileDamage * upgrades.ProjectileDamageMultiplier;
            Fx.Burst(transform.position, new Color(1f, 0.25f, 0.25f), 14, 6f, 0.2f, 0.45f);
            CameraShake.Shake(0.25f, 0.25f);
            invulnUntil = Time.time + InvulnerabilityTime;

            if (damageTaken >= MaxHearts * UnitsPerHeart - 0.01f)
            {
                Dead = true;
                if (Died != null) Died();
            }
            return HitResult.Damaged;
        }

        void Update()
        {
            if (body == null) return;
            if (Invulnerable)
            {
                // Hard white flash first, then a readable blink for the rest of the i-frames.
                float since = Time.time - (invulnUntil - InvulnerabilityTime);
                if (since < 0.08f) body.color = Color.white;
                else body.color = Mathf.Repeat(Time.time * 12f, 1f) < 0.5f ? new Color(1f, 1f, 1f, 0.55f) : baseColor;
            }
            else body.color = baseColor;
        }
    }
}
