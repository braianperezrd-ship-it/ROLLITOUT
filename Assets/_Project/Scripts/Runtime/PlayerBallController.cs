using UnityEngine;

namespace RollItOut
{
    /// <summary>Momentum-based arcade movement: accelerate toward input, ease to rest without it.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerBallController : MonoBehaviour
    {
        public const float Acceleration = 18f;
        public const float Deceleration = 12f;
        public const float DashCooldown = 4f;
        public const float DashDuration = 0.15f;
        public const float DashSpeedMultiplier = 2.6f; // of current max speed

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
        bool dashQueued;
        Vector2 dashDir;
        float dashTimer;
        int dashCharges = 1;
        float rechargeAt;          // Time.time when the next stored dash comes back
        float lastDashTime = -99f;
        SpriteRenderer[] sprites;

        /// <summary>Stored dashes plus progress toward the next one, as a fraction of the max (0..1).</summary>
        public float DashCharge
        {
            get
            {
                int max = up.MaxDashCharges;
                float next = dashCharges >= max ? 0f : 1f - Mathf.Clamp01((rechargeAt - Time.time) / up.DashCooldown);
                return Mathf.Clamp01((dashCharges + next) / max);
            }
        }
        public bool DashReady => dashCharges > 0;
        public bool Dashing => dashTimer > 0f;
        /// <summary>True during a dash and for a short grace window after it, while the ball still carries dash speed.</summary>
        public bool RecentlyDashed => Time.time < lastDashTime + DashDuration + DashGrace;
        const float DashGrace = 0.2f;
        /// <summary>Phase Dash and Reflect Dash protect for this long after a dash starts.</summary>
        public const float DashProtectionTime = 1f;
        public bool InDashMotion => Time.time < lastDashTime + DashProtectionTime;
        const float DashTrailSpeed = 1.15f;

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
            sprites = GetComponentsInChildren<SpriteRenderer>(true);
        }

        /// <summary>Brief bulge of the ball's sprites when it lands on something it crushed.</summary>
        public void Squash() { squashTimer = SquashTime; }

        void Update()
        {
            input = controlEnabled ? InputReader.Move() : Vector2.zero;
            RechargeDash();
            // timeScale 0 means the cards / game-over screen is up; Space there selects, it must not queue a dash.
            if (controlEnabled && Time.timeScale > 0f && !PauseMenu.BlocksInput && DashReady && InputReader.DashPressed()) dashQueued = true;
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
            if (dashQueued)
            {
                dashQueued = false;
                // Dash toward the stick; with no input, keep rolling the way we already go.
                Vector2 dir = input.sqrMagnitude > 0.01f ? input.normalized : v.sqrMagnitude > 0.04f ? v.normalized : Vector2.zero;
                if (dir != Vector2.zero)
                {
                    dashDir = dir;
                    dashTimer = DashDuration;
                    lastDashTime = Time.time;
                    // The recharge clock starts when the first stored dash is spent.
                    if (dashCharges >= up.MaxDashCharges) rechargeAt = Time.time + up.DashCooldown;
                    dashCharges--;
                    up.impactArmed = true;
                    Fx.Burst(transform.position, new Color(0.8f, 0.9f, 1f), 8, 5f, 0.15f, 0.3f);
                }
            }
            if (dashTimer > 0f)
            {
                // Hold full dash speed for the burst; normal acceleration eases it back to max afterwards.
                dashTimer -= dt;
                v = dashDir * up.MaxSpeed * DashSpeedMultiplier;
                Spark.Create(transform.position, -v * 0.05f, new Color(0.8f, 0.9f, 1f, 0.5f), 0.3f * up.SizeScale, 0.25f);
            }
            else if (input.sqrMagnitude > 0.01f)
            {
                // Faster correction when reversing so turning stays responsive, not slippery.
                float align = v.sqrMagnitude > 0.04f ? Vector2.Dot(v.normalized, input.normalized) : 1f;
                float mul = Mathf.Lerp(2f * up.TurnBoost, 1f, (align + 1f) * 0.5f);
                v = Vector2.MoveTowards(v, input * up.MaxSpeed, up.Acceleration * mul * dt);
                if (v.magnitude >= up.MaxSpeed * 0.97f) up.impactArmed = true;
            }
            else
            {
                v = Vector2.MoveTowards(v, Vector2.zero, up.Deceleration * dt);
            }
            rb.SetVel(v);
            PreStepVelocity = v;

            // Motion blur: afterimages while dashing and while still bleeding off the extra speed.
            if (sprites != null && (dashTimer > 0f || v.magnitude > up.MaxSpeed * DashTrailSpeed))
                Ghost.Create(sprites, new Color(0.6f, 0.85f, 1f), dashTimer > 0f ? 0.5f : 0.3f, 0.18f);
        }

        /// <summary>Brings stored dashes back one at a time (Double Dash stores two).</summary>
        void RechargeDash()
        {
            int max = up.MaxDashCharges;
            if (dashCharges > max) dashCharges = max;
            while (dashCharges < max && Time.time >= rechargeAt)
            {
                dashCharges++;
                if (dashCharges < max) rechargeAt = Mathf.Max(rechargeAt + up.DashCooldown, Time.time);
            }
        }

        /// <summary>Bounces the ball off a wall: mirrors its velocity (and any dash) and multiplies the speed, up to cap.</summary>
        /// <param name="restartDash">Slingshot: launch a fresh dash burst along the bounce, so the next wall can bounce it too.</param>
        public void WallBounce(Vector2 pre, Vector2 normal, float gain, float cap, bool restartDash = false)
        {
            Vector2 v = Vector2.Reflect(pre, normal);
            SetVelocity(v.normalized * Mathf.Min(v.magnitude * gain, Mathf.Max(cap, pre.magnitude)));
            if (dashTimer > 0f) dashDir = Vector2.Reflect(dashDir, normal).normalized;
            if (restartDash && v.sqrMagnitude > 0.01f)
            {
                if (dashTimer <= 0f) dashDir = v.normalized;
                dashTimer = DashDuration;
                lastDashTime = Time.time;
            }
        }

        public void SetVelocity(Vector2 v)
        {
            rb.SetVel(v);
            PreStepVelocity = v;
        }
    }
}
