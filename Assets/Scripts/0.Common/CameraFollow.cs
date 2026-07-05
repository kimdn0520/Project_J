using UnityEngine;

namespace Core
{
    /// <summary>
    /// Smoothly follows the player character and supports dynamic map boundaries.
    /// Place this on the Main Camera in the Persistent scene.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform target;
        [SerializeField] private string playerTag = "Player";

        [Header("Follow Settings")]
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

        [Header("Boundary Settings (Optional)")]
        [SerializeField] private bool useBounds = false;
        [SerializeField] private Vector2 minBounds;
        [SerializeField] private Vector2 maxBounds;

        private void Start()
        {
            if (target == null)
            {
                FindPlayerTarget();
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                FindPlayerTarget();
                return;
            }

            Vector3 targetPosition = target.position + offset;
            
            // Apply smooth movement using Lerp
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

            // Clamp values if map boundaries are defined
            if (useBounds)
            {
                float clampedX = Mathf.Clamp(smoothedPosition.x, minBounds.x, maxBounds.x);
                float clampedY = Mathf.Clamp(smoothedPosition.y, minBounds.y, maxBounds.y);
                smoothedPosition = new Vector3(clampedX, clampedY, smoothedPosition.z);
            }

            transform.position = smoothedPosition;
        }

        private void FindPlayerTarget()
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                target = player.transform;
            }
        }

        /// <summary>
        /// Updates the camera boundaries dynamically (e.g., when transitioning to a new map).
        /// </summary>
        public void SetBounds(Vector2 min, Vector2 max)
        {
            minBounds = min;
            maxBounds = max;
            useBounds = true;
        }

        /// <summary>
        /// Disables boundary checking so camera can follow player infinitely.
        /// </summary>
        public void DisableBounds()
        {
            useBounds = false;
        }
    }
}
