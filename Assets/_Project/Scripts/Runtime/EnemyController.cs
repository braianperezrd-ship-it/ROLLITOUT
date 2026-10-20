using UnityEngine;

namespace RollItOut
{
    public enum EnemyType { Skeleton, Goblin, Orc, Slime, SlimeSmall, Archer, Shaman }
    public enum EnemyBehavior { Chaser, Archer, Shaman }

    public class EnemyDef
    {
        public EnemyType type;
        public string name;
        public EnemyBehavior behavior;
        public float hp, speed, size, knockbackResistance;
        public Color color;
        public int splitCount;
        public EnemyType splitType;
        // projectile attack (every enemy has one); minRange/maxRange only used by ranged units
        public float telegraph, projectileSpeed, cooldown, minRange, maxRange;
        public Color projectileColor;
    }

    public static class EnemyDefs
    {
        public static EnemyDef Get(EnemyType t)
        {
            switch (t)
            {
                case EnemyType.Skeleton:
                    return new EnemyDef { type = t, name = "Skeleton", behavior = EnemyBehavior.Chaser, hp = 20, speed = 3.0f, size = 0.9f, knockbackResistance = 1f, color = new Color(0.88f, 0.87f, 0.8f), telegraph = 1.0f, projectileSpeed = 5f, cooldown = 3.2f, projectileColor = new Color(1f, 0.95f, 0.5f) };
                case EnemyType.Goblin:
                    return new EnemyDef { type = t, name = "Goblin", behavior = EnemyBehavior.Chaser, hp = 14, speed = 4.6f, size = 0.6f, knockbackResistance = 0.9f, color = new Color(0.45f, 0.85f, 0.3f), telegraph = 0.8f, projectileSpeed = 6.5f, cooldown = 2.6f, projectileColor = new Color(0.7f, 1f, 0.2f) };
                case EnemyType.Orc:
                    return new EnemyDef { type = t, name = "Orc", behavior = EnemyBehavior.Chaser, hp = 90, speed = 1.7f, size = 1.5f, knockbackResistance = 5f, color = new Color(0.35f, 0.5f, 0.2f), telegraph = 1.3f, projectileSpeed = 4f, cooldown = 4f, projectileColor = new Color(1f, 0.35f, 0.15f) };
                case EnemyType.Slime:
                    return new EnemyDef { type = t, name = "Slime", behavior = EnemyBehavior.Chaser, hp = 30, speed = 1.8f, size = 1.1f, knockbackResistance = 1f, color = new Color(0.25f, 0.85f, 0.8f), splitCount = 2, splitType = EnemyType.SlimeSmall, telegraph = 1.0f, projectileSpeed = 4.5f, cooldown = 3.5f, projectileColor = new Color(0.2f, 1f, 0.9f) };
                case EnemyType.SlimeSmall:
                    return new EnemyDef { type = t, name = "Small Slime", behavior = EnemyBehavior.Chaser, hp = 12, speed = 2.5f, size = 0.6f, knockbackResistance = 1f, color = new Color(0.4f, 0.95f, 0.9f), telegraph = 0.9f, projectileSpeed = 4.5f, cooldown = 4.5f, projectileColor = new Color(0.2f, 1f, 0.9f) };
                case EnemyType.Archer:
                    return new EnemyDef { type = t, name = "Skeleton Archer", behavior = EnemyBehavior.Archer, hp = 20, speed = 2.0f, size = 0.9f, knockbackResistance = 1f, color = new Color(0.95f, 0.85f, 0.65f), telegraph = 0.9f, projectileSpeed = 7f, cooldown = 2.2f, minRange = 5f, maxRange = 9f, projectileColor = new Color(1f, 0.55f, 0.1f) };
                default:
                    return new EnemyDef { type = EnemyType.Shaman, name = "Goblin Shaman", behavior = EnemyBehavior.Shaman, hp = 14, speed = 2.6f, size = 0.7f, knockbackResistance = 1f, color = new Color(0.7f, 0.4f, 0.95f), telegraph = 0.65f, projectileSpeed = 5.5f, minRange = 6f, maxRange = 10f, projectileColor = new Color(1f, 0.25f, 0.85f) };
            }
        }
    }

    /// <summary>Shared enemy logic: chase / keep-distance movement, knockback, wound, ranged telegraph + fire.</summary>
    public class EnemyController : MonoBehaviour
    {
        public EnemyDef Def { get; private set; }
        public bool Dead { get; private set; }
        public bool Active => spawnTimer <= 0f;
        public float nextCollisionHitTime;
        public const float HitFlashTime = 0.12f;

        RoundManager round;
        Transform player;
        float speedScale, projSpeedScale, cooldownScale;
        Rigidbody2D rb;
        CircleCollider2D col;
        SpriteRenderer body;
        SpriteRenderer[] parts;   // every body sprite, so a hit flashes the whole enemy white
        Color[] partColors;
        Transform hpFill, telegraph;
        SpriteRenderer telegraphSr;
        float hp, maxHp, spawnTimer, spawnTotal, flashTimer, woundLeft, woundTick;
        Vector2 knockVel;
        bool telegraphing;
        float stateTimer, strafeSign = 1f;
        Vector2 aimDir = Vector2.right;
        Color baseColor;

        public void Init(EnemyDef def, RoundManager r, Transform p, float hpScale, float spdScale, float projScale, float cdScale, float spawnDelay)
        {
            Def = def; round = r; player = p;
            speedScale = spdScale; projSpeedScale = projScale; cooldownScale = cdScale;
            maxHp = hp = def.hp * hpScale;
            spawnTimer = spawnTotal = spawnDelay;
            strafeSign = Random.value < 0.5f ? -1f : 1f;
            stateTimer = Random.Range(0.8f, 1.8f);
            baseColor = def.color;

            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.mass = 1f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = def.size * 0.5f;
            col.sharedMaterial = new PhysicsMaterial2D { friction = 0f, bounciness = 0f };
            col.enabled = spawnDelay <= 0f;

            float s = def.size;
            body = SpriteLib.Add(transform, "Body", SpriteLib.Circle, baseColor, 5, Vector3.zero, Vector3.one * s);
            SpriteLib.Add(transform, "Outline", SpriteLib.Ring, new Color(0.05f, 0.04f, 0.08f), 6, Vector3.zero, Vector3.one * s * 1.04f);
            Color eye = def.type == EnemyType.Orc ? new Color(1f, 0.2f, 0.1f) : new Color(0.08f, 0.06f, 0.1f);
            SpriteLib.Add(transform, "EyeL", SpriteLib.Circle, eye, 7, new Vector3(-0.17f, 0.12f, 0f) * s, Vector3.one * 0.17f * s);
            SpriteLib.Add(transform, "EyeR", SpriteLib.Circle, eye, 7, new Vector3(0.17f, 0.12f, 0f) * s, Vector3.one * 0.17f * s);
            if (def.behavior == EnemyBehavior.Archer)
                SpriteLib.Add(transform, "Bow", SpriteLib.Ring, def.projectileColor, 7, Vector3.zero, Vector3.one * s * 0.55f);
            if (def.behavior == EnemyBehavior.Shaman)
                SpriteLib.Add(transform, "Aura", SpriteLib.Ring, def.projectileColor, 4, Vector3.zero, Vector3.one * s * 1.35f);
            if (def.type == EnemyType.Goblin)
            {
                SpriteLib.Add(transform, "EarL", SpriteLib.Diamond, baseColor, 4, new Vector3(-0.5f, 0.1f, 0f) * s, new Vector3(0.45f, 0.3f, 1f) * s);
                SpriteLib.Add(transform, "EarR", SpriteLib.Diamond, baseColor, 4, new Vector3(0.5f, 0.1f, 0f) * s, new Vector3(0.45f, 0.3f, 1f) * s);
            }

            parts = GetComponentsInChildren<SpriteRenderer>();
            partColors = new Color[parts.Length];
            for (int i = 0; i < parts.Length; i++) partColors[i] = parts[i].color;

            // HP bar (always above the body, never rotates).
            float y = s * 0.5f + 0.22f;
            SpriteLib.Add(transform, "HpBack", SpriteLib.Square, new Color(0f, 0f, 0f, 0.7f), 8, new Vector3(0f, y, 0f), new Vector3(s + 0.06f, 0.14f, 1f));
            hpFill = SpriteLib.Add(transform, "HpFill", SpriteLib.Square, new Color(0.9f, 0.25f, 0.25f), 9, new Vector3(0f, y, 0f), new Vector3(s, 0.09f, 1f)).transform;

            if (def.telegraph > 0f)
            {
                telegraphSr = SpriteLib.Add(transform, "Telegraph", SpriteLib.Square, def.projectileColor, 3, Vector3.zero, Vector3.one);
                telegraph = telegraphSr.transform;
                telegraph.gameObject.SetActive(false);
            }
            SetBar();
        }

        void SetBar()
        {
            float ratio = Mathf.Clamp01(hp / maxHp);
            hpFill.localScale = new Vector3(Def.size * ratio, 0.09f, 1f);
            hpFill.localPosition = new Vector3(-(1f - ratio) * Def.size * 0.5f, Def.size * 0.5f + 0.22f, 0f);
        }

        void Update()
        {
            if (Dead) return;
            float dt = Time.deltaTime;

            if (spawnTimer > 0f)
            {
                spawnTimer -= dt;
                float k = spawnTotal > 0f ? 1f - Mathf.Clamp01(spawnTimer / spawnTotal) : 1f;
                body.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.25f + 0.2f * Mathf.Sin(Time.time * 20f) * (1f - k) + 0.2f * k);
                if (spawnTimer <= 0f) col.enabled = true;
                return;
            }

            if (woundLeft > 0f)
            {
                woundLeft -= dt;
                woundTick += dt;
                if (woundTick >= 0.5f)
                {
                    woundTick -= 0.5f;
                    Fx.Burst(transform.position, new Color(0.8f, 0.05f, 0.1f), 2, 2f, 0.12f, 0.3f);
                    DamageNumbers.Spawn(PopupPos(), PlayerUpgradeState.WoundDps * 0.5f, new Color(1f, 0.35f, 0.35f), false);
                    Damage(PlayerUpgradeState.WoundDps * 0.5f);
                    if (Dead) return;
                }
            }

            if (flashTimer > 0f)
            {
                flashTimer -= dt;
                bool on = flashTimer > 0f;
                for (int i = 0; i < parts.Length; i++) parts[i].color = on ? Color.white : partColors[i];
            }
            if (flashTimer <= 0f) body.color = woundLeft > 0f ? Color.Lerp(baseColor, new Color(0.85f, 0.1f, 0.15f), 0.4f) : baseColor;

            if (Def.telegraph > 0f && player != null) UpdateRanged(dt);
        }

        void UpdateRanged(float dt)
        {
            Vector2 toP = player.position - transform.position;
            float dist = toP.magnitude;
            stateTimer -= dt;

            if (!telegraphing)
            {
                if (stateTimer <= 0f && dist < 14f)
                {
                    telegraphing = true;
                    stateTimer = Def.telegraph;
                    aimDir = toP.normalized;
                    telegraph.gameObject.SetActive(true);
                }
                return;
            }

            if (stateTimer > 0.25f) aimDir = toP.normalized; // aim locks for the final 0.25s so the shot is dodgeable
            float k = 1f - Mathf.Clamp01(stateTimer / Def.telegraph);
            bool locked = stateTimer <= 0.25f;
            float len = 7f;
            float ang = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            telegraph.localPosition = aimDir * (Def.size * 0.5f + len * 0.5f);
            telegraph.localRotation = Quaternion.Euler(0f, 0f, ang);
            telegraph.localScale = new Vector3(len, locked ? 0.16f : 0.08f, 1f);
            Color c = locked ? Color.white : Def.projectileColor;
            telegraphSr.color = new Color(c.r, c.g, c.b, 0.15f + 0.5f * k);

            if (stateTimer <= 0f)
            {
                Projectile.Spawn((Vector2)transform.position + aimDir * (Def.size * 0.5f + 0.2f), aimDir, Def.projectileSpeed * projSpeedScale, Def.projectileColor, round.ProjectileParent);
                telegraphing = false;
                telegraph.gameObject.SetActive(false);
                stateTimer = (Def.behavior == EnemyBehavior.Shaman ? Random.Range(1.3f, 2.6f) : Def.cooldown * Random.Range(0.9f, 1.1f)) * cooldownScale;
            }
        }

        void FixedUpdate()
        {
            if (Dead) return;
            if (spawnTimer > 0f || player == null) { rb.SetVel(knockVel); DecayKnock(); return; }

            Vector2 toP = player.position - transform.position;
            float dist = toP.magnitude;
            Vector2 dir = dist > 0.001f ? toP / dist : Vector2.zero;
            Vector2 desired = Vector2.zero;
            float spd = Def.speed * speedScale;

            switch (Def.behavior)
            {
                case EnemyBehavior.Chaser:
                    desired = dir * spd * (telegraphing ? 0.35f : 1f); // slows while winding up a shot
                    break;
                case EnemyBehavior.Archer:
                    if (!telegraphing)
                    {
                        if (dist > Def.maxRange) desired = dir * spd;
                        else if (dist < Def.minRange) desired = -dir * spd;
                    }
                    break;
                case EnemyBehavior.Shaman:
                    {
                        float mul = telegraphing ? 0.3f : 1f;
                        Vector2 perp = new Vector2(-dir.y, dir.x) * strafeSign;
                        if (dist < Def.minRange) desired = (-dir + perp * 0.4f).normalized * spd * mul;
                        else if (dist > Def.maxRange) desired = dir * spd * mul;
                        else desired = perp * spd * 0.6f * mul;
                        break;
                    }
            }

            // Ranged units steer back toward the middle near walls so they don't pin themselves in corners.
            if (Def.behavior != EnemyBehavior.Chaser)
            {
                Vector2 p = transform.position;
                Vector2 half = round.ArenaHalf;
                if (Mathf.Abs(p.x) > half.x - 1.3f || Mathf.Abs(p.y) > half.y - 1.3f)
                {
                    desired += -p.normalized * spd;
                    if (Random.value < 0.02f) strafeSign = -strafeSign;
                }
            }

            rb.SetVel(desired + knockVel);
            DecayKnock();
        }

        void DecayKnock()
        {
            knockVel = Vector2.MoveTowards(knockVel, Vector2.zero, 30f * Time.fixedDeltaTime);
        }

        public void TakeHit(int damage, Vector2 dir, float impactSpeed, float woundDuration)
        {
            if (Dead) return;
            Fx.Burst(transform.position, Def.color, 6, 5f, 0.15f, 0.3f);
            flashTimer = HitFlashTime;
            DamageNumbers.Spawn(PopupPos(), damage, Color.white, true);
            if (woundDuration > 0f) { woundLeft = woundDuration; woundTick = 0f; }
            Damage(damage);
            if (Dead) return;
            // Knockback scales with impact speed (clamped); heavy enemies barely move.
            knockVel = dir * Mathf.Clamp(impactSpeed * 2f, 4f, 16f) / Def.knockbackResistance;
        }

        Vector3 PopupPos() { return transform.position + Vector3.up * (Def.size * 0.5f + 0.45f); }

        void Damage(float amount)
        {
            hp -= amount;
            SetBar();
            if (hp <= 0f) Die();
        }

        public void Kick(Vector2 v) { knockVel = v; }

        void Die()
        {
            if (Dead) return;
            Dead = true;
            col.enabled = false;
            rb.simulated = false;
            Fx.Burst(transform.position, Def.color, 12, 6f, 0.2f, 0.5f);
            // Spawn children first so the round can never read as cleared mid-split.
            if (Def.splitCount > 0)
                for (int i = 0; i < Def.splitCount; i++)
                {
                    Vector2 off = Random.insideUnitCircle.normalized;
                    var child = round.SpawnEnemy(Def.splitType, (Vector2)transform.position + off * 0.3f, 0.15f);
                    child.Kick(off * 5f);
                }
            round.OnEnemyDied(this);
            Destroy(gameObject);
        }
    }
}
