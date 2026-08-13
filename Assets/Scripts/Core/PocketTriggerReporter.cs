using UnityEngine;

namespace GyroCue.Core
{
    /// <summary>
    /// Attach to pocket trigger colliders to forward entering balls to the
    /// central pocket table controller.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class PocketTriggerReporter : MonoBehaviour
    {
        [SerializeField]
        private PocketTableController pocketTableController;

        private void Reset()
        {
            pocketTableController = GetComponentInParent<PocketTableController>();

            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (pocketTableController == null)
            {
                return;
            }

            pocketTableController.TryPocketFromTrigger(other, (Vector2)transform.position);
        }
    }
}
