using System;
using UnityEngine;

namespace GyroCue.Practice
{
    /// <summary>
    /// Stable numbered-ball identity for runtime-built 3D balls. Rules never infer a
    /// ball from its GameObject name, material, colour, or rack-list position.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BallIdentity : MonoBehaviour
    {
        [SerializeField, Range(EightBallBall.CueBallNumber, EightBallBall.LastObjectBallNumber)]
        private int ballNumber;

        public int BallNumber => ballNumber;

        public EightBallGroup Group => EightBallBall.GroupFor(ballNumber);

        public void Configure(int configuredBallNumber)
        {
            if (configuredBallNumber < EightBallBall.CueBallNumber ||
                configuredBallNumber > EightBallBall.LastObjectBallNumber)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(configuredBallNumber),
                    "A pool ball number must be between 0 (cue) and 15.");
            }

            ballNumber = configuredBallNumber;
        }
    }
}
