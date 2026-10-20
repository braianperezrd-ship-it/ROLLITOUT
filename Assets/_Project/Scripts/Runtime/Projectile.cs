using UnityEngine;

namespace RollItOut
{
    /// <summary>Enemy shot, straight or briefly homing. The only direct source of player damage.</summary>
    public class Projectile : MonoBehaviour
    {
        public const float Damage = 1f;
        /// <summary>Reflect Dash: damage a sent-back shot deals to the enemy it hits, and its speed boost.</summary>
        public const float ReflectedDamage = 15f, ReflectSpeedMultiplier = 1.4f;

        Rigidbody2D rb;
        Vector2 dir;
        float speed, life = 7f;
        Transform target;
        float turnRate, homingLeft;
        bool reflected; // sent back by Reflect Dash: hurts enemies, ignores the player

        public static Projectile Spawn(Vector2 pos, Vector2 dir, float speed, Color color, Transform parent, float size = 1f)
        {
            var go = new GameObject("Projectile");
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = pos;
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            go.transform.rotation = Quaternion.Euler(0f, 0f, ang);

            var p = go.AddComponent<Projectile>();
            p.dir = dir.normalized; p.speed = speed;
            p.rb = go.AddComponent<Rigidbody2D>();
            p.rb.bodyType = RigidbodyType2D.Kinematic;
            p.rb.gravityScale = 0f;
            var c = go.AddComponent<CircleCollider2D>();
            c.radius = 0.2f * size;
            c.isTrigger = true;

            // High-contrast: dark outline, soft glow, bright core, elongated along travel direction.
            SpriteLib.Add(go.transform, "Glow", SpriteLib.Circle, new Color(color.r, color.g, color.b, 0.3f), 12, Vector3.zero, Vector3.one * 0.85f * size);
            SpriteLib.Add(go.transform, "Outline", SpriteLib.Diamond, new Color(0.05f, 0.03f, 0.08f), 13, Vector3.zero, new Vector3(0.82f, 0.5f, 1f) * size);
            SpriteLib.Add(go.transform, "Core", SpriteLib.Diamond, color, 14, Vector3.zero, new Vector3(0.68f, 0.38f, 1f) * size);
            SpriteLib.Add(go.transform, "Hot", SpriteLib.Diamond, Color.white, 15, new Vector3(0.05f, 0f, 0f) * size, new Vector3(0.32f, 0.16f, 1f) * size);
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
            rb.SetVel(dir * speed);
            life -= Time.fixedDeltaTime;
            if (life <= 0f) Destroy(gameObject);
        }

        /// <summary>Reflect Dash: fly back the way it came, faster, no longer homing.</summary>
        void Reflect()
        {
            reflected = true;
            dir = -dir;
            speed *= ReflectSpeedMultiplier;
            homingLeft = 0f;
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
            if (o.GetComponent<ArenaWall>() != null) Destroy(gameObject);
        }
    }

    /// <summary>Marker for the arena boundary colliders.</summary>
    public class ArenaWall : MonoBehaviour { }
}
