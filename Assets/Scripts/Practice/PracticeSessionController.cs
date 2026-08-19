using System;
using System.Collections.Generic;
using UnityEngine;

namespace GyroCue.Practice
{
    public enum PracticePhase
    {
        Aiming = 0,
        Simulating = 1
    }

    /// <summary>
    /// The single-player practice loop: aim, stroke, watch the table settle, aim
    /// again. There are no turns and no opponent, so a scratch just spots the cue
    /// ball and play continues; clearing the rack re-racks it.
    /// </summary>
    public sealed class PracticeSessionController : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        private float maxBallSpeedMetresPerSecond = 5f;

        [SerializeField, Min(0f)]
        private float settleDebounceSeconds = 0.2f;

        private readonly List<Rigidbody> objectBalls = new List<Rigidbody>();
        private readonly List<ClothContactMotion> ballMotions = new List<ClothContactMotion>();

        private Rigidbody cueBall;
        private PracticeInputRouter inputRouter;
        private OrbitAimController orbitAim;
        private CueStickView cueStick;
        private float settledDuration;

        public event Action<CueStrikeResult> ShotTaken;

        public event Action SessionStateChanged;

        public PracticePhase Phase { get; private set; } = PracticePhase.Aiming;

        public int ShotsTaken { get; private set; }

        public int BallsPocketed { get; private set; }

        public int Scratches { get; private set; }

        public bool LastShotMiscued { get; private set; }

        public int BallsRemaining
        {
            get
            {
                var remaining = 0;
                for (var i = 0; i < objectBalls.Count; i++)
                {
                    if (objectBalls[i] != null && objectBalls[i].gameObject.activeSelf)
                    {
                        remaining++;
                    }
                }

                return remaining;
            }
        }

        public void Configure(
            Rigidbody cueBallBody,
            IReadOnlyList<Rigidbody> rackedBalls,
            IReadOnlyList<PracticePocket> pockets,
            PracticeInputRouter router,
            OrbitAimController orbitAimController,
            CueStickView cueStickView)
        {
            cueBall = cueBallBody;
            inputRouter = router;
            orbitAim = orbitAimController;
            cueStick = cueStickView;

            objectBalls.Clear();
            objectBalls.AddRange(rackedBalls);

            ballMotions.Clear();
            CollectMotion(cueBallBody);
            for (var i = 0; i < objectBalls.Count; i++)
            {
                CollectMotion(objectBalls[i]);
            }

            for (var i = 0; i < pockets.Count; i++)
            {
                pockets[i].BallEntered += HandleBallPocketed;
            }

            if (inputRouter != null)
            {
                inputRouter.StrokeCompleted += HandleStrokeCompleted;
            }
        }

        /// <summary>Applies a stroke directly. Used by the input router and by tests.</summary>
        public bool TryTakeShot(CueStrokeSample stroke)
        {
            if (Phase != PracticePhase.Aiming || cueBall == null)
            {
                return false;
            }

            var aim = orbitAim != null ? orbitAim.AimDirection : Vector3.forward;
            var elevation = inputRouter != null ? inputRouter.ElevationDegrees : 0f;

            var strike = new CueStrike(aim, stroke.Power01, stroke.StrikeOffset, elevation);
            var result = CueStrikeMath.Resolve(
                strike,
                PracticeTableLayout.BallRadiusMetres,
                maxBallSpeedMetresPerSecond);

            cueBall.velocity = result.LinearVelocity;
            cueBall.angularVelocity = result.AngularVelocity;

            ShotsTaken++;
            LastShotMiscued = result.IsMiscue;
            EnterSimulation();
            ShotTaken?.Invoke(result);
            SessionStateChanged?.Invoke();
            return true;
        }

        public void SpotCueBall()
        {
            if (cueBall == null)
            {
                return;
            }

            cueBall.gameObject.SetActive(true);
            cueBall.velocity = Vector3.zero;
            cueBall.angularVelocity = Vector3.zero;
            cueBall.position = PracticeTableLayout.CueBallSpot;
            cueBall.transform.position = PracticeTableLayout.CueBallSpot;
        }

        public void RackAgain()
        {
            var positions = PracticeTableLayout.RackPositions(
                PracticeTableLayout.RackApex,
                PracticeTableLayout.BallRadiusMetres);

            for (var i = 0; i < objectBalls.Count && i < positions.Length; i++)
            {
                var ball = objectBalls[i];
                if (ball == null)
                {
                    continue;
                }

                ball.gameObject.SetActive(true);
                ball.velocity = Vector3.zero;
                ball.angularVelocity = Vector3.zero;
                ball.position = positions[i];
                ball.transform.position = positions[i];
            }

            SpotCueBall();
            BallsPocketed = 0;
            EnterAiming();
            SessionStateChanged?.Invoke();
        }

        private void Update()
        {
            UpdateCueStick();

            if (Phase != PracticePhase.Simulating)
            {
                return;
            }

            if (!AllBallsAtRest())
            {
                settledDuration = 0f;
                return;
            }

            settledDuration += Time.deltaTime;
            if (settledDuration >= settleDebounceSeconds)
            {
                EnterAiming();
                SessionStateChanged?.Invoke();
            }
        }

        private void UpdateCueStick()
        {
            if (cueStick == null || orbitAim == null)
            {
                return;
            }

            var aiming = Phase == PracticePhase.Aiming;
            cueStick.SetVisible(aiming);

            if (!aiming || inputRouter == null)
            {
                return;
            }

            cueStick.UpdatePose(
                orbitAim.AimDirection,
                inputRouter.StrokeGesture.Backswing01,
                inputRouter.StrokeGesture.StrikeOffset,
                inputRouter.ElevationDegrees);
        }

        private void HandleStrokeCompleted(CueStrokeSample stroke)
        {
            TryTakeShot(stroke);
        }

        private void HandleBallPocketed(Rigidbody body, PracticePocket pocket)
        {
            if (body == null)
            {
                return;
            }

            if (body == cueBall)
            {
                Scratches++;
                SpotCueBall();
                SessionStateChanged?.Invoke();
                return;
            }

            if (!body.gameObject.activeSelf)
            {
                return;
            }

            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.gameObject.SetActive(false);
            BallsPocketed++;
            SessionStateChanged?.Invoke();

            if (BallsRemaining == 0)
            {
                RackAgain();
            }
        }

        private void EnterSimulation()
        {
            Phase = PracticePhase.Simulating;
            settledDuration = 0f;
            inputRouter?.SetInputLocked(true);
        }

        private void EnterAiming()
        {
            Phase = PracticePhase.Aiming;
            settledDuration = 0f;
            inputRouter?.SetInputLocked(false);
            orbitAim?.SetFocus(cueBall != null ? cueBall.transform : null);
        }

        private bool AllBallsAtRest()
        {
            for (var i = 0; i < ballMotions.Count; i++)
            {
                var motion = ballMotions[i];
                if (motion == null || !motion.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!motion.IsAtRest)
                {
                    return false;
                }
            }

            return true;
        }

        private void CollectMotion(Rigidbody body)
        {
            if (body == null)
            {
                return;
            }

            var motion = body.GetComponent<ClothContactMotion>();
            if (motion != null)
            {
                ballMotions.Add(motion);
            }
        }
    }
}
