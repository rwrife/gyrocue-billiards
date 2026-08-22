using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GyroCue.Core;

namespace GyroCue.Practice
{
    public enum EightBallGroup
    {
        Unassigned = 0,
        Solids = 1,
        Stripes = 2,
        Eight = 3,
        Cue = 4
    }

    public enum EightBallFoulReason
    {
        None = 0,
        NoContact = 1,
        Scratch = 2,
        WrongFirstContact = 3,
        BallOffTable = 4,
        IllegalEightBall = 5
    }

    public static class EightBallBall
    {
        public const int CueBallNumber = 0;
        public const int EightBallNumber = 8;
        public const int FirstObjectBallNumber = 1;
        public const int LastObjectBallNumber = 15;

        public static EightBallGroup GroupFor(int ballNumber)
        {
            if (ballNumber == CueBallNumber)
            {
                return EightBallGroup.Cue;
            }

            if (ballNumber >= 1 && ballNumber <= 7)
            {
                return EightBallGroup.Solids;
            }

            if (ballNumber == EightBallNumber)
            {
                return EightBallGroup.Eight;
            }

            if (ballNumber >= 9 && ballNumber <= 15)
            {
                return EightBallGroup.Stripes;
            }

            return EightBallGroup.Unassigned;
        }

        public static bool IsObjectBall(int ballNumber)
        {
            return ballNumber >= FirstObjectBallNumber && ballNumber <= LastObjectBallNumber;
        }
    }

    /// <summary>
    /// Deterministic casual rack. The 8-ball is in the centre of row three and the
    /// two back corners contain opposite groups. Ball numbers are stable identities,
    /// not display names or assumptions about hierarchy order.
    /// </summary>
    public static class EightBallRack
    {
        private static readonly int[] Numbers =
        {
            1,
            9, 2,
            10, 8, 3,
            4, 11, 12, 5,
            13, 6, 14, 15, 7
        };

        private static readonly ReadOnlyCollection<int> ReadOnlyNumbers = Array.AsReadOnly(Numbers);

        public static IReadOnlyList<int> StandardBallNumbers => ReadOnlyNumbers;

        public static int RackSlotFor(int ballNumber)
        {
            for (var i = 0; i < Numbers.Length; i++)
            {
                if (Numbers[i] == ballNumber)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    public readonly struct EightBallShotRecord
    {
        public const int NoBall = -1;

        public EightBallShotRecord(
            int firstContactBallNumber,
            IReadOnlyList<int> pocketedBallNumbers = null,
            bool cueBallScratched = false,
            IReadOnlyList<int> offTableBallNumbers = null)
        {
            FirstContactBallNumber = firstContactBallNumber;
            PocketedBallNumbers = pocketedBallNumbers ?? Array.Empty<int>();
            CueBallScratched = cueBallScratched;
            OffTableBallNumbers = offTableBallNumbers ?? Array.Empty<int>();
        }

        public int FirstContactBallNumber { get; }

        /// <summary>Object balls in pocket-event order; the order resolves open-table assignment.</summary>
        public IReadOnlyList<int> PocketedBallNumbers { get; }

        public bool CueBallScratched { get; }

        public IReadOnlyList<int> OffTableBallNumbers { get; }
    }

    public readonly struct EightBallShotResolution
    {
        public EightBallShotResolution(
            TurnResolutionResult turnResolution,
            EightBallFoulReason foulReason,
            string foulMessage,
            EightBallGroup assignedGroup)
        {
            TurnResolution = turnResolution;
            FoulReason = foulReason;
            FoulMessage = foulMessage ?? string.Empty;
            AssignedGroup = assignedGroup;
        }

        public TurnResolutionResult TurnResolution { get; }

        public EightBallFoulReason FoulReason { get; }

        /// <summary>Concise player-facing copy suitable for the match HUD.</summary>
        public string FoulMessage { get; }

        public EightBallGroup AssignedGroup { get; }
    }

    /// <summary>
    /// Pure two-player casual 8-ball rules core for the 3D table. Physics code reports
    /// a shot record after the balls settle; this class owns groups, scores, remaining
    /// balls, fouls, and terminal 8-ball outcomes.
    /// </summary>
    public sealed class EightBallRules
    {
        private readonly bool[] ballOnTable = new bool[16];
        private readonly EightBallGroup[] playerGroups = new EightBallGroup[2];

        public EightBallRules()
        {
            ResetMatch();
        }

        public int RemainingSolids => CountRemaining(EightBallGroup.Solids);

        public int RemainingStripes => CountRemaining(EightBallGroup.Stripes);

        public void ResetMatch()
        {
            Array.Clear(ballOnTable, 0, ballOnTable.Length);
            for (var number = EightBallBall.FirstObjectBallNumber; number <= EightBallBall.LastObjectBallNumber; number++)
            {
                ballOnTable[number] = true;
            }

            playerGroups[0] = EightBallGroup.Unassigned;
            playerGroups[1] = EightBallGroup.Unassigned;
        }

        public EightBallGroup GetPlayerGroup(int playerIndex)
        {
            ValidatePlayerIndex(playerIndex);
            return playerGroups[playerIndex];
        }

        public int GetPlayerScore(int playerIndex)
        {
            var group = GetPlayerGroup(playerIndex);
            switch (group)
            {
                case EightBallGroup.Solids:
                    return 7 - RemainingSolids;
                case EightBallGroup.Stripes:
                    return 7 - RemainingStripes;
                default:
                    return 0;
            }
        }

        public bool IsBallOnTable(int ballNumber)
        {
            return ballNumber >= 0 && ballNumber < ballOnTable.Length && ballOnTable[ballNumber];
        }

        public EightBallShotResolution ResolveShot(int shootingPlayerIndex, EightBallShotRecord shot)
        {
            ValidatePlayerIndex(shootingPlayerIndex);
            shot = Normalize(shot);

            var groupBeforeShot = playerGroups[shootingPlayerIndex];
            var foul = ResolveNonTerminalFoul(groupBeforeShot, shot);
            var eightPocketed = Contains(shot.PocketedBallNumbers, EightBallBall.EightBallNumber);
            var eightLeftTable = Contains(shot.OffTableBallNumbers, EightBallBall.EightBallNumber);

            if (eightPocketed || eightLeftTable)
            {
                var canShootEight = groupBeforeShot != EightBallGroup.Unassigned &&
                                    CountRemaining(groupBeforeShot) == 0;
                var legalEight = eightPocketed &&
                                 !eightLeftTable &&
                                 !shot.CueBallScratched &&
                                 foul == EightBallFoulReason.None &&
                                 canShootEight &&
                                 shot.FirstContactBallNumber == EightBallBall.EightBallNumber;

                ballOnTable[EightBallBall.EightBallNumber] = false;
                if (legalEight)
                {
                    return new EightBallShotResolution(
                        TurnResolutionResult.Win(),
                        EightBallFoulReason.None,
                        string.Empty,
                        EightBallGroup.Unassigned);
                }

                return new EightBallShotResolution(
                    TurnResolutionResult.Loss(),
                    EightBallFoulReason.IllegalEightBall,
                    "Loss: illegal 8-ball",
                    EightBallGroup.Unassigned);
            }

            var assignedThisShot = EightBallGroup.Unassigned;
            if (foul == EightBallFoulReason.None && groupBeforeShot == EightBallGroup.Unassigned)
            {
                assignedThisShot = FirstScoredPocketedGroup(shot);
                if (assignedThisShot == EightBallGroup.Solids || assignedThisShot == EightBallGroup.Stripes)
                {
                    playerGroups[shootingPlayerIndex] = assignedThisShot;
                    playerGroups[1 - shootingPlayerIndex] = Opposite(assignedThisShot);
                }
            }

            var activeGroup = playerGroups[shootingPlayerIndex];
            var pocketedOwnBall = ContainsScoredPocketedGroup(shot, activeGroup);
            RemoveLegallyPocketedBalls(shot);
            var committedFoul = foul != EightBallFoulReason.None;
            var turnResolution = TurnResolutionResult.Continue(
                keepTurn: pocketedOwnBall && !committedFoul,
                committedFoul: committedFoul);

            return new EightBallShotResolution(
                turnResolution,
                foul,
                FoulMessageFor(foul),
                assignedThisShot);
        }

        private EightBallShotRecord Normalize(EightBallShotRecord shot)
        {
            var rawPocketed = shot.PocketedBallNumbers ?? Array.Empty<int>();
            var rawOffTable = shot.OffTableBallNumbers ?? Array.Empty<int>();
            var pocketed = FilterCurrentObjectBalls(rawPocketed, includeCueBall: false);
            var offTable = FilterCurrentObjectBalls(rawOffTable, includeCueBall: true);
            var firstContact = IsCurrentObjectBall(shot.FirstContactBallNumber)
                ? shot.FirstContactBallNumber
                : EightBallShotRecord.NoBall;

            return new EightBallShotRecord(
                firstContact,
                pocketed,
                shot.CueBallScratched,
                offTable);
        }

        private IReadOnlyList<int> FilterCurrentObjectBalls(
            IReadOnlyList<int> numbers,
            bool includeCueBall)
        {
            var filtered = new List<int>();
            var seen = new bool[EightBallBall.LastObjectBallNumber + 1];
            for (var i = 0; i < numbers.Count; i++)
            {
                var number = numbers[i];
                var isCueBall = includeCueBall && number == EightBallBall.CueBallNumber;
                if ((!isCueBall && !IsCurrentObjectBall(number)) ||
                    number < 0 ||
                    number >= seen.Length ||
                    seen[number])
                {
                    continue;
                }

                seen[number] = true;
                filtered.Add(number);
            }

            return filtered;
        }

        private bool IsCurrentObjectBall(int ballNumber)
        {
            return EightBallBall.IsObjectBall(ballNumber) && ballOnTable[ballNumber];
        }

        private EightBallFoulReason ResolveNonTerminalFoul(EightBallGroup activeGroup, EightBallShotRecord shot)
        {
            if (shot.CueBallScratched || Contains(shot.OffTableBallNumbers, EightBallBall.CueBallNumber))
            {
                return EightBallFoulReason.Scratch;
            }

            if (shot.OffTableBallNumbers.Count > 0)
            {
                return EightBallFoulReason.BallOffTable;
            }

            if (!EightBallBall.IsObjectBall(shot.FirstContactBallNumber))
            {
                return EightBallFoulReason.NoContact;
            }

            var firstGroup = EightBallBall.GroupFor(shot.FirstContactBallNumber);
            if (activeGroup == EightBallGroup.Unassigned)
            {
                return firstGroup == EightBallGroup.Eight
                    ? EightBallFoulReason.WrongFirstContact
                    : EightBallFoulReason.None;
            }

            var requiredFirstGroup = CountRemaining(activeGroup) == 0
                ? EightBallGroup.Eight
                : activeGroup;
            return firstGroup == requiredFirstGroup
                ? EightBallFoulReason.None
                : EightBallFoulReason.WrongFirstContact;
        }

        private void RemoveLegallyPocketedBalls(EightBallShotRecord shot)
        {
            for (var i = 0; i < shot.PocketedBallNumbers.Count; i++)
            {
                var number = shot.PocketedBallNumbers[i];
                if (!EightBallBall.IsObjectBall(number) || number == EightBallBall.EightBallNumber)
                {
                    continue;
                }

                // A ball reported as leaving the table is re-spotted instead of scored.
                if (Contains(shot.OffTableBallNumbers, number))
                {
                    ballOnTable[number] = true;
                    continue;
                }

                ballOnTable[number] = false;
            }
        }

        private int CountRemaining(EightBallGroup group)
        {
            var remaining = 0;
            for (var number = EightBallBall.FirstObjectBallNumber; number <= EightBallBall.LastObjectBallNumber; number++)
            {
                if (ballOnTable[number] && EightBallBall.GroupFor(number) == group)
                {
                    remaining++;
                }
            }

            return remaining;
        }

        private EightBallGroup FirstScoredPocketedGroup(EightBallShotRecord shot)
        {
            for (var i = 0; i < shot.PocketedBallNumbers.Count; i++)
            {
                var number = shot.PocketedBallNumbers[i];
                if (!IsNewlyScoredBall(number, shot.OffTableBallNumbers))
                {
                    continue;
                }

                var group = EightBallBall.GroupFor(number);
                if (group == EightBallGroup.Solids || group == EightBallGroup.Stripes)
                {
                    return group;
                }
            }

            return EightBallGroup.Unassigned;
        }

        private bool ContainsScoredPocketedGroup(EightBallShotRecord shot, EightBallGroup group)
        {
            if (group != EightBallGroup.Solids && group != EightBallGroup.Stripes)
            {
                return false;
            }

            for (var i = 0; i < shot.PocketedBallNumbers.Count; i++)
            {
                var number = shot.PocketedBallNumbers[i];
                if (IsNewlyScoredBall(number, shot.OffTableBallNumbers) &&
                    EightBallBall.GroupFor(number) == group)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsNewlyScoredBall(int number, IReadOnlyList<int> offTableBallNumbers)
        {
            return EightBallBall.IsObjectBall(number) &&
                   number != EightBallBall.EightBallNumber &&
                   ballOnTable[number] &&
                   !Contains(offTableBallNumbers, number);
        }

        private static bool Contains(IReadOnlyList<int> numbers, int value)
        {
            for (var i = 0; i < numbers.Count; i++)
            {
                if (numbers[i] == value)
                {
                    return true;
                }
            }

            return false;
        }

        private static EightBallGroup Opposite(EightBallGroup group)
        {
            return group == EightBallGroup.Solids
                ? EightBallGroup.Stripes
                : EightBallGroup.Solids;
        }

        private static string FoulMessageFor(EightBallFoulReason foul)
        {
            switch (foul)
            {
                case EightBallFoulReason.NoContact:
                    return "Foul: no ball contacted";
                case EightBallFoulReason.Scratch:
                    return "Foul: Scratch";
                case EightBallFoulReason.WrongFirstContact:
                    return "Foul: wrong ball contacted first";
                case EightBallFoulReason.BallOffTable:
                    return "Foul: ball left the table";
                default:
                    return string.Empty;
            }
        }

        private static void ValidatePlayerIndex(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(playerIndex), "Eight-ball matches have exactly two players.");
            }
        }
    }
}
