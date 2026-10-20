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
        float squashTimer;
        Transform[] visuals;
        Vector3[] visualScales;
        const float SquashTime = 0.18f;

        /// <summary>Velocity we set before the last physics step; the true speed at impact.</summary>
        public Vector2 PreStepVelocity { get; private set; }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            up = GetComponent<PlayerUpgradeState>();
        }

        void Start()
        {
            // Sprite children only (not the collider's transform), so the squash never changes physics.
            var list = new System.Collections.Generic.List<Transform>();
            foreach (Transform t in transform) list.Add(t);
            visuals = list.ToArray();
            visualScales = new Vector3[visuals.Length];
            for (int i = 0; i < visuals.Length; i++) visualScales[i] = visuals[i].localScale;
        }

        /// <summary>Brief bulge of the ball's sprites when it lands on something it crushed.</summary>
        public void Squash() { squashTimer = SquashTime; }

        void Update()
        {
            input = controlEnabled ? InputReader.Move() : Vector2.zero;
            transform.localScale = Vector3.one * up.SizeScale;
            if (stoneShield != null) stoneShield.SetActive(up.stoneSkinCharges > 0);

            if (squashTimer > 0f && visuals != null)
            {
                squashTimer -= Time.unscaledDeltaTime; // keeps animating through hit-stop
                float k = Mathf.Clamp01(squashTimer / SquashTime);
                float bulge = 1f + 0.22f * Mathf.Sin(k * Mathf.PI);
                for (int i = 0; i < visuals.Length; i++) visuals[i].localScale = visualScales[i] * bulge;
            }

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
