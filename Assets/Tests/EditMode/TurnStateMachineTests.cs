using GyroCue.Core;
using NUnit.Framework;

namespace GyroCue.Tests.EditMode
{
    public sealed class TurnStateMachineTests
    {
        [Test]
        public void Lifecycle_AdvancesFromShotToResolutionBackToAwaitingShot()
        {
            var machine = new TurnStateMachine();

            Assert.That(machine.Phase, Is.EqualTo(TurnLifecyclePhase.AwaitingShot));
            Assert.That(machine.TryBeginShot(), Is.True);
            Assert.That(machine.Phase, Is.EqualTo(TurnLifecyclePhase.ShotSimulation));
            Assert.That(machine.TryMarkSimulationComplete(), Is.True);
            Assert.That(machine.Phase, Is.EqualTo(TurnLifecyclePhase.TurnResolution));

            var resolved = machine.TryResolveTurn(TurnResolutionResult.Continue(keepTurn: true));

            Assert.That(resolved, Is.True);
            Assert.That(machine.Phase, Is.EqualTo(TurnLifecyclePhase.AwaitingShot));
            Assert.That(machine.CurrentPlayerIndex, Is.EqualTo(0));
            Assert.That(machine.TurnNumber, Is.EqualTo(1));
        }

        [Test]
        public void IllegalActions_AreBlockedDuringShotSimulation()
        {
            var machine = new TurnStateMachine();

            Assert.That(machine.TryBeginShot(), Is.True);
            Assert.That(machine.InputLocked, Is.True);

            // Cannot begin another shot while simulation is active.
            Assert.That(machine.TryBeginShot(), Is.False);
            // Cannot resolve before simulation completion transitions to TurnResolution.
            Assert.That(machine.TryResolveTurn(TurnResolutionResult.Continue(keepTurn: true)), Is.False);

            Assert.That(machine.Phase, Is.EqualTo(TurnLifecyclePhase.ShotSimulation));
        }

        [Test]
        public void ResolveTurn_FoulPassesTurnToOpponent()
        {
            var machine = new TurnStateMachine();
            Assert.That(machine.CurrentPlayerIndex, Is.EqualTo(0));

            machine.TryBeginShot();
            machine.TryMarkSimulationComplete();
            var resolved = machine.TryResolveTurn(TurnResolutionResult.Continue(keepTurn: true, committedFoul: true));

            Assert.That(resolved, Is.True);
            Assert.That(machine.Phase, Is.EqualTo(TurnLifecyclePhase.AwaitingShot));
            Assert.That(machine.CurrentPlayerIndex, Is.EqualTo(1));
            Assert.That(machine.TurnNumber, Is.EqualTo(2));
        }

        [Test]
        public void TerminalOutcomes_SetWinOrLossAndBlockFutureShots()
        {
            var winMachine = new TurnStateMachine();
            winMachine.TryBeginShot();
            winMachine.TryMarkSimulationComplete();

            Assert.That(winMachine.TryResolveTurn(TurnResolutionResult.Win()), Is.True);
            Assert.That(winMachine.Phase, Is.EqualTo(TurnLifecyclePhase.MatchWon));
            Assert.That(winMachine.IsTerminal, Is.True);
            Assert.That(winMachine.TryBeginShot(), Is.False);

            var lossMachine = new TurnStateMachine();
            lossMachine.TryBeginShot();
            lossMachine.TryMarkSimulationComplete();

            Assert.That(lossMachine.TryResolveTurn(TurnResolutionResult.Loss()), Is.True);
            Assert.That(lossMachine.Phase, Is.EqualTo(TurnLifecyclePhase.MatchLost));
            Assert.That(lossMachine.IsTerminal, Is.True);
            Assert.That(lossMachine.TryBeginShot(), Is.False);
        }
    }
}
