using System;
using UnityEngine;

namespace GyroCue.Practice
{
    /// <summary>
    /// Single pointer router for practice mode. One drag does one thing, decided by
    /// where it starts: inside the ball-face widget it is a stroke, on the right-hand
    /// strip it sets cue elevation, anywhere else it swings the camera to aim.
    ///
    /// Reads mouse and touch through the same path so the editor plays like the phone.
    /// </summary>
    public sealed class PracticeInputRouter : MonoBehaviour
    {
        private enum DragTarget
        {
            None = 0,
            Aim = 1,
            Stroke = 2,
            Elevation = 3
        }

        [SerializeField]
        private OrbitAimController orbitAimController;

        private readonly CueStrokeGesture strokeGesture = new CueStrokeGesture();
        private DragTarget activeTarget = DragTarget.None;
        private Vector2 lastPointerPosition;
        private bool inputLocked;

        public event Action<CueStrokeSample> StrokeCompleted;

        public CueStrokeGesture StrokeGesture => strokeGesture;

        public float ElevationDegrees { get; private set; }

        public bool InputLocked => inputLocked;

        public void Configure(OrbitAimController orbitController)
        {
            orbitAimController = orbitController;
        }

        public void SetInputLocked(bool locked)
        {
            inputLocked = locked;
            if (locked)
            {
                activeTarget = DragTarget.None;
                strokeGesture.Cancel();
            }
        }

        private void Update()
        {
            if (inputLocked)
            {
                return;
            }

            if (TryReadPointer(out var position, out var phase))
            {
                RoutePointer(position, phase);
            }
        }

        private void RoutePointer(Vector2 position, TouchPhase phase)
        {
            var now = Time.unscaledTime;

            switch (phase)
            {
                case TouchPhase.Began:
                    activeTarget = ResolveTarget(position);
                    lastPointerPosition = position;

                    if (activeTarget == DragTarget.Stroke)
                    {
                        strokeGesture.Begin(PracticeControlLayout.ToFacePosition(position), now);
                    }
                    else if (activeTarget == DragTarget.Elevation)
                    {
                        ElevationDegrees = PracticeControlLayout.ToElevationDegrees(position);
                    }

                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    ApplyDrag(position, now);
                    lastPointerPosition = position;
                    break;

                case TouchPhase.Ended:
                    ApplyDrag(position, now);

                    if (activeTarget == DragTarget.Stroke &&
                        strokeGesture.TryRelease(PracticeControlLayout.ToFacePosition(position), now, out var sample))
                    {
                        StrokeCompleted?.Invoke(sample);
                    }

                    activeTarget = DragTarget.None;
                    break;

                case TouchPhase.Canceled:
                    strokeGesture.Cancel();
                    activeTarget = DragTarget.None;
                    break;
            }
        }

        private void ApplyDrag(Vector2 position, float now)
        {
            switch (activeTarget)
            {
                case DragTarget.Aim:
                    orbitAimController?.ApplyDrag(position - lastPointerPosition);
                    break;
                case DragTarget.Stroke:
                    strokeGesture.Update(PracticeControlLayout.ToFacePosition(position), now);
                    break;
                case DragTarget.Elevation:
                    ElevationDegrees = PracticeControlLayout.ToElevationDegrees(position);
                    break;
            }
        }

        private static DragTarget ResolveTarget(Vector2 position)
        {
            if (PracticeControlLayout.Contains(PracticeControlLayout.StrokeWidget, position))
            {
                return DragTarget.Stroke;
            }

            return PracticeControlLayout.Contains(PracticeControlLayout.ElevationStrip, position)
                ? DragTarget.Elevation
                : DragTarget.Aim;
        }

        private static bool TryReadPointer(out Vector2 position, out TouchPhase phase)
        {
            if (UnityEngine.Input.touchCount > 0)
            {
                var touch = UnityEngine.Input.GetTouch(0);
                position = touch.position;
                phase = touch.phase;
                return true;
            }

            position = UnityEngine.Input.mousePosition;

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                phase = TouchPhase.Began;
                return true;
            }

            if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                phase = TouchPhase.Ended;
                return true;
            }

            if (UnityEngine.Input.GetMouseButton(0))
            {
                phase = TouchPhase.Moved;
                return true;
            }

            phase = TouchPhase.Canceled;
            return false;
        }
    }
}
