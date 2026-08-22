using UnityEngine;

namespace GyroCue.Practice
{
    /// <summary>
    /// 3D collision hook that captures the cue ball's first numbered object-ball
    /// contact for the current shot. Match orchestration calls BeginShot before the
    /// stroke and reads FirstContactBallNumber after the table settles.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CueBallContactTracker : MonoBehaviour
    {
        public int FirstContactBallNumber { get; private set; } = EightBallShotRecord.NoBall;

        public void BeginShot()
        {
            FirstContactBallNumber = EightBallShotRecord.NoBall;
        }

        public bool TryRecordContact(BallIdentity identity)
        {
            if (identity == null ||
                identity.Group == EightBallGroup.Cue ||
                identity.Group == EightBallGroup.Unassigned ||
                FirstContactBallNumber != EightBallShotRecord.NoBall)
            {
                return false;
            }

            FirstContactBallNumber = identity.BallNumber;
            return true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null || collision.gameObject == null)
            {
                return;
            }

            TryRecordContact(collision.gameObject.GetComponent<BallIdentity>());
        }
    }
}
