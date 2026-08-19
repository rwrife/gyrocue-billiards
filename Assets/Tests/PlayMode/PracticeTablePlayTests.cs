using System.Collections;
using GyroCue.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GyroCue.Tests.PlayMode
{
    /// <summary>
    /// End-to-end checks on the 3D practice table: it builds, a stroke drives the cue
    /// ball, spin behaves the way the cue applied it, and an elevated jump shot leaves
    /// the cloth.
    /// </summary>
    public sealed class PracticeTablePlayTests
    {
        private PracticeTableBuilder builder;
        private PracticeSessionController session;

        [UnitySetUp]
        public IEnumerator LoadPracticeScene()
        {
            SceneManager.LoadScene("Practice");
            yield return null;
            yield return new WaitForFixedUpdate();

            builder = Object.FindObjectOfType<PracticeTableBuilder>();
            session = builder != null ? builder.Session : null;
        }

        [UnityTest]
        public IEnumerator PracticeScene_BuildsTableRackAndPockets()
        {
            Assert.That(builder, Is.Not.Null, "Practice scene is missing its table builder.");
            Assert.That(builder.CueBall, Is.Not.Null);
            Assert.That(builder.ObjectBalls.Count, Is.EqualTo(PracticeTableLayout.RackBallCount));
            Assert.That(builder.Pockets.Count, Is.EqualTo(6));
            Assert.That(session, Is.Not.Null);
            Assert.That(session.Phase, Is.EqualTo(PracticePhase.Aiming));

            // Everything must start resting on the cloth, not intersecting it.
            Assert.That(
                builder.CueBall.position.y,
                Is.EqualTo(PracticeTableLayout.BallRestHeight).Within(0.002f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BallsStayOnTheTable_AndSettleAfterAShot()
        {
            var cueBall = builder.CueBall;

            Assert.That(session.TryTakeShot(new CueStrokeSample(0.9f, Vector2.zero, 9f)), Is.True);
            Assert.That(session.Phase, Is.EqualTo(PracticePhase.Simulating));

            var elapsed = 0f;
            while (elapsed < 25f && session.Phase == PracticePhase.Simulating)
            {
                elapsed += Time.deltaTime;

                Assert.That(
                    Mathf.Abs(cueBall.position.x),
                    Is.LessThan(PracticeTableLayout.HalfLength + 0.2f),
                    "Cue ball escaped down the length of the table.");
                Assert.That(
                    Mathf.Abs(cueBall.position.z),
                    Is.LessThan(PracticeTableLayout.HalfWidth + 0.2f),
                    "Cue ball escaped across the width of the table.");

                yield return null;
            }

            Assert.That(session.Phase, Is.EqualTo(PracticePhase.Aiming), $"Table never settled ({elapsed:F1}s).");
            Assert.That(session.ShotsTaken, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DrawShot_PullsTheCueBallBackAfterContact()
        {
            // Clear the rack so only the struck ball matters. The shot runs along +X,
            // across the table's length, so the object ball does not run into a pocket
            // and re-rack the table mid-test.
            foreach (var ball in builder.ObjectBalls)
            {
                ball.gameObject.SetActive(false);
            }

            var target = builder.ObjectBalls[0];
            target.gameObject.SetActive(true);
            target.velocity = Vector3.zero;
            target.angularVelocity = Vector3.zero;
            target.position = new Vector3(0f, PracticeTableLayout.BallRestHeight, 0.15f);

            var cueBall = builder.CueBall;
            cueBall.position = new Vector3(-0.3f, PracticeTableLayout.BallRestHeight, 0.15f);
            cueBall.velocity = Vector3.zero;
            cueBall.angularVelocity = Vector3.zero;
            yield return new WaitForFixedUpdate();

            var targetStartX = target.position.x;
            var contactX = targetStartX - (PracticeTableLayout.BallRadiusMetres * 2f);

            // Hard and low on the ball: backspin should survive the short roll and pull
            // the cue ball back once it transfers its speed to the object ball.
            var strike = CueStrikeMath.Resolve(
                new CueStrike(Vector3.right, 0.85f, new Vector2(0f, -0.45f), 0f),
                PracticeTableLayout.BallRadiusMetres,
                5f);

            Assert.That(strike.AngularVelocity.z, Is.GreaterThan(0f), "Low tip should spin against the roll.");

            cueBall.velocity = strike.LinearVelocity;
            cueBall.angularVelocity = strike.AngularVelocity;

            var sawContact = false;
            var elapsed = 0f;
            while (elapsed < 5f)
            {
                elapsed += Time.deltaTime;

                if (!sawContact && target.velocity.magnitude > 0.05f)
                {
                    sawContact = true;
                }

                if (sawContact && cueBall.velocity.magnitude < 0.02f)
                {
                    break;
                }

                yield return null;
            }

            Assert.That(sawContact, Is.True, "The cue ball never reached the object ball.");
            Assert.That(
                target.position.x,
                Is.GreaterThan(targetStartX + 0.05f),
                "The object ball should have been driven forward.");
            Assert.That(
                cueBall.position.x,
                Is.LessThan(contactX),
                $"Draw failed: the cue ball settled at x={cueBall.position.x:F3}, ahead of contact at x={contactX:F3}.");
        }

        [UnityTest]
        public IEnumerator ElevatedHighStrike_LiftsTheCueBallOffTheCloth()
        {
            foreach (var ball in builder.ObjectBalls)
            {
                ball.gameObject.SetActive(false);
            }

            var cueBall = builder.CueBall;
            cueBall.position = new Vector3(-0.5f, PracticeTableLayout.BallRestHeight, 0f);
            cueBall.velocity = Vector3.zero;
            cueBall.angularVelocity = Vector3.zero;
            yield return new WaitForFixedUpdate();

            var strike = CueStrikeMath.Resolve(
                new CueStrike(Vector3.forward, 1f, new Vector2(0f, 0.45f), 50f),
                PracticeTableLayout.BallRadiusMetres,
                5f);
            Assert.That(strike.IsAirborne, Is.True);

            cueBall.velocity = strike.LinearVelocity;
            cueBall.angularVelocity = strike.AngularVelocity;

            var peakHeight = cueBall.position.y;
            var elapsed = 0f;
            while (elapsed < 2f)
            {
                elapsed += Time.deltaTime;
                peakHeight = Mathf.Max(peakHeight, cueBall.position.y);
                yield return null;
            }

            var clearance = peakHeight - PracticeTableLayout.BallRestHeight;
            Assert.That(
                clearance,
                Is.GreaterThan(PracticeTableLayout.BallRadiusMetres),
                $"Jump shot only cleared {clearance * 1000f:F0}mm; it should clear a ball radius.");
            Assert.That(
                cueBall.position.y,
                Is.LessThan(PracticeTableLayout.BallRestHeight + 0.01f),
                "The ball should have landed again.");
        }

        [UnityTest]
        public IEnumerator PocketingTheCueBall_CountsAScratchAndSpotsIt()
        {
            var cueBall = builder.CueBall;
            var pocket = PracticeTableLayout.PocketCentres()[0];

            cueBall.position = new Vector3(pocket.x + 0.25f, PracticeTableLayout.BallRestHeight, pocket.z + 0.25f);
            cueBall.velocity = Vector3.zero;
            yield return new WaitForFixedUpdate();

            cueBall.velocity = (new Vector3(pocket.x, PracticeTableLayout.BallRestHeight, pocket.z) - cueBall.position).normalized * 1.5f;

            var elapsed = 0f;
            while (elapsed < 4f && session.Scratches == 0)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.That(session.Scratches, Is.EqualTo(1), "The cue ball never dropped.");
            Assert.That(
                Vector3.Distance(cueBall.position, PracticeTableLayout.CueBallSpot),
                Is.LessThan(0.02f),
                "A scratch should spot the cue ball back on the head half.");
        }
    }
}
