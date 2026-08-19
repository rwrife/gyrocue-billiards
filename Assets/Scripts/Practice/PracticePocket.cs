using System;
using UnityEngine;

namespace GyroCue.Practice
{
    /// <summary>
    /// A pocket mouth. Reports balls whose centre drops inside it.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class PracticePocket : MonoBehaviour
    {
        public event Action<Rigidbody, PracticePocket> BallEntered;

        private void OnTriggerEnter(Collider other)
        {
            var body = other.attachedRigidbody;
            if (body != null)
            {
                BallEntered?.Invoke(body, this);
            }
        }
    }
}
