using GyroCue.Core;
using UnityEngine;

namespace GyroCue.Input
{
    /// <summary>
    /// Drives the touch gesture pipeline from mouse input so the table is playable in
    /// the editor and on desktop, where <c>Input.touchCount</c> stays zero.
    ///
    /// Also serves the cue-ball-in-hand click after a scratch.
    /// </summary>
    public sealed class PointerShotInput : MonoBehaviour
    {
        private const int PointerFingerId = 0;

        [SerializeField]
        private TouchAimSwipeController touchAimSwipeController;

        [SerializeField]
        private CueBallPlacementController cueBallPlacementController;

        [SerializeField]
        private Camera worldCamera;

        public void Configure(
            TouchAimSwipeController touchController,
            CueBallPlacementController placementController,
            Camera camera)
        {
            touchAimSwipeController = touchController;
            cueBallPlacementController = placementController;
            worldCamera = camera;
        }

        private void Update()
        {
            ResolveReferences();

            if (TryHandleCueBallPlacement())
            {
                return;
            }

            if (touchAimSwipeController == null)
            {
                return;
            }

            var pointerPosition = (Vector2)UnityEngine.Input.mousePosition;

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                touchAimSwipeController.ProcessTouchPhase(TouchPhase.Began, pointerPosition, PointerFingerId, out _);
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                touchAimSwipeController.ProcessTouchPhase(TouchPhase.Ended, pointerPosition, PointerFingerId, out _);
            }
            else if (UnityEngine.Input.GetMouseButton(0))
            {
                touchAimSwipeController.ProcessTouchPhase(TouchPhase.Moved, pointerPosition, PointerFingerId, out _);
            }
        }

        private bool TryHandleCueBallPlacement()
        {
            if (cueBallPlacementController == null || !cueBallPlacementController.IsPlacementModeActive)
            {
                return false;
            }

            if (!UnityEngine.Input.GetMouseButtonDown(0) || worldCamera == null)
            {
                return true;
            }

            var worldPoint = worldCamera.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
            cueBallPlacementController.TryPlaceCueBall(worldPoint, out _);
            return true;
        }

        private void ResolveReferences()
        {
            if (touchAimSwipeController == null)
            {
                touchAimSwipeController = GetComponent<TouchAimSwipeController>();
            }

            if (cueBallPlacementController == null)
            {
                cueBallPlacementController = GetComponent<CueBallPlacementController>();
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }
        }
    }
}
