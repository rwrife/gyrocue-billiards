namespace GyroCue.UI
{
    public enum GameMode
    {
        Practice = 0
    }

    public enum GameFlowPhase
    {
        Title = 0,
        Playing = 1,
        Paused = 2
    }

    /// <summary>Single source of truth for modes that are actually playable.</summary>
    public static class GameModeCatalog
    {
        public static bool IsAvailable(GameMode mode)
        {
            return mode == GameMode.Practice;
        }

        public static string DisplayName(GameMode mode)
        {
            return mode == GameMode.Practice ? "PRACTICE" : "UNAVAILABLE";
        }

        public static string SceneName(GameMode mode)
        {
            return mode == GameMode.Practice ? "Practice" : string.Empty;
        }

        public static GameMode FromPersistedValue(int value)
        {
            var mode = (GameMode)value;
            return IsAvailable(mode) ? mode : GameMode.Practice;
        }
    }

    /// <summary>
    /// Engine-independent navigation state used by both title and table UI. Invalid
    /// transitions fail closed so repeated taps cannot double-load or unpause a scene.
    /// </summary>
    public sealed class GameFlowState
    {
        public GameMode SelectedMode { get; private set; } = GameMode.Practice;

        public GameFlowPhase Phase { get; private set; } = GameFlowPhase.Title;

        public int SessionRevision { get; private set; }

        public bool SelectMode(GameMode mode)
        {
            if (Phase != GameFlowPhase.Title || !GameModeCatalog.IsAvailable(mode))
            {
                return false;
            }

            SelectedMode = mode;
            return true;
        }

        public bool StartSelectedMode()
        {
            if (Phase != GameFlowPhase.Title || !GameModeCatalog.IsAvailable(SelectedMode))
            {
                return false;
            }

            Phase = GameFlowPhase.Playing;
            return true;
        }

        public bool Pause()
        {
            if (Phase != GameFlowPhase.Playing)
            {
                return false;
            }

            Phase = GameFlowPhase.Paused;
            return true;
        }

        public bool Resume()
        {
            if (Phase != GameFlowPhase.Paused)
            {
                return false;
            }

            Phase = GameFlowPhase.Playing;
            return true;
        }

        public bool Restart()
        {
            if (Phase != GameFlowPhase.Playing && Phase != GameFlowPhase.Paused)
            {
                return false;
            }

            SessionRevision++;
            Phase = GameFlowPhase.Playing;
            return true;
        }

        public bool ReturnToTitle()
        {
            if (Phase == GameFlowPhase.Title)
            {
                return false;
            }

            Phase = GameFlowPhase.Title;
            return true;
        }
    }
}
