using UnityEngine;

namespace RollItOut
{
    /// <summary>Momentum-based arcade movement: accelerate toward input, ease to rest without it.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerBallController : MonoBehaviour
    {
        public const float Acceleration = 18f;
        public const float Deceleration = 12f;

        public Transform roller;       // rotates to show rolling
        public GameObject stoneShield; // visible while Stone Skin charges remain
        public bool controlEnabled = true;

        Rigidbody2D rb;
        PlayerUpgradeState up;
        Vector2 input;
        float dustTimer;

        /// <summary>Velocity we set before the last physics step; the true speed at impact.</summary>
        public Vector2 PreStepVelocity { get; private set; }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            up = GetComponent<PlayerUpgradeState>();
        }

        void Update()
        {
            input = controlEnabled ? InputReader.Move() : Vector2.zero;
            transform.localScale = Vector3.one * up.SizeScale;
            if (stoneShield != null) stoneShield.SetActive(up.stoneSkinCharges > 0);

            Vector2 v = rb.GetVel();
            float speed = v.magnitude;
            if (roller != null && speed > 0.05f)
            {
                float dir = v.x != 0f ? -Mathf.Sign(v.x) : -Mathf.Sign(v.y);
                roller.Rotate(0f, 0f, dir * speed * Mathf.Rad2Deg * Time.deltaTime / 0.5f);
            }

            // Dust only on fast movement, kept small and low alpha so projectiles stay readable.
            dustTimer -= Time.deltaTime;
            if (speed > 4.5f && dustTimer <= 0f)
            {
                dustTimer = 0.07f;
                Spark.Create((Vector2)transform.position - v.normalized * 0.4f * up.SizeScale, -v * 0.1f, new Color(0.8f, 0.78f, 0.7f, 0.35f), 0.2f, 0.35f);
            }
        }

        void FixedUpdate()
        {
            Vector2 v = rb.GetVel();
            float dt = Time.fixedDeltaTime;
            if (input.sqrMagnitude > 0.01f)
            {
                // Faster correction when reversing so turning stays responsive, not slippery.
                float align = v.sqrMagnitude > 0.04f ? Vector2.Dot(v.normalized, input.normalized) : 1f;
                float mul = Mathf.Lerp(2f, 1f, (align + 1f) * 0.5f);
                v = Vector2.MoveTowards(v, input * up.MaxSpeed, Acceleration * mul * dt);
            }
            else
            {
                v = Vector2.MoveTowards(v, Vector2.zero, Deceleration * dt);
            }
            rb.SetVel(v);
            PreStepVelocity = v;
        }

        public void SetVelocity(Vector2 v)
        {
            rb.SetVel(v);
            PreStepVelocity = v;
        }
    }
}
