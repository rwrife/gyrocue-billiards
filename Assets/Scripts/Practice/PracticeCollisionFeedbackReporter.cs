using System;
using UnityEngine;

namespace GyroCue.Practice
{
    /// <summary>Reports meaningful 3D ball and cushion impacts to the feedback mixer.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PracticeCollisionFeedbackReporter : MonoBehaviour
    {
        private const float MinimumAudibleSpeed = 0.08f;
        private const float FullIntensitySpeed = 4f;

        public event Action<PracticeFeedbackCue, float> ContactFeedback;

        public bool TryReportContact(
            bool otherIsBall,
            string otherName,
            float relativeSpeed,
            int otherInstanceId)
        {
            if (relativeSpeed < MinimumAudibleSpeed ||
                !PracticeContactClassifier.TryClassify(otherIsBall, otherName, out var cue))
            {
                return false;
            }

            // Both balls receive the same PhysX callback. Let the lower instance ID
            // report it once; cushion contacts have only one ball-side reporter.
            if (otherIsBall && gameObject.GetInstanceID() > otherInstanceId)
            {
                return false;
            }

            ContactFeedback?.Invoke(cue, Mathf.Clamp01(relativeSpeed / FullIntensitySpeed));
            return true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null || collision.gameObject == null)
            {
                return;
            }

            var other = collision.gameObject;
            TryReportContact(
                other.GetComponent<BallIdentity>() != null,
                other.name,
                collision.relativeVelocity.magnitude,
                other.GetInstanceID());
        }
    }
}
