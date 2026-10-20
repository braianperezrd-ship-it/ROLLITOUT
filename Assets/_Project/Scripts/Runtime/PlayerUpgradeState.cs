using UnityEngine;

namespace RollItOut
{
    /// <summary>Stack counts / charges for the six upgrades, plus the derived stats.</summary>
    public class PlayerUpgradeState : MonoBehaviour
    {
        public const float BaseSpeed = 6f;
        public const int BaseDamage = 10;
        public const int MaxResistanceStacks = 3;
        public const float WoundDps = 4f;

        public int speed;
        public int bigBall;
        public int heavyBall;
        public int wound;
        public int stoneSkinCharges;
        public int resistance;

        public float MaxSpeed => BaseSpeed * (1f + 0.12f * speed);
        public float SizeScale => 1f + 0.12f * bigBall;
        public int CollisionDamage => Mathf.RoundToInt(BaseDamage * (1f + 0.2f * heavyBall)); // 10 / 12 / 14 / 16
        public float ProjectileDamageMultiplier => 1f - 0.2f * Mathf.Min(resistance, MaxResistanceStacks);
        public float WoundDuration => wound > 0 ? 3f + (wound - 1) : 0f;
    }
}
