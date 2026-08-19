using GyroCue.Input;
using UnityEngine;
using UnityEngine.UI;

namespace GyroCue.UI
{
    /// <summary>
    /// Minimal mobile HUD presenter for turn, power, foul, and terminal match text.
    ///
    /// Wire this to three UI.Text fields on a world-space or screen-space canvas,
    /// then feed turn/rules transitions from gameplay orchestration scripts.
    /// </summary>
    public sealed class MinimalHudPresenter : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField]
        private Text turnLabel;

        [SerializeField]
        private Text powerLabel;

        [SerializeField]
        private Text statusLabel;

        [Header("Input Sources (optional)")]
        [SerializeField]
        private TouchAimSwipeController touchAimSwipeController;

        [SerializeField]
        private RemoteSensorInputAdapter remoteSensorInputAdapter;

        [SerializeField]
        private CueInputCoordinator cueInputCoordinator;

        [Header("Scale")]
        [SerializeField, Min(1)]
        private int baseTurnFontSize = 34;

        [SerializeField, Min(1)]
        private int basePowerFontSize = 30;

        [SerializeField, Min(1)]
        private int baseStatusFontSize = 28;

        [SerializeField, Min(0.1f)]
        private float minHudScale = 0.85f;

        [SerializeField, Min(0.1f)]
        private float maxHudScale = 1.3f;

        [Header("Status Colors")]
        [SerializeField]
        private Color infoColor = new Color(0.95f, 0.95f, 0.95f, 1f);

        [SerializeField]
        private Color warningColor = new Color(1f, 0.83f, 0.45f, 1f);

        [SerializeField]
        private Color successColor = new Color(0.5f, 0.95f, 0.58f, 1f);

        [SerializeField]
        private Color dangerColor = new Color(1f, 0.52f, 0.52f, 1f);

        private readonly MinimalHudState hudState = new MinimalHudState();

        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;

        public MinimalHudSnapshot CurrentSnapshot => hudState.Snapshot;

        /// <summary>
        /// Binds labels created at runtime, for scenes that build their HUD in code
        /// instead of authoring it in the inspector.
        /// </summary>
        public void ConfigureLabels(Text turn, Text power, Text status)
        {
            turnLabel = turn;
            powerLabel = power;
            statusLabel = status;
            RefreshAllLabels();
        }

        public void ResetForNewMatch(bool playerOneStarts = true)
        {
            hudState.ResetMatch(playerOneStarts);
            RefreshAllLabels();
        }

        public void SetTurnState(int playerIndex, int currentTurnNumber, bool requiresCueBallPlacement)
        {
            hudState.SetTurnContext(playerIndex, currentTurnNumber, requiresCueBallPlacement);
            RefreshAllLabels();
        }

        public void ApplyTurnResolution(
            bool keepTurn,
            bool committedFoul,
            bool wonMatch,
            bool lostMatch,
            bool requiresCueBallPlacement,
            int resultingPlayerIndex,
            int resultingTurnNumber)
        {
            hudState.ApplyTurnResolution(
                keepTurn,
                committedFoul,
                wonMatch,
                lostMatch,
                requiresCueBallPlacement,
                resultingPlayerIndex,
                resultingTurnNumber);
            RefreshAllLabels();
        }

        public void SetStatusMessage(string message, HudStatusTone tone = HudStatusTone.Info)
        {
            hudState.SetStatusMessage(message, tone);
            RefreshStatusLabel();
        }

        private void Awake()
        {
            ResolveReferences();
            hudState.ResetMatch(playerOneStarts: true);
            RefreshScaleIfNeeded(force: true);
            RefreshAllLabels();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (cueInputCoordinator != null)
            {
                cueInputCoordinator.ShotReleased += HandleShotReleased;
            }
        }

        private void OnDisable()
        {
            if (cueInputCoordinator != null)
            {
                cueInputCoordinator.ShotReleased -= HandleShotReleased;
            }
        }

        private void Update()
        {
            RefreshScaleIfNeeded(force: false);
            RefreshPowerPreview();
        }

        private void ResolveReferences()
        {
            if (cueInputCoordinator == null)
            {
                cueInputCoordinator = GetComponent<CueInputCoordinator>();
            }

            if (touchAimSwipeController == null)
            {
                touchAimSwipeController = GetComponent<TouchAimSwipeController>();
            }

            if (remoteSensorInputAdapter == null)
            {
                remoteSensorInputAdapter = GetComponent<RemoteSensorInputAdapter>();
            }
        }

        private void RefreshPowerPreview()
        {
            var previewPower01 = 0f;

            if (remoteSensorInputAdapter != null && remoteSensorInputAdapter.IsRemoteControlActive)
            {
                previewPower01 = remoteSensorInputAdapter.PreviewPower01;
            }
            else if (touchAimSwipeController != null && touchAimSwipeController.TouchInputEnabled && !touchAimSwipeController.InputLocked)
            {
                previewPower01 = touchAimSwipeController.PreviewPower01;
            }

            hudState.SetPreviewPower(previewPower01);
            RefreshPowerLabel();
        }

        private void HandleShotReleased(ShotCommand _)
        {
            hudState.SetPreviewPower(0f);
            RefreshPowerLabel();
        }

        private void RefreshScaleIfNeeded(bool force)
        {
            var currentWidth = Screen.width;
            var currentHeight = Screen.height;

            if (!force && currentWidth == lastScreenWidth && currentHeight == lastScreenHeight)
            {
                return;
            }

            lastScreenWidth = currentWidth;
            lastScreenHeight = currentHeight;

            var scale = HudScaleUtility.ResolveScaleFactor(currentWidth, currentHeight, minHudScale, maxHudScale);

            if (turnLabel != null)
            {
                turnLabel.fontSize = HudScaleUtility.ResolveScaledFontSize(baseTurnFontSize, scale);
            }

            if (powerLabel != null)
            {
                powerLabel.fontSize = HudScaleUtility.ResolveScaledFontSize(basePowerFontSize, scale);
            }

            if (statusLabel != null)
            {
                statusLabel.fontSize = HudScaleUtility.ResolveScaledFontSize(baseStatusFontSize, scale);
            }
        }

        private void RefreshAllLabels()
        {
            RefreshTurnLabel();
            RefreshPowerLabel();
            RefreshStatusLabel();
        }

        private void RefreshTurnLabel()
        {
            if (turnLabel == null)
            {
                return;
            }

            turnLabel.text = hudState.Snapshot.TurnText;
        }

        private void RefreshPowerLabel()
        {
            if (powerLabel == null)
            {
                return;
            }

            powerLabel.text = hudState.Snapshot.PowerText;
        }

        private void RefreshStatusLabel()
        {
            if (statusLabel == null)
            {
                return;
            }

            var snapshot = hudState.Snapshot;
            statusLabel.text = snapshot.StatusText;
            statusLabel.color = ResolveStatusColor(snapshot.StatusTone);
        }

        private Color ResolveStatusColor(HudStatusTone tone)
        {
            switch (tone)
            {
                case HudStatusTone.Warning:
                    return warningColor;
                case HudStatusTone.Success:
                    return successColor;
                case HudStatusTone.Danger:
                    return dangerColor;
                default:
                    return infoColor;
            }
        }
    }
}
