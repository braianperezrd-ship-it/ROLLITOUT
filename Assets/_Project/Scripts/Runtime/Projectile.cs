using UnityEngine;

namespace RollItOut
{
    /// <summary>How a shot flies once fired. Each kind has its own core shape so it reads at a glance.</summary>
    public enum ShotKind
    {
        Normal,     // straight line (diamond)
        Accel,      // starts slow, then speeds past normal (long thin needle)
        Wave,       // weaves side to side along its path (round orb)
        Split,      // slow blob that pops into a small ring after a moment (orb with a dark shell)
        Boomerang,  // brakes, then flies back the way it came (spinning ring)
        Bounce      // ricochets off the arena walls a couple of times (square)
    }

    /// <summary>Enemy shot, straight or briefly homing. The only direct source of player damage.</summary>
    public class Projectile : MonoBehaviour
    {
        public const float Damage = 1f;
        /// <summary>Reflect Dash: damage a sent-back shot deals to the enemy it hits, and its speed boost.</summary>
        public const float ReflectedDamage = 15f, ReflectSpeedMultiplier = 1.4f;
        /// <summary>Seconds an enemy shot lives after being fired (halved from 7s so the crowded screen clears faster).</summary>
        public const float Lifetime = 3.5f;
        // Shot kind tuning.
        const float AccelStart = 0.35f, AccelEnd = 1.7f, AccelTime = 1.1f;
        const float WaveAmp = 0.7f, WaveFreq = 7f;
        const float SplitDelay = 0.75f; const int SplitShots = 6;
        const float BoomerangTurn = 0.85f; // seconds until it stops and starts coming back
        const int Bounces = 2;

        Rigidbody2D rb;
        Vector2 dir;
        float speed, life = Lifetime;
        Transform target;
        float turnRate, homingLeft;
        bool reflected; // sent back by Reflect Dash: hurts enemies, ignores the player
        ShotKind kind;
        float age, baseSpeed, size, wallCooldown;
        int bouncesLeft;
        Color color;
        Transform spin; // core sprite turned by Boomerang / pulsed by Split

        public static Projectile Spawn(Vector2 pos, Vector2 dir, float speed, Color color, Transform parent, float size = 1f, ShotKind kind = ShotKind.Normal)
        {
            var go = new GameObject("Projectile");
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = pos;
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            go.transform.rotation = Quaternion.Euler(0f, 0f, ang);

            var p = go.AddComponent<Projectile>();
            p.dir = dir.normalized; p.speed = p.baseSpeed = speed;
            p.kind = kind; p.size = size; p.color = color; p.bouncesLeft = Bounces;
            p.rb = go.AddComponent<Rigidbody2D>();
            p.rb.bodyType = RigidbodyType2D.Kinematic;
            p.rb.gravityScale = 0f;
            var c = go.AddComponent<CircleCollider2D>();
            c.radius = 0.2f * size;
            c.isTrigger = true;

            // High-contrast: dark outline, soft glow, bright core, elongated along travel direction.
            // The core's shape tells the player how the shot will move.
            Sprite shape = SpriteLib.Diamond;
            Vector3 core = new Vector3(0.68f, 0.38f, 1f);
            switch (kind)
            {
                case ShotKind.Accel: core = new Vector3(0.95f, 0.24f, 1f); break;
                case ShotKind.Wave: shape = SpriteLib.Circle; core = new Vector3(0.5f, 0.5f, 1f); break;
                case ShotKind.Split: shape = SpriteLib.Circle; core = new Vector3(0.62f, 0.62f, 1f); break;
                case ShotKind.Boomerang: shape = SpriteLib.Ring; core = new Vector3(0.72f, 0.42f, 1f); break; // oval, so the spin shows
                case ShotKind.Bounce: shape = SpriteLib.Square; core = new Vector3(0.44f, 0.44f, 1f); break;
            }
            SpriteLib.Add(go.transform, "Glow", SpriteLib.Circle, new Color(color.r, color.g, color.b, 0.3f), 12, Vector3.zero, Vector3.one * 0.85f * size);
            SpriteLib.Add(go.transform, "Outline", shape, new Color(0.05f, 0.03f, 0.08f), 13, Vector3.zero, (core + new Vector3(0.14f, 0.12f, 0f)) * size);
            var coreSr = SpriteLib.Add(go.transform, "Core", shape, color, 14, Vector3.zero, core * size);
            if (kind == ShotKind.Split) SpriteLib.Add(coreSr.transform, "Shell", SpriteLib.Ring, new Color(0.05f, 0.03f, 0.08f), 15, Vector3.zero, Vector3.one * 0.8f);
            if (kind == ShotKind.Boomerang || kind == ShotKind.Split) p.spin = coreSr.transform;
            else SpriteLib.Add(go.transform, "Hot", shape, Color.white, 15, new Vector3(0.05f, 0f, 0f) * size, Vector3.Scale(core, new Vector3(0.47f, 0.42f, 1f)) * size);
            return p;
        }

        /// <summary>Steer toward the target at turnRate deg/s for a limited time, then fly straight so it can be outrun.</summary>
        public void Home(Transform t, float turnRate, float duration)
        {
            target = t; this.turnRate = turnRate; homingLeft = duration;
        }

        void FixedUpdate()
        {
            if (homingLeft > 0f && target != null)
            {
                homingLeft -= Time.fixedDeltaTime;
                Vector2 to = (Vector2)target.position - rb.position;
                if (to.sqrMagnitude > 0.0001f)
                {
                    float step = turnRate * Time.fixedDeltaTime;
                    float delta = Mathf.Clamp(Vector2.SignedAngle(dir, to), -step, step);
                    dir = (Vector2)(Quaternion.Euler(0f, 0f, delta) * dir);
                    transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                }
            }
            float dt = Time.fixedDeltaTime;
            age += dt;
            wallCooldown -= dt;
            Vector2 vel = dir * speed;
            switch (kind)
            {
                case ShotKind.Accel:
                    speed = baseSpeed * Mathf.Lerp(AccelStart, AccelEnd, age / AccelTime);
                    vel = dir * speed;
                    break;
                case ShotKind.Wave:
                    vel += new Vector2(-dir.y, dir.x) * (WaveAmp * WaveFreq * Mathf.Cos(age * WaveFreq));
                    break;
                case ShotKind.Boomerang:
                    // Linear brake through zero, then back out at full speed: it returns along its own line.
                    vel = dir * baseSpeed * Mathf.Max(-1f, 1f - age / BoomerangTurn);
                    spin.localRotation = Quaternion.Euler(0f, 0f, age * 720f);
                    break;
                case ShotKind.Split:
                    spin.localScale = Vector3.one * (1f + 0.25f * Mathf.Clamp01(age / SplitDelay) * Mathf.Abs(Mathf.Sin(age * 18f))) * 0.62f * size;
                    if (age >= SplitDelay) { Pop(); return; }
                    break;
            }
            if (kind == ShotKind.Wave)
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg);
            rb.SetVel(vel);
            life -= dt;
            if (life <= 0f) Destroy(gameObject);
        }

        /// <summary>Split: burst into a small ring of plain shots, one spoke still aimed along the original path.</summary>
        void Pop()
        {
            for (int i = 0; i < SplitShots; i++)
                Spawn(rb.position, Quaternion.Euler(0f, 0f, 360f * i / SplitShots) * dir, baseSpeed * 1.4f, color, transform.parent, 0.65f);
            Fx.Burst(rb.position, color, 6, 3f, 0.12f, 0.25f);
            Destroy(gameObject);
        }

        /// <summary>Reflect Dash: fly back the way it came, faster, no longer homing.</summary>
        void Reflect()
        {
            reflected = true;
            // Turn around relative to how it is moving now (a returning boomerang already flies along -dir).
            if (!(kind == ShotKind.Boomerang && age > BoomerangTurn)) dir = -dir;
            speed *= ReflectSpeedMultiplier;
            homingLeft = 0f;
            if (kind == ShotKind.Boomerang || kind == ShotKind.Accel) speed = baseSpeed * ReflectSpeedMultiplier;
            kind = ShotKind.Normal; // a sent-back shot just flies straight
            if (spin != null) spin.localRotation = Quaternion.identity;
            life = 3f;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
                if (sr.name == "Core" || sr.name == "Glow") sr.color = new Color(0.6f, 0.9f, 1f, sr.name == "Glow" ? 0.3f : 1f);
        }

        void OnTriggerEnter2D(Collider2D o) { Resolve(o); }
        void OnTriggerStay2D(Collider2D o) { Resolve(o); }

        void Resolve(Collider2D o)
        {
            if (reflected)
            {
                var e = o.GetComponentInParent<EnemyController>();
                if (e != null && !e.Dead && e.Active)
                {
                    e.TakeAreaDamage(ReflectedDamage, transform.position, 4f);
                    Fx.Burst(transform.position, new Color(0.6f, 0.9f, 1f), 8, 5f, 0.14f, 0.3f);
                    Destroy(gameObject);
                    return;
                }
            }
            var hp = o.GetComponent<PlayerHealth>();
            if (hp != null)
            {
                if (reflected) return;
                // Ignored hits (i-frames) pass through; reflected ones turn around; blocked or damaging hits destroy it.
                var result = hp.TakeProjectileHit(Damage, transform.position);
                if (result == HitResult.Reflected) Reflect();
                else if (result != HitResult.Ignored) Destroy(gameObject);
                return;
            }
            if (o.GetComponent<ArenaWall>() != null)
            {
                if (kind != ShotKind.Bounce || reflected) { Destroy(gameObject); return; }
                if (wallCooldown > 0f) return;
                // Ricochet: mirror off the wall face nearest the shot, only if it was heading into it.
                Vector2 n = rb.position - o.ClosestPoint(rb.position);
                if (n.sqrMagnitude < 0.0001f) n = -dir;
                n.Normalize();
                if (Vector2.Dot(dir, n) >= 0f) return;
                if (bouncesLeft-- <= 0) { Destroy(gameObject); return; }
                dir = Vector2.Reflect(dir, n);
                wallCooldown = 0.1f;
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                Fx.Burst(rb.position, color, 4, 3f, 0.1f, 0.2f);
            }
        }
    }

    /// <summary>Marker for the arena boundary colliders.</summary>
    public class ArenaWall : MonoBehaviour { }
}
