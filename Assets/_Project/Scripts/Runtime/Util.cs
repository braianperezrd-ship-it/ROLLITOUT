using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace RollItOut
{
    /// <summary>Rigidbody2D velocity accessors that compile on both Unity 6 and older versions.</summary>
    public static class RbExt
    {
        public static Vector2 GetVel(this Rigidbody2D rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static void SetVel(this Rigidbody2D rb, Vector2 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }
    }

    /// <summary>Procedurally generated sprites so the prototype needs no art assets.</summary>
    public static class SpriteLib
    {
        static Sprite circle, ring, square, diamond, checker;

        public static Sprite Circle => circle != null ? circle : (circle = Make(128, (x, y) => Mathf.Clamp01((1f - Mathf.Sqrt(x * x + y * y)) * 24f)));

        public static Sprite Ring => ring != null ? ring : (ring = Make(128, (x, y) =>
        {
            float d = Mathf.Sqrt(x * x + y * y);
            return Mathf.Clamp01((1f - d) * 24f) * Mathf.Clamp01((d - 0.84f) * 24f);
        }));

        public static Sprite Diamond => diamond != null ? diamond : (diamond = Make(128, (x, y) => Mathf.Clamp01((1f - (Mathf.Abs(x) + Mathf.Abs(y))) * 24f)));

        public static Sprite Square
        {
            get
            {
                if (square != null) return square;
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                var px = new Color[16];
                for (int i = 0; i < px.Length; i++) px[i] = Color.white;
                tex.SetPixels(px);
                tex.Apply();
                return square = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            }
        }

        /// <summary>2x2 unit checker tile; draw it with SpriteDrawMode.Tiled.</summary>
        public static Sprite Checker
        {
            get
            {
                if (checker != null) return checker;
                var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                var a = new Color(0.15f, 0.16f, 0.21f);
                var b = new Color(0.185f, 0.195f, 0.25f);
                for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                        tex.SetPixel(x, y, ((x / 32) + (y / 32)) % 2 == 0 ? a : b);
                tex.Apply();
                return checker = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
            }
        }

        static Sprite Make(int size, Func<float, float, float> alpha)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha(nx, ny)));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        public static SpriteRenderer Add(Transform parent, string name, Sprite sprite, Color color, int order, Vector3 localPos, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }
    }

    /// <summary>Input from keyboard (WASD / arrows) and gamepad left stick; works with either input backend.</summary>
    public static class InputReader
    {
        public static Vector2 Move()
        {
            Vector2 v = Vector2.zero;
#if ENABLE_LEGACY_INPUT_MANAGER
            v = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#elif ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
            }
            var pad = Gamepad.current;
            if (pad != null && v == Vector2.zero) v = pad.leftStick.ReadValue();
#endif
            if (v.sqrMagnitude < 0.0225f) return Vector2.zero;
            // Normalized so diagonals are not faster; analog sticks keep partial magnitude.
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }


        static int lastNav;

        /// <summary>-1 / +1 once per left/right press (d-pad, stick, A/D, arrows); 0 otherwise.</summary>
        public static int NavStep()
        {
            float x = 0f;
#if ENABLE_LEGACY_INPUT_MANAGER
            x = Input.GetAxisRaw("Horizontal");
#elif ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            }
            var pad = Gamepad.current;
            if (pad != null && x == 0f) x = pad.leftStick.ReadValue().x + pad.dpad.ReadValue().x;
#endif
            int dir = x > 0.6f ? 1 : x < -0.6f ? -1 : 0;
            int step = dir != 0 && dir != lastNav ? dir : 0;
            lastNav = dir;
            return step;
        }

        /// <summary>Gamepad A / Enter / Space.</summary>
        public static bool SubmitPressed()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0);
#elif ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            return (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                || (pad != null && pad.buttonSouth.wasPressedThisFrame);
#else
            return false;
#endif
        }

        public static bool DigitPressed(int index)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Alpha1 + index) || Input.GetKeyDown(KeyCode.Keypad1 + index);
#elif ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            switch (index)
            {
                case 0: return kb.digit1Key.wasPressedThisFrame;
                case 1: return kb.digit2Key.wasPressedThisFrame;
                default: return kb.digit3Key.wasPressedThisFrame;
            }
#else
            return false;
#endif
        }

        public static bool ConfirmPressed()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0);
#elif ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            return (kb != null && (kb.rKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                || (pad != null && pad.buttonSouth.wasPressedThisFrame);
#else
            return false;
#endif
        }
    }

    /// <summary>Tiny self-destructing particle.</summary>
    public class Spark : MonoBehaviour
    {
        Vector2 vel;
        float life, age, size;
        SpriteRenderer sr;
        Color color;

        public static void Create(Vector2 pos, Vector2 vel, Color color, float size, float life)
        {
            var go = new GameObject("Spark");
            if (Fx.Root != null) go.transform.SetParent(Fx.Root, false);
            go.transform.position = pos;
            var s = go.AddComponent<Spark>();
            s.vel = vel; s.color = color; s.size = size; s.life = life;
            s.sr = go.AddComponent<SpriteRenderer>();
            s.sr.sprite = SpriteLib.Circle;
            s.sr.sortingOrder = 20;
            s.sr.color = color;
            go.transform.localScale = Vector3.one * size;
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= life) { Destroy(gameObject); return; }
            float k = age / life;
            transform.position += (Vector3)(vel * Time.deltaTime);
            vel *= 1f - 4f * Time.deltaTime;
            transform.localScale = Vector3.one * size * (1f - k);
            sr.color = new Color(color.r, color.g, color.b, color.a * (1f - k));
        }
    }

    public static class Fx
    {
        public static Transform Root;

        public static void Burst(Vector2 pos, Color color, int count, float speed, float size = 0.16f, float life = 0.4f)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
                Spark.Create(pos, dir * speed * UnityEngine.Random.Range(0.4f, 1f), color, size * UnityEngine.Random.Range(0.6f, 1.2f), life * UnityEngine.Random.Range(0.7f, 1.1f));
            }
        }
    }

    /// <summary>Small screen shake, reserved for strong impacts.</summary>
    public class CameraShake : MonoBehaviour
    {
        static CameraShake instance;
        float amp, dur, left;
        Vector3 basePos;

        void Awake() { instance = this; basePos = transform.position; }

        public static void Shake(float amplitude, float duration)
        {
            if (instance == null) return;
            instance.amp = Mathf.Max(instance.amp * (instance.left > 0 ? 1f : 0f), amplitude);
            instance.dur = duration;
            instance.left = duration;
        }

        void LateUpdate()
        {
            if (left > 0f)
            {
                left -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(left / dur);
                transform.position = basePos + (Vector3)(UnityEngine.Random.insideUnitCircle * amp * k);
            }
            else
            {
                amp = 0f;
                transform.position = basePos;
            }
        }
    }
}
