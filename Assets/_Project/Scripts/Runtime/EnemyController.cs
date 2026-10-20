using UnityEngine;

namespace RollItOut
{
    public enum EnemyType { Skeleton, Goblin, Orc, Slime, SlimeSmall, Archer, Shaman }
    public enum EnemyBehavior { Chaser, Archer, Shaman }
    /// <summary>How a shot is fired, so each attack reads differently: one bolt, a quick volley, a cone, a ring, a seeker,
    /// a rotating spiral stream, a flat wall of parallel shots, or a ragged shotgun blast.</summary>
    public enum AttackPattern { Single, Burst, Fan, Ring, Homing, Spiral, Wall, Scatter }

    /// <summary>One attack an enemy can use. Each has its own wind-up and cooldown, so an enemy's rhythm changes with what it picks.</summary>
    public class AttackDef
    {
        public AttackPattern pattern;
        public ShotKind kind;
        public float telegraph, cooldown, speed, size = 1f;
        // shots = volley length (Burst/Spiral), cone/ring/wall count (Fan/Ring/Wall/Scatter)
        public int shots = 1;
        // spread = cone width in degrees (Fan/Scatter), degrees turned per step (Spiral), wall width in units (Wall)
        public float spread, burstGap;
    }

    public class EnemyDef
    {
        public EnemyType type;
        public string name;
        public EnemyBehavior behavior;
        public float hp, speed, size, knockbackResistance;
        public Color color;
        public int splitCount;
        public EnemyType splitType;
        // projectile attacks (every enemy has at least one, picked at random, never the same one twice in a row when it has several);
        // minRange/maxRange only used by ranged units
        public AttackDef[] attacks;
        public float minRange, maxRange;
        public Color projectileColor;
    }

    public static class EnemyDefs
    {
        static AttackDef A(AttackPattern pattern, ShotKind kind, float telegraph, float cooldown, float speed, int shots = 1, float spread = 0f, float burstGap = 0f, float size = 1f)
        {
            return new AttackDef { pattern = pattern, kind = kind, telegraph = telegraph, cooldown = cooldown, speed = speed, shots = shots, spread = spread, burstGap = burstGap, size = size };
        }

        public static EnemyDef Get(EnemyType t)
        {
            switch (t)
            {
                case EnemyType.Skeleton:
                    return new EnemyDef { type = t, name = "Skeleton", behavior = EnemyBehavior.Chaser, hp = 20, speed = 3.0f, size = 0.9f, knockbackResistance = 1f, color = new Color(0.88f, 0.87f, 0.8f), projectileColor = new Color(1f, 0.95f, 0.5f),
                        attacks = new[] {
                            A(AttackPattern.Single, ShotKind.Normal, 1.0f, 3.2f, 5f),
                            A(AttackPattern.Burst, ShotKind.Accel, 0.9f, 4.2f, 5f, shots: 2, burstGap: 0.3f),
                            A(AttackPattern.Single, ShotKind.Bounce, 1.1f, 3.8f, 5.5f) } };
                case EnemyType.Goblin:
                    return new EnemyDef { type = t, name = "Goblin", behavior = EnemyBehavior.Chaser, hp = 14, speed = 4.6f, size = 0.6f, knockbackResistance = 0.9f, color = new Color(0.45f, 0.85f, 0.3f), projectileColor = new Color(0.7f, 1f, 0.2f),
                        attacks = new[] {
                            A(AttackPattern.Burst, ShotKind.Normal, 0.8f, 3.0f, 6.5f, shots: 3, burstGap: 0.14f, size: 0.8f),
                            A(AttackPattern.Scatter, ShotKind.Normal, 0.7f, 3.8f, 6f, shots: 5, spread: 45f, size: 0.7f),
                            A(AttackPattern.Burst, ShotKind.Wave, 0.6f, 2.2f, 5.5f, shots: 2, burstGap: 0.2f, size: 0.8f) } };
                case EnemyType.Orc:
                    return new EnemyDef { type = t, name = "Orc", behavior = EnemyBehavior.Chaser, hp = 90, speed = 1.7f, size = 1.5f, knockbackResistance = 5f, color = new Color(0.35f, 0.5f, 0.2f), projectileColor = new Color(1f, 0.35f, 0.15f),
                        attacks = new[] {
                            A(AttackPattern.Fan, ShotKind.Normal, 1.3f, 4f, 4f, shots: 5, spread: 70f, size: 1.25f),
                            A(AttackPattern.Wall, ShotKind.Normal, 1.4f, 5f, 3.5f, shots: 5, spread: 3.2f, size: 1.1f),
                            A(AttackPattern.Single, ShotKind.Split, 1.2f, 5.5f, 3.5f, size: 1.7f) } };
                case EnemyType.Slime:
                    return new EnemyDef { type = t, name = "Slime", behavior = EnemyBehavior.Chaser, hp = 30, speed = 1.8f, size = 1.1f, knockbackResistance = 1f, color = new Color(0.25f, 0.85f, 0.8f), splitCount = 2, splitType = EnemyType.SlimeSmall, projectileColor = new Color(0.2f, 1f, 0.9f),
                        attacks = new[] {
                            A(AttackPattern.Ring, ShotKind.Normal, 1.0f, 3.5f, 4.5f, shots: 8),
                            A(AttackPattern.Ring, ShotKind.Wave, 1.1f, 4.5f, 3.8f, shots: 6),
                            A(AttackPattern.Single, ShotKind.Split, 0.9f, 4f, 4f, size: 1.3f) } };
                case EnemyType.SlimeSmall:
                    return new EnemyDef { type = t, name = "Small Slime", behavior = EnemyBehavior.Chaser, hp = 12, speed = 2.5f, size = 0.6f, knockbackResistance = 1f, color = new Color(0.4f, 0.95f, 0.9f), projectileColor = new Color(0.2f, 1f, 0.9f),
                        attacks = new[] {
                            A(AttackPattern.Single, ShotKind.Normal, 0.9f, 4.5f, 4.5f),
                            A(AttackPattern.Single, ShotKind.Wave, 0.9f, 5.5f, 4f) } };
                case EnemyType.Archer:
                    return new EnemyDef { type = t, name = "Skeleton Archer", behavior = EnemyBehavior.Archer, hp = 20, speed = 2.0f, size = 0.9f, knockbackResistance = 1f, color = new Color(0.95f, 0.85f, 0.65f), minRange = 5f, maxRange = 9f, projectileColor = new Color(1f, 0.55f, 0.1f),
                        attacks = new[] {
                            A(AttackPattern.Fan, ShotKind.Normal, 0.9f, 2.6f, 7f, shots: 3, spread: 18f),
                            A(AttackPattern.Single, ShotKind.Accel, 1.3f, 3.4f, 7.5f, size: 0.9f),
                            A(AttackPattern.Fan, ShotKind.Bounce, 1.0f, 3.2f, 6f, shots: 2, spread: 40f) } };
                default:
                    return new EnemyDef { type = EnemyType.Shaman, name = "Goblin Shaman", behavior = EnemyBehavior.Shaman, hp = 14, speed = 2.6f, size = 0.7f, knockbackResistance = 1f, color = new Color(0.7f, 0.4f, 0.95f), minRange = 6f, maxRange = 10f, projectileColor = new Color(1f, 0.25f, 0.85f),
                        attacks = new[] {
                            A(AttackPattern.Homing, ShotKind.Normal, 0.8f, 2.6f, 4.5f, size: 1.3f),
                            A(AttackPattern.Spiral, ShotKind.Normal, 1.0f, 4.5f, 3.8f, shots: 10, spread: 30f, burstGap: 0.1f, size: 0.8f),
                            A(AttackPattern.Fan, ShotKind.Boomerang, 0.9f, 3.4f, 6f, shots: 3, spread: 50f, size: 0.9f) } };
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
        SpriteRenderer[] telegraphs;   // aim lines: one per shot of a Fan/Wall, cone edges for Scatter, else a single line
        SpriteRenderer ringTelegraph;  // swelling ring for Ring and Spiral
        float hp, maxHp, spawnTimer, spawnTotal, flashTimer, woundLeft, woundTick, woundDps, zoneDamage, zoneTimer, bloodTimer, flatTimer;
        bool crushedToDeath;
        readonly int[] zoneFrame = { -1, -1, -1, -1 };
        Vector2 crushDir = Vector2.right;
        Vector2 knockVel;
        bool telegraphing;
        int burstLeft, burstIndex;
        float burstTimer;
        AttackDef atk;     // the attack being wound up or fired, picked fresh before each wind-up
        int lastAttack = -1;
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

            if (def.attacks != null && def.attacks.Length > 0)
            {
                int n = 1;
                foreach (var a in def.attacks) n = Mathf.Max(n, LineCount(a));
                telegraphs = new SpriteRenderer[n];
                for (int i = 0; i < n; i++)
                {
                    telegraphs[i] = SpriteLib.Add(transform, "Telegraph", SpriteLib.Square, def.projectileColor, 3, Vector3.zero, Vector3.one);
                    telegraphs[i].gameObject.SetActive(false);
                }
                ringTelegraph = SpriteLib.Add(transform, "TelegraphRing", SpriteLib.Ring, def.projectileColor, 3, Vector3.zero, Vector3.one);
                ringTelegraph.gameObject.SetActive(false);
                PickAttack();
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
                    DamageZone.Spawn(round, ZoneKind.Blood, transform.position, 0.45f, 8f, 1.5f, new Color(0.7f, 0.05f, 0.1f, 0.45f), this);
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

            if (atk != null && player != null) UpdateRanged(dt);
        }

        void UpdateRanged(float dt)
        {
            Vector2 toP = player.position - transform.position;
            float dist = toP.magnitude;
            stateTimer -= dt;

            if (burstLeft > 0)
            {
                burstTimer -= dt;
                if (burstTimer <= 0f) { BurstStep(); burstLeft--; burstTimer = atk.burstGap; }
                return;
            }

            if (!telegraphing)
            {
                if (stateTimer <= 0f && dist < 14f)
                {
                    PickAttack();
                    telegraphing = true;
                    stateTimer = atk.telegraph;
                    aimDir = toP.normalized;
                    SetTelegraph(true);
                }
                return;
            }

            if (stateTimer > 0.25f) aimDir = toP.normalized; // aim locks for the final 0.25s so the shot is dodgeable
            float k = 1f - Mathf.Clamp01(stateTimer / atk.telegraph);
            DrawTelegraph(k, stateTimer <= 0.25f);

            if (stateTimer <= 0f)
            {
                Fire();
                telegraphing = false;
                SetTelegraph(false);
                // The cooldown belongs to the attack just used, so a heavy attack buys a longer breather than a quick one.
                stateTimer = atk.cooldown * Random.Range(0.85f, 1.15f) * cooldownScale;
            }
        }

        /// <summary>Choose the next attack at random, never repeating the last one when there is a choice.</summary>
        void PickAttack()
        {
            var list = Def.attacks;
            int i = Random.Range(0, list.Length);
            if (list.Length > 1 && i == lastAttack) i = (i + 1 + Random.Range(0, list.Length - 1)) % list.Length;
            lastAttack = i;
            atk = list[i];
        }

        static int LineCount(AttackDef a)
        {
            switch (a.pattern)
            {
                case AttackPattern.Fan: case AttackPattern.Wall: return a.shots;
                case AttackPattern.Scatter: return 2;
                default: return 1;
            }
        }

        void Fire()
        {
            switch (atk.pattern)
            {
                case AttackPattern.Burst:
                case AttackPattern.Spiral:
                    burstIndex = 0;
                    BurstStep();
                    burstLeft = atk.shots - 1;
                    burstTimer = atk.burstGap;
                    break;
                case AttackPattern.Fan:
                    for (int i = 0; i < atk.shots; i++) Shoot(Rotate(aimDir, FanAngle(i)));
                    break;
                case AttackPattern.Ring:
                    for (int i = 0; i < atk.shots; i++) Shoot(Rotate(aimDir, 360f * i / atk.shots)); // one spoke always points at the player
                    break;
                case AttackPattern.Homing:
                    Shoot(aimDir).Home(player, 110f, 1.4f);
                    break;
                case AttackPattern.Wall:
                    for (int i = 0; i < atk.shots; i++) Shoot(aimDir, WallOffset(i)); // parallel shots: dodge through the ends, not the middle
                    break;
                case AttackPattern.Scatter:
                    // Shotgun: random angles and speeds inside the cone, so no two blasts look alike.
                    for (int i = 0; i < atk.shots; i++) Shoot(Rotate(aimDir, Random.Range(-0.5f, 0.5f) * atk.spread), 0f, Random.Range(0.75f, 1.15f));
                    break;
                default:
                    Shoot(aimDir);
                    break;
            }
        }

        /// <summary>One shot of a volley: Burst repeats the locked aim, Spiral fires two opposite arms that turn each step.</summary>
        void BurstStep()
        {
            if (atk.pattern == AttackPattern.Spiral)
            {
                Vector2 d = Rotate(aimDir, atk.spread * burstIndex * strafeSign);
                Shoot(d); Shoot(-d);
            }
            else Shoot(aimDir);
            burstIndex++;
        }

        Projectile Shoot(Vector2 dir, float sideOffset = 0f, float speedMul = 1f)
        {
            Vector2 side = new Vector2(-dir.y, dir.x) * sideOffset;
            return Projectile.Spawn((Vector2)transform.position + dir * (Def.size * 0.5f + 0.2f) + side, dir, atk.speed * speedMul * projSpeedScale, Def.projectileColor, round.ProjectileParent, atk.size, atk.kind);
        }

        float FanAngle(int i) { return atk.shots > 1 ? -atk.spread * 0.5f + atk.spread * i / (atk.shots - 1) : 0f; }
        float WallOffset(int i) { return atk.shots > 1 ? -atk.spread * 0.5f + atk.spread * i / (atk.shots - 1) : 0f; }

        static Vector2 Rotate(Vector2 v, float deg) { return Quaternion.Euler(0f, 0f, deg) * v; }

        void SetTelegraph(bool on)
        {
            if (telegraphs == null) return;
            bool ring = atk.pattern == AttackPattern.Ring || atk.pattern == AttackPattern.Spiral;
            int lines = ring ? 0 : LineCount(atk);
            for (int i = 0; i < telegraphs.Length; i++) telegraphs[i].gameObject.SetActive(on && i < lines);
            ringTelegraph.gameObject.SetActive(on && ring);
        }

        void DrawTelegraph(float k, bool locked)
        {
            Color c = locked ? Color.white : Def.projectileColor;
            c.a = 0.15f + 0.5f * k;
            if (atk.pattern == AttackPattern.Ring || atk.pattern == AttackPattern.Spiral)
            {
                // A ring that swells around the body: shots will fly out in every direction (a Spiral's ring also spins).
                var r = ringTelegraph.transform;
                r.localPosition = Vector3.zero;
                r.localRotation = Quaternion.Euler(0f, 0f, atk.pattern == AttackPattern.Spiral ? k * 360f * strafeSign : 0f);
                r.localScale = Vector3.one * Def.size * Mathf.Lerp(1.2f, 2.4f, k) * (atk.pattern == AttackPattern.Spiral ? 0.85f : 1f);
                ringTelegraph.color = c;
                return;
            }
            // Fans draw a shorter line per shot; seekers and boomerangs a short stub; a shotgun only its two cone edges.
            float len = atk.pattern == AttackPattern.Fan || atk.pattern == AttackPattern.Scatter ? 5f : atk.pattern == AttackPattern.Homing || atk.kind == ShotKind.Boomerang ? 3f : 7f;
            float width = (locked ? 0.16f : 0.08f) * (atk.pattern == AttackPattern.Burst ? 1.5f : 1f) * (atk.kind == ShotKind.Normal ? 1f : 1.3f);
            int lines = LineCount(atk);
            for (int i = 0; i < lines; i++)
            {
                Vector2 d = aimDir;
                float side = 0f;
                if (atk.pattern == AttackPattern.Fan) d = Rotate(aimDir, FanAngle(i));
                else if (atk.pattern == AttackPattern.Scatter) d = Rotate(aimDir, (i == 0 ? -0.5f : 0.5f) * atk.spread);
                else if (atk.pattern == AttackPattern.Wall) side = WallOffset(i);
                var t = telegraphs[i].transform;
                t.localPosition = d * (Def.size * 0.5f + len * 0.5f) + new Vector2(-d.y, d.x) * side;
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

            if (Wounded && round.PlayerUpgrades.Has(UpgradeType.OpenWound)) desired *= 0.8f;
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

        /// <summary>
        /// Overlapping zones of one kind (a trail is many zones) hurt like a single zone: only the first to
        /// reach us each frame deals damage.
        /// </summary>
        public bool ClaimZoneTick(ZoneKind kind)
        {
            int f = Time.frameCount;
            if (zoneFrame[(int)kind] == f) return false;
            zoneFrame[(int)kind] = f;
            return true;
        }

        /// <summary>The ball rolled over us: flatten, take the hit, and splat if it was lethal. Returns true if it killed.</summary>
        public bool Crush(float damage, Vector2 dir, float flatTime)
        {
            if (Dead) return false;
            flatTimer = flatTime;
            crushDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
            flashTimer = HitFlashTime;
            if (Attacking)
            {
                telegraphing = false;
                burstLeft = 0; // a crush also cuts a volley short
                SetTelegraph(false);
                stateTimer = atk.telegraph; // getting crushed resets the wind-up
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
