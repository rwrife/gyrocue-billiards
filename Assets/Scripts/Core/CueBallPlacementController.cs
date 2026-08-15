using System;
using UnityEngine;

namespace GyroCue.Core
{
    /// <summary>
    /// Handles cue-ball-in-hand placement after fouls and clamps placement
    /// inside playable table bounds.
    /// </summary>
    public sealed class CueBallPlacementController : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody2D cueBallBody;

        [SerializeField]
        private Vector2 tableCenter = Vector2.zero;

        [SerializeField, Min(0f)]
        private float tableWidth = TableLayoutConstants.TableWidthWorldUnits;

        [SerializeField, Min(0f)]
        private float tableHeight = TableLayoutConstants.TableHeightWorldUnits;

        [SerializeField, Min(0f)]
        private float cushionInset = TableLayoutConstants.CushionThickness;

        [SerializeField, Min(0f)]
        private float placementPadding = 0.02f;

        [SerializeField, Min(0f)]
        private float fallbackCueBallRadius = 0.28f;

        public event Action<Vector2> CueBallPlaced;

        public bool IsPlacementModeActive { get; private set; }

        public Rigidbody2D CueBallBody
        {
            get => cueBallBody;
            set => cueBallBody = value;
        }

        public bool BeginCueBallInHand()
        {
            if (cueBallBody == null)
            {
                return false;
            }

            cueBallBody.gameObject.SetActive(true);
            cueBallBody.velocity = Vector2.zero;
            cueBallBody.angularVelocity = 0f;
            cueBallBody.simulated = false;
            IsPlacementModeActive = true;
            return true;
        }

        public bool TryPlaceCueBall(Vector2 desiredWorldPosition, out Vector2 placedWorldPosition)
        {
            placedWorldPosition = default;

            if (!IsPlacementModeActive || cueBallBody == null)
            {
                return false;
            }

            var cueBallRadius = ResolveCueBallRadius(cueBallBody);
            placedWorldPosition = ClampToPlayableBounds(desiredWorldPosition, cueBallRadius);

            cueBallBody.position = placedWorldPosition;
            cueBallBody.transform.position = placedWorldPosition;
            cueBallBody.velocity = Vector2.zero;
            cueBallBody.angularVelocity = 0f;
            cueBallBody.simulated = true;

            IsPlacementModeActive = false;
            CueBallPlaced?.Invoke(placedWorldPosition);
            return true;
        }

        public Vector2 ClampToPlayableBounds(Vector2 desiredWorldPosition, float cueBallRadiusWorldUnits)
        {
            var effectiveRadius = Mathf.Max(0f, cueBallRadiusWorldUnits);
            var halfPlayableWidth = Mathf.Max(0f, (tableWidth * 0.5f) - cushionInset - effectiveRadius - placementPadding);
            var halfPlayableHeight = Mathf.Max(0f, (tableHeight * 0.5f) - cushionInset - effectiveRadius - placementPadding);

            return new Vector2(
                Mathf.Clamp(desiredWorldPosition.x, tableCenter.x - halfPlayableWidth, tableCenter.x + halfPlayableWidth),
                Mathf.Clamp(desiredWorldPosition.y, tableCenter.y - halfPlayableHeight, tableCenter.y + halfPlayableHeight));
        }

        private float ResolveCueBallRadius(Rigidbody2D body)
        {
            if (body.TryGetComponent<CircleCollider2D>(out var circleCollider))
            {
                var scale = body.transform.lossyScale;
                var scaleMagnitude = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                var resolved = circleCollider.radius * Mathf.Max(0.01f, scaleMagnitude);
                if (resolved > 0f)
                {
                    return resolved;
                }
            }

            return Mathf.Max(0f, fallbackCueBallRadius);
        }
    }
}
