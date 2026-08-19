using UnityEngine;

namespace GyroCue.Practice
{
    /// <summary>
    /// Primitive cue stick that sits behind the cue ball along the aim line, pulls
    /// back with the draw, and tilts with the elevation control.
    /// </summary>
    public sealed class CueStickView : MonoBehaviour
    {
        private const float StickLengthMetres = 1.45f;
        private const float StickRadiusMetres = 0.008f;
        private const float RestGapMetres = 0.02f;
        private const float MaximumDrawMetres = 0.22f;

        private Transform stick;
        private Transform cueBall;

        public void Configure(Transform cueBallTransform, Material material)
        {
            cueBall = cueBallTransform;

            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            primitive.name = "CueStick";
            Destroy(primitive.GetComponent<Collider>());
            primitive.GetComponent<Renderer>().sharedMaterial = material;
            primitive.transform.SetParent(transform, worldPositionStays: false);

            // Unity cylinders are 2 units tall and stand on Y, so half the length
            // scales it and the parent handles orientation.
            primitive.transform.localScale = new Vector3(
                StickRadiusMetres * 2f,
                StickLengthMetres * 0.5f,
                StickRadiusMetres * 2f);
            primitive.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            primitive.transform.localPosition = new Vector3(0f, 0f, -StickLengthMetres * 0.5f);

            stick = primitive.transform;
        }

        public void SetVisible(bool visible)
        {
            if (stick != null && stick.gameObject.activeSelf != visible)
            {
                stick.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Places the cue for the current aim, draw, strike point, and elevation.
        /// </summary>
        public void UpdatePose(Vector3 aimDirection, float backswing01, Vector2 strikeOffset, float elevationDegrees)
        {
            if (cueBall == null || stick == null)
            {
                return;
            }

            var ballRadius = PracticeTableLayout.BallRadiusMetres;
            var right = Vector3.Cross(Vector3.up, aimDirection).normalized;

            // Offset the butt so the tip lines up with the chosen contact point.
            var tipOffset = ((right * strikeOffset.x) + (Vector3.up * strikeOffset.y)) * ballRadius;
            var drawBack = RestGapMetres + (Mathf.Clamp01(backswing01) * MaximumDrawMetres);

            transform.position = cueBall.position + tipOffset - (aimDirection * drawBack);
            transform.rotation = Quaternion.LookRotation(aimDirection, Vector3.up) *
                                 Quaternion.Euler(Mathf.Clamp(elevationDegrees, 0f, 90f), 0f, 0f);
        }
    }
}
