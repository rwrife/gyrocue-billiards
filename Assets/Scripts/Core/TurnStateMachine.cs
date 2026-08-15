namespace GyroCue.Core
{
    /// <summary>
    /// Lightweight match flow state machine for a basic 8-ball-style turn loop.
    /// It intentionally owns only turn/terminal transitions; physics and rule
    /// evaluation feed the result back in through <see cref="TryResolveTurn"/>.
    /// </summary>
    public sealed class TurnStateMachine
    {
        public TurnLifecyclePhase Phase { get; private set; } = TurnLifecyclePhase.AwaitingShot;

        /// <summary>
        /// Zero-based player index (0/1) for two-player local matches.
        /// </summary>
        public int CurrentPlayerIndex { get; private set; }

        /// <summary>
        /// Increments when control passes to the other player.
        /// </summary>
        public int TurnNumber { get; private set; } = 1;

        /// <summary>
        /// True when the active player must place the cue ball before taking a shot
        /// (for example, after a scratch/no-contact foul).
        /// </summary>
        public bool RequiresCueBallPlacement { get; private set; }

        public bool IsTerminal => Phase == TurnLifecyclePhase.MatchWon || Phase == TurnLifecyclePhase.MatchLost;

        /// <summary>
        /// Input should be considered locked while shot simulation or resolution is in progress,
        /// and after terminal outcomes.
        /// </summary>
        public bool InputLocked => Phase != TurnLifecyclePhase.AwaitingShot || RequiresCueBallPlacement;

        public void ResetMatch(bool playerOneStarts = true)
        {
            CurrentPlayerIndex = playerOneStarts ? 0 : 1;
            TurnNumber = 1;
            Phase = TurnLifecyclePhase.AwaitingShot;
            RequiresCueBallPlacement = false;
        }

        public bool TryBeginShot()
        {
            if (Phase != TurnLifecyclePhase.AwaitingShot || RequiresCueBallPlacement)
            {
                return false;
            }

            Phase = TurnLifecyclePhase.ShotSimulation;
            return true;
        }

        public bool TryMarkSimulationComplete()
        {
            if (Phase != TurnLifecyclePhase.ShotSimulation)
            {
                return false;
            }

            Phase = TurnLifecyclePhase.TurnResolution;
            return true;
        }

        public bool TryResolveTurn(TurnResolutionResult result)
        {
            if (Phase != TurnLifecyclePhase.TurnResolution)
            {
                return false;
            }

            if (result.WonMatch)
            {
                RequiresCueBallPlacement = false;
                Phase = TurnLifecyclePhase.MatchWon;
                return true;
            }

            if (result.LostMatch)
            {
                RequiresCueBallPlacement = false;
                Phase = TurnLifecyclePhase.MatchLost;
                return true;
            }

            var keepCurrentTurn = result.KeepTurn && !result.CommittedFoul;
            if (!keepCurrentTurn)
            {
                CurrentPlayerIndex = 1 - CurrentPlayerIndex;
                TurnNumber++;
            }

            RequiresCueBallPlacement = result.CommittedFoul;

            Phase = TurnLifecyclePhase.AwaitingShot;
            return true;
        }

        public bool TryCompleteCueBallPlacement()
        {
            if (Phase != TurnLifecyclePhase.AwaitingShot || !RequiresCueBallPlacement)
            {
                return false;
            }

            RequiresCueBallPlacement = false;
            return true;
        }
    }

    public enum TurnLifecyclePhase
    {
        AwaitingShot = 0,
        ShotSimulation = 1,
        TurnResolution = 2,
        MatchWon = 3,
        MatchLost = 4
    }

    public readonly struct TurnResolutionResult
    {
        public TurnResolutionResult(bool keepTurn, bool committedFoul, bool wonMatch, bool lostMatch)
        {
            KeepTurn = keepTurn;
            CommittedFoul = committedFoul;
            WonMatch = wonMatch;
            LostMatch = lostMatch;
        }

        public bool KeepTurn { get; }

        public bool CommittedFoul { get; }

        public bool WonMatch { get; }

        public bool LostMatch { get; }

        public static TurnResolutionResult Continue(bool keepTurn, bool committedFoul = false)
        {
            return new TurnResolutionResult(keepTurn, committedFoul, wonMatch: false, lostMatch: false);
        }

        public static TurnResolutionResult Win()
        {
            return new TurnResolutionResult(keepTurn: false, committedFoul: false, wonMatch: true, lostMatch: false);
        }

        public static TurnResolutionResult Loss()
        {
            return new TurnResolutionResult(keepTurn: false, committedFoul: false, wonMatch: false, lostMatch: true);
        }
    }
}
