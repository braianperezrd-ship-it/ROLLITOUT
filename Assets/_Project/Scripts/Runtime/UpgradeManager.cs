using System.Collections.Generic;
using UnityEngine;

namespace RollItOut
{
    // Order matches the card roster in the design doc (XP / elite utility cards are left out until those systems exist).
    public enum UpgradeType
    {
        Speed, BigBall, HeavyBall, Wound, StoneSkin, Resistance, QuickStart, Momentum, Agile, RollingFury,
        Rush, Crusher, Impact, Boulder, ChainImpact, Ricochet, DeepWound, SpreadingWound, OpenWound, BloodTrail,
        IronShell, ReinforcedCore, SecondChance, Hardened, SafeRoll, DustTrail, Shockwave, ExplosiveImpact, Spin, Aftershock,
        RollingFire
    }

    public enum Rarity { Common, Rare, Epic }

    public enum CardIcon { Lines, Grow, Heavy, Drops, Shield, Bar, Chevron, Wave, Turn, Burst, Heart, Trail, Pulse }

    /// <summary>One upgrade card: pure data. Gameplay reads stack counts from PlayerUpgradeState.</summary>
    public class CardDef
    {
        public UpgradeType id;
        public string title, category, text;
        public Rarity rarity;
        public int maxStacks = 1; // 0 = unlimited; cards only stack when their text says so
        public bool requiresWound;
        public Color color;
        public CardIcon icon;
    }

    public static class CardDefs
    {
        static readonly Color Move = new Color(0.3f, 0.8f, 1f);
        static readonly Color Crash = new Color(1f, 0.55f, 0.2f);
        static readonly Color Bleed = new Color(0.9f, 0.2f, 0.3f);
        static readonly Color Guard = new Color(0.65f, 0.68f, 0.75f);
        static readonly Color Ball = new Color(1f, 0.85f, 0.25f);

        static CardDef C(UpgradeType id, string title, string category, Rarity r, string text, Color color, CardIcon icon, int maxStacks = 1, bool requiresWound = false)
        {
            return new CardDef { id = id, title = title, category = category, rarity = r, text = text, color = color, icon = icon, maxStacks = maxStacks, requiresWound = requiresWound };
        }

        static readonly CardDef[] defs =
        {
            C(UpgradeType.Speed, "SPEED", "Movement", Rarity.Common, "Move 12% faster. Stacks.", Move, CardIcon.Lines, 0),
            C(UpgradeType.BigBall, "BIG BALL", "Movement", Rarity.Common, "Become 12% bigger. Stacks.", new Color(0.4f, 0.9f, 0.45f), CardIcon.Grow, 0),
            C(UpgradeType.HeavyBall, "HEAVY BALL", "Collision", Rarity.Common, "Deal 20% more collision damage. Stacks.", Crash, CardIcon.Heavy, 0),
            C(UpgradeType.Wound, "WOUND", "Damage Over Time", Rarity.Common, "Enemies you hit bleed 4/s for 3s. Stacks add +1s.", Bleed, CardIcon.Drops, 0),
            C(UpgradeType.StoneSkin, "STONE SKIN", "Defense", Rarity.Common, "Block the next projectile that hits you. +1 charge.", Guard, CardIcon.Shield, 0),
            C(UpgradeType.Resistance, "RESISTANCE", "Defense", Rarity.Common, "Take 20% less projectile damage. Max 3.", new Color(0.8f, 0.5f, 1f), CardIcon.Bar, 3),
            C(UpgradeType.QuickStart, "QUICK START", "Movement", Rarity.Common, "Accelerate 20% faster.", Move, CardIcon.Chevron),
            C(UpgradeType.Momentum, "MOMENTUM", "Movement", Rarity.Common, "Keep your speed longer after letting go.", Move, CardIcon.Wave),
            C(UpgradeType.Agile, "AGILE", "Movement", Rarity.Common, "Turn 15% faster.", Move, CardIcon.Turn),
            C(UpgradeType.RollingFury, "ROLLING FURY", "Movement", Rarity.Rare, "+10% speed for 2s after hitting an enemy.", Move, CardIcon.Lines),
            C(UpgradeType.Rush, "RUSH", "Defense", Rarity.Rare, "+30% speed for 2s after a projectile hurts you.", Guard, CardIcon.Chevron),
            C(UpgradeType.Crusher, "CRUSHER", "Collision", Rarity.Common, "Knock enemies back 15% further.", Crash, CardIcon.Chevron),
            C(UpgradeType.Impact, "IMPACT", "Collision", Rarity.Rare, "First hit after reaching full speed deals +50%.", Crash, CardIcon.Burst),
            C(UpgradeType.Boulder, "BOULDER", "Collision", Rarity.Rare, "+25% collision damage, but 5% slower.", Crash, CardIcon.Heavy),
            C(UpgradeType.ChainImpact, "CHAIN IMPACT", "Collision", Rarity.Rare, "A hit gives your next hit within 1.5s +20% damage.", Crash, CardIcon.Burst),
            C(UpgradeType.Ricochet, "RICOCHET", "Collision", Rarity.Rare, "Bounce off enemies harder and get a speed burst.", Crash, CardIcon.Turn),
            C(UpgradeType.DeepWound, "DEEP WOUND", "Damage Over Time", Rarity.Rare, "Wounded enemies take 50% more bleed damage.", Bleed, CardIcon.Drops, 1, true),
            C(UpgradeType.SpreadingWound, "SPREADING WOUND", "Damage Over Time", Rarity.Rare, "Killing a Wounded enemy wounds the nearest enemy.", Bleed, CardIcon.Drops, 1, true),
            C(UpgradeType.OpenWound, "OPEN WOUND", "Damage Over Time", Rarity.Rare, "Wounded enemies move 15% slower.", Bleed, CardIcon.Drops, 1, true),
            C(UpgradeType.BloodTrail, "BLOOD TRAIL", "Damage Over Time", Rarity.Epic, "Wounded enemies leave a trail that hurts enemies.", Bleed, CardIcon.Trail, 1, true),
            C(UpgradeType.IronShell, "IRON SHELL", "Defense", Rarity.Common, "+1 maximum heart.", Guard, CardIcon.Heart),
            C(UpgradeType.ReinforcedCore, "REINFORCED CORE", "Defense", Rarity.Common, "Take 10% less projectile damage.", Guard, CardIcon.Bar),
            C(UpgradeType.SecondChance, "SECOND CHANCE", "Defense", Rarity.Epic, "Once per round, survive a killing shot with 1 heart.", Guard, CardIcon.Heart),
            C(UpgradeType.Hardened, "HARDENED", "Defense", Rarity.Rare, "After 5s without damage, take 10% less. Lost when hit.", Guard, CardIcon.Shield),
            C(UpgradeType.SafeRoll, "SAFE ROLL", "Defense", Rarity.Rare, "Hitting an enemy makes you projectile-proof for 0.15s.", Guard, CardIcon.Shield),
            C(UpgradeType.DustTrail, "DUST TRAIL", "Ball Effect", Rarity.Rare, "Leave a damaging dust trail at high speed.", Ball, CardIcon.Trail),
            C(UpgradeType.Shockwave, "SHOCKWAVE", "Ball Effect", Rarity.Rare, "Every 5th hit sends out a damaging shockwave.", Ball, CardIcon.Pulse),
            C(UpgradeType.ExplosiveImpact, "EXPLOSIVE IMPACT", "Ball Effect", Rarity.Rare, "Enemies explode on death, hurting those nearby.", Ball, CardIcon.Burst),
            C(UpgradeType.Spin, "SPIN", "Ball Effect", Rarity.Rare, "Every hit releases a small damage pulse.", Ball, CardIcon.Pulse),
            C(UpgradeType.Aftershock, "AFTERSHOCK", "Ball Effect", Rarity.Epic, "Hard hits leave a damaging area for 2s.", Ball, CardIcon.Pulse),
            C(UpgradeType.RollingFire, "ROLLING FIRE", "Ball Effect", Rarity.Epic, "Leave a burning trail at maximum speed.", new Color(1f, 0.45f, 0.1f), CardIcon.Trail),
        };

        static CardDef[] byId;

        public static int Count => System.Enum.GetValues(typeof(UpgradeType)).Length;

        public static CardDef Get(UpgradeType t)
        {
            if (byId == null)
            {
                byId = new CardDef[Count];
                foreach (var d in defs) byId[(int)d.id] = d;
            }
            return byId[(int)t];
        }

        public static float Weight(Rarity r) { return r == Rarity.Common ? 6f : r == Rarity.Rare ? 3f : 1f; }

        public static Color RarityColor(Rarity r)
        {
            return r == Rarity.Common ? new Color(0.78f, 0.78f, 0.82f) : r == Rarity.Rare ? new Color(0.4f, 0.7f, 1f) : new Color(0.9f, 0.5f, 1f);
        }
    }

    public class UpgradeManager : MonoBehaviour
    {
        public PlayerUpgradeState state;

        public int Stacks(UpgradeType t) { return t == UpgradeType.StoneSkin ? state.stoneSkinCharges : state.Count(t); }

        bool Eligible(CardDef d)
        {
            if (d.maxStacks > 0 && state.Count(d.id) >= d.maxStacks) return false;
            if (d.requiresWound && !state.Has(UpgradeType.Wound)) return false;
            return true;
        }

        /// <summary>Unique, still-acquirable cards, weighted by rarity (Common 6 : Rare 3 : Epic 1).</summary>
        public List<UpgradeType> Roll(int count = 3)
        {
            var pool = new List<CardDef>();
            foreach (UpgradeType t in System.Enum.GetValues(typeof(UpgradeType)))
            {
                var d = CardDefs.Get(t);
                if (Eligible(d)) pool.Add(d);
            }
            var result = new List<UpgradeType>();
            while (result.Count < count && pool.Count > 0)
            {
                float total = 0f;
                foreach (var d in pool) total += CardDefs.Weight(d.rarity);
                float r = Random.value * total;
                int i = 0;
                for (; i < pool.Count - 1; i++)
                {
                    r -= CardDefs.Weight(pool[i].rarity);
                    if (r < 0f) break;
                }
                result.Add(pool[i].id);
                pool.RemoveAt(i);
            }
            return result;
        }

        public void Apply(UpgradeType t) { state.Add(t); }
    }
}
