using System;
using UnityEngine;

namespace GyroCue.Input
{
    /// <summary>
    /// Mobile-first touch controls: drag to aim, pull opposite the aim line, release to shoot.
    ///
    /// This script emits shot intents only; downstream physics/gameplay systems apply forces.
    /// </summary>
    public sealed class TouchAimSwipeController : MonoBehaviour
    {
        [SerializeField]
        private bool touchInputEnabled = true;

        [SerializeField]
        private float aimDeadzonePixels = 14f;

        [SerializeField]
        private float maxPullDistancePixels = 280f;

        [SerializeField, Range(0f, 1f)]
        private float minimumLaunchPower = 0.08f;

        [SerializeField]
        private AnimationCurve powerCurve = AnimationCurve.EaseInOut(0f, 0.05f, 1f, 1f);

        private bool isGestureActive;
        private int activeFingerId = -1;
        private Vector2 gestureStartScreenPosition;
        private Vector2 latestScreenPosition;
        private Vector2 aimDirection = Vector2.right;
        private float pullDistancePixels;
        private bool ballsAreMoving;

        public event Action<ShotCommand> ShotReleased;

        public Vector2 AimDirection => aimDirection;

        public float PullDistancePixels => pullDistancePixels;

        public bool InputLocked => ballsAreMoving;

        public float PreviewPower01
        {
            get
            {
                var normalizedPull = TouchShotMath.NormalizePullDistance(pullDistancePixels, maxPullDistancePixels);
                return TouchShotMath.EvaluatePower(powerCurve, normalizedPull);
            }
        }

        /// <summary>
        /// Locks input while the table is active so taps/swipes cannot queue overlapping shots.
        /// </summary>
        public void SetBallsMoving(bool areMoving)
        {
            ballsAreMoving = areMoving;
            if (areMoving)
            {
                CancelGesture();
            }
        }

        /// <summary>
        /// Injectable touch-phase entry point for runtime use and edit-mode tests.
        /// </summary>
        public bool ProcessTouchPhase(TouchPhase phase, Vector2 screenPosition, int fingerId, out ShotCommand shotCommand)
        {
            shotCommand = default;

            if (!touchInputEnabled || ballsAreMoving)
            {
                if (ballsAreMoving)
                {
                    CancelGesture();
                }

                return false;
            }

            if (phase == TouchPhase.Began)
            {
                BeginGesture(screenPosition, fingerId);
                return false;
            }

            if (!isGestureActive || fingerId != activeFingerId)
            {
                return false;
            }

            switch (phase)
            {
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    UpdateGesture(screenPosition);
                    return false;
                case TouchPhase.Ended:
                    UpdateGesture(screenPosition);
                    return CompleteGesture(out shotCommand);
                case TouchPhase.Canceled:
                    CancelGesture();
                    return false;
                default:
                    return false;
            }
        }

        private void Update()
        {
            if (!touchInputEnabled || ballsAreMoving || UnityEngine.Input.touchCount <= 0)
            {
                return;
            }

            for (var i = 0; i < UnityEngine.Input.touchCount; i++)
            {
                var touch = UnityEngine.Input.GetTouch(i);

                if (!isGestureActive)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        ProcessTouchPhase(touch.phase, touch.position, touch.fingerId, out _);
                    }

                    continue;
                }

                if (touch.fingerId == activeFingerId)
                {
                    ProcessTouchPhase(touch.phase, touch.position, touch.fingerId, out _);
                }
            }
        }

        private void BeginGesture(Vector2 screenPosition, int fingerId)
        {
            isGestureActive = true;
            activeFingerId = fingerId;
            gestureStartScreenPosition = screenPosition;
            latestScreenPosition = screenPosition;
            pullDistancePixels = 0f;
        }

        private void UpdateGesture(Vector2 screenPosition)
        {
            latestScreenPosition = screenPosition;
            var dragDelta = latestScreenPosition - gestureStartScreenPosition;
            aimDirection = TouchShotMath.ResolveAimDirection(dragDelta, aimDirection, aimDeadzonePixels);
            pullDistancePixels = TouchShotMath.CalculatePullDistancePixels(
                gestureStartScreenPosition,
                latestScreenPosition,
                aimDirection);
        }

        private bool CompleteGesture(out ShotCommand shotCommand)
        {
            shotCommand = default;

            var normalizedPower = PreviewPower01;
            var shouldReleaseShot = normalizedPower >= minimumLaunchPower;
            var releaseAimDirection = aimDirection;
            CancelGesture();

            if (!shouldReleaseShot)
            {
                return false;
            }

            shotCommand = new ShotCommand(releaseAimDirection, normalizedPower);
            ShotReleased?.Invoke(shotCommand);
            return true;
        }

        private void CancelGesture()
        {
            isGestureActive = false;
            activeFingerId = -1;
            pullDistancePixels = 0f;
        }
    }
}
