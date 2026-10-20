using UnityEngine;

namespace RollItOut
{
    /// <summary>IMGUI HUD + reward cards + game-over screen. IMGUI keeps it working while Time.timeScale is 0.</summary>
    public class HUD : MonoBehaviour
    {
        public RoundManager round;
        public PlayerHealth health;
        public PlayerUpgradeState upgrades;
        public UpgradeManager upgradeMgr;

        GUIStyle big, mid, midSmall, small, tiny;
        const float H = 720f;

        void Styles()
        {
            if (big != null) return;
            big = new GUIStyle { fontSize = 54, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            mid = new GUIStyle { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            midSmall = new GUIStyle(mid) { fontSize = 21 };
            small = new GUIStyle { fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            tiny = new GUIStyle { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
        }

        static void Fill(Rect r, Color c) { GUI.DrawTexture(r, SpriteLib.Square.texture, ScaleMode.StretchToFill, true, 0f, c, 0f, 0f); }
        static void Disc(Rect r, Color c) { GUI.DrawTexture(r, SpriteLib.Circle.texture, ScaleMode.StretchToFill, true, 0f, c, 0f, 0f); }
        static void RingTex(Rect r, Color c) { GUI.DrawTexture(r, SpriteLib.Ring.texture, ScaleMode.StretchToFill, true, 0f, c, 0f, 0f); }
        static void Dia(Rect r, Color c) { GUI.DrawTexture(r, SpriteLib.Diamond.texture, ScaleMode.StretchToFill, true, 0f, c, 0f, 0f); }

        void Text(Rect r, string s, GUIStyle st, Color c)
        {
            st.normal.textColor = new Color(0f, 0f, 0f, c.a * 0.8f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), s, st);
            st.normal.textColor = c;
            GUI.Label(r, s, st);
        }

        void OnGUI()
        {
            if (round == null) return;
            Styles();
            float s = Screen.height / H;
            float W = Screen.width / s;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));

            DrawHearts();
            Text(new Rect(W * 0.5f - 150, 8, 300, 36), "ROUND " + round.Round, mid, Color.white);
            Text(new Rect(W - 230, 8, 220, 28), "Enemies: " + round.AliveCount, small, new Color(1f, 1f, 1f, 0.85f));
            DrawStacks(W);
            if (round.State == GameState.Playing) DrawDash();

            if (round.State == GameState.Playing && round.BannerTime > 0f)
            {
                float a = Mathf.Clamp01(round.BannerTime / 0.6f);
                Text(new Rect(0, 250, W, 70), round.Banner, big, new Color(1f, 1f, 1f, a));
                if (round.BannerSub != "") Text(new Rect(0, 320, W, 40), round.BannerSub, small, new Color(1f, 0.9f, 0.5f, a));
            }

            if (round.State == GameState.Reward) DrawRewards(W);
            else if (round.State == GameState.GameOver) DrawGameOver(W);
        }

        void DrawHearts()
        {
            for (int i = 0; i < health.MaxHearts; i++)
            {
                var r = new Rect(14 + i * 44, 10, 38, 38);
                Disc(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), new Color(0f, 0f, 0f, 0.8f));
                Disc(r, new Color(0.3f, 0.1f, 0.12f));
                if (i < health.Hearts)
                {
                    // The heart currently being worn down dims as the damage meter fills.
                    bool current = i == health.Hearts - 1;
                    float fill = current ? 1f - health.PartialFraction * 0.65f : 1f;
                    Disc(r, new Color(0.95f * fill + 0.1f, 0.2f, 0.25f));
                }
            }
            if (upgrades.stoneSkinCharges > 0)
            {
                var r = new Rect(14, 76, 28, 28);
                Disc(r, new Color(0.7f, 0.72f, 0.78f));
                RingTex(r, Color.black);
                Text(new Rect(46, 76, 60, 28), "x" + upgrades.stoneSkinCharges, small, Color.white);
            }
            float y = 108;
            if (health.SecondChanceReady) { Text(new Rect(10, y, 160, 22), "Second Chance ready", tiny, new Color(1f, 0.9f, 0.4f)); y += 20; }
            if (health.HardenedActive) { Text(new Rect(10, y, 160, 22), "Hardened", tiny, new Color(0.8f, 0.85f, 1f)); y += 20; }
            if (health.LastStandActive) Text(new Rect(10, y, 160, 22), "Last Stand", tiny, new Color(1f, 0.35f, 0.35f));
        }

        PlayerBallController ball;

        void DrawDash()
        {
            if (ball == null) ball = upgrades.GetComponent<PlayerBallController>();
            if (ball == null) return;
            // Sits right under the hearts, as wide as the heart row.
            float bw = Mathf.Max(126f, health.MaxHearts * 44f - 6f);
            var r = new Rect(14, 58, bw, 10f);
            Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), new Color(0f, 0f, 0f, 0.75f));
            Fill(r, new Color(0.15f, 0.17f, 0.22f));
            bool ready = ball.DashReady;
            // Pulses brighter once ready so the player notices it without looking down.
            float pulse = ready ? 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 6f) : 1f;
            Color c = ready ? new Color(0.55f * pulse, 0.85f * pulse, 1f) : new Color(0.35f, 0.5f, 0.7f);
            Fill(new Rect(r.x, r.y, r.width * ball.DashCharge, r.height), c);
            // Double Dash: split the bar, one segment per stored dash.
            int slots = upgrades.MaxDashCharges;
            for (int i = 1; i < slots; i++) Fill(new Rect(r.x + r.width * i / slots - 1.5f, r.y, 3f, r.height), new Color(0f, 0f, 0f, 0.85f));

            // Wall bounce charges banked for the next hit, as pips right of the dash bar.
            int charges = upgrades.ActiveWallCharges;
            for (int i = 0; i < PlayerCollisionDamage.MaxWallCharges; i++)
            {
                var p = new Rect(r.xMax + 12 + i * 18, r.y - 2, 14, 14);
                Disc(new Rect(p.x - 2, p.y - 2, p.width + 4, p.height + 4), new Color(0f, 0f, 0f, 0.75f));
                Disc(p, i < charges ? new Color(1f, 0.55f - 0.12f * i, 0.15f) : new Color(0.2f, 0.18f, 0.16f));
            }
            float pipsEnd = r.xMax + 12 + PlayerCollisionDamage.MaxWallCharges * 18;
            if (charges > 0) Text(new Rect(pipsEnd, r.y - 5, 80, 20), "BANK x" + charges, tiny, new Color(1f, 0.7f, 0.3f));
        }

        void DrawStacks(float W)
        {
            float y = 40;
            foreach (UpgradeType t in System.Enum.GetValues(typeof(UpgradeType)))
            {
                int n = upgrades.Count(t);
                if (n <= 0 || t == UpgradeType.StoneSkin) continue; // Stone Skin charges show under the hearts
                var d = CardDefs.Get(t);
                Text(new Rect(W - 230, y, 220, 18), d.title + (d.maxStacks == 1 ? "" : " x" + n), tiny, new Color(d.color.r, d.color.g, d.color.b, 0.85f));
                y += 18;
            }
        }

        void DrawRewards(float W)
        {
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0.03f, 0.72f));
            Text(new Rect(0, 90, W, 60), "CHOOSE AN UPGRADE", big, Color.white);

            Text(new Rect(0, 150, W, 30), "Click, press 1 / 2 / 3, or stick left/right + A", small, new Color(1f, 1f, 1f, 0.6f));
            const float cw = 250f, ch = 310f, gap = 32f;
            int n = round.Offered.Count;
            float x0 = (W - (n * cw + (n - 1) * gap)) * 0.5f;
            var e = Event.current;

            for (int i = 0; i < n; i++)
            {
                var type = round.Offered[i];
                var info = CardDefs.Get(type);
                bool chosen = round.Selected == i;
                var r = new Rect(x0 + i * (cw + gap), 190, cw, ch);
                bool hover = round.Selected < 0 && r.Contains(e.mousePosition);
                if (hover && e.type == EventType.MouseMove) round.Highlight = i;

                if (chosen) { float g = round.SelectProgress * 10f; r = new Rect(r.x - g, r.y - g, r.width + 2 * g, r.height + 2 * g); }
                bool lit = chosen || (round.Selected < 0 && round.Highlight == i);
                Fill(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), lit ? Color.white : CardDefs.RarityColor(info.rarity));
                Fill(r, lit ? new Color(0.2f, 0.2f, 0.27f) : new Color(0.1f, 0.1f, 0.15f));

                Text(new Rect(r.x + 8, r.y + 6, r.width - 16, 20), info.rarity.ToString().ToUpper() + "  ·  " + info.category.ToUpper(), tiny, CardDefs.RarityColor(info.rarity));
                DrawIcon(info.icon, new Rect(r.x + cw / 2 - 45, r.y + 30, 90, 90), info.color);
                Text(new Rect(r.x + 8, r.y + 130, r.width - 16, 40), info.title, info.title.Length > 12 ? midSmall : mid, Color.white);
                Text(new Rect(r.x + 14, r.y + 172, r.width - 28, 76), info.text, small, new Color(0.9f, 0.9f, 0.95f));
                int owned = upgradeMgr.Stacks(type);
                if (owned > 0) Text(new Rect(r.x, r.y + ch - 62, r.width, 24), (type == UpgradeType.StoneSkin ? "Charges: " : "Owned: x") + owned, small, new Color(1f, 1f, 1f, 0.55f));
                Text(new Rect(r.x, r.y + ch - 34, r.width, 24), "[" + (i + 1) + "]", small, new Color(1f, 1f, 1f, 0.5f));

                if (e.type == EventType.MouseDown && e.button == 0 && hover)
                {
                    round.Select(i);
                    e.Use();
                }
            }
        }

        // Simple shape icons so each card reads at a glance.
        static void DrawIcon(CardIcon t, Rect r, Color c)
        {
            Disc(r, new Color(c.r * 0.35f, c.g * 0.35f, c.b * 0.35f));
            RingTex(r, c);
            float cx = r.center.x, cy = r.center.y;
            switch (t)
            {
                case CardIcon.Lines:
                    for (int i = 0; i < 3; i++) Fill(new Rect(cx - 30 + i * 6, cy - 22 + i * 16, 50 - i * 6, 8), c);
                    break;
                case CardIcon.Grow:
                    Disc(new Rect(cx - 12, cy - 12, 24, 24), c);
                    RingTex(new Rect(cx - 30, cy - 30, 60, 60), c);
                    break;
                case CardIcon.Heavy:
                    Disc(new Rect(cx - 28, cy - 28, 56, 56), c);
                    Fill(new Rect(cx - 16, cy - 4, 32, 8), new Color(0.1f, 0.05f, 0.02f));
                    break;
                case CardIcon.Drops:
                    Disc(new Rect(cx - 22, cy - 6, 20, 20), c);
                    Disc(new Rect(cx + 4, cy + 2, 16, 16), c);
                    Disc(new Rect(cx - 8, cy - 26, 14, 14), c);
                    break;
                case CardIcon.Shield:
                    Disc(new Rect(cx - 26, cy - 26, 52, 52), c);
                    Fill(new Rect(cx - 3, cy - 20, 6, 40), new Color(0.2f, 0.2f, 0.25f));
                    Fill(new Rect(cx - 20, cy - 3, 40, 6), new Color(0.2f, 0.2f, 0.25f));
                    break;
                case CardIcon.Bar:
                    RingTex(new Rect(cx - 28, cy - 28, 56, 56), c);
                    Fill(new Rect(cx - 18, cy - 4, 36, 8), c);
                    break;
                case CardIcon.Chevron:
                    for (int i = 0; i < 3; i++) Dia(new Rect(cx - 30 + i * 16, cy - 16, 24, 32), new Color(c.r, c.g, c.b, 0.5f + 0.25f * i));
                    break;
                case CardIcon.Wave:
                    Disc(new Rect(cx + 6, cy - 14, 28, 28), c);
                    for (int i = 0; i < 3; i++) Disc(new Rect(cx - 30 + i * 11, cy - 5, 10, 10), new Color(c.r, c.g, c.b, 0.3f + 0.2f * i));
                    break;
                case CardIcon.Turn:
                    RingTex(new Rect(cx - 26, cy - 26, 52, 52), c);
                    Disc(new Rect(cx + 14, cy - 26, 16, 16), c);
                    Disc(new Rect(cx - 8, cy - 8, 16, 16), c);
                    break;
                case CardIcon.Burst:
                    Dia(new Rect(cx - 30, cy - 8, 60, 16), c);
                    Dia(new Rect(cx - 8, cy - 30, 16, 60), c);
                    Disc(new Rect(cx - 12, cy - 12, 24, 24), Color.white);
                    break;
                case CardIcon.Heart:
                    Disc(new Rect(cx - 24, cy - 20, 26, 26), c);
                    Disc(new Rect(cx - 2, cy - 20, 26, 26), c);
                    Dia(new Rect(cx - 24, cy - 12, 48, 40), c);
                    break;
                case CardIcon.Trail:
                    Disc(new Rect(cx + 6, cy - 14, 28, 28), Color.white);
                    for (int i = 0; i < 4; i++) Disc(new Rect(cx - 34 + i * 10, cy - 4 - i * 2, 10 + i * 3, 10 + i * 3), new Color(c.r, c.g, c.b, 0.35f + 0.15f * i));
                    break;
                case CardIcon.Dash:
                    Disc(new Rect(cx + 2, cy - 14, 28, 28), Color.white);
                    for (int i = 0; i < 3; i++) Dia(new Rect(cx - 34 + i * 10, cy - 10 + i * 2, 34 - i * 4, 20 - i * 4), new Color(c.r, c.g, c.b, 0.4f + 0.2f * i));
                    break;
                default: // Pulse
                    Disc(new Rect(cx - 10, cy - 10, 20, 20), c);
                    RingTex(new Rect(cx - 22, cy - 22, 44, 44), c);
                    RingTex(new Rect(cx - 34, cy - 34, 68, 68), new Color(c.r, c.g, c.b, 0.5f));
                    break;
            }
        }

        void DrawGameOver(float W)
        {
            Fill(new Rect(0, 0, W, H), new Color(0.05f, 0f, 0f, 0.75f));
            Text(new Rect(0, 200, W, 80), "YOU ROLLED OUT", big, new Color(1f, 0.4f, 0.4f));
            Text(new Rect(0, 290, W, 40), "You reached round " + round.Round, mid, Color.white);
            var btn = new Rect(W * 0.5f - 130, 370, 260, 60);
            bool hover = btn.Contains(Event.current.mousePosition);
            Fill(btn, hover ? new Color(1f, 1f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.6f));
            Text(btn, "ROLL AGAIN", mid, new Color(0.1f, 0.1f, 0.15f));
            Text(new Rect(0, 445, W, 30), "or press R / Enter / A", small, new Color(1f, 1f, 1f, 0.6f));
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && hover)
            {
                Event.current.Use();
                round.Restart();
            }
        }
    }
}
