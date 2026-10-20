using System.Collections.Generic;
using UnityEngine;

namespace RollItOut
{
    // Order matches the card roster in the design doc. The five XP / elite utility cards wait for those systems;
    // their slots are filled by the cards after RollingFire (healing, crush and the Movement / Collision epics).
    public enum UpgradeType
    {
        Speed, BigBall, HeavyBall, Wound, StoneSkin, Resistance, QuickStart, Momentum, Agile, RollingFury,
        Rush, Crusher, Impact, Boulder, ChainImpact, Ricochet, DeepWound, SpreadingWound, OpenWound, BloodTrail,
        IronShell, ReinforcedCore, SecondChance, Hardened, SafeRoll, DustTrail, Shockwave, ExplosiveImpact, Spin, Aftershock,
        RollingFire, Regenerate, Deflect, Steamroller, WreckingBall, Overdrive,
        QuickDash, DoubleDash, PhaseDash, ReflectDash, Slingshot, Rebound, Pinball,
        FirstAid, Vampire, LastStand, GiantHeart
    }

    public enum Rarity { Common, Rare, Epic }

    public enum CardIcon { Lines, Grow, Heavy, Drops, Shield, Bar, Chevron, Wave, Turn, Burst, Heart, Trail, Pulse, Dash }

    /// <summary>One upgrade card: pure data. Gameplay reads stack counts from PlayerUpgradeState.</summary>
    public class CardDef
    {
        public UpgradeType id;
        public string title, category, text;
        public Rarity rarity;
        public int maxStacks = 1; // 0 = unlimited; cards only stack when their text says so
        public bool requiresWound;
        public bool requiresDamage; // only offered while the player is missing health
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
        static readonly Color Dash = new Color(0.6f, 0.9f, 1f);
        static readonly Color Life = new Color(0.4f, 1f, 0.5f);

        static CardDef C(UpgradeType id, string title, string category, Rarity r, string text, Color color, CardIcon icon, int maxStacks = 1, bool requiresWound = false)
        {
            return new CardDef { id = id, title = title, category = category, rarity = r, text = text, color = color, icon = icon, maxStacks = maxStacks, requiresWound = requiresWound };
        }

        static CardDef Hurt(CardDef d) { d.requiresDamage = true; return d; }

        static readonly CardDef[] defs =
        {
            C(UpgradeType.Speed, "SPEED", "Movement", Rarity.Common, "Move 12% faster. Max 5.", Move, CardIcon.Lines, 5),
            C(UpgradeType.BigBall, "BIG BALL", "Movement", Rarity.Common, "Become 12% bigger and crush bigger enemies. Max 4.", new Color(0.4f, 0.9f, 0.45f), CardIcon.Grow, 4),
            C(UpgradeType.HeavyBall, "HEAVY BALL", "Collision", Rarity.Common, "Deal 20% more collision damage. Max 5.", Crash, CardIcon.Heavy, 5),
            C(UpgradeType.Wound, "WOUND", "Damage Over Time", Rarity.Common, "Enemies you hit bleed 4/s for 3s. Stacks add +2/s. Max 4.", Bleed, CardIcon.Drops, 4),
            C(UpgradeType.StoneSkin, "STONE SKIN", "Defense", Rarity.Common, "Block the next projectile that hits you. +1 charge.", Guard, CardIcon.Shield, 0),
            C(UpgradeType.Resistance, "RESISTANCE", "Defense", Rarity.Common, "Take 15% less projectile damage. Max 3.", new Color(0.8f, 0.5f, 1f), CardIcon.Bar, 3),
            C(UpgradeType.QuickStart, "QUICK START", "Movement", Rarity.Common, "Accelerate 30% faster.", Move, CardIcon.Chevron),
            C(UpgradeType.Momentum, "MOMENTUM", "Movement", Rarity.Common, "Keep your speed longer after letting go.", Move, CardIcon.Wave),
            C(UpgradeType.Agile, "AGILE", "Movement", Rarity.Common, "Turn 25% faster.", Move, CardIcon.Turn),
            C(UpgradeType.RollingFury, "ROLLING FURY", "Movement", Rarity.Rare, "Each hit gives +5% speed for 2s, up to +25%.", Move, CardIcon.Lines),
            C(UpgradeType.Rush, "RUSH", "Defense", Rarity.Rare, "+30% speed for 2s after a projectile hurts you.", Guard, CardIcon.Chevron),
            C(UpgradeType.Crusher, "CRUSHER", "Collision", Rarity.Common, "Crushing deals +30% damage and pins enemies longer.", Crash, CardIcon.Chevron),
            C(UpgradeType.Impact, "IMPACT", "Collision", Rarity.Rare, "First hit after reaching full speed deals +50%.", Crash, CardIcon.Burst),
            C(UpgradeType.Boulder, "BOULDER", "Collision", Rarity.Rare, "+25% collision damage, but 5% slower.", Crash, CardIcon.Heavy),
            C(UpgradeType.ChainImpact, "CHAIN IMPACT", "Collision", Rarity.Rare, "Hits within 1.5s of each other add +10% damage, up to +50%.", Crash, CardIcon.Burst),
            C(UpgradeType.Ricochet, "RICOCHET", "Collision", Rarity.Rare, "Bounce off enemies harder and get a speed burst.", Crash, CardIcon.Turn),
            C(UpgradeType.DeepWound, "DEEP WOUND", "Damage Over Time", Rarity.Rare, "Wounded enemies take 50% more bleed damage.", Bleed, CardIcon.Drops, 1, true),
            C(UpgradeType.SpreadingWound, "SPREADING WOUND", "Damage Over Time", Rarity.Rare, "Killing a Wounded enemy wounds the nearest enemy.", Bleed, CardIcon.Drops, 1, true),
            C(UpgradeType.OpenWound, "OPEN WOUND", "Damage Over Time", Rarity.Rare, "Wounded enemies move 20% slower.", Bleed, CardIcon.Drops, 1, true),
            C(UpgradeType.BloodTrail, "BLOOD TRAIL", "Damage Over Time", Rarity.Epic, "Wounded enemies leave a trail that hurts other enemies.", Bleed, CardIcon.Trail, 1, true),
            C(UpgradeType.IronShell, "IRON SHELL", "Defense", Rarity.Common, "+1 maximum heart. Max 2.", Guard, CardIcon.Heart, 2),
            C(UpgradeType.ReinforcedCore, "REINFORCED CORE", "Defense", Rarity.Common, "Start every round with at least 1 Stone Skin charge.", Guard, CardIcon.Shield),
            C(UpgradeType.SecondChance, "SECOND CHANCE", "Defense", Rarity.Epic, "Once per round, survive a killing shot with 1 heart.", Guard, CardIcon.Heart),
            C(UpgradeType.Hardened, "HARDENED", "Defense", Rarity.Rare, "After 4s without damage, take 25% less. Lost when hit.", Guard, CardIcon.Shield),
            C(UpgradeType.SafeRoll, "SAFE ROLL", "Defense", Rarity.Rare, "Hitting an enemy makes you projectile-proof for 0.4s.", Guard, CardIcon.Shield),
            C(UpgradeType.DustTrail, "DUST TRAIL", "Ball Effect", Rarity.Rare, "Leave a damaging dust trail at high speed.", Ball, CardIcon.Trail),
            C(UpgradeType.Shockwave, "SHOCKWAVE", "Ball Effect", Rarity.Rare, "Every 5th hit sends out a damaging shockwave.", Ball, CardIcon.Pulse),
            C(UpgradeType.ExplosiveImpact, "EXPLOSIVE IMPACT", "Ball Effect", Rarity.Rare, "Enemies explode on death, hurting those nearby.", Ball, CardIcon.Burst),
            C(UpgradeType.Spin, "SPIN", "Ball Effect", Rarity.Rare, "Every hit releases a small damage pulse.", Ball, CardIcon.Pulse),
            C(UpgradeType.Aftershock, "AFTERSHOCK", "Ball Effect", Rarity.Epic, "Hard hits leave a damaging area for 2s.", Ball, CardIcon.Pulse),
            C(UpgradeType.RollingFire, "ROLLING FIRE", "Ball Effect", Rarity.Epic, "Leave a burning trail at maximum speed.", new Color(1f, 0.45f, 0.1f), CardIcon.Trail),
            C(UpgradeType.Regenerate, "REGENERATE", "Defense", Rarity.Rare, "Heal 1 heart after every round you clear.", Guard, CardIcon.Heart),
            C(UpgradeType.Deflect, "DEFLECT", "Collision", Rarity.Rare, "Roll into projectiles at high speed to smash them.", Crash, CardIcon.Shield),
            C(UpgradeType.Steamroller, "STEAMROLLER", "Collision", Rarity.Rare, "Crush enemies up to 30% bigger than your ball.", Crash, CardIcon.Grow),
            C(UpgradeType.WreckingBall, "WRECKING BALL", "Collision", Rarity.Epic, "At full speed, smash through big enemies without bouncing.", Crash, CardIcon.Heavy),
            C(UpgradeType.Overdrive, "OVERDRIVE", "Movement", Rarity.Epic, "Each kill this round: +2% speed and damage, up to +30%.", Move, CardIcon.Lines),
            C(UpgradeType.QuickDash, "QUICK DASH", "Dash", Rarity.Common, "Dash recharges 15% faster. Max 3.", Dash, CardIcon.Dash, 3),
            C(UpgradeType.DoubleDash, "DOUBLE DASH", "Dash", Rarity.Rare, "Store 2 dashes and chain them back to back.", Dash, CardIcon.Dash),
            C(UpgradeType.PhaseDash, "PHASE DASH", "Dash", Rarity.Rare, "You can't take damage while dashing.", Dash, CardIcon.Shield),
            C(UpgradeType.ReflectDash, "REFLECT DASH", "Dash", Rarity.Rare, "Dashing into projectiles sends them back to hurt enemies.", Dash, CardIcon.Turn),
            C(UpgradeType.Slingshot, "SLINGSHOT", "Dash", Rarity.Rare, "Wall bounces restart your dash and launch you much farther.", Dash, CardIcon.Dash),
            C(UpgradeType.Rebound, "REBOUND", "Collision", Rarity.Rare, "Rolling into a wall at full speed bounces you off it, no dash needed.", Crash, CardIcon.Turn),
            C(UpgradeType.Pinball, "PINBALL", "Collision", Rarity.Rare, "Crashing into an enemy bounces you into the nearest other one. Up to 4 in a row.", Crash, CardIcon.Turn),
            Hurt(C(UpgradeType.FirstAid, "FIRST AID", "Health", Rarity.Common, "Heal 2 hearts right now. Only offered when hurt.", Life, CardIcon.Heart, 0)),
            C(UpgradeType.Vampire, "VAMPIRE", "Health", Rarity.Rare, "Heal 10% of a heart every time an enemy dies.", Bleed, CardIcon.Heart),
            C(UpgradeType.LastStand, "LAST STAND", "Health", Rarity.Rare, "At 1 heart, deal 30% more damage and move 15% faster.", Bleed, CardIcon.Heart),
            C(UpgradeType.GiantHeart, "GIANT HEART", "Health", Rarity.Epic, "+2 maximum hearts and heal to full.", Life, CardIcon.Heart),
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

        PlayerHealth health;
        PlayerHealth Health => health != null ? health : (health = state.GetComponent<PlayerHealth>());

        public int Stacks(UpgradeType t) { return t == UpgradeType.StoneSkin ? state.stoneSkinCharges : state.Count(t); }

        bool Eligible(CardDef d)
        {
            if (d.maxStacks > 0 && state.Count(d.id) >= d.maxStacks) return false;
            if (d.requiresWound && !state.Has(UpgradeType.Wound)) return false;
            if (d.requiresDamage && (Health == null || !Health.Hurt)) return false;
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
