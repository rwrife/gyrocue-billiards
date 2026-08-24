using UnityEngine;

namespace GyroCue.UI
{
    /// <summary>Keeps a RectTransform inside the current device safe area at runtime.</summary>
    [DisallowMultipleComponent]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.safeArea != lastSafeArea || screenSize != lastScreenSize)
            {
                Apply();
            }
        }

        public void Apply()
        {
            var rect = transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            var safe = SafeAreaLayout.Normalize(
                Screen.safeArea,
                new Vector2(Screen.width, Screen.height));
            rect.anchorMin = safe.min;
            rect.anchorMax = safe.max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            lastSafeArea = Screen.safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
