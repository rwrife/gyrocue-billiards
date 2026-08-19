using System.Collections;
using GyroCue.Core;
using GyroCue.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GyroCue.Tests.PlayMode
{
    /// <summary>
    /// End-to-end checks that the main scene builds a real, simulating table:
    /// geometry exists, a shot moves the cue ball, and the table settles again.
    /// </summary>
    public sealed class TableSceneBuilderPlayTests
    {
        private const string GameplaySceneName = "MainTable";

        [UnitySetUp]
        public IEnumerator LoadGameplayScene()
        {
            SceneManager.LoadScene(GameplaySceneName);
            yield return null;
            yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator MainTable_BuildsCueBallRackCushionsAndPockets()
        {
            var builder = Object.FindObjectOfType<TableSceneBuilder>();
            Assert.That(builder, Is.Not.Null, "MainTable is missing its TableSceneBuilder.");

            Assert.That(builder.CueBallBody, Is.Not.Null, "No cue ball was built.");
            Assert.That(
                builder.BallBodies.Count,
                Is.EqualTo(TableRackMath.RackBallCount + 1),
                "Expected a cue ball plus a full rack.");

            var triggers = Object.FindObjectsOfType<PocketTriggerReporter>();
            Assert.That(triggers.Length, Is.EqualTo(TableRackMath.PocketCount), "Wrong pocket count.");

            var cushions = 0;
            foreach (var boxCollider in Object.FindObjectsOfType<BoxCollider2D>())
            {
                if (boxCollider.name.StartsWith("Cushion"))
                {
                    cushions++;
                }
            }

            Assert.That(cushions, Is.EqualTo(6), "Expected six cushion rails.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ShotFromRest_MovesCueBallThenSettles()
        {
            var builder = Object.FindObjectOfType<TableSceneBuilder>();
            var lifecycle = Object.FindObjectOfType<ShotLifecycleController>();
            Assert.That(lifecycle, Is.Not.Null, "MainTable has no shot lifecycle.");

            var cueBall = builder.CueBallBody;
            var startPosition = cueBall.position;

            var started = lifecycle.TryStartShot(new ShotCommand(Vector2.right, normalizedPower: 1f));
            Assert.That(started, Is.True, "The shot was refused.");

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(cueBall.velocity.magnitude, Is.GreaterThan(0.5f), "The cue ball did not accelerate.");

            // Let the table run; drag must bring every ball back to rest.
            var elapsed = 0f;
            while (elapsed < 15f && lifecycle.IsTableMoving)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.That(lifecycle.IsTableMoving, Is.False, $"Table never settled within {elapsed:F1}s.");
            Assert.That(
                Vector2.Distance(cueBall.position, startPosition),
                Is.GreaterThan(0.5f),
                "The cue ball ended where it started.");
        }

        [UnityTest]
        public IEnumerator CueBallDrivenIntoCornerPocket_IsRemovedFromPlay()
        {
            var builder = Object.FindObjectOfType<TableSceneBuilder>();
            var pocketTable = Object.FindObjectOfType<PocketTableController>();
            var cueBall = builder.CueBallBody;

            var cornerPocket = TableRackMath.ResolvePocketPositions()[0];
            cueBall.position = cornerPocket + new Vector2(1.5f, 0f);
            cueBall.velocity = Vector2.zero;
            yield return new WaitForFixedUpdate();

            cueBall.velocity = (cornerPocket - cueBall.position).normalized * 4f;

            var elapsed = 0f;
            while (elapsed < 3f && !pocketTable.ScratchOccurredThisTurn)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.That(pocketTable.ScratchOccurredThisTurn, Is.True, "Pocket trigger never fired for the cue ball.");
            Assert.That(cueBall.simulated, Is.False, "A pocketed cue ball should stop simulating.");
        }
    }
}
