using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace RollItOut
{
    /// <summary>
    /// Pause menu opened with gamepad Start, Esc or P during a round. Freezes time and offers Resume / Restart / Quit.
    /// IMGUI like the HUD so it keeps working while Time.timeScale is 0.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        public RoundManager round;

        public static bool IsPaused { get; private set; }

        static readonly string[] Options = { "RESUME", "RESTART", "QUIT" };
        const float H = 720f;
        const float BtnW = 280f, BtnH = 58f, BtnGap = 16f, BtnTop = 300f;

        int highlight;
        int lastNav;
        GUIStyle big, mid, small;

        void OnDestroy() { if (IsPaused) { IsPaused = false; Time.timeScale = 1f; } }

        void Update()
        {
            if (round == null) return;

            if (!IsPaused)
            {
                bool canPause = round.State == GameState.Playing || round.State == GameState.Clearing;
                if (canPause && PausePressed()) Open();
                return;
            }

            if (PausePressed() || BackPressed()) { Resume(); return; }
            int step = NavStep();
            if (step != 0) highlight = (highlight + step + Options.Length) % Options.Length;
            if (InputReader.SubmitPressed()) Choose(highlight);
        }

        void Open()
        {
            IsPaused = true;
            highlight = 0;
            NavStep(); // swallow the stick state held from play
            Time.timeScale = 0f;
        }

        void Resume()
        {
            IsPaused = false;
            // Always back to 1: a hit-stop that was running when we paused has already timed out on unscaled time.
            Time.timeScale = 1f;
        }

        void Choose(int i)
        {
            switch (i)
            {
                case 0: Resume(); break;
                case 1: IsPaused = false; round.Restart(); break;
                default: Quit(); break;
            }
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---- input ----

        /// <summary>Gamepad Start, Esc or P.</summary>
        static bool PausePressed()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.JoystickButton7);
#elif ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            return (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame))
                || (pad != null && pad.startButton.wasPressedThisFrame);
#else
            return false;
#endif
        }

        /// <summary>Gamepad B / Backspace closes the menu too.</summary>
        static bool BackPressed()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.JoystickButton1);
#elif ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            return (kb != null && kb.backspaceKey.wasPressedThisFrame)
                || (pad != null && pad.buttonEast.wasPressedThisFrame);
#else
            return false;
#endif
        }

        /// <summary>+1 (down) / -1 (up) once per press (d-pad, stick, W/S, arrows); 0 otherwise.</summary>
        int NavStep()
        {
            float y = 0f;
#if ENABLE_LEGACY_INPUT_MANAGER
            y = Input.GetAxisRaw("Vertical");
#elif ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
            }
            var pad = Gamepad.current;
            if (pad != null && y == 0f) y = pad.leftStick.ReadValue().y + pad.dpad.ReadValue().y;
#endif
            int dir = y < -0.6f ? 1 : y > 0.6f ? -1 : 0;
            int step = dir != 0 && dir != lastNav ? dir : 0;
            lastNav = dir;
            return step;
        }

        // ---- drawing ----

        void Styles()
        {
            if (big != null) return;
            big = new GUIStyle { fontSize = 54, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            mid = new GUIStyle { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            small = new GUIStyle { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        }

        static void Fill(Rect r, Color c) { GUI.DrawTexture(r, SpriteLib.Square.texture, ScaleMode.StretchToFill, true, 0f, c, 0f, 0f); }

        static void Text(Rect r, string s, GUIStyle st, Color c)
        {
            st.normal.textColor = new Color(0f, 0f, 0f, c.a * 0.8f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), s, st);
            st.normal.textColor = c;
            GUI.Label(r, s, st);
        }

        void OnGUI()
        {
            if (!IsPaused) return;
            Styles();
            GUI.depth = -10; // above the HUD
            float s = Screen.height / H;
            float W = Screen.width / s;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            var e = Event.current;

            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0.03f, 0.72f));
            Text(new Rect(0, 180, W, 70), "PAUSED", big, Color.white);
            Text(new Rect(0, 250, W, 30), "Round " + round.Round, small, new Color(1f, 1f, 1f, 0.6f));

            for (int i = 0; i < Options.Length; i++)
            {
                var r = new Rect(W * 0.5f - BtnW * 0.5f, BtnTop + i * (BtnH + BtnGap), BtnW, BtnH);
                bool hover = r.Contains(e.mousePosition);
                if (hover && e.type == EventType.MouseMove) highlight = i;
                bool lit = highlight == i;
                Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), lit ? Color.white : new Color(1f, 1f, 1f, 0.25f));
                Fill(r, lit ? new Color(0.2f, 0.2f, 0.27f) : new Color(0.1f, 0.1f, 0.15f));
                Text(r, Options[i], mid, lit ? Color.white : new Color(1f, 1f, 1f, 0.7f));

                if (e.type == EventType.MouseDown && e.button == 0 && hover)
                {
                    e.Use();
                    Choose(i);
                    return;
                }
            }

            Text(new Rect(0, BtnTop + Options.Length * (BtnH + BtnGap) + 10, W, 30),
                "Start / Esc to resume  ·  stick or W/S + A / Enter to choose", small, new Color(1f, 1f, 1f, 0.6f));
        }
    }
}
