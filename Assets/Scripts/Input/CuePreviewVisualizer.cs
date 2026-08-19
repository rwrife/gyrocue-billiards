using UnityEngine;

namespace GyroCue.Input
{
    /// <summary>
    /// Renders a lightweight cue indicator and preview trajectory line for touch/remote aiming.
    /// </summary>
    public sealed class CuePreviewVisualizer : MonoBehaviour
    {
        [SerializeField]
        private TouchAimSwipeController touchAimSwipeController;

        [SerializeField]
        private RemoteSensorInputAdapter remoteSensorInputAdapter;

        [SerializeField]
        private LineRenderer previewLineRenderer;

        [SerializeField]
        private Transform cueBallAnchor;

        [SerializeField]
        private Transform cueIndicatorTransform;

        [SerializeField]
        private float previewLengthWorldUnits = 2.25f;

        [SerializeField]
        private bool hideWhileBallsAreMoving = true;

        private bool ballsAreMoving;

        public bool IsVisible => previewLineRenderer != null && previewLineRenderer.enabled;

        public void SetBallsMoving(bool areMoving)
        {
            ballsAreMoving = areMoving;
            RefreshVisuals();
        }

        public void SetCueBallAnchor(Transform anchor)
        {
            cueBallAnchor = anchor;
            RefreshVisuals();
        }

        public void SetCueIndicator(Transform indicator)
        {
            cueIndicatorTransform = indicator;
            RefreshVisuals();
        }

        public void RefreshVisuals()
        {
            ResolveReferences();

            if (!TryResolveAimDirection(out var aimDirection))
            {
                HideVisuals();
                return;
            }

            var start = cueBallAnchor != null ? cueBallAnchor.position : transform.position;
            var direction = new Vector3(aimDirection.x, aimDirection.y, 0f).normalized;
            var length = Mathf.Max(0.05f, previewLengthWorldUnits);
            var end = start + (direction * length);

            if (previewLineRenderer != null)
            {
                previewLineRenderer.positionCount = 2;
                previewLineRenderer.SetPosition(0, start);
                previewLineRenderer.SetPosition(1, end);
                previewLineRenderer.enabled = true;
            }

            if (cueIndicatorTransform != null)
            {
                cueIndicatorTransform.position = start;
                cueIndicatorTransform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
                if (!cueIndicatorTransform.gameObject.activeSelf)
                {
                    cueIndicatorTransform.gameObject.SetActive(true);
                }
            }
        }

        private void Awake()
        {
            ResolveReferences();
            RefreshVisuals();
        }

        private void Update()
        {
            RefreshVisuals();
        }

        private bool TryResolveAimDirection(out Vector2 aimDirection)
        {
            aimDirection = Vector2.zero;

            var shouldHideForShotState = hideWhileBallsAreMoving &&
                (ballsAreMoving || (touchAimSwipeController != null && touchAimSwipeController.InputLocked));
            if (shouldHideForShotState)
            {
                return false;
            }

            var remoteIsFresh = remoteSensorInputAdapter != null && remoteSensorInputAdapter.IsRemoteControlActive;
            if (remoteIsFresh)
            {
                aimDirection = remoteSensorInputAdapter.AimDirection;
            }
            else if (touchAimSwipeController != null)
            {
                aimDirection = touchAimSwipeController.AimDirection;
            }
            else if (remoteSensorInputAdapter != null)
            {
                aimDirection = remoteSensorInputAdapter.AimDirection;
            }

            return aimDirection.sqrMagnitude > 0.0001f;
        }

        private void HideVisuals()
        {
            if (previewLineRenderer != null)
            {
                previewLineRenderer.enabled = false;
            }

            if (cueIndicatorTransform != null && cueIndicatorTransform.gameObject.activeSelf)
            {
                cueIndicatorTransform.gameObject.SetActive(false);
            }
        }

        private void ResolveReferences()
        {
            if (touchAimSwipeController == null)
            {
                touchAimSwipeController = GetComponent<TouchAimSwipeController>();
            }

            if (remoteSensorInputAdapter == null)
            {
                remoteSensorInputAdapter = GetComponent<RemoteSensorInputAdapter>();
            }

            if (previewLineRenderer == null)
            {
                previewLineRenderer = GetComponent<LineRenderer>();
            }

            if (cueBallAnchor == null)
            {
                cueBallAnchor = transform;
            }
        }
    }
}
