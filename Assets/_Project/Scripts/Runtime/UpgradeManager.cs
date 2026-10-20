using System.Collections.Generic;
using UnityEngine;

namespace RollItOut
{
    public enum UpgradeType { Speed, BigBall, HeavyBall, Wound, StoneSkin, Resistance }

    public struct UpgradeInfo
    {
        public string title, text;
        public Color color;
    }

    public class UpgradeManager : MonoBehaviour
    {
        public PlayerUpgradeState state;

        public static UpgradeInfo Info(UpgradeType t)
        {
            switch (t)
            {
                case UpgradeType.Speed: return new UpgradeInfo { title = "SPEED", text = "Move 12% faster.", color = new Color(0.3f, 0.8f, 1f) };
                case UpgradeType.BigBall: return new UpgradeInfo { title = "BIG BALL", text = "Become 12% bigger.", color = new Color(0.4f, 0.9f, 0.45f) };
                case UpgradeType.HeavyBall: return new UpgradeInfo { title = "HEAVY BALL", text = "Deal 20% more collision damage.", color = new Color(1f, 0.55f, 0.2f) };
                case UpgradeType.Wound: return new UpgradeInfo { title = "WOUND", text = "Enemies you hit bleed for 3 seconds.", color = new Color(0.9f, 0.2f, 0.3f) };
                case UpgradeType.StoneSkin: return new UpgradeInfo { title = "STONE SKIN", text = "Block the next projectile that hits you.", color = new Color(0.65f, 0.68f, 0.75f) };
                default: return new UpgradeInfo { title = "RESISTANCE", text = "Take 20% less projectile damage.", color = new Color(0.8f, 0.5f, 1f) };
            }
        }

        public int Stacks(UpgradeType t)
        {
            switch (t)
            {
                case UpgradeType.Speed: return state.speed;
                case UpgradeType.BigBall: return state.bigBall;
                case UpgradeType.HeavyBall: return state.heavyBall;
                case UpgradeType.Wound: return state.wound;
                case UpgradeType.StoneSkin: return state.stoneSkinCharges;
                default: return state.resistance;
            }
        }

        bool Eligible(UpgradeType t)
        {
            // Resistance stops being offered at its 3-stack cap.
            return t != UpgradeType.Resistance || state.resistance < PlayerUpgradeState.MaxResistanceStacks;
        }

        /// <summary>Three unique, still-acquirable upgrades.</summary>
        public List<UpgradeType> Roll(int count = 3)
        {
            var pool = new List<UpgradeType>();
            foreach (UpgradeType t in System.Enum.GetValues(typeof(UpgradeType)))
                if (Eligible(t)) pool.Add(t);
            var result = new List<UpgradeType>();
            while (result.Count < count && pool.Count > 0)
            {
                int i = Random.Range(0, pool.Count);
                result.Add(pool[i]);
                pool.RemoveAt(i);
            }
            return result;
        }

        public void Apply(UpgradeType t)
        {
            switch (t)
            {
                case UpgradeType.Speed: state.speed++; break;
                case UpgradeType.BigBall: state.bigBall++; break;
                case UpgradeType.HeavyBall: state.heavyBall++; break;
                case UpgradeType.Wound: state.wound++; break;
                case UpgradeType.StoneSkin: state.stoneSkinCharges++; break;
                case UpgradeType.Resistance: state.resistance = Mathf.Min(state.resistance + 1, PlayerUpgradeState.MaxResistanceStacks); break;
            }
        }
    }
}
