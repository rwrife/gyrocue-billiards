using System.Collections.Generic;
using GyroCue.Core;
using GyroCue.Input;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class ShotLifecycleControllerTests
    {
        [Test]
        public void CalculateImpulse_NormalizesAimAndScalesPower()
        {
            var command = new ShotCommand(new Vector2(3f, 4f), 0.5f);

            var impulse = ShotLifecycleMath.CalculateImpulse(command, 10f);

            Assert.That(impulse.x, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(impulse.y, Is.EqualTo(4f).Within(0.0001f));
        }

        [Test]
        public void SettleDetector_RequiresContinuousDebounceAndResetsWhenMovementReturns()
        {
            var ballObject = new GameObject("settle-detector-ball");

            try
            {
                var body = ballObject.AddComponent<Rigidbody2D>();
                var detector = new ShotSettleDetector(0.06f, 3f, 0.2f);
                var bodies = new List<Rigidbody2D> { body };

                body.velocity = Vector2.zero;
                Assert.That(detector.Advance(bodies, 0.15f), Is.False);

                body.velocity = new Vector2(0.07f, 0f);
                Assert.That(detector.Advance(bodies, 0.1f), Is.False);
                Assert.That(detector.SettledDurationSeconds, Is.EqualTo(0f));

                body.velocity = new Vector2(0.02f, 0f);
                body.angularVelocity = 2f;
                Assert.That(detector.Advance(bodies, 0.1f), Is.False);
                Assert.That(detector.Advance(bodies, 0.11f), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(ballObject);
            }
        }

        [Test]
        public void TryStartShot_RejectsOverlappingShotWithoutChangingImpulse()
        {
            using (var fixture = new LifecycleFixture())
            {
                var accepted = fixture.Controller.TryStartShot(new ShotCommand(Vector2.right, 0.4f));
                var firstImpulse = fixture.Controller.LastAppliedImpulse;

                var rejected = fixture.Controller.TryStartShot(new ShotCommand(Vector2.up, 1f));

                Assert.That(accepted, Is.True);
                Assert.That(rejected, Is.False);
                Assert.That(fixture.Controller.TurnState.Phase, Is.EqualTo(TurnLifecyclePhase.ShotSimulation));
                Assert.That(fixture.Controller.LastAppliedImpulse, Is.EqualTo(firstImpulse));
                Assert.That(fixture.Controller.IsTableMoving, Is.True);
            }
        }

        [Test]
        public void TickShotLifecycle_AfterDebouncedRest_ResolvesTurnAndUnlocksTable()
        {
            using (var fixture = new LifecycleFixture())
            {
                Assert.That(fixture.Controller.TryStartShot(new ShotCommand(new Vector2(3f, 4f), 0.5f)), Is.True);
                Assert.That(fixture.Controller.LastAppliedImpulse.x, Is.EqualTo(3f).Within(0.0001f));
                Assert.That(fixture.Controller.LastAppliedImpulse.y, Is.EqualTo(4f).Within(0.0001f));

                fixture.CueBall.velocity = Vector2.zero;
                fixture.ObjectBall.velocity = Vector2.zero;

                Assert.That(fixture.Controller.TickShotLifecycle(0.1f), Is.False);
                Assert.That(fixture.Controller.TickShotLifecycle(0.11f), Is.True);

                Assert.That(fixture.RuleResolver.CallCount, Is.EqualTo(1));
                Assert.That(fixture.Controller.TurnState.Phase, Is.EqualTo(TurnLifecyclePhase.AwaitingShot));
                Assert.That(fixture.Controller.TurnState.CurrentPlayerIndex, Is.EqualTo(1));
                Assert.That(fixture.Controller.IsTableMoving, Is.False);
                Assert.That(fixture.CueBall.velocity, Is.EqualTo(Vector2.zero));
                Assert.That(fixture.ObjectBall.velocity, Is.EqualTo(Vector2.zero));
            }
        }

        private sealed class LifecycleFixture : System.IDisposable
        {
            private readonly GameObject root;
            private readonly GameObject cueBallObject;
            private readonly GameObject objectBallObject;
            private readonly PoolPhysicsTuningProfile physicsProfile;

            public LifecycleFixture()
            {
                root = new GameObject("shot-lifecycle-root");
                cueBallObject = new GameObject("shot-lifecycle-cue-ball");
                objectBallObject = new GameObject("shot-lifecycle-object-ball");

                CueBall = cueBallObject.AddComponent<Rigidbody2D>();
                ObjectBall = objectBallObject.AddComponent<Rigidbody2D>();
                var pocketTable = root.AddComponent<PocketTableController>();
                pocketTable.CueBallBody = CueBall;
                physicsProfile = ScriptableObject.CreateInstance<PoolPhysicsTuningProfile>();
                RuleResolver = new PassTurnRuleResolver();
                Controller = root.AddComponent<ShotLifecycleController>();
                Controller.ConfigureForTests(
                    CueBall,
                    new[] { CueBall, ObjectBall },
                    physicsProfile,
                    pocketTable,
                    RuleResolver,
                    maxCueBallImpulse: 10f,
                    settleDebounceSeconds: 0.2f,
                    angularStopThresholdDegreesPerSecond: 3f);
            }

            public ShotLifecycleController Controller { get; }

            public Rigidbody2D CueBall { get; }

            public Rigidbody2D ObjectBall { get; }

            public PassTurnRuleResolver RuleResolver { get; }

            public void Dispose()
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(cueBallObject);
                Object.DestroyImmediate(objectBallObject);
                Object.DestroyImmediate(physicsProfile);
            }
        }

        private sealed class PassTurnRuleResolver : IShotRuleResolver
        {
            public int CallCount { get; private set; }

            public TurnResolutionResult ResolveShot(ShotRuleContext context)
            {
                CallCount++;
                return TurnResolutionResult.Continue(keepTurn: false, committedFoul: context.CueBallScratched);
            }
        }
    }
}
