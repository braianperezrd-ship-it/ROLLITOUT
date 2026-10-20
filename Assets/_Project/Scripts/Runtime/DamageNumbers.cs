using System.Collections.Generic;
using UnityEngine;

namespace RollItOut
{
    /// <summary>Floating damage numbers drawn with IMGUI above whatever was hit.</summary>
    public class DamageNumbers : MonoBehaviour
    {
        public const float Life = 0.75f;
        const float RisePx = 60f;   // in 720p reference pixels
        const float H = 720f;

        struct Popup { public Vector3 world; public string text; public Color color; public int size; public float age; public float drift; }

        static DamageNumbers instance;
        readonly List<Popup> popups = new List<Popup>();
        GUIStyle style;

        /// <summary>Shows a number at a world position. big = direct hit, otherwise a smaller damage-over-time tick.</summary>
        public static void Spawn(Vector3 worldPos, float amount, Color color, bool big)
        {
            if (instance == null)
            {
                var go = new GameObject("DamageNumbers");
                if (Fx.Root != null) go.transform.SetParent(Fx.Root, false);
                instance = go.AddComponent<DamageNumbers>();
            }
            instance.popups.Add(new Popup
            {
                world = worldPos,
                text = Mathf.Max(1, Mathf.RoundToInt(amount)).ToString(),
                color = color,
                size = big ? 30 : 20,
                drift = Random.Range(-14f, 14f),
            });
        }

        void Update()
        {
            // Unscaled so numbers don't hang on screen while the reward cards pause the game.
            float dt = Time.unscaledDeltaTime;
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                var p = popups[i];
                p.age += dt;
                if (p.age >= Life) popups.RemoveAt(i);
                else popups[i] = p;
            }
        }

        void OnGUI()
        {
            var cam = Camera.main;
            if (cam == null || popups.Count == 0) return;
            if (style == null) style = new GUIStyle { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

            GUI.depth = 10; // behind the HUD (reward cards, game over)
            float s = Screen.height / H;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));

            foreach (var p in popups)
            {
                float k = p.age / Life;
                Vector3 sp = cam.WorldToScreenPoint(p.world);
                float x = sp.x / s + p.drift * k;
                float y = (Screen.height - sp.y) / s - RisePx * (1f - (1f - k) * (1f - k)); // ease-out rise
                float pop = k < 0.15f ? Mathf.Lerp(1.6f, 1f, k / 0.15f) : 1f;              // quick punch-in
                float a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;

                style.fontSize = Mathf.RoundToInt(p.size * pop);
                var r = new Rect(x - 60f, y - 25f, 120f, 50f);
                style.normal.textColor = new Color(0f, 0f, 0f, a * 0.85f);
                GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), p.text, style);
                style.normal.textColor = new Color(p.color.r, p.color.g, p.color.b, a);
                GUI.Label(r, p.text, style);
            }
        }
    }
}
