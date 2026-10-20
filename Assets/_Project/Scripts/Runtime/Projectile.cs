using UnityEngine;

namespace RollItOut
{
    /// <summary>Straight-flying enemy shot. The only direct source of player damage.</summary>
    public class Projectile : MonoBehaviour
    {
        public const float Damage = 1f;

        Rigidbody2D rb;
        Vector2 dir;
        float speed, life = 7f;

        public static Projectile Spawn(Vector2 pos, Vector2 dir, float speed, Color color, Transform parent)
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
            c.radius = 0.2f;
            c.isTrigger = true;

            // High-contrast: dark outline, soft glow, bright core, elongated along travel direction.
            SpriteLib.Add(go.transform, "Glow", SpriteLib.Circle, new Color(color.r, color.g, color.b, 0.3f), 12, Vector3.zero, Vector3.one * 0.85f);
            SpriteLib.Add(go.transform, "Outline", SpriteLib.Diamond, new Color(0.05f, 0.03f, 0.08f), 13, Vector3.zero, new Vector3(0.82f, 0.5f, 1f));
            SpriteLib.Add(go.transform, "Core", SpriteLib.Diamond, color, 14, Vector3.zero, new Vector3(0.68f, 0.38f, 1f));
            SpriteLib.Add(go.transform, "Hot", SpriteLib.Diamond, Color.white, 15, new Vector3(0.05f, 0f, 0f), new Vector3(0.32f, 0.16f, 1f));
            return p;
        }

        void FixedUpdate()
        {
            rb.SetVel(dir * speed);
            life -= Time.fixedDeltaTime;
            if (life <= 0f) Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D o) { Resolve(o); }
        void OnTriggerStay2D(Collider2D o) { Resolve(o); }

        void Resolve(Collider2D o)
        {
            var hp = o.GetComponent<PlayerHealth>();
            if (hp != null)
            {
                // Ignored hits (i-frames) pass through; blocked or damaging hits destroy the projectile.
                if (hp.TakeProjectileHit(Damage, transform.position) != HitResult.Ignored) Destroy(gameObject);
                return;
            }
            if (o.GetComponent<ArenaWall>() != null) Destroy(gameObject);
        }
    }

    /// <summary>Marker for the arena boundary colliders.</summary>
    public class ArenaWall : MonoBehaviour { }
}
