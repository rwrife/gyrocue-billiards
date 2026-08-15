using UnityEngine;

namespace GyroCue.UI
{
    public enum HudStatusTone
    {
        Info = 0,
        Warning = 1,
        Success = 2,
        Danger = 3
    }

    public readonly struct MinimalHudSnapshot
    {
        public MinimalHudSnapshot(string turnText, string powerText, string statusText, HudStatusTone statusTone, bool isTerminal)
        {
            TurnText = turnText;
            PowerText = powerText;
            StatusText = statusText;
            StatusTone = statusTone;
            IsTerminal = isTerminal;
        }

        public string TurnText { get; }

        public string PowerText { get; }

        public string StatusText { get; }

        public HudStatusTone StatusTone { get; }

        public bool IsTerminal { get; }
    }

    /// <summary>
    /// Headless-safe HUD state formatter for turn, power, foul, and terminal match text.
    /// Keep gameplay systems coupled to this model (instead of UI primitives) so state
    /// transitions remain easy to test in edit mode.
    /// </summary>
    public sealed class MinimalHudState
    {
        private const string DefaultStatusText = "Line up your shot.";

        private int activePlayerIndex;
        private int turnNumber = 1;
        private float previewPower01;
        private bool isTerminal;
        private string statusText = DefaultStatusText;
        private HudStatusTone statusTone = HudStatusTone.Info;

        public int ActivePlayerIndex => activePlayerIndex;

        public int TurnNumber => turnNumber;

        public float PreviewPower01 => previewPower01;

        public bool IsTerminal => isTerminal;

        public MinimalHudSnapshot Snapshot => new MinimalHudSnapshot(
            $"Turn {turnNumber} • Player {activePlayerIndex + 1}",
            $"Power {Mathf.RoundToInt(previewPower01 * 100f)}%",
            statusText,
            statusTone,
            isTerminal);

        public void ResetMatch(bool playerOneStarts = true)
        {
            activePlayerIndex = playerOneStarts ? 0 : 1;
            turnNumber = 1;
            previewPower01 = 0f;
            isTerminal = false;
            statusText = DefaultStatusText;
            statusTone = HudStatusTone.Info;
        }

        public void SetPreviewPower(float normalizedPower)
        {
            previewPower01 = Mathf.Clamp01(normalizedPower);
        }

        public void SetTurnContext(int playerIndex, int currentTurnNumber, bool requiresCueBallPlacement)
        {
            activePlayerIndex = Mathf.Max(0, playerIndex);
            turnNumber = Mathf.Max(1, currentTurnNumber);

            if (isTerminal)
            {
                return;
            }

            statusText = requiresCueBallPlacement
                ? $"Player {activePlayerIndex + 1}: foul recovery — place the cue ball."
                : $"Player {activePlayerIndex + 1} to shoot.";
            statusTone = requiresCueBallPlacement ? HudStatusTone.Warning : HudStatusTone.Info;
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
            activePlayerIndex = Mathf.Max(0, resultingPlayerIndex);
            turnNumber = Mathf.Max(1, resultingTurnNumber);

            if (wonMatch)
            {
                isTerminal = true;
                statusText = $"Player {activePlayerIndex + 1} wins the rack.";
                statusTone = HudStatusTone.Success;
                previewPower01 = 0f;
                return;
            }

            if (lostMatch)
            {
                isTerminal = true;
                statusText = $"Player {activePlayerIndex + 1} loses on foul.";
                statusTone = HudStatusTone.Danger;
                previewPower01 = 0f;
                return;
            }

            isTerminal = false;

            if (committedFoul)
            {
                statusText = requiresCueBallPlacement
                    ? $"Foul. Player {activePlayerIndex + 1} places cue ball."
                    : $"Foul. Player {activePlayerIndex + 1} to shoot.";
                statusTone = HudStatusTone.Warning;
                return;
            }

            statusText = keepTurn
                ? $"Player {activePlayerIndex + 1} keeps the turn."
                : $"Turn over. Player {activePlayerIndex + 1} to shoot.";
            statusTone = HudStatusTone.Info;
        }

        public void SetStatusMessage(string message, HudStatusTone tone = HudStatusTone.Info)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            statusText = message.Trim();
            statusTone = tone;
        }
    }
}
