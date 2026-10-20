using System.Collections;
using UnityEngine;

namespace RollItOut
{
    /// <summary>
    /// Builds the whole prototype at runtime (arena, player, managers, HUD) so it runs from any scene
    /// with no scene wiring: open the project and press Play.
    /// </summary>
    public static class GameBootstrap
    {
        static GameObject root;
        static readonly Vector2 ArenaHalf = new Vector2(116.875f, 63.75f); // ~10.6x the original 22x12 playfield; the camera follows the ball

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (root != null) return;
            SetupCamera();
            Build();
        }

        public static void Restart()
        {
            Time.timeScale = 1f;
            if (root != null) Object.Destroy(root);
            root = null;
            new GameObject("RollItOutRestart").AddComponent<Restarter>();
        }

        class Restarter : MonoBehaviour
        {
            IEnumerator Start()
            {
                yield return null; // let the old root finish destroying
                Build();
                Destroy(gameObject);
            }
        }

        static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 7.2f; // same view as the old single-screen arena; CameraShake follows the player
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.06f, 0.09f);
            if (cam.GetComponent<CameraShake>() == null) cam.gameObject.AddComponent<CameraShake>();
        }

        static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static void Build()
        {
            Time.timeScale = 1f;
            root = new GameObject("RollItOut");
            Fx.Root = Child(root.transform, "Fx");

            BuildArena(root.transform);
            var player = BuildPlayer(root.transform);
            CameraShake.Follow(player.transform, ArenaHalf);

            var mgrGo = Child(root.transform, "Managers").gameObject;
            var upgrades = mgrGo.AddComponent<UpgradeManager>();
            var state = player.GetComponent<PlayerUpgradeState>();
            upgrades.state = state;

            var rm = mgrGo.AddComponent<RoundManager>();
            rm.ArenaHalf = ArenaHalf;
            rm.EnemyParent = Child(root.transform, "Enemies");
            rm.ProjectileParent = Child(root.transform, "Projectiles");
            rm.OrbParent = Child(root.transform, "XpOrbs");
            rm.player = player.transform;
            rm.health = player.GetComponent<PlayerHealth>();
            rm.ball = player.GetComponent<PlayerBallController>();
            rm.upgrades = upgrades;
            rm.effects = player.GetComponent<PlayerCardEffects>();
            rm.effects.round = rm;

            // Under the root so Restart wipes them along with everything else.
            Checkpoint.BuildAll(Child(root.transform, "Checkpoints"), ArenaHalf, player.transform, rm);

            var hud = mgrGo.AddComponent<HUD>();
            hud.round = rm;
            hud.health = rm.health;
            hud.upgrades = state;
            hud.upgradeMgr = upgrades;

            mgrGo.AddComponent<PauseMenu>().round = rm;
        }

        static void BuildArena(Transform parent)
        {
            var arena = Child(parent, "Arena");

            var floor = new GameObject("Floor");
            floor.transform.SetParent(arena, false);
            var sr = floor.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteLib.Checker;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = ArenaHalf * 2f;
            sr.sortingOrder = -10;

            Color frame = new Color(0.4f, 0.32f, 0.5f);
            float t = 0.25f, hx = ArenaHalf.x, hy = ArenaHalf.y;
            // Visible border just outside the playfield, plus a solid collider behind it.
            Wall(arena, "Top", new Vector2(0f, hy + 1f), new Vector2(hx * 2f + 6f, 2f), frame, new Vector2(0f, hy + t * 0.5f), new Vector2(hx * 2f + 2f * t, t));
            Wall(arena, "Bottom", new Vector2(0f, -hy - 1f), new Vector2(hx * 2f + 6f, 2f), frame, new Vector2(0f, -hy - t * 0.5f), new Vector2(hx * 2f + 2f * t, t));
            Wall(arena, "Left", new Vector2(-hx - 1f, 0f), new Vector2(2f, hy * 2f + 6f), frame, new Vector2(-hx - t * 0.5f, 0f), new Vector2(t, hy * 2f));
            Wall(arena, "Right", new Vector2(hx + 1f, 0f), new Vector2(2f, hy * 2f + 6f), frame, new Vector2(hx + t * 0.5f, 0f), new Vector2(t, hy * 2f));
        }

        static void Wall(Transform parent, string name, Vector2 colPos, Vector2 colSize, Color color, Vector2 visPos, Vector2 visSize)
        {
            var go = new GameObject("Wall" + name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = colPos;
            go.AddComponent<ArenaWall>();
            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = colSize;
            bc.sharedMaterial = new PhysicsMaterial2D { friction = 0f, bounciness = 0.25f };
            SpriteLib.Add(parent, name + "Frame", SpriteLib.Square, color, -5, visPos, new Vector3(visSize.x, visSize.y, 1f));
        }

        static GameObject BuildPlayer(Transform parent)
        {
            var go = new GameObject("Player");
            go.transform.SetParent(parent, false);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.mass = 5f; // heavy enough that enemy bodies can't shove the ball around
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.sharedMaterial = new PhysicsMaterial2D { friction = 0f, bounciness = 0.2f };

            // The ball is an armadillo rolled up: banded shell spins with velocity, head and tail poke out toward the move direction.
            Color ballColor = new Color(0.78f, 0.62f, 0.46f);
            Color skin = new Color(0.86f, 0.6f, 0.52f);
            Color ink = new Color(0.08f, 0.05f, 0.02f);
            var face = Child(go.transform, "Face");
            // Ink silhouettes under the skin parts so head and tail read on sand too.
            SpriteLib.Add(face, "TailInk", SpriteLib.Diamond, ink, 7, new Vector3(0f, -0.58f, 0f), new Vector3(0.24f, 0.62f, 1f));
            SpriteLib.Add(face, "SnoutInk", SpriteLib.Diamond, ink, 7, new Vector3(0f, 0.6f, 0f), new Vector3(0.48f, 0.74f, 1f));
            SpriteLib.Add(face, "EarLInk", SpriteLib.Circle, ink, 7, new Vector3(-0.2f, 0.53f, 0f), new Vector3(0.22f, 0.3f, 1f));
            SpriteLib.Add(face, "EarRInk", SpriteLib.Circle, ink, 7, new Vector3(0.2f, 0.53f, 0f), new Vector3(0.22f, 0.3f, 1f));
            SpriteLib.Add(face, "Tail", SpriteLib.Diamond, skin, 9, new Vector3(0f, -0.58f, 0f), new Vector3(0.16f, 0.5f, 1f));
            SpriteLib.Add(face, "Snout", SpriteLib.Diamond, skin, 9, new Vector3(0f, 0.6f, 0f), new Vector3(0.38f, 0.6f, 1f));
            SpriteLib.Add(face, "EarL", SpriteLib.Circle, skin, 9, new Vector3(-0.2f, 0.53f, 0f), new Vector3(0.16f, 0.24f, 1f));
            SpriteLib.Add(face, "EarR", SpriteLib.Circle, skin, 9, new Vector3(0.2f, 0.53f, 0f), new Vector3(0.16f, 0.24f, 1f));
            SpriteLib.Add(face, "EyeL", SpriteLib.Circle, ink, 10, new Vector3(-0.08f, 0.66f, 0f), Vector3.one * 0.07f);
            SpriteLib.Add(face, "EyeR", SpriteLib.Circle, ink, 10, new Vector3(0.08f, 0.66f, 0f), Vector3.one * 0.07f);
            SpriteLib.Add(face, "Nose", SpriteLib.Circle, ink, 10, new Vector3(0f, 0.87f, 0f), Vector3.one * 0.07f);

            var roller = Child(go.transform, "Roller");
            var body = SpriteLib.Add(roller, "Shell", SpriteLib.ArmadilloShell, ballColor, 11, Vector3.zero, Vector3.one);
            SpriteLib.Add(go.transform, "Outline", SpriteLib.Ring, ink, 12, Vector3.zero, Vector3.one * 1.04f);

            var shield = SpriteLib.Add(go.transform, "StoneSkin", SpriteLib.Ring, new Color(0.72f, 0.75f, 0.82f), 9, Vector3.zero, Vector3.one * 1.3f);

            var trail = go.AddComponent<TrailRenderer>();
            var shader = Shader.Find("Sprites/Default");
            if (shader != null) trail.material = new Material(shader);
            trail.time = 0.25f;
            trail.startWidth = 0.45f;
            trail.endWidth = 0f;
            trail.startColor = new Color(0.85f, 0.7f, 0.5f, 0.35f);
            trail.endColor = new Color(0.85f, 0.7f, 0.5f, 0f);
            trail.sortingOrder = 8; // below the ball and projectiles
            trail.numCapVertices = 4;

            var state = go.AddComponent<PlayerUpgradeState>();
            var health = go.AddComponent<PlayerHealth>();
            health.body = body;
            health.baseColor = ballColor;
            health.upgrades = state;
            var ctrl = go.AddComponent<PlayerBallController>();
            ctrl.roller = roller;
            ctrl.face = face;
            ctrl.stoneShield = shield.gameObject;
            go.AddComponent<PlayerCardEffects>(); // before PlayerCollisionDamage, which looks it up in Awake
            go.AddComponent<PlayerCollisionDamage>();
            return go;
        }
    }
}
