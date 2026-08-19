using UnityEngine;
using UnityEngine.UI;

namespace GyroCue.Practice
{
    /// <summary>
    /// Practice HUD: session stats, a live readout of the stroke, and on-screen
    /// targets for the two drag widgets so the controls are discoverable.
    /// </summary>
    public sealed class PracticeHud : MonoBehaviour
    {
        private PracticeSessionController session;
        private PracticeInputRouter router;
        private OrbitAimController orbit;

        private Text statsLabel;
        private Text strokeLabel;
        private Text hintLabel;
        private RectTransform tipMarker;
        private RectTransform elevationFill;

        public void Configure(
            PracticeSessionController sessionController,
            PracticeInputRouter inputRouter,
            OrbitAimController orbitAimController)
        {
            session = sessionController;
            router = inputRouter;
            orbit = orbitAimController;

            BuildCanvas();
        }

        private void BuildCanvas()
        {
            var canvasObject = new GameObject("PracticeHud", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = (RectTransform)canvasObject.transform;

            statsLabel = CreateLabel("Stats", root, new Vector2(0.5f, 0.96f), 38, TextAnchor.MiddleCenter);
            strokeLabel = CreateLabel("Stroke", root, new Vector2(0.5f, 0.92f), 30, TextAnchor.MiddleCenter);
            hintLabel = CreateLabel("Hint", root, new Vector2(0.5f, 0.40f), 26, TextAnchor.MiddleCenter);
            hintLabel.text = "Drag the table to aim  •  draw down and stroke up on the ball  •  right strip raises the cue";

            BuildStrokeWidget(root);
            BuildElevationStrip(root);
        }

        private void BuildStrokeWidget(RectTransform root)
        {
            var rect = PracticeControlLayout.StrokeWidget;
            var panel = CreatePanel("StrokeWidget", root, rect, new Color(1f, 1f, 1f, 0.06f));

            // The ball face occupies the top third of the widget; below it is draw room.
            var faceHeight = (PracticeControlLayout.FaceTop - -1f) /
                             (PracticeControlLayout.FaceTop - PracticeControlLayout.FaceBottom);

            var face = CreateChildRect("BallFace", panel);
            face.anchorMin = new Vector2(0.5f, 1f - faceHeight);
            face.anchorMax = new Vector2(0.5f, 1f);
            face.pivot = new Vector2(0.5f, 0.5f);
            face.anchoredPosition = Vector2.zero;
            face.sizeDelta = new Vector2(240f, 0f);
            AddImage(face, new Color(0.95f, 0.95f, 0.95f, 0.20f));

            var centreMark = CreateChildRect("CentreMark", face);
            centreMark.anchorMin = new Vector2(0.5f, 0.5f);
            centreMark.anchorMax = new Vector2(0.5f, 0.5f);
            centreMark.sizeDelta = new Vector2(12f, 12f);
            centreMark.anchoredPosition = Vector2.zero;
            AddImage(centreMark, new Color(1f, 1f, 1f, 0.55f));

            tipMarker = CreateChildRect("TipMarker", face);
            tipMarker.anchorMin = new Vector2(0.5f, 0.5f);
            tipMarker.anchorMax = new Vector2(0.5f, 0.5f);
            tipMarker.sizeDelta = new Vector2(28f, 28f);
            AddImage(tipMarker, new Color(0.45f, 0.9f, 1f, 0.9f));
        }

        private void BuildElevationStrip(RectTransform root)
        {
            var panel = CreatePanel("ElevationStrip", root, PracticeControlLayout.ElevationStrip, new Color(1f, 1f, 1f, 0.06f));

            elevationFill = CreateChildRect("ElevationFill", panel);
            elevationFill.anchorMin = Vector2.zero;
            elevationFill.anchorMax = new Vector2(1f, 0f);
            elevationFill.pivot = new Vector2(0.5f, 0f);
            elevationFill.offsetMin = Vector2.zero;
            elevationFill.offsetMax = new Vector2(0f, 10f);
            AddImage(elevationFill, new Color(1f, 0.75f, 0.35f, 0.75f));
        }

        private void Update()
        {
            if (session == null || router == null)
            {
                return;
            }

            statsLabel.text =
                $"Shots {session.ShotsTaken}   Pocketed {session.BallsPocketed}   Left {session.BallsRemaining}   Scratches {session.Scratches}";

            var gesture = router.StrokeGesture;
            var offset = gesture.StrikeOffset;
            strokeLabel.text = session.Phase == PracticePhase.Simulating
                ? "Balls rolling..."
                : $"Power {gesture.PreviewPower01 * 100f:0}%   Tip {DescribeTip(offset)}   Cue {router.ElevationDegrees:0}°" +
                  (session.LastShotMiscued ? "   (last shot miscued)" : string.Empty);

            UpdateTipMarker(offset);
            UpdateElevationFill();
        }

        private void UpdateTipMarker(Vector2 offset)
        {
            if (tipMarker == null)
            {
                return;
            }

            var parent = (RectTransform)tipMarker.parent;
            var halfWidth = parent.rect.width * 0.5f;
            var halfHeight = parent.rect.height * 0.5f;
            tipMarker.anchoredPosition = new Vector2(offset.x * halfWidth, offset.y * halfHeight);
        }

        private void UpdateElevationFill()
        {
            if (elevationFill == null)
            {
                return;
            }

            var parent = (RectTransform)elevationFill.parent;
            var normalized = Mathf.Clamp01(router.ElevationDegrees / PracticeControlLayout.MaximumElevationDegrees);
            elevationFill.offsetMax = new Vector2(0f, Mathf.Max(10f, parent.rect.height * normalized));
        }

        private static string DescribeTip(Vector2 offset)
        {
            var vertical = offset.y > 0.25f ? "follow" : offset.y < -0.25f ? "draw" : "centre";
            var horizontal = offset.x > 0.25f ? " + right" : offset.x < -0.25f ? " + left" : string.Empty;
            return vertical + horizontal;
        }

        private static RectTransform CreatePanel(string name, RectTransform parent, Rect viewportRect, Color color)
        {
            var rect = CreateChildRect(name, parent);
            rect.anchorMin = new Vector2(viewportRect.xMin, viewportRect.yMin);
            rect.anchorMax = new Vector2(viewportRect.xMax, viewportRect.yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            AddImage(rect, color);
            return rect;
        }

        private static RectTransform CreateChildRect(string name, Transform parent)
        {
            var created = new GameObject(name, typeof(RectTransform));
            created.transform.SetParent(parent, worldPositionStays: false);
            return (RectTransform)created.transform;
        }

        private static Image AddImage(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateLabel(string name, RectTransform parent, Vector2 anchor, int fontSize, TextAnchor alignment)
        {
            var rect = CreateChildRect(name, parent);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1040f, 70f);

            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
