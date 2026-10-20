using UnityEngine;

namespace RollItOut
{
    /// <summary>Card stack counts, Stone Skin charges and short-lived card buffs, plus the derived stats.</summary>
    public class PlayerUpgradeState : MonoBehaviour
    {
        public const float BaseSpeed = 6f;
        public const int BaseDamage = 10;
        public const float WoundDps = 4f;
        public const float WoundDpsPerStack = 2f;
        public const float HardenedDelay = 4f;
        public const int MaxChainStacks = 5, MaxFuryStacks = 5;
        public const float OverdrivePerKill = 0.02f, OverdriveMax = 0.3f;
        /// <summary>Vampire: hearts healed per enemy death.</summary>
        public const float VampireHeal = 0.1f;
        public const float LastStandDamage = 0.3f, LastStandSpeed = 0.15f;

        int[] stacks;
        public int stoneSkinCharges;

        // Time.time deadlines for timed card effects.
        public float furyUntil = -1f, rushUntil = -1f, ricochetUntil = -1f, chainUntil = -1f;
        /// <summary>Impact: set once the ball reaches full speed, spent by the next hit.</summary>
        public bool impactArmed;
        /// <summary>Shockwave: collisions landed so far.</summary>
        public int hitCount;
        /// <summary>Chain Impact / Rolling Fury: hits landed in the current chain (each refreshes the window).</summary>
        public int chainStacks, furyStacks;
        /// <summary>Overdrive: kills this round.</summary>
        public int roundKills;
        /// <summary>Wall bounces banked for the next enemy hit, and when they fade (Time.time).</summary>
        public int wallCharges;
        public float wallChargeUntil = -1f;

        public int ActiveWallCharges => Time.time < wallChargeUntil ? wallCharges : 0;

        PlayerHealth health;
        bool LastStand => (health != null || (health = GetComponent<PlayerHealth>()) != null) && health.LastStandActive;

        int[] Stacks => stacks ?? (stacks = new int[CardDefs.Count]);

        public int Count(UpgradeType t) { return Stacks[(int)t]; }
        public bool Has(UpgradeType t) { return Stacks[(int)t] > 0; }

        public void Add(UpgradeType t)
        {
            Stacks[(int)t]++;
            if (t == UpgradeType.StoneSkin) stoneSkinCharges++;
        }

        /// <summary>Clears per-round buffs; Reinforced Core tops Stone Skin back up to one charge.</summary>
        public void OnRoundStart()
        {
            furyUntil = rushUntil = ricochetUntil = chainUntil = -1f;
            chainStacks = furyStacks = roundKills = wallCharges = 0;
            wallChargeUntil = -1f;
            impactArmed = false;
            if (Has(UpgradeType.ReinforcedCore)) stoneSkinCharges = Mathf.Max(stoneSkinCharges, 1);
        }

        public float MaxSpeed
        {
            get
            {
                float bonus = 0.12f * Count(UpgradeType.Speed);
                float now = Time.time;
                if (now < furyUntil) bonus += 0.05f * furyStacks;
                if (now < rushUntil) bonus += 0.3f;
                if (now < ricochetUntil) bonus += 0.25f;
                if (LastStand) bonus += LastStandSpeed;
                float m = 1f + bonus + OverdriveBonus;
                if (Has(UpgradeType.Boulder)) m *= 0.95f;
                return BaseSpeed * m;
            }
        }

        public float Acceleration => PlayerBallController.Acceleration * (Has(UpgradeType.QuickStart) ? 1.3f : 1f);
        public float Deceleration => PlayerBallController.Deceleration * (Has(UpgradeType.Momentum) ? 0.5f : 1f);
        /// <summary>Quick Dash: each copy cuts the dash recharge by 15%.</summary>
        public float DashCooldown => PlayerBallController.DashCooldown * (1f - 0.15f * Count(UpgradeType.QuickDash));
        /// <summary>Double Dash: dashes stored at once.</summary>
        public int MaxDashCharges => Has(UpgradeType.DoubleDash) ? 2 : 1;
        public float TurnBoost => Has(UpgradeType.Agile) ? 1.25f : 1f;
        public float SizeScale => 1f + 0.12f * Count(UpgradeType.BigBall);
        public float CollisionDamage => BaseDamage * (1f + 0.2f * Count(UpgradeType.HeavyBall) + (Has(UpgradeType.Boulder) ? 0.25f : 0f)) * (1f + OverdriveBonus) * (LastStand ? 1f + LastStandDamage : 1f);
        /// <summary>Overdrive: +2% speed and damage per kill this round.</summary>
        public float OverdriveBonus => Has(UpgradeType.Overdrive) ? Mathf.Min(OverdriveMax, OverdrivePerKill * roundKills) : 0f;
        /// <summary>Chain Impact: +10% per earlier hit in the current chain.</summary>
        public float ChainBonus => Has(UpgradeType.ChainImpact) && Time.time < chainUntil ? 0.1f * chainStacks : 0f;
        public float CrushDamageMultiplier => Has(UpgradeType.Crusher) ? 1.3f : 1f;
        public float CrushFlatMultiplier => Has(UpgradeType.Crusher) ? 1.5f : 1f;
        /// <summary>Largest enemy size, relative to the ball's diameter, that can be rolled over.</summary>
        public float CrushSizeRatio => Has(UpgradeType.Steamroller) ? 1.3f : 1.05f;

        public float ProjectileDamageMultiplier(bool hardened)
        {
            float m = 1f - 0.15f * Count(UpgradeType.Resistance) - (hardened ? 0.25f : 0f);
            return Mathf.Max(0.3f, m);
        }

        public float WoundDuration => Has(UpgradeType.Wound) ? 3f : 0f;
        public float WoundDamagePerSecond => (WoundDps + WoundDpsPerStack * Mathf.Max(0, Count(UpgradeType.Wound) - 1)) * (Has(UpgradeType.DeepWound) ? 1.5f : 1f);
    }
}
