using UnityEngine;

namespace RollItOut
{
    /// <summary>Card stack counts, Stone Skin charges and short-lived card buffs, plus the derived stats.</summary>
    public class PlayerUpgradeState : MonoBehaviour
    {
        public const float BaseSpeed = 6f;
        public const int BaseDamage = 10;
        public const float WoundDps = 4f;
        public const float HardenedDelay = 5f;

        int[] stacks;
        public int stoneSkinCharges;

        // Time.time deadlines for timed card effects.
        public float furyUntil = -1f, rushUntil = -1f, ricochetUntil = -1f, chainUntil = -1f;
        /// <summary>Impact: set once the ball reaches full speed, spent by the next hit.</summary>
        public bool impactArmed;
        /// <summary>Shockwave: collisions landed so far.</summary>
        public int hitCount;

        int[] Stacks => stacks ?? (stacks = new int[CardDefs.Count]);

        public int Count(UpgradeType t) { return Stacks[(int)t]; }
        public bool Has(UpgradeType t) { return Stacks[(int)t] > 0; }

        public void Add(UpgradeType t)
        {
            Stacks[(int)t]++;
            if (t == UpgradeType.StoneSkin) stoneSkinCharges++;
        }

        public float MaxSpeed
        {
            get
            {
                float bonus = 0.12f * Count(UpgradeType.Speed);
                float now = Time.time;
                if (now < furyUntil) bonus += 0.1f;
                if (now < rushUntil) bonus += 0.3f;
                if (now < ricochetUntil) bonus += 0.25f;
                float m = 1f + bonus;
                if (Has(UpgradeType.Boulder)) m *= 0.95f;
                return BaseSpeed * m;
            }
        }

        public float Acceleration => PlayerBallController.Acceleration * (Has(UpgradeType.QuickStart) ? 1.2f : 1f);
        public float Deceleration => PlayerBallController.Deceleration * (Has(UpgradeType.Momentum) ? 0.5f : 1f);
        public float TurnBoost => Has(UpgradeType.Agile) ? 1.15f : 1f;
        public float SizeScale => 1f + 0.12f * Count(UpgradeType.BigBall);
        public float CollisionDamage => BaseDamage * (1f + 0.2f * Count(UpgradeType.HeavyBall) + (Has(UpgradeType.Boulder) ? 0.25f : 0f));
        public float KnockbackMultiplier => Has(UpgradeType.Crusher) ? 1.15f : 1f;

        public float ProjectileDamageMultiplier(bool hardened)
        {
            float m = 1f - 0.2f * Count(UpgradeType.Resistance) - 0.1f * Count(UpgradeType.ReinforcedCore) - (hardened ? 0.1f : 0f);
            return Mathf.Max(0.2f, m);
        }

        public float WoundDuration => Has(UpgradeType.Wound) ? 3f + (Count(UpgradeType.Wound) - 1) : 0f;
        public float WoundDamagePerSecond => WoundDps * (Has(UpgradeType.DeepWound) ? 1.5f : 1f);
    }
}
