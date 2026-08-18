using System;
using System.Collections.Generic;
using UnityEngine;

namespace GyroCue.Core
{
    /// <summary>
    /// Central pocket-resolution service: de-duplicates trigger contacts,
    /// removes pocketed balls from simulation, and surfaces scratch signals
    /// for the rules/turn layer.
    /// </summary>
    public sealed class PocketTableController : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody2D cueBallBody;

        [SerializeField]
        private string cueBallTag = "CueBall";

        [SerializeField]
        private bool deactivatePocketedBalls = true;

        private readonly HashSet<int> pocketedBodyInstanceIds = new HashSet<int>();

        public event Action<PocketedBallEvent> BallPocketed;

        public event Action<PocketedBallEvent> CueBallScratched;

        public bool ScratchOccurredThisTurn { get; private set; }

        public int PocketedBallCount => pocketedBodyInstanceIds.Count;

        public int ObjectBallsPocketedThisTurn { get; private set; }

        public Rigidbody2D CueBallBody
        {
            get => cueBallBody;
            set => cueBallBody = value;
        }

        /// <summary>
        /// Clears turn-scoped scratch state so the rules layer can start
        /// evaluating a fresh shot/turn.
        /// </summary>
        public void BeginTurnPocketTracking()
        {
            ScratchOccurredThisTurn = false;
            ObjectBallsPocketedThisTurn = 0;

            // The cue ball returns to play after ball-in-hand, unlike object balls.
            // Remove its prior scratch marker so a later turn can detect a new scratch.
            if (cueBallBody != null)
            {
                pocketedBodyInstanceIds.Remove(cueBallBody.GetInstanceID());
            }
        }

        public bool TryPocketFromTrigger(Collider2D other, Vector2 pocketPosition)
        {
            if (other == null)
            {
                return false;
            }

            var body = other.attachedRigidbody;
            if (body == null)
            {
                body = other.GetComponentInParent<Rigidbody2D>();
            }

            return TryPocketBody(body, pocketPosition);
        }

        public bool TryPocketBody(Rigidbody2D body, Vector2 pocketPosition)
        {
            if (body == null)
            {
                return false;
            }

            var instanceId = body.GetInstanceID();
            if (!pocketedBodyInstanceIds.Add(instanceId))
            {
                return false;
            }

            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;

            if (deactivatePocketedBalls)
            {
                body.gameObject.SetActive(false);
            }

            var isCueBall = IsCueBall(body);
            var pocketEvent = new PocketedBallEvent(body, pocketPosition, isCueBall);

            BallPocketed?.Invoke(pocketEvent);

            if (isCueBall)
            {
                ScratchOccurredThisTurn = true;
                CueBallScratched?.Invoke(pocketEvent);
            }
            else
            {
                ObjectBallsPocketedThisTurn++;
            }

            return true;
        }

        public bool WasPocketed(Rigidbody2D body)
        {
            return body != null && pocketedBodyInstanceIds.Contains(body.GetInstanceID());
        }

        private bool IsCueBall(Rigidbody2D body)
        {
            if (cueBallBody != null)
            {
                return body == cueBallBody;
            }

            return !string.IsNullOrWhiteSpace(cueBallTag) && body.CompareTag(cueBallTag);
        }
    }

    public readonly struct PocketedBallEvent
    {
        public PocketedBallEvent(Rigidbody2D body, Vector2 pocketPosition, bool isCueBall)
        {
            Body = body;
            PocketPosition = pocketPosition;
            IsCueBall = isCueBall;
        }

        public Rigidbody2D Body { get; }

        public Vector2 PocketPosition { get; }

        public bool IsCueBall { get; }
    }
}
