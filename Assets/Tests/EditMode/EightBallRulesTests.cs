using System.Linq;
using GyroCue.Practice;
using NUnit.Framework;
using UnityEngine;

namespace GyroCue.Tests.EditMode
{
    public sealed class EightBallRulesTests
    {
        [Test]
        public void StandardRack_HasStableIdentitiesAndLegalEightBallPlacement()
        {
            var numbers = EightBallRack.StandardBallNumbers;

            Assert.That(numbers.Count, Is.EqualTo(15));
            Assert.That(numbers.Distinct().Count(), Is.EqualTo(15));
            Assert.That(numbers, Is.EquivalentTo(Enumerable.Range(1, 15)));
            Assert.That(numbers[4], Is.EqualTo(8), "The 8-ball belongs in the centre of row three.");
            Assert.That(EightBallBall.GroupFor(numbers[10]), Is.Not.EqualTo(EightBallBall.GroupFor(numbers[14])));
        }

        [TestCase(0, EightBallGroup.Cue)]
        [TestCase(1, EightBallGroup.Solids)]
        [TestCase(7, EightBallGroup.Solids)]
        [TestCase(8, EightBallGroup.Eight)]
        [TestCase(9, EightBallGroup.Stripes)]
        [TestCase(15, EightBallGroup.Stripes)]
        public void GroupFor_MapsStableBallNumbers(int number, EightBallGroup expected)
        {
            Assert.That(EightBallBall.GroupFor(number), Is.EqualTo(expected));
        }

        [Test]
        public void OpenTable_FirstLegalPocketAssignsGroupsAndKeepsTurn()
        {
            var rules = new EightBallRules();

            var resolution = rules.ResolveShot(0, Shot(firstContact: 1, pocketed: new[] { 1 }));

            Assert.That(rules.GetPlayerGroup(0), Is.EqualTo(EightBallGroup.Solids));
            Assert.That(rules.GetPlayerGroup(1), Is.EqualTo(EightBallGroup.Stripes));
            Assert.That(rules.RemainingSolids, Is.EqualTo(6));
            Assert.That(rules.GetPlayerScore(0), Is.EqualTo(1));
            Assert.That(resolution.TurnResolution.KeepTurn, Is.True);
            Assert.That(resolution.FoulReason, Is.EqualTo(EightBallFoulReason.None));
        }

        [Test]
        public void OpenTable_MixedPocketsAssignByFirstPocketAndRemoveBothGroups()
        {
            var rules = new EightBallRules();

            var resolution = rules.ResolveShot(0, Shot(firstContact: 9, pocketed: new[] { 9, 2 }));

            Assert.That(rules.GetPlayerGroup(0), Is.EqualTo(EightBallGroup.Stripes));
            Assert.That(rules.RemainingStripes, Is.EqualTo(6));
            Assert.That(rules.RemainingSolids, Is.EqualTo(6));
            Assert.That(resolution.TurnResolution.KeepTurn, Is.True);
        }

        [Test]
        public void PreviouslyRemovedBall_CannotAssignAnOpenTableOrKeepTheTurn()
        {
            var rules = new EightBallRules();
            rules.ResolveShot(0, Shot(firstContact: 1, pocketed: new[] { 1 }, scratch: true));

            var resolution = rules.ResolveShot(1, Shot(firstContact: 2, pocketed: new[] { 1 }));

            Assert.That(rules.GetPlayerGroup(1), Is.EqualTo(EightBallGroup.Unassigned));
            Assert.That(resolution.TurnResolution.KeepTurn, Is.False);
        }

        [Test]
        public void Scratch_IsAFoulAndPassesTheTurn()
        {
            var rules = AssignedMatch(EightBallGroup.Solids);

            var resolution = rules.ResolveShot(0, Shot(firstContact: 1, pocketed: new[] { 1 }, scratch: true));

            Assert.That(resolution.TurnResolution.CommittedFoul, Is.True);
            Assert.That(resolution.TurnResolution.KeepTurn, Is.False);
            Assert.That(resolution.FoulReason, Is.EqualTo(EightBallFoulReason.Scratch));
            Assert.That(resolution.FoulMessage, Does.Contain("Scratch"));
        }

        [Test]
        public void NoContact_IsAFoulWithHudReason()
        {
            var rules = new EightBallRules();

            var resolution = rules.ResolveShot(0, Shot(firstContact: EightBallShotRecord.NoBall));

            Assert.That(resolution.TurnResolution.CommittedFoul, Is.True);
            Assert.That(resolution.FoulReason, Is.EqualTo(EightBallFoulReason.NoContact));
            Assert.That(resolution.FoulMessage, Is.EqualTo("Foul: no ball contacted"));
        }

        [Test]
        public void DefaultShotRecord_IsHandledAsNoContactInsteadOfThrowing()
        {
            var rules = new EightBallRules();

            var resolution = rules.ResolveShot(0, default);

            Assert.That(resolution.TurnResolution.CommittedFoul, Is.True);
            Assert.That(resolution.FoulReason, Is.EqualTo(EightBallFoulReason.NoContact));
        }

        [Test]
        public void WrongGroupFirstContact_IsAFoulEvenWhenOwnBallDrops()
        {
            var rules = AssignedMatch(EightBallGroup.Solids);

            var resolution = rules.ResolveShot(0, Shot(firstContact: 9, pocketed: new[] { 1 }));

            Assert.That(resolution.TurnResolution.CommittedFoul, Is.True);
            Assert.That(resolution.TurnResolution.KeepTurn, Is.False);
            Assert.That(resolution.FoulReason, Is.EqualTo(EightBallFoulReason.WrongFirstContact));
        }

        [Test]
        public void PocketingOnlyOpponentsBall_PassesWithoutInventingAFoul()
        {
            var rules = AssignedMatch(EightBallGroup.Solids);

            var resolution = rules.ResolveShot(0, Shot(firstContact: 1, pocketed: new[] { 9 }));

            Assert.That(resolution.TurnResolution.CommittedFoul, Is.False);
            Assert.That(resolution.TurnResolution.KeepTurn, Is.False);
            Assert.That(rules.RemainingStripes, Is.EqualTo(6));
        }

        [Test]
        public void LegalEightBallAfterClearingGroup_Wins()
        {
            var rules = AssignedMatch(EightBallGroup.Solids);
            rules.ResolveShot(0, Shot(firstContact: 1, pocketed: Enumerable.Range(1, 7).ToArray()));

            var resolution = rules.ResolveShot(0, Shot(firstContact: 8, pocketed: new[] { 8 }));

            Assert.That(rules.RemainingSolids, Is.EqualTo(0));
            Assert.That(resolution.TurnResolution.WonMatch, Is.True);
            Assert.That(resolution.TurnResolution.LostMatch, Is.False);
        }

        [Test]
        public void EarlyEightBall_IsALoss()
        {
            var rules = AssignedMatch(EightBallGroup.Stripes);

            var resolution = rules.ResolveShot(0, Shot(firstContact: 9, pocketed: new[] { 8 }));

            Assert.That(resolution.TurnResolution.LostMatch, Is.True);
            Assert.That(resolution.FoulReason, Is.EqualTo(EightBallFoulReason.IllegalEightBall));
        }

        [Test]
        public void FinalGroupBallAndEightBallOnSameShot_IsStillAnEarlyEightBallLoss()
        {
            var rules = AssignedMatch(EightBallGroup.Solids);

            var resolution = rules.ResolveShot(
                0,
                Shot(firstContact: 2, pocketed: new[] { 2, 3, 4, 5, 6, 7, 8 }));

            Assert.That(resolution.TurnResolution.LostMatch, Is.True);
            Assert.That(resolution.FoulReason, Is.EqualTo(EightBallFoulReason.IllegalEightBall));
        }

        [Test]
        public void EightBallScratch_IsALossEvenAfterGroupCleared()
        {
            var rules = AssignedMatch(EightBallGroup.Stripes);
            rules.ResolveShot(0, Shot(firstContact: 9, pocketed: Enumerable.Range(9, 7).ToArray()));

            var resolution = rules.ResolveShot(0, Shot(firstContact: 8, pocketed: new[] { 8 }, scratch: true));

            Assert.That(resolution.TurnResolution.LostMatch, Is.True);
            Assert.That(resolution.FoulReason, Is.EqualTo(EightBallFoulReason.IllegalEightBall));
        }

        [Test]
        public void ObjectBallOffTable_IsFoulAndBallRemainsAvailableForRespot()
        {
            var rules = AssignedMatch(EightBallGroup.Solids);

            var resolution = rules.ResolveShot(
                0,
                new EightBallShotRecord(firstContactBallNumber: 2, pocketedBallNumbers: null, cueBallScratched: false, offTableBallNumbers: new[] { 2 }));

            Assert.That(resolution.TurnResolution.CommittedFoul, Is.True);
            Assert.That(resolution.FoulReason, Is.EqualTo(EightBallFoulReason.BallOffTable));
            Assert.That(rules.IsBallOnTable(2), Is.True, "Non-8 object balls are re-spotted after leaving the table.");
        }

        [Test]
        public void InvalidAndStaleOffTableEvents_DoNotCreateFoulsOrResurrectBalls()
        {
            var rules = new EightBallRules();
            rules.ResolveShot(0, Shot(firstContact: 1, pocketed: new[] { 1 }, scratch: true));

            var resolution = rules.ResolveShot(
                1,
                new EightBallShotRecord(
                    firstContactBallNumber: 2,
                    pocketedBallNumbers: new[] { 1 },
                    cueBallScratched: false,
                    offTableBallNumbers: new[] { 1, 99 }));

            Assert.That(resolution.TurnResolution.CommittedFoul, Is.False);
            Assert.That(rules.IsBallOnTable(1), Is.False);
        }

        [Test]
        public void StaleEightBallEvent_AfterTerminalResultDoesNotResolveAgain()
        {
            var rules = AssignedMatch(EightBallGroup.Solids);
            rules.ResolveShot(0, Shot(firstContact: 2, pocketed: new[] { 2, 3, 4, 5, 6, 7 }));
            Assert.That(rules.ResolveShot(0, Shot(firstContact: 8, pocketed: new[] { 8 })).TurnResolution.WonMatch, Is.True);

            var stale = rules.ResolveShot(0, Shot(firstContact: 8, pocketed: new[] { 8 }));

            Assert.That(stale.TurnResolution.WonMatch, Is.False);
            Assert.That(stale.TurnResolution.LostMatch, Is.False);
        }

        [Test]
        public void ResetMatch_RestoresRackGroupsScoresAndBallAvailability()
        {
            var rules = AssignedMatch(EightBallGroup.Solids);
            rules.ResolveShot(0, Shot(firstContact: 1, pocketed: new[] { 1, 2, 3 }));

            rules.ResetMatch();

            Assert.That(rules.GetPlayerGroup(0), Is.EqualTo(EightBallGroup.Unassigned));
            Assert.That(rules.GetPlayerScore(0), Is.EqualTo(0));
            Assert.That(rules.RemainingSolids, Is.EqualTo(7));
            Assert.That(rules.RemainingStripes, Is.EqualTo(7));
            Assert.That(Enumerable.Range(1, 15).All(rules.IsBallOnTable), Is.True);
        }

        [Test]
        public void BallIdentity_DoesNotDependOnGameObjectName()
        {
            var gameObject = new GameObject("Decorative sphere");
            try
            {
                var identity = gameObject.AddComponent<BallIdentity>();
                identity.Configure(12);

                gameObject.name = "Renamed at runtime";

                Assert.That(identity.BallNumber, Is.EqualTo(12));
                Assert.That(identity.Group, Is.EqualTo(EightBallGroup.Stripes));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CueBallContactTracker_RecordsOnlyTheFirstObjectBallPerShot()
        {
            var cue = new GameObject("Cue");
            var one = new GameObject("One");
            var nine = new GameObject("Nine");
            try
            {
                var tracker = cue.AddComponent<CueBallContactTracker>();
                var oneIdentity = one.AddComponent<BallIdentity>();
                var nineIdentity = nine.AddComponent<BallIdentity>();
                oneIdentity.Configure(1);
                nineIdentity.Configure(9);

                tracker.BeginShot();
                Assert.That(tracker.TryRecordContact(oneIdentity), Is.True);
                Assert.That(tracker.TryRecordContact(nineIdentity), Is.False);
                Assert.That(tracker.FirstContactBallNumber, Is.EqualTo(1));

                tracker.BeginShot();
                Assert.That(tracker.FirstContactBallNumber, Is.EqualTo(EightBallShotRecord.NoBall));
            }
            finally
            {
                Object.DestroyImmediate(cue);
                Object.DestroyImmediate(one);
                Object.DestroyImmediate(nine);
            }
        }

        private static EightBallRules AssignedMatch(EightBallGroup playerZeroGroup)
        {
            var rules = new EightBallRules();
            var assignmentBall = playerZeroGroup == EightBallGroup.Solids ? 1 : 9;
            rules.ResolveShot(0, Shot(firstContact: assignmentBall, pocketed: new[] { assignmentBall }));
            return rules;
        }

        private static EightBallShotRecord Shot(
            int firstContact,
            int[] pocketed = null,
            bool scratch = false)
        {
            return new EightBallShotRecord(firstContact, pocketed, scratch);
        }
    }
}
