using System;
using UnityEngine;

namespace RollItOut
{
    public enum HitResult { Ignored, Blocked, Damaged, Reflected }

    /// <summary>
    /// 3 hearts (+Iron Shell) backed by a damage meter (100 units = 1 heart) so damage reduction never rounds away.
    /// Resolution order: Deflect -> invulnerable / Safe Roll -> Stone Skin -> reductions -> subtract + i-frames -> Second Chance -> death.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        public const int BaseHearts = 3;
        public const float UnitsPerHeart = 100f;
        public const float InvulnerabilityTime = 0.6f;
        /// <summary>Deflect: fraction of max speed at which the ball smashes projectiles instead of taking them.</summary>
        public const float DeflectSpeedFraction = 0.8f;

        public SpriteRenderer body;
        public Color baseColor = Color.white;
        public PlayerUpgradeState upgrades;

        float damageTaken;
        float invulnUntil = -1f, immuneUntil = -1f, lastDamagedTime;
        bool secondChanceUsed;
        PlayerBallController ctrl;

        public bool Dead { get; private set; }
        public bool Invulnerable => Time.time < invulnUntil;
        public int MaxHearts => BaseHearts + upgrades.Count(UpgradeType.IronShell) + 2 * upgrades.Count(UpgradeType.GiantHeart);
        /// <summary>Missing any health, even part of a heart.</summary>
        public bool Hurt => !Dead && damageTaken > 0.01f;
        /// <summary>Last Stand: down to the final heart.</summary>
        public bool LastStandActive => upgrades.Has(UpgradeType.LastStand) && !Dead && Hearts == 1;
        /// <summary>Hardened: no projectile damage for a while.</summary>
        public bool HardenedActive => upgrades.Has(UpgradeType.Hardened) && Time.time - lastDamagedTime >= PlayerUpgradeState.HardenedDelay;
        public bool SecondChanceReady => upgrades.Has(UpgradeType.SecondChance) && !secondChanceUsed;
        public int Hearts => Mathf.Max(0, MaxHearts - Mathf.FloorToInt(damageTaken / UnitsPerHeart + 1e-4f));
        /// <summary>0..1 progress toward losing the current heart (for HUD).</summary>
        public float PartialFraction => Dead ? 0f : (damageTaken % UnitsPerHeart) / UnitsPerHeart;
        public event Action Died;

        void Start()
        {
            // Looked up in Start: GameBootstrap adds PlayerBallController after this component, so it isn't there in Awake.
            ctrl = GetComponent<PlayerBallController>();
            lastDamagedTime = Time.time;
        }

        public void OnRoundStart() { secondChanceUsed = false; }

        /// <summary>Give back hearts (fractions allowed, e.g. Vampire), never above the maximum.</summary>
        public void Heal(float hearts, bool bigFx = true)
        {
            if (Dead || hearts <= 0f || damageTaken <= 0f) return;
            damageTaken = Mathf.Max(0f, damageTaken - hearts * UnitsPerHeart);
            if (bigFx) Fx.Burst(transform.position, new Color(0.4f, 1f, 0.5f), 16, 5f, 0.18f, 0.5f);
            else Fx.Burst(transform.position, new Color(0.4f, 1f, 0.5f), 4, 3f, 0.1f, 0.3f);
        }

        /// <summary>Cards that heal the moment they are picked.</summary>
        public void OnCardTaken(UpgradeType t)
        {
            if (t == UpgradeType.FirstAid) Heal(2f);
            else if (t == UpgradeType.GiantHeart) Heal(MaxHearts);
        }

        /// <summary>Safe Roll: brief projectile immunity without the hurt blink.</summary>
        public void GrantImmunity(float seconds) { immuneUntil = Mathf.Max(immuneUntil, Time.time + seconds); }

        public HitResult TakeProjectileHit(float projectileDamage, Vector2 hitPos)
        {
            if (Dead) return HitResult.Ignored;

            if (ctrl == null) ctrl = GetComponent<PlayerBallController>();
            bool dashing = ctrl != null && ctrl.InDashMotion;
            if (dashing && upgrades.Has(UpgradeType.ReflectDash))
            {
                Fx.Burst(hitPos, new Color(0.6f, 0.9f, 1f), 10, 6f, 0.14f, 0.3f);
                return HitResult.Reflected;
            }
            if (dashing && upgrades.Has(UpgradeType.PhaseDash))
            {
                // Absorb the shot instead of letting it pass: one still overlapping when the dash ends would hit.
                Fx.Burst(hitPos, new Color(0.6f, 0.9f, 1f, 0.6f), 6, 4f, 0.12f, 0.25f);
                return HitResult.Blocked;
            }

            if (upgrades.Has(UpgradeType.Deflect) && ctrl != null && ctrl.PreStepVelocity.magnitude >= upgrades.MaxSpeed * DeflectSpeedFraction)
            {
                Fx.Burst(hitPos, new Color(1f, 0.7f, 0.3f), 8, 6f, 0.14f, 0.3f);
                return HitResult.Blocked;
            }

            if (Invulnerable || Time.time < immuneUntil) return HitResult.Ignored;

            if (upgrades.stoneSkinCharges > 0)
            {
                upgrades.stoneSkinCharges--;
                Fx.Burst(hitPos, new Color(0.75f, 0.78f, 0.82f), 10, 5f, 0.16f, 0.35f);
                CameraShake.Shake(0.08f, 0.12f);
                return HitResult.Blocked;
            }

            float max = MaxHearts * UnitsPerHeart;
            damageTaken += UnitsPerHeart * projectileDamage * upgrades.ProjectileDamageMultiplier(HardenedActive);
            lastDamagedTime = Time.time;
            if (upgrades.Has(UpgradeType.Rush)) upgrades.rushUntil = Time.time + 2f;
            if (damageTaken >= max - 0.01f && SecondChanceReady)
            {
                secondChanceUsed = true;
                damageTaken = max - UnitsPerHeart;
                Fx.Burst(transform.position, new Color(1f, 0.9f, 0.4f), 24, 8f, 0.22f, 0.6f);
            }
            Fx.Burst(transform.position, new Color(1f, 0.25f, 0.25f), 14, 6f, 0.2f, 0.45f);
            CameraShake.Shake(0.25f, 0.25f);
            invulnUntil = Time.time + InvulnerabilityTime;

            if (damageTaken >= max - 0.01f)
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
