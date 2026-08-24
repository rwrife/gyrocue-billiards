using GyroCue.Practice;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GyroCue.UI
{
    /// <summary>Minimal in-table shell for pause, resume, restart, and title return.</summary>
    [DisallowMultipleComponent]
    public sealed class PracticeShellController : MonoBehaviour
    {
        private const string TitleSceneName = "Title";

        private readonly GameFlowState flow = new GameFlowState();
        private PracticeSessionController session;
        private GameObject pauseOverlay;

        public GameFlowState Flow => flow;

        public bool IsPaused => flow.Phase == GameFlowPhase.Paused;

        private void Awake()
        {
            Time.timeScale = 1f;
            flow.SelectMode(GameMode.Practice);
            flow.StartSelectedMode();
            EnsureEventSystem();
            BuildCanvas();
        }

        public void Configure(PracticeSessionController sessionController)
        {
            session = sessionController;
        }

        public void Pause()
        {
            if (!flow.Pause())
            {
                return;
            }

            session?.SetPaused(true);
            Time.timeScale = 0f;
            SetOverlayVisible(true);
        }

        public void Resume()
        {
            if (!flow.Resume())
            {
                return;
            }

            Time.timeScale = 1f;
            session?.SetPaused(false);
            SetOverlayVisible(false);
        }

        public void RestartPractice()
        {
            if (!flow.Restart())
            {
                return;
            }

            Time.timeScale = 1f;
            session?.SetPaused(false);
            session?.RestartSession();
            SetOverlayVisible(false);
        }

        public void ReturnToTitle()
        {
            if (!Application.CanStreamedLevelBeLoaded(TitleSceneName))
            {
                Debug.LogError($"{nameof(PracticeShellController)}: scene '{TitleSceneName}' is not in Build Settings.", this);
                return;
            }

            flow.ReturnToTitle();
            Time.timeScale = 1f;
            session?.SetPaused(false);
            SceneManager.LoadScene(TitleSceneName);
        }

        private void OnDestroy()
        {
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
            }
        }

        private void BuildCanvas()
        {
            var canvasObject = new GameObject("PracticeShell", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var canvasRect = (RectTransform)canvasObject.transform;
            var safeRoot = CreateChild("SafeArea", canvasRect);
            Stretch(safeRoot);
            safeRoot.gameObject.AddComponent<SafeAreaFitter>();

            CreateButton(
                "PauseButton",
                safeRoot,
                new Vector2(1f, 1f),
                new Vector2(-28f, -28f),
                new Vector2(210f, 92f),
                "PAUSE",
                Pause);

            pauseOverlay = new GameObject("PauseOverlay", typeof(RectTransform), typeof(Image));
            pauseOverlay.transform.SetParent(canvasRect, false);
            var overlayRect = (RectTransform)pauseOverlay.transform;
            Stretch(overlayRect);
            pauseOverlay.GetComponent<Image>().color = new Color(0.01f, 0.03f, 0.025f, 0.90f);

            var overlaySafe = CreateChild("SafeArea", overlayRect);
            Stretch(overlaySafe);
            overlaySafe.gameObject.AddComponent<SafeAreaFitter>();

            var heading = CreateText("Heading", overlaySafe, "PRACTICE PAUSED", 64, FontStyle.Bold);
            Anchor(heading, new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(900f, 120f));
            var note = CreateText("Note", overlaySafe, "The table is frozen. Choose where to continue.", 30, FontStyle.Normal);
            Anchor(note, new Vector2(0.5f, 0.61f), Vector2.zero, new Vector2(960f, 100f));

            CreateButton("ResumeButton", overlaySafe, new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(620f, 128f), "RESUME", Resume);
            CreateButton("RestartButton", overlaySafe, new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(620f, 128f), "RESTART PRACTICE", RestartPractice);
            CreateButton("TitleButton", overlaySafe, new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(620f, 128f), "RETURN TO TITLE", ReturnToTitle);

            pauseOverlay.SetActive(false);
        }

        private void SetOverlayVisible(bool visible)
        {
            if (pauseOverlay != null)
            {
                pauseOverlay.SetActive(visible);
            }
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        private static Button CreateButton(
            string name,
            RectTransform parent,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            string label,
            UnityEngine.Events.UnityAction action)
        {
            var rect = CreateChild(name, parent);
            Anchor(rect, anchor, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.20f, 0.64f, 0.40f, 0.96f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            var text = CreateText("Label", rect, label, 36, FontStyle.Bold);
            Stretch(text);
            return button;
        }

        private static RectTransform CreateText(string name, Transform parent, string value, int fontSize, FontStyle style)
        {
            var rect = CreateChild(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return rect;
        }

        private static RectTransform CreateChild(string name, Transform parent)
        {
            var created = new GameObject(name, typeof(RectTransform));
            created.transform.SetParent(parent, false);
            return (RectTransform)created.transform;
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(anchor.x >= 0.99f ? 1f : 0.5f, anchor.y >= 0.99f ? 1f : 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
