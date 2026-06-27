using UnityEngine;

namespace MapSystem
{
    /// <summary>
    /// Represents a spawn point in a map scene.
    /// SceneTransitionManager uses this to locate and position the player upon scene entry.
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private string spawnId;

        /// <summary>
        /// Unique identifier for this spawn point within the scene.
        /// </summary>
        public string SpawnId => spawnId;

        private void OnDrawGizmos()
        {
            // Draw a green wire sphere at the spawn point position in the Editor
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // Draw a line pointing "up" (or forward) to show spawn direction
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + transform.up * 0.5f);
        }
    }
}
