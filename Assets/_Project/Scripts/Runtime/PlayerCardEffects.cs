using System.Collections.Generic;
using UnityEngine;

namespace RollItOut
{
    /// <summary>Event-driven card effects: on-hit, on-kill and movement trails. Stat cards live in PlayerUpgradeState.</summary>
    [RequireComponent(typeof(PlayerBallController))]
    public class PlayerCardEffects : MonoBehaviour
    {
        public const int ShockwaveEvery = 5;

        public RoundManager round;

        PlayerUpgradeState up;
        PlayerBallController ctrl;
        PlayerHealth health;
        float dustTimer, fireTimer;

        void Awake()
        {
            up = GetComponent<PlayerUpgradeState>();
            ctrl = GetComponent<PlayerBallController>();
            health = GetComponent<PlayerHealth>();
        }

        void Update()
        {
            if (round == null || round.State != GameState.Playing) return;
            Vector2 pos = transform.position;
            float speed = ctrl.PreStepVelocity.magnitude;
            float max = up.MaxSpeed;
            float r = 0.45f * up.SizeScale;

            dustTimer -= Time.deltaTime;
            if (up.Has(UpgradeType.DustTrail) && speed > 3f && speed >= max * 0.75f && dustTimer <= 0f)
            {
                dustTimer = 0.12f;
                DamageZone.Spawn(round, ZoneKind.Dust, pos, r, 8f, 1f, new Color(0.85f, 0.78f, 0.6f, 0.35f));
            }

            fireTimer -= Time.deltaTime;
            if (up.Has(UpgradeType.RollingFire) && speed >= max * 0.95f && fireTimer <= 0f)
            {
                fireTimer = 0.1f;
                DamageZone.Spawn(round, ZoneKind.Fire, pos, r * 1.1f, 14f, 1.4f, new Color(1f, 0.45f, 0.1f, 0.5f));
            }
        }

        public void OnEnemyHit(EnemyController e, float impact)
        {
            float now = Time.time;
            Vector2 pos = transform.position;
            up.hitCount++;

            if (up.Has(UpgradeType.RollingFury))
            {
                up.furyStacks = now < up.furyUntil ? Mathf.Min(PlayerUpgradeState.MaxFuryStacks, up.furyStacks + 1) : 1;
                up.furyUntil = now + 2f;
            }
            if (up.Has(UpgradeType.Ricochet)) up.ricochetUntil = now + 0.8f;
            // Chain Impact: this hit already used the bonus; it now counts toward the next one.
            up.chainStacks = now < up.chainUntil ? Mathf.Min(PlayerUpgradeState.MaxChainStacks, up.chainStacks + 1) : 1;
            up.chainUntil = now + 1.5f;
            if (up.Has(UpgradeType.SafeRoll)) health.GrantImmunity(0.4f);

            if (up.Has(UpgradeType.Spin))
                Area(pos, 1.4f * up.SizeScale, 4f, 4f, e, new Color(1f, 0.9f, 0.4f, 0.8f));

            if (up.Has(UpgradeType.Shockwave) && up.hitCount % ShockwaveEvery == 0)
            {
                Area(pos, 2.4f * up.SizeScale, 10f, 9f, null, new Color(0.5f, 0.85f, 1f, 0.9f));
                CameraShake.Shake(0.1f, 0.12f);
            }

            if (up.Has(UpgradeType.Aftershock) && impact >= PlayerUpgradeState.BaseSpeed * 0.9f)
                DamageZone.Spawn(round, ZoneKind.Aftershock, e.transform.position, 1.2f, 10f, 2f, new Color(0.65f, 0.5f, 0.3f, 0.45f));
        }

        public void OnEnemyKilled(EnemyController e)
        {
            Vector2 pos = e.transform.position;
            up.roundKills++;
            if (up.Has(UpgradeType.SpreadingWound) && e.Wounded)
            {
                EnemyController best = null;
                float bestD = 7f;
                foreach (var o in round.Alive)
                {
                    if (o == e || o.Dead || !o.Active) continue;
                    float d = Vector2.Distance(pos, o.transform.position);
                    if (d < bestD) { bestD = d; best = o; }
                }
                if (best != null)
                {
                    best.ApplyWound(up.WoundDuration, up.WoundDamagePerSecond);
                    Fx.Burst(best.transform.position, new Color(0.8f, 0.05f, 0.1f), 6, 3f, 0.15f, 0.35f);
                }
            }

            if (up.Has(UpgradeType.ExplosiveImpact))
                Area(pos, 1.8f, 10f, 8f, e, new Color(1f, 0.55f, 0.15f, 0.9f));
        }

        /// <summary>Instant ring of damage. Iterates a copy: kills inside can chain into more deaths.</summary>
        void Area(Vector2 pos, float radius, float damage, float push, EnemyController skip, Color color)
        {
            PulseRing.Spawn(pos, radius, color);
            var targets = new List<EnemyController>(round.Alive);
            foreach (var o in targets)
            {
                if (o == skip || o.Dead || !o.Active) continue;
                if (Vector2.Distance(pos, o.transform.position) <= radius + o.Radius) o.TakeAreaDamage(damage, pos, push);
            }
        }
    }

    /// <summary>Damage zone families; zones of one kind don't stack on the same enemy (see EnemyController.ClaimZoneTick).</summary>
    public enum ZoneKind { Dust, Fire, Blood, Aftershock }

    /// <summary>Ground area that damages enemies (never the player) every frame it overlaps them.</summary>
    public class DamageZone : MonoBehaviour
    {
        // Shared snapshot of the living enemies: zones update one at a time, and kills mid-loop must not break iteration.
        static readonly List<EnemyController> targets = new List<EnemyController>();

        RoundManager round;
        ZoneKind kind;
        EnemyController owner; // Blood Trail: the bleeding enemy doesn't hurt itself
        float radius, dps, life, age;
        SpriteRenderer sr;
        Color color;

        public static DamageZone Spawn(RoundManager round, ZoneKind kind, Vector2 pos, float radius, float dps, float life, Color color, EnemyController owner = null)
        {
            var go = new GameObject("DamageZone");
            go.transform.SetParent(round.ProjectileParent, false); // cleared with projectiles between rounds
            go.transform.position = pos;
            var z = go.AddComponent<DamageZone>();
            z.round = round; z.kind = kind; z.owner = owner; z.radius = radius; z.dps = dps; z.life = life; z.color = color;
            z.sr = SpriteLib.Add(go.transform, "Area", SpriteLib.Circle, color, -4, Vector3.zero, Vector3.one * radius * 2f);
            return z;
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= life) { Destroy(gameObject); return; }
            float k = age / life;
            sr.color = new Color(color.r, color.g, color.b, color.a * (1f - k * k));

            Vector2 pos = transform.position;
            targets.Clear();
            targets.AddRange(round.Alive);
            foreach (var o in targets)
            {
                if (o == null || o == owner || o.Dead || !o.Active) continue;
                if (Vector2.Distance(pos, o.transform.position) <= radius + o.Radius && o.ClaimZoneTick(kind))
                    o.TakeAreaDamage(dps * Time.deltaTime, pos, 0f);
            }
        }
    }

    /// <summary>Expanding ring that shows the reach of an instant area effect.</summary>
    public class PulseRing : MonoBehaviour
    {
        const float Life = 0.25f;
        float radius, age;
        SpriteRenderer sr;
        Color color;

        public static void Spawn(Vector2 pos, float radius, Color color)
        {
            var go = new GameObject("Pulse");
            if (Fx.Root != null) go.transform.SetParent(Fx.Root, false);
            go.transform.position = pos;
            var p = go.AddComponent<PulseRing>();
            p.radius = radius; p.color = color;
            p.sr = SpriteLib.Add(go.transform, "Ring", SpriteLib.Ring, color, 19, Vector3.zero, Vector3.one * 0.2f);
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= Life) { Destroy(gameObject); return; }
            float k = age / Life;
            sr.transform.localScale = Vector3.one * radius * 2f * Mathf.Lerp(0.3f, 1f, Mathf.Sqrt(k));
            sr.color = new Color(color.r, color.g, color.b, color.a * (1f - k));
        }
    }
}
