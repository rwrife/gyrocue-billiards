using GyroCue.Core;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class GameplayTestHarnessTests
    {
        [Test]
        public void RunShotScenario_ObjectBallPocketedWithoutFoul_KeepsTurn()
        {
            var harness = new GameplayTurnHarness();

            try
            {
                var outcome = harness.RunShotScenario(new ShotScenario(
                    legalContact: true,
                    pocketObjectBall: true,
                    pocketEightBall: false,
                    cueBallScratch: false,
                    duplicateCueScratch: false));

                Assert.That(outcome.ResolveAccepted, Is.True);
                Assert.That(outcome.CommittedFoul, Is.False);
                Assert.That(outcome.BallPocketedEventCount, Is.EqualTo(1));
                Assert.That(outcome.CueScratchEventCount, Is.EqualTo(0));
                Assert.That(outcome.Phase, Is.EqualTo(TurnLifecyclePhase.AwaitingShot));
                Assert.That(outcome.CurrentPlayerIndex, Is.EqualTo(0));
                Assert.That(outcome.TurnNumber, Is.EqualTo(1));
            }
            finally
            {
                harness.Dispose();
            }
        }

        [Test]
        public void RunShotScenario_CueBallScratch_PassesTurnToOpponent()
        {
            var harness = new GameplayTurnHarness();

            try
            {
                var outcome = harness.RunShotScenario(new ShotScenario(
                    legalContact: true,
                    pocketObjectBall: false,
                    pocketEightBall: false,
                    cueBallScratch: true,
                    duplicateCueScratch: false));

                Assert.That(outcome.ResolveAccepted, Is.True);
                Assert.That(outcome.CommittedFoul, Is.True);
                Assert.That(outcome.CueScratchEventCount, Is.EqualTo(1));
                Assert.That(outcome.Phase, Is.EqualTo(TurnLifecyclePhase.AwaitingShot));
                Assert.That(outcome.CurrentPlayerIndex, Is.EqualTo(1));
                Assert.That(outcome.TurnNumber, Is.EqualTo(2));
            }
            finally
            {
                harness.Dispose();
            }
        }

        [Test]
        public void RunShotScenario_LegalEightBallPocket_WinsMatch()
        {
            var harness = new GameplayTurnHarness();

            try
            {
                var outcome = harness.RunShotScenario(new ShotScenario(
                    legalContact: true,
                    pocketObjectBall: false,
                    pocketEightBall: true,
                    cueBallScratch: false,
                    duplicateCueScratch: false));

                Assert.That(outcome.ResolveAccepted, Is.True);
                Assert.That(outcome.Phase, Is.EqualTo(TurnLifecyclePhase.MatchWon));
                Assert.That(outcome.IsTerminal, Is.True);
                Assert.That(outcome.CurrentPlayerIndex, Is.EqualTo(0));
                Assert.That(outcome.TurnNumber, Is.EqualTo(1));
            }
            finally
            {
                harness.Dispose();
            }
        }

        [Test]
        public void RunShotScenario_EightBallPocketWithScratch_RecordsLossRegressionFixture()
        {
            var harness = new GameplayTurnHarness();

            try
            {
                var outcome = harness.RunShotScenario(new ShotScenario(
                    legalContact: true,
                    pocketObjectBall: false,
                    pocketEightBall: true,
                    cueBallScratch: true,
                    duplicateCueScratch: false));

                Assert.That(outcome.ResolveAccepted, Is.True);
                Assert.That(outcome.CommittedFoul, Is.True);
                Assert.That(outcome.CueScratchEventCount, Is.EqualTo(1));
                Assert.That(outcome.Phase, Is.EqualTo(TurnLifecyclePhase.MatchLost));
                Assert.That(outcome.IsTerminal, Is.True);
            }
            finally
            {
                harness.Dispose();
            }
        }

        [Test]
        public void RunShotScenario_DuplicateCueScratchSignal_OnlyCountsFirstPocketRegressionFixture()
        {
            var harness = new GameplayTurnHarness();

            try
            {
                var outcome = harness.RunShotScenario(new ShotScenario(
                    legalContact: true,
                    pocketObjectBall: false,
                    pocketEightBall: false,
                    cueBallScratch: true,
                    duplicateCueScratch: true));

                Assert.That(outcome.ResolveAccepted, Is.True);
                Assert.That(outcome.CommittedFoul, Is.True);
                Assert.That(outcome.CueScratchEventCount, Is.EqualTo(1));
                Assert.That(outcome.DuplicateCuePocketIgnored, Is.True);
                Assert.That(outcome.Phase, Is.EqualTo(TurnLifecyclePhase.AwaitingShot));
                Assert.That(outcome.CurrentPlayerIndex, Is.EqualTo(1));
                Assert.That(outcome.TurnNumber, Is.EqualTo(2));
            }
            finally
            {
                harness.Dispose();
            }
        }

        [Test]
        public void RunShotScenario_NoLegalContactWithoutScratch_TreatedAsFoulRegressionFixture()
        {
            var harness = new GameplayTurnHarness();

            try
            {
                var outcome = harness.RunShotScenario(new ShotScenario(
                    legalContact: false,
                    pocketObjectBall: false,
                    pocketEightBall: false,
                    cueBallScratch: false,
                    duplicateCueScratch: false));

                Assert.That(outcome.ResolveAccepted, Is.True);
                Assert.That(outcome.CommittedFoul, Is.True);
                Assert.That(outcome.CueScratchEventCount, Is.EqualTo(0));
                Assert.That(outcome.Phase, Is.EqualTo(TurnLifecyclePhase.AwaitingShot));
                Assert.That(outcome.CurrentPlayerIndex, Is.EqualTo(1));
                Assert.That(outcome.TurnNumber, Is.EqualTo(2));
            }
            finally
            {
                harness.Dispose();
            }
        }

        private sealed class GameplayTurnHarness
        {
            private readonly GameObject tableObject;
            private readonly GameObject cueBallObject;
            private readonly GameObject objectBallObject;
            private readonly GameObject eightBallObject;
            private readonly PocketTableController pocketTable;
            private readonly Rigidbody2D cueBallBody;
            private readonly Rigidbody2D objectBallBody;
            private readonly Rigidbody2D eightBallBody;
            private readonly TurnStateMachine turnStateMachine = new TurnStateMachine();

            public GameplayTurnHarness()
            {
                tableObject = new GameObject("gameplay-harness-table");
                cueBallObject = new GameObject("gameplay-harness-cue");
                objectBallObject = new GameObject("gameplay-harness-object");
                eightBallObject = new GameObject("gameplay-harness-eight");

                pocketTable = tableObject.AddComponent<PocketTableController>();

                cueBallBody = cueBallObject.AddComponent<Rigidbody2D>();
                objectBallBody = objectBallObject.AddComponent<Rigidbody2D>();
                eightBallBody = eightBallObject.AddComponent<Rigidbody2D>();

                pocketTable.CueBallBody = cueBallBody;
            }

            public void Dispose()
            {
                Object.DestroyImmediate(tableObject);
                Object.DestroyImmediate(cueBallObject);
                Object.DestroyImmediate(objectBallObject);
                Object.DestroyImmediate(eightBallObject);
            }

            public ShotOutcome RunShotScenario(ShotScenario scenario)
            {
                pocketTable.BeginTurnPocketTracking();

                var beginAccepted = turnStateMachine.TryBeginShot();

                var ballPocketedEvents = 0;
                var cueScratchEvents = 0;
                pocketTable.BallPocketed += _ => ballPocketedEvents++;
                pocketTable.CueBallScratched += _ => cueScratchEvents++;

                if (scenario.PocketObjectBall)
                {
                    pocketTable.TryPocketBody(objectBallBody, new Vector2(1f, 0.5f));
                }

                if (scenario.PocketEightBall)
                {
                    pocketTable.TryPocketBody(eightBallBody, new Vector2(-1f, 0.5f));
                }

                var duplicateCuePocketIgnored = false;
                if (scenario.CueBallScratch)
                {
                    pocketTable.TryPocketBody(cueBallBody, new Vector2(0f, 0f));

                    if (scenario.DuplicateCueScratch)
                    {
                        duplicateCuePocketIgnored = !pocketTable.TryPocketBody(cueBallBody, new Vector2(0.2f, 0.2f));
                    }
                }

                var simulationAccepted = turnStateMachine.TryMarkSimulationComplete();

                var committedFoul = !scenario.LegalContact || pocketTable.ScratchOccurredThisTurn;
                var wonMatch = scenario.PocketEightBall && !committedFoul;
                var lostMatch = scenario.PocketEightBall && committedFoul;
                var keepTurn = scenario.PocketObjectBall && !committedFoul && !scenario.PocketEightBall;

                var resolution = wonMatch
                    ? TurnResolutionResult.Win()
                    : lostMatch
                        ? TurnResolutionResult.Loss()
                        : TurnResolutionResult.Continue(keepTurn, committedFoul);

                var resolveAccepted = turnStateMachine.TryResolveTurn(resolution);

                return new ShotOutcome(
                    beginAccepted,
                    simulationAccepted,
                    resolveAccepted,
                    committedFoul,
                    turnStateMachine.Phase,
                    turnStateMachine.CurrentPlayerIndex,
                    turnStateMachine.TurnNumber,
                    turnStateMachine.IsTerminal,
                    ballPocketedEvents,
                    cueScratchEvents,
                    duplicateCuePocketIgnored);
            }
        }

        private readonly struct ShotScenario
        {
            public ShotScenario(bool legalContact, bool pocketObjectBall, bool pocketEightBall, bool cueBallScratch, bool duplicateCueScratch)
            {
                LegalContact = legalContact;
                PocketObjectBall = pocketObjectBall;
                PocketEightBall = pocketEightBall;
                CueBallScratch = cueBallScratch;
                DuplicateCueScratch = duplicateCueScratch;
            }

            public bool LegalContact { get; }

            public bool PocketObjectBall { get; }

            public bool PocketEightBall { get; }

            public bool CueBallScratch { get; }

            public bool DuplicateCueScratch { get; }
        }

        private readonly struct ShotOutcome
        {
            public ShotOutcome(
                bool beginAccepted,
                bool simulationAccepted,
                bool resolveAccepted,
                bool committedFoul,
                TurnLifecyclePhase phase,
                int currentPlayerIndex,
                int turnNumber,
                bool isTerminal,
                int ballPocketedEventCount,
                int cueScratchEventCount,
                bool duplicateCuePocketIgnored)
            {
                BeginAccepted = beginAccepted;
                SimulationAccepted = simulationAccepted;
                ResolveAccepted = resolveAccepted;
                CommittedFoul = committedFoul;
                Phase = phase;
                CurrentPlayerIndex = currentPlayerIndex;
                TurnNumber = turnNumber;
                IsTerminal = isTerminal;
                BallPocketedEventCount = ballPocketedEventCount;
                CueScratchEventCount = cueScratchEventCount;
                DuplicateCuePocketIgnored = duplicateCuePocketIgnored;
            }

            public bool BeginAccepted { get; }

            public bool SimulationAccepted { get; }

            public bool ResolveAccepted { get; }

            public bool CommittedFoul { get; }

            public TurnLifecyclePhase Phase { get; }

            public int CurrentPlayerIndex { get; }

            public int TurnNumber { get; }

            public bool IsTerminal { get; }

            public int BallPocketedEventCount { get; }

            public int CueScratchEventCount { get; }

            public bool DuplicateCuePocketIgnored { get; }
        }
    }
}
