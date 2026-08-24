using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GyroCue.UI
{
    /// <summary>Headless-safe title copy, kept apart from runtime UI construction.</summary>
    public static class TitleScreenCopy
    {
        public const string MissingSceneHint = "Gameplay scene is missing from Build Settings.";

        public static string ResolveStartPrompt(bool gameplaySceneAvailable, string gameplaySceneName)
        {
            if (!gameplaySceneAvailable)
            {
                return MissingSceneHint;
            }

            var trimmedName = string.IsNullOrWhiteSpace(gameplaySceneName)
                ? "the table"
                : gameplaySceneName.Trim();

            return $"Tap PLAY to open {trimmedName}.";
        }
    }

    /// <summary>Runtime-built, safe-area-aware title and honest playable-mode picker.</summary>
    public sealed class TitleScreenController : MonoBehaviour
    {
        private const int ReferenceWidthPixels = 1080;
        private const int ReferenceHeightPixels = 1920;
        private const string SelectedModePreference = "gyrocue.selected-mode";

        [Header("Copy")]
        [SerializeField]
        private string titleText = "GYROCUE";

        [SerializeField]
        private string subtitleText = "Mobile Billiards";

        [Header("Palette")]
        [SerializeField]
        private Color backgroundColor = new Color(0.04f, 0.16f, 0.11f, 1f);

        [SerializeField]
        private Color accentColor = new Color(0.36f, 0.78f, 0.52f, 1f);

        private readonly GameFlowState flow = new GameFlowState();
        private Text statusLabel;
        private Text practiceModeLabel;

        private void Awake()
        {
            EnsureEventSystem();
            flow.SelectMode(GameModeCatalog.FromPersistedValue(
                PlayerPrefs.GetInt(SelectedModePreference, (int)GameMode.Practice)));
            BuildCanvas();
            RefreshLabels();
        }

        private void BuildCanvas()
        {
            var canvasObject = new GameObject("TitleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, worldPositionStays: false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidthPixels, ReferenceHeightPixels);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var canvasRect = (RectTransform)canvasObject.transform;
            var background = CreateChild("Background", canvasRect);
            Stretch(background);
            AddImage(background, backgroundColor);

            var safeRoot = CreateChild("SafeArea", canvasRect);
            Stretch(safeRoot);
            safeRoot.gameObject.AddComponent<SafeAreaFitter>();

            var title = CreateText("Title", safeRoot, titleText, 120, FontStyle.Bold, Color.white);
            Anchor(title, new Vector2(0.5f, 0.78f), new Vector2(900f, 200f));

            var subtitle = CreateText("Subtitle", safeRoot, subtitleText, 52, FontStyle.Normal, accentColor);
            Anchor(subtitle, new Vector2(0.5f, 0.69f), new Vector2(900f, 120f));

            var modeHeading = CreateText("ModeHeading", safeRoot, "SELECT MODE", 32, FontStyle.Bold, Color.white);
            Anchor(modeHeading, new Vector2(0.5f, 0.57f), new Vector2(900f, 80f));

            BuildPracticeModeButton(safeRoot);

            var comingSoon = CreateText(
                "ComingSoon",
                safeRoot,
                "LOCAL 8-BALL  •  COMING NEXT",
                30,
                FontStyle.Normal,
                new Color(1f, 1f, 1f, 0.48f));
            Anchor(comingSoon, new Vector2(0.5f, 0.43f), new Vector2(820f, 80f));

            BuildPlayButton(safeRoot);

            var status = CreateText("Status", safeRoot, string.Empty, 34, FontStyle.Normal, new Color(1f, 1f, 1f, 0.72f));
            Anchor(status, new Vector2(0.5f, 0.14f), new Vector2(960f, 120f));
            statusLabel = status.GetComponent<Text>();
        }

        private void BuildPracticeModeButton(RectTransform parent)
        {
            var buttonRect = CreateChild("PracticeModeButton", parent);
            Anchor(buttonRect, new Vector2(0.5f, 0.50f), new Vector2(720f, 110f));
            var image = AddImage(buttonRect, new Color(accentColor.r, accentColor.g, accentColor.b, 0.28f));
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(HandlePracticeSelected);

            var label = CreateText("Label", buttonRect, string.Empty, 40, FontStyle.Bold, Color.white);
            Stretch(label);
            practiceModeLabel = label.GetComponent<Text>();
        }

        private void BuildPlayButton(RectTransform parent)
        {
            var buttonRect = CreateChild("PlayButton", parent);
            Anchor(buttonRect, new Vector2(0.5f, 0.30f), new Vector2(520f, 150f));
            var image = AddImage(buttonRect, accentColor);
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(HandlePlayClicked);

            var label = CreateText("Label", buttonRect, "PLAY", 64, FontStyle.Bold, new Color(0.03f, 0.12f, 0.08f, 1f));
            Stretch(label);
        }

        private void HandlePracticeSelected()
        {
            if (flow.SelectMode(GameMode.Practice))
            {
                PlayerPrefs.SetInt(SelectedModePreference, (int)flow.SelectedMode);
                PlayerPrefs.Save();
                RefreshLabels();
            }
        }

        private void HandlePlayClicked()
        {
            var sceneName = GameModeCatalog.SceneName(flow.SelectedMode);
            if (!IsSceneAvailable(sceneName))
            {
                Debug.LogError($"{nameof(TitleScreenController)}: scene '{sceneName}' is not in Build Settings.", this);
                RefreshLabels();
                return;
            }

            if (flow.StartSelectedMode())
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        private void RefreshLabels()
        {
            var sceneName = GameModeCatalog.SceneName(flow.SelectedMode);
            if (practiceModeLabel != null)
            {
                practiceModeLabel.text = flow.SelectedMode == GameMode.Practice
                    ? "✓  PRACTICE"
                    : "PRACTICE";
            }

            if (statusLabel != null)
            {
                statusLabel.text = TitleScreenCopy.ResolveStartPrompt(IsSceneAvailable(sceneName), sceneName);
            }
        }

        private static bool IsSceneAvailable(string sceneName)
        {
            return !string.IsNullOrWhiteSpace(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        private static RectTransform CreateChild(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, worldPositionStays: false);
            return (RectTransform)child.transform;
        }

        private static RectTransform CreateText(string name, Transform parent, string content, int fontSize, FontStyle fontStyle, Color color)
        {
            var rect = CreateChild(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = ResolveFont();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return rect;
        }

        private static Image AddImage(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private static Font ResolveFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Font.CreateDynamicFontFromOSFont("Arial", 32);
        }
    }
}
