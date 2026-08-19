using System.Collections.Generic;
using GyroCue.Input;
using GyroCue.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GyroCue.Core
{
    /// <summary>
    /// Builds a playable table at runtime: felt, cushions, pocket triggers, a racked
    /// set of balls, the input/rules/HUD rig, and camera framing.
    ///
    /// Everything is constructed in code so the scene asset stays a single component
    /// and there are no inspector references to drift out of sync.
    /// </summary>
    public sealed class TableSceneBuilder : MonoBehaviour
    {
        private static readonly Color[] ObjectBallColors =
        {
            new Color(0.95f, 0.78f, 0.20f), new Color(0.20f, 0.36f, 0.80f),
            new Color(0.83f, 0.24f, 0.22f), new Color(0.45f, 0.26f, 0.62f),
            new Color(0.92f, 0.49f, 0.16f), new Color(0.16f, 0.55f, 0.32f),
            new Color(0.55f, 0.20f, 0.22f), new Color(0.12f, 0.12f, 0.14f),
            new Color(0.97f, 0.85f, 0.45f), new Color(0.42f, 0.58f, 0.88f),
            new Color(0.88f, 0.45f, 0.42f), new Color(0.64f, 0.50f, 0.78f),
            new Color(0.95f, 0.68f, 0.42f), new Color(0.40f, 0.72f, 0.52f),
            new Color(0.72f, 0.42f, 0.44f)
        };

        [Header("Ball")]
        [SerializeField, Min(0.05f)]
        private float ballRadius = 0.26f;

        [Header("Shot Tuning")]
        [SerializeField, Min(0f)]
        private float maxCueBallImpulse = 2f;

        [SerializeField, Min(0f)]
        private float settleDebounceSeconds = 0.25f;

        [Header("Palette")]
        [SerializeField]
        private Color feltColor = new Color(0.09f, 0.42f, 0.26f, 1f);

        [SerializeField]
        private Color cushionColor = new Color(0.30f, 0.18f, 0.11f, 1f);

        [SerializeField]
        private Color pocketColor = new Color(0.05f, 0.05f, 0.06f, 1f);

        private readonly List<Rigidbody2D> ballBodies = new List<Rigidbody2D>();

        private Sprite circleSprite;
        private Sprite quadSprite;
        private PhysicsMaterial2D ballMaterial;
        private PoolPhysicsTuningProfile physicsProfile;
        private Rigidbody2D cueBallBody;

        public IReadOnlyList<Rigidbody2D> BallBodies => ballBodies;

        public Rigidbody2D CueBallBody => cueBallBody;

        private void Awake()
        {
            circleSprite = SpriteFactory.CreateCircleSprite();
            quadSprite = SpriteFactory.CreateQuadSprite();

            physicsProfile = ScriptableObject.CreateInstance<PoolPhysicsTuningProfile>();
            ballMaterial = new PhysicsMaterial2D("PoolBallRuntimeMaterial")
            {
                friction = physicsProfile.Friction,
                bounciness = physicsProfile.Restitution
            };

            BuildFelt();
            BuildCushions();
            var pocketRoots = BuildPockets();
            BuildBalls();
            FrameCamera();

            BuildRig(pocketRoots);
        }

        private void BuildFelt()
        {
            var felt = CreateSpriteObject("Felt", quadSprite, feltColor, sortingOrder: -20);
            felt.transform.localScale = new Vector3(
                TableLayoutConstants.TableWidthWorldUnits,
                TableLayoutConstants.TableHeightWorldUnits,
                1f);

            var cloth = CreateSpriteObject("PlayingSurface", quadSprite, feltColor * 1.12f, sortingOrder: -19);
            cloth.transform.localScale = new Vector3(
                TableRackMath.PlayableHalfWidth * 2f,
                TableRackMath.PlayableHalfHeight * 2f,
                1f);
        }

        private void BuildCushions()
        {
            var segments = TableRackMath.ResolveCushionSegments();
            for (var i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                var cushion = CreateSpriteObject($"Cushion{i}", quadSprite, cushionColor, sortingOrder: -10);
                cushion.transform.position = segment.Center;
                cushion.transform.localScale = new Vector3(segment.Size.x, segment.Size.y, 1f);

                var collider = cushion.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one;
                collider.sharedMaterial = ballMaterial;
            }
        }

        private List<PocketTriggerReporter> BuildPockets()
        {
            var reporters = new List<PocketTriggerReporter>();
            var pockets = TableRackMath.ResolvePocketPositions();

            for (var i = 0; i < pockets.Length; i++)
            {
                var pocket = CreateSpriteObject($"Pocket{i}", circleSprite, pocketColor, sortingOrder: -15);
                pocket.transform.position = pockets[i];
                pocket.transform.localScale = Vector3.one * (TableLayoutConstants.PocketRadius * 2f);

                var collider = pocket.AddComponent<CircleCollider2D>();
                collider.radius = 0.5f;
                collider.isTrigger = true;

                reporters.Add(pocket.AddComponent<PocketTriggerReporter>());
            }

            return reporters;
        }

        private void BuildBalls()
        {
            cueBallBody = CreateBall("CueBall", TableRackMath.ResolveCueBallSpot(), Color.white);
            ballBodies.Add(cueBallBody);

            var rack = TableRackMath.ResolveRackPositions(TableRackMath.ResolveRackApex(), ballRadius);
            for (var i = 0; i < rack.Length; i++)
            {
                ballBodies.Add(CreateBall($"Ball{i + 1}", rack[i], ObjectBallColors[i % ObjectBallColors.Length]));
            }
        }

        private Rigidbody2D CreateBall(string name, Vector2 position, Color color)
        {
            var ball = CreateSpriteObject(name, circleSprite, color, sortingOrder: 10);
            ball.transform.position = position;
            ball.transform.localScale = Vector3.one * (ballRadius * 2f);

            var collider = ball.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            collider.sharedMaterial = ballMaterial;

            var body = ball.AddComponent<Rigidbody2D>();
            // Top-down table: the world gravity vector must not pull balls to one rail.
            body.gravityScale = 0f;
            physicsProfile.ApplyTo(body, collider);

            return body;
        }

        private void BuildRig(List<PocketTriggerReporter> pocketReporters)
        {
            var pocketTable = gameObject.AddComponent<PocketTableController>();
            pocketTable.CueBallBody = cueBallBody;
            for (var i = 0; i < pocketReporters.Count; i++)
            {
                pocketReporters[i].PocketTableController = pocketTable;
            }

            var placement = gameObject.AddComponent<CueBallPlacementController>();
            placement.CueBallBody = cueBallBody;

            var touch = gameObject.AddComponent<TouchAimSwipeController>();
            gameObject.AddComponent<CueInputCoordinator>();

            var lineRenderer = gameObject.AddComponent<LineRenderer>();
            ConfigureAimLine(lineRenderer);

            var preview = gameObject.AddComponent<CuePreviewVisualizer>();
            preview.SetCueBallAnchor(cueBallBody.transform);

            var hud = gameObject.AddComponent<MinimalHudPresenter>();
            BuildHud(hud);

            var lifecycle = gameObject.AddComponent<ShotLifecycleController>();
            lifecycle.ConfigureTable(
                cueBallBody,
                ballBodies.ToArray(),
                physicsProfile,
                pocketTable,
                configuredRuleResolver: null,
                maxCueBallImpulse,
                settleDebounceSeconds,
                angularStopThresholdDegreesPerSecond: 3f);

            var pointer = gameObject.AddComponent<PointerShotInput>();
            pointer.Configure(touch, placement, Camera.main);
        }

        private void ConfigureAimLine(LineRenderer lineRenderer)
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = new Color(1f, 1f, 1f, 0.85f);
            lineRenderer.endColor = new Color(1f, 1f, 1f, 0.05f);
            lineRenderer.startWidth = 0.07f;
            lineRenderer.endWidth = 0.07f;
            lineRenderer.numCapVertices = 4;
            lineRenderer.useWorldSpace = true;
            lineRenderer.sortingOrder = 20;
        }

        private static void BuildHud(MinimalHudPresenter presenter)
        {
            var canvasObject = new GameObject("HudCanvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var canvasRect = (RectTransform)canvasObject.transform;
            var turn = CreateHudLabel("TurnLabel", canvasRect, new Vector2(0.5f, 0.95f), 40);
            var power = CreateHudLabel("PowerLabel", canvasRect, new Vector2(0.5f, 0.90f), 34);
            var status = CreateHudLabel("StatusLabel", canvasRect, new Vector2(0.5f, 0.06f), 32);

            presenter.ConfigureLabels(turn, power, status);
            presenter.ResetForNewMatch(playerOneStarts: true);
        }

        private static Text CreateHudLabel(string name, Transform parent, Vector2 anchor, int fontSize)
        {
            var labelObject = new GameObject(name, typeof(RectTransform));
            labelObject.transform.SetParent(parent, worldPositionStays: false);

            var rect = (RectTransform)labelObject.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1000f, 80f);

            var text = labelObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return text;
        }

        private static void FrameCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.orthographic = true;
            camera.orthographicSize = TableLayoutConstants.CalculateOrthoSize(camera.aspect);
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private GameObject CreateSpriteObject(string name, Sprite sprite, Color color, int sortingOrder)
        {
            var created = new GameObject(name);
            created.transform.SetParent(transform, worldPositionStays: false);

            var renderer = created.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            return created;
        }
    }

    /// <summary>
    /// Generates the two primitive sprites the table needs, so the project carries
    /// no imported art for the placeholder presentation.
    /// </summary>
    public static class SpriteFactory
    {
        public static Sprite CreateQuadSprite()
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, mipChain: false);
            var pixels = new Color32[16];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), pixelsPerUnit: 4f);
        }

        public static Sprite CreateCircleSprite(int resolution = 128)
        {
            var size = Mathf.Max(8, resolution);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false);
            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            var radius = size * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Mathf.Sqrt(((x - center) * (x - center)) + ((y - center) * (y - center)));
                    // Feather the last pixel so edges do not stair-step when scaled up.
                    var alpha = Mathf.Clamp01(radius - distance);
                    pixels[(y * size) + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
