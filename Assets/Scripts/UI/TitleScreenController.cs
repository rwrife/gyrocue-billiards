using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GyroCue.UI
{
    /// <summary>
    /// Headless-safe title screen copy, kept apart from UI construction so the wording
    /// stays verifiable in edit mode.
    /// </summary>
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

    /// <summary>
    /// Self-contained title screen that builds its own canvas at runtime.
    ///
    /// Nothing needs wiring in the inspector: drop this component on an empty
    /// GameObject and press play. It exists so the project has one scene that
    /// visibly runs while table gameplay is still placeholder geometry.
    /// </summary>
    public sealed class TitleScreenController : MonoBehaviour
    {
        private const int ReferenceWidthPixels = 1080;
        private const int ReferenceHeightPixels = 1920;

        [Header("Navigation")]
        [SerializeField]
        private string gameplaySceneName = "Practice";

        [Header("Copy")]
        [SerializeField]
        private string titleText = "GYROCUE";

        [SerializeField]
        private string subtitleText = "Practice Mode";

        [Header("Palette")]
        [SerializeField]
        private Color backgroundColor = new Color(0.04f, 0.16f, 0.11f, 1f);

        [SerializeField]
        private Color accentColor = new Color(0.36f, 0.78f, 0.52f, 1f);

        private Text statusLabel;

        private void Awake()
        {
            EnsureEventSystem();
            BuildCanvas();
            RefreshStatusLabel();
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

            var title = CreateText("Title", canvasRect, titleText, 120, FontStyle.Bold, Color.white);
            Anchor(title, new Vector2(0.5f, 0.72f), new Vector2(900f, 200f));

            var subtitle = CreateText("Subtitle", canvasRect, subtitleText, 52, FontStyle.Normal, accentColor);
            Anchor(subtitle, new Vector2(0.5f, 0.63f), new Vector2(900f, 120f));

            BuildPlayButton(canvasRect);

            var status = CreateText("Status", canvasRect, string.Empty, 36, FontStyle.Normal, new Color(1f, 1f, 1f, 0.7f));
            Anchor(status, new Vector2(0.5f, 0.16f), new Vector2(960f, 120f));
            statusLabel = status.GetComponent<Text>();
        }

        private void BuildPlayButton(RectTransform canvasRect)
        {
            var buttonRect = CreateChild("PlayButton", canvasRect);
            Anchor(buttonRect, new Vector2(0.5f, 0.42f), new Vector2(520f, 160f));

            var image = AddImage(buttonRect, accentColor);

            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(HandlePlayClicked);

            var label = CreateText("Label", buttonRect, "PLAY", 64, FontStyle.Bold, new Color(0.03f, 0.12f, 0.08f, 1f));
            Stretch(label);
        }

        private void HandlePlayClicked()
        {
            if (!IsGameplaySceneAvailable())
            {
                Debug.LogError(
                    $"{nameof(TitleScreenController)}: scene '{gameplaySceneName}' is not in Build Settings.",
                    this);
                RefreshStatusLabel();
                return;
            }

            SceneManager.LoadScene(gameplaySceneName);
        }

        private void RefreshStatusLabel()
        {
            if (statusLabel == null)
            {
                return;
            }

            statusLabel.text = TitleScreenCopy.ResolveStartPrompt(IsGameplaySceneAvailable(), gameplaySceneName);
        }

        private bool IsGameplaySceneAvailable()
        {
            return !string.IsNullOrWhiteSpace(gameplaySceneName)
                && Application.CanStreamedLevelBeLoaded(gameplaySceneName);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static RectTransform CreateChild(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, worldPositionStays: false);
            return (RectTransform)child.transform;
        }

        private static RectTransform CreateText(
            string name,
            Transform parent,
            string content,
            int fontSize,
            FontStyle fontStyle,
            Color color)
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
            // Unity 2022 ships LegacyRuntime.ttf as the built-in UI.Text font.
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Font.CreateDynamicFontFromOSFont("Arial", 32);
        }
    }
}
