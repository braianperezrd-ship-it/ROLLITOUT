using UnityEngine;

namespace RollItOut
{
    public enum EnemyType { Skeleton, Goblin, Orc, Slime, SlimeSmall, Archer, Shaman }
    public enum EnemyBehavior { Chaser, Archer, Shaman }
    /// <summary>How a shot is fired, so each enemy reads differently: one bolt, a quick volley, a cone, a ring, or a seeker.</summary>
    public enum AttackPattern { Single, Burst, Fan, Ring, Homing }

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
        // attack pattern: shots = volley length (Burst), cone/ring count (Fan/Ring); spread = Fan cone width in degrees
        public AttackPattern attack;
        public int shots = 1;
        public float spread, burstGap, projectileSize = 1f;
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
                    return new EnemyDef { type = t, name = "Goblin", behavior = EnemyBehavior.Chaser, hp = 14, speed = 4.6f, size = 0.6f, knockbackResistance = 0.9f, color = new Color(0.45f, 0.85f, 0.3f), telegraph = 0.8f, projectileSpeed = 6.5f, cooldown = 3.0f, projectileColor = new Color(0.7f, 1f, 0.2f), attack = AttackPattern.Burst, shots = 3, burstGap = 0.14f, projectileSize = 0.8f };
                case EnemyType.Orc:
                    return new EnemyDef { type = t, name = "Orc", behavior = EnemyBehavior.Chaser, hp = 90, speed = 1.7f, size = 1.5f, knockbackResistance = 5f, color = new Color(0.35f, 0.5f, 0.2f), telegraph = 1.3f, projectileSpeed = 4f, cooldown = 4f, projectileColor = new Color(1f, 0.35f, 0.15f), attack = AttackPattern.Fan, shots = 5, spread = 70f, projectileSize = 1.25f };
                case EnemyType.Slime:
                    return new EnemyDef { type = t, name = "Slime", behavior = EnemyBehavior.Chaser, hp = 30, speed = 1.8f, size = 1.1f, knockbackResistance = 1f, color = new Color(0.25f, 0.85f, 0.8f), splitCount = 2, splitType = EnemyType.SlimeSmall, telegraph = 1.0f, projectileSpeed = 4.5f, cooldown = 3.5f, projectileColor = new Color(0.2f, 1f, 0.9f), attack = AttackPattern.Ring, shots = 8 };
                case EnemyType.SlimeSmall:
                    return new EnemyDef { type = t, name = "Small Slime", behavior = EnemyBehavior.Chaser, hp = 12, speed = 2.5f, size = 0.6f, knockbackResistance = 1f, color = new Color(0.4f, 0.95f, 0.9f), telegraph = 0.9f, projectileSpeed = 4.5f, cooldown = 4.5f, projectileColor = new Color(0.2f, 1f, 0.9f) };
                case EnemyType.Archer:
                    return new EnemyDef { type = t, name = "Skeleton Archer", behavior = EnemyBehavior.Archer, hp = 20, speed = 2.0f, size = 0.9f, knockbackResistance = 1f, color = new Color(0.95f, 0.85f, 0.65f), telegraph = 0.9f, projectileSpeed = 7f, cooldown = 2.6f, minRange = 5f, maxRange = 9f, projectileColor = new Color(1f, 0.55f, 0.1f), attack = AttackPattern.Fan, shots = 3, spread = 18f };
                default:
                    return new EnemyDef { type = EnemyType.Shaman, name = "Goblin Shaman", behavior = EnemyBehavior.Shaman, hp = 14, speed = 2.6f, size = 0.7f, knockbackResistance = 1f, color = new Color(0.7f, 0.4f, 0.95f), telegraph = 0.8f, projectileSpeed = 4.5f, minRange = 6f, maxRange = 10f, projectileColor = new Color(1f, 0.25f, 0.85f), attack = AttackPattern.Homing, projectileSize = 1.3f };
            }
        }
    }

    /// <summary>Shared enemy logic: chase / keep-distance movement, knockback, wound cards, ranged telegraph + fire.</summary>
    public class EnemyController : MonoBehaviour
    {
        public EnemyDef Def { get; private set; }
        public bool Dead { get; private set; }
        public bool Active => spawnTimer <= 0f;
        public bool Wounded => woundLeft > 0f;
        public float Radius => Def.size * 0.5f;
        public float nextCollisionHitTime;
        public CircleCollider2D Collider => col;
        public const float FlatDuration = 0.9f;
        public const float HitFlashTime = 0.12f;

        RoundManager round;
        Transform player;
        float speedScale, projSpeedScale, cooldownScale;
        Rigidbody2D rb;
        CircleCollider2D col;
        SpriteRenderer body;
        SpriteRenderer[] parts;   // every body sprite, so a hit flashes the whole enemy white
        Color[] partColors;
        Transform vis, hpFill;
        SpriteRenderer[] telegraphs;   // one aim line per shot of a Fan, else a single line (or a ring for Ring)
        float hp, maxHp, spawnTimer, spawnTotal, flashTimer, woundLeft, woundTick, woundDps, zoneDamage, zoneTimer, bloodTimer, flatTimer;
        bool crushedToDeath;
        Vector2 crushDir = Vector2.right;
        Vector2 knockVel;
        bool telegraphing;
        int burstLeft;
        float burstTimer;
        bool Attacking => telegraphing || burstLeft > 0;
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
            // Body parts live under one child so a crush can squash them without touching the collider or HP bar.
            vis = new GameObject("Visual").transform;
            vis.SetParent(transform, false);
            body = SpriteLib.Add(vis, "Body", SpriteLib.Circle, baseColor, 5, Vector3.zero, Vector3.one * s);
            SpriteLib.Add(vis, "Outline", SpriteLib.Ring, new Color(0.05f, 0.04f, 0.08f), 6, Vector3.zero, Vector3.one * s * 1.04f);
            Color eye = def.type == EnemyType.Orc ? new Color(1f, 0.2f, 0.1f) : new Color(0.08f, 0.06f, 0.1f);
            SpriteLib.Add(vis, "EyeL", SpriteLib.Circle, eye, 7, new Vector3(-0.17f, 0.12f, 0f) * s, Vector3.one * 0.17f * s);
            SpriteLib.Add(vis, "EyeR", SpriteLib.Circle, eye, 7, new Vector3(0.17f, 0.12f, 0f) * s, Vector3.one * 0.17f * s);
            if (def.behavior == EnemyBehavior.Archer)
                SpriteLib.Add(vis, "Bow", SpriteLib.Ring, def.projectileColor, 7, Vector3.zero, Vector3.one * s * 0.55f);
            if (def.behavior == EnemyBehavior.Shaman)
                SpriteLib.Add(vis, "Aura", SpriteLib.Ring, def.projectileColor, 4, Vector3.zero, Vector3.one * s * 1.35f);
            if (def.type == EnemyType.Goblin)
            {
                SpriteLib.Add(vis, "EarL", SpriteLib.Diamond, baseColor, 4, new Vector3(-0.5f, 0.1f, 0f) * s, new Vector3(0.45f, 0.3f, 1f) * s);
                SpriteLib.Add(vis, "EarR", SpriteLib.Diamond, baseColor, 4, new Vector3(0.5f, 0.1f, 0f) * s, new Vector3(0.45f, 0.3f, 1f) * s);
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
                int n = def.attack == AttackPattern.Fan ? def.shots : 1;
                telegraphs = new SpriteRenderer[n];
                for (int i = 0; i < n; i++)
                {
                    telegraphs[i] = SpriteLib.Add(transform, "Telegraph", def.attack == AttackPattern.Ring ? SpriteLib.Ring : SpriteLib.Square, def.projectileColor, 3, Vector3.zero, Vector3.one);
                    telegraphs[i].gameObject.SetActive(false);
                }
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

            FlushZoneNumber(dt);

            if (woundLeft > 0f)
            {
                woundLeft -= dt;
                woundTick += dt;
                if (woundTick >= 0.5f)
                {
                    woundTick -= 0.5f;
                    Fx.Burst(transform.position, new Color(0.8f, 0.05f, 0.1f), 2, 2f, 0.12f, 0.3f);
                    Damage(woundDps * 0.5f, WoundNumber, false);
                    if (Dead) return;
                }
                // Blood Trail: a wounded enemy on the move drips puddles that hurt other enemies.
                bloodTimer -= dt;
                if (bloodTimer <= 0f && round.PlayerUpgrades.Has(UpgradeType.BloodTrail) && rb.GetVel().sqrMagnitude > 0.25f)
                {
                    bloodTimer = 0.3f;
                    DamageZone.Spawn(round, transform.position, 0.45f, 6f, 1.5f, new Color(0.7f, 0.05f, 0.1f, 0.45f));
                }
            }

            if (flashTimer > 0f)
            {
                flashTimer -= dt;
                bool on = flashTimer > 0f;
                for (int i = 0; i < parts.Length; i++) parts[i].color = on ? Color.white : partColors[i];
            }
            if (flashTimer <= 0f) body.color = woundLeft > 0f ? Color.Lerp(baseColor, new Color(0.85f, 0.1f, 0.15f), 0.4f) : baseColor;

            if (flatTimer > 0f)
            {
                flatTimer -= dt;
                UpdateFlatVisual();
                if (flashTimer <= 0f) body.color = Color.Lerp(body.color, Color.black, 0.25f);
                return; // pancakes don't shoot
            }

            if (Def.telegraph > 0f && player != null) UpdateRanged(dt);
        }

        void UpdateRanged(float dt)
        {
            Vector2 toP = player.position - transform.position;
            float dist = toP.magnitude;
            stateTimer -= dt;

            if (burstLeft > 0)
            {
                burstTimer -= dt;
                if (burstTimer <= 0f) { Shoot(aimDir); burstLeft--; burstTimer = Def.burstGap; }
                return;
            }

            if (!telegraphing)
            {
                if (stateTimer <= 0f && dist < 14f)
                {
                    telegraphing = true;
                    stateTimer = Def.telegraph;
                    aimDir = toP.normalized;
                    SetTelegraph(true);
                }
                return;
            }

            if (stateTimer > 0.25f) aimDir = toP.normalized; // aim locks for the final 0.25s so the shot is dodgeable
            float k = 1f - Mathf.Clamp01(stateTimer / Def.telegraph);
            DrawTelegraph(k, stateTimer <= 0.25f);

            if (stateTimer <= 0f)
            {
                Fire();
                telegraphing = false;
                SetTelegraph(false);
                stateTimer = (Def.behavior == EnemyBehavior.Shaman ? Random.Range(2.0f, 3.2f) : Def.cooldown * Random.Range(0.9f, 1.1f)) * cooldownScale;
            }
        }

        void Fire()
        {
            switch (Def.attack)
            {
                case AttackPattern.Burst:
                    Shoot(aimDir);
                    burstLeft = Def.shots - 1;
                    burstTimer = Def.burstGap;
                    break;
                case AttackPattern.Fan:
                    for (int i = 0; i < Def.shots; i++) Shoot(Rotate(aimDir, FanAngle(i)));
                    break;
                case AttackPattern.Ring:
                    for (int i = 0; i < Def.shots; i++) Shoot(Rotate(aimDir, 360f * i / Def.shots)); // one spoke always points at the player
                    break;
                case AttackPattern.Homing:
                    Shoot(aimDir).Home(player, 110f, 1.4f);
                    break;
                default:
                    Shoot(aimDir);
                    break;
            }
        }

        Projectile Shoot(Vector2 dir)
        {
            return Projectile.Spawn((Vector2)transform.position + dir * (Def.size * 0.5f + 0.2f), dir, Def.projectileSpeed * projSpeedScale, Def.projectileColor, round.ProjectileParent, Def.projectileSize);
        }

        float FanAngle(int i) { return Def.shots > 1 ? -Def.spread * 0.5f + Def.spread * i / (Def.shots - 1) : 0f; }

        static Vector2 Rotate(Vector2 v, float deg) { return Quaternion.Euler(0f, 0f, deg) * v; }

        void SetTelegraph(bool on)
        {
            if (telegraphs == null) return;
            foreach (var t in telegraphs) t.gameObject.SetActive(on);
        }

        void DrawTelegraph(float k, bool locked)
        {
            Color c = locked ? Color.white : Def.projectileColor;
            c.a = 0.15f + 0.5f * k;
            if (Def.attack == AttackPattern.Ring)
            {
                // A ring that swells around the body: shots will fly out in every direction.
                var r = telegraphs[0].transform;
                r.localPosition = Vector3.zero;
                r.localRotation = Quaternion.identity;
                r.localScale = Vector3.one * Def.size * Mathf.Lerp(1.2f, 2.4f, k);
                telegraphs[0].color = c;
                return;
            }
            // Fans draw a shorter line per shot; seekers a short stub, since they won't fly straight anyway.
            float len = Def.attack == AttackPattern.Fan ? 5f : Def.attack == AttackPattern.Homing ? 3f : 7f;
            float width = (locked ? 0.16f : 0.08f) * (Def.attack == AttackPattern.Burst ? 1.5f : 1f);
            for (int i = 0; i < telegraphs.Length; i++)
            {
                Vector2 d = telegraphs.Length > 1 ? Rotate(aimDir, FanAngle(i)) : aimDir;
                var t = telegraphs[i].transform;
                t.localPosition = d * (Def.size * 0.5f + len * 0.5f);
                t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                t.localScale = new Vector3(len, width, 1f);
                telegraphs[i].color = c;
            }
        }

        void FixedUpdate()
        {
            if (Dead) return;
            if (spawnTimer > 0f || player == null || flatTimer > 0f) { rb.SetVel(knockVel); DecayKnock(); return; }

            Vector2 toP = player.position - transform.position;
            float dist = toP.magnitude;
            Vector2 dir = dist > 0.001f ? toP / dist : Vector2.zero;
            Vector2 desired = Vector2.zero;
            float spd = Def.speed * speedScale;

            switch (Def.behavior)
            {
                case EnemyBehavior.Chaser:
                    desired = dir * spd * (Attacking ? 0.35f : 1f); // slows while winding up or firing a volley
                    break;
                case EnemyBehavior.Archer:
                    if (!Attacking)
                    {
                        if (dist > Def.maxRange) desired = dir * spd;
                        else if (dist < Def.minRange) desired = -dir * spd;
                    }
                    break;
                case EnemyBehavior.Shaman:
                    {
                        float mul = Attacking ? 0.3f : 1f;
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

            if (Wounded && round.PlayerUpgrades.Has(UpgradeType.OpenWound)) desired *= 0.85f;
            rb.SetVel(desired + knockVel);
            DecayKnock();
        }

        void DecayKnock()
        {
            knockVel = Vector2.MoveTowards(knockVel, Vector2.zero, 30f * Time.fixedDeltaTime);
        }

        public void ApplyWound(float duration, float dps)
        {
            if (Dead || duration <= 0f) return;
            if (woundLeft <= 0f) woundTick = 0f;
            woundLeft = Mathf.Max(woundLeft, duration);
            woundDps = Mathf.Max(woundDps, dps);
        }

        public void TakeHit(float damage, Vector2 dir, float impactSpeed, float knockbackMul)
        {
            if (Dead) return;
            Fx.Burst(transform.position, Def.color, 6, 5f, 0.15f, 0.3f);
            flashTimer = HitFlashTime;
            Damage(damage, Color.white, true);
            if (Dead) return;
            // Knockback scales with impact speed (clamped); heavy enemies barely move.
            knockVel = dir * Mathf.Clamp(impactSpeed * 2f, 4f, 16f) * knockbackMul / Def.knockbackResistance;
        }

        /// <summary>Damage from card effects (pulses, explosions, trails). Optional outward push.</summary>
        public void TakeAreaDamage(float amount, Vector2 from, float push)
        {
            if (Dead || !Active) return;
            if (push > 0f)
            {
                Vector2 d = (Vector2)transform.position - from;
                knockVel = (d.sqrMagnitude > 0.0001f ? d.normalized : Random.insideUnitCircle.normalized) * push / Def.knockbackResistance;
                flashTimer = 0.08f;
            }
            if (push > 0f) { Damage(amount, AreaNumber, false); return; }
            // Lingering zones hurt every frame: bank it and show one number per NumberBatch instead of a stream.
            zoneDamage += amount;
            Damage(amount);
        }

        /// <summary>The ball rolled over us: flatten, take the hit, and splat if it was lethal. Returns true if it killed.</summary>
        public bool Crush(float damage, Vector2 dir)
        {
            if (Dead) return false;
            flatTimer = FlatDuration;
            crushDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
            flashTimer = HitFlashTime;
            if (Attacking)
            {
                telegraphing = false;
                burstLeft = 0; // a crush also cuts a volley short
                SetTelegraph(false);
                stateTimer = Def.telegraph; // getting crushed resets the wind-up
            }
            knockVel = crushDir * 1.5f / Def.knockbackResistance; // pressed into the floor, barely slides
            UpdateFlatVisual();
            Fx.Burst(transform.position, Def.color, 8, 6f, 0.14f, 0.3f);
            crushedToDeath = true;
            Damage(damage, Color.white, true);
            crushedToDeath = false;
            return Dead;
        }

        void UpdateFlatVisual()
        {
            // Flat pancake stretched across the roll direction, then a springy pop back in the last quarter.
            float ang = Mathf.Atan2(crushDir.y, crushDir.x) * Mathf.Rad2Deg + 90f;
            float k = Mathf.Clamp01(flatTimer / FlatDuration);
            Vector3 flat = new Vector3(1.5f, 0.3f, 1f);
            if (k > 0.25f)
            {
                float wobble = 1f + 0.06f * Mathf.Sin(Time.time * 40f) * (k - 0.25f);
                vis.localScale = new Vector3(flat.x * wobble, flat.y, 1f);
                vis.localRotation = Quaternion.Euler(0f, 0f, ang);
            }
            else
            {
                float t = 1f - k / 0.25f;
                float back = 1f + 2.2f * Mathf.Pow(t - 1f, 3f) + 1.2f * Mathf.Pow(t - 1f, 2f); // ease-out-back
                vis.localScale = new Vector3(Mathf.LerpUnclamped(flat.x, 1f, back), Mathf.LerpUnclamped(flat.y, 1f, back), 1f);
                vis.localRotation = Quaternion.Euler(0f, 0f, ang * (1f - t));
            }
            if (flatTimer <= 0f) { vis.localScale = Vector3.one; vis.localRotation = Quaternion.identity; }
        }

        static readonly Color WoundNumber = new Color(1f, 0.35f, 0.35f);
        static readonly Color AreaNumber = new Color(1f, 0.8f, 0.3f);
        const float NumberBatch = 0.4f;

        void FlushZoneNumber(float dt)
        {
            zoneTimer -= dt;
            if (zoneTimer > 0f || zoneDamage < 1f) return;
            DamageNumbers.Spawn(PopupPos(), zoneDamage, AreaNumber, false);
            zoneDamage = 0f;
            zoneTimer = NumberBatch;
        }

        Vector3 PopupPos() { return transform.position + Vector3.up * (Def.size * 0.5f + 0.45f); }

        void Damage(float amount, Color numberColor, bool big)
        {
            DamageNumbers.Spawn(PopupPos(), amount, numberColor, big);
            Damage(amount);
        }

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
            if (crushedToDeath) Splat.Create(transform.position, Def.color, Def.size, crushDir);
            else Fx.Burst(transform.position, Def.color, 12, 6f, 0.2f, 0.5f);
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
