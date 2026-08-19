using UnityEngine;

namespace GyroCue.Practice
{
    /// <summary>
    /// Drag anywhere outside the stroke and elevation widgets to swing the camera
    /// around the cue ball. Where the camera looks is where the shot goes, so aiming
    /// and framing are the same gesture.
    /// </summary>
    public sealed class OrbitAimController : MonoBehaviour
    {
        [SerializeField, Min(0.05f)]
        private float orbitDegreesPerScreenWidth = 220f;

        [SerializeField]
        private float minimumPitchDegrees = 8f;

        [SerializeField]
        private float maximumPitchDegrees = 72f;

        [SerializeField, Min(0.1f)]
        private float distanceMetres = 1.15f;

        [SerializeField, Min(0f)]
        private float heightOffsetMetres = 0.05f;

        private Camera targetCamera;
        private Transform focus;
        private float yawDegrees;
        private float pitchDegrees = 26f;

        /// <summary>Horizontal aim on the cloth plane, pointing away from the camera.</summary>
        public Vector3 AimDirection
        {
            get
            {
                var yawRadians = yawDegrees * Mathf.Deg2Rad;
                return new Vector3(Mathf.Sin(yawRadians), 0f, Mathf.Cos(yawRadians));
            }
        }

        public float YawDegrees => yawDegrees;

        public float PitchDegrees => pitchDegrees;

        public void Configure(Camera camera, Transform focusTransform)
        {
            targetCamera = camera;
            focus = focusTransform;
            ApplyCameraPose();
        }

        public void SetFocus(Transform focusTransform)
        {
            focus = focusTransform;
        }

        /// <summary>Applies a drag in pixels. Called by the practice input router.</summary>
        public void ApplyDrag(Vector2 dragPixels)
        {
            var screenWidth = Mathf.Max(1, Screen.width);
            yawDegrees += dragPixels.x / screenWidth * orbitDegreesPerScreenWidth;
            pitchDegrees = Mathf.Clamp(
                pitchDegrees - (dragPixels.y / screenWidth * orbitDegreesPerScreenWidth),
                minimumPitchDegrees,
                maximumPitchDegrees);
        }

        private void LateUpdate()
        {
            ApplyCameraPose();
        }

        private void ApplyCameraPose()
        {
            if (targetCamera == null || focus == null)
            {
                return;
            }

            var pivot = focus.position + (Vector3.up * heightOffsetMetres);
            var rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            targetCamera.transform.position = pivot - (rotation * Vector3.forward * distanceMetres);
            targetCamera.transform.rotation = Quaternion.LookRotation(pivot - targetCamera.transform.position, Vector3.up);
        }
    }
}
