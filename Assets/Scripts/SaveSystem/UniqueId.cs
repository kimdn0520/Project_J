using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SaveSystem
{
    /// <summary>
    /// Automatically generates a persistent, globally unique ID (GUID) in the Unity Editor.
    /// Used by saveable objects to prevent ID duplication and save state conflicts.
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class UniqueId : MonoBehaviour
    {
        [SerializeField] private string id = "";

        public string Id => id;

        private void Awake()
        {
#if UNITY_EDITOR
            // Generate a new ID if it's empty, and we are in the editor (not playing)
            if (string.IsNullOrEmpty(id) && !Application.isPlaying)
            {
                GenerateId();
            }
#endif
        }

#if UNITY_EDITOR
        [ContextMenu("Generate New ID")]
        public void GenerateId()
        {
            id = System.Guid.NewGuid().ToString();
            EditorUtility.SetDirty(this);
            
            // If part of a prefab stage, mark the prefab as dirty
            if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            }
        }

        // Detect duplication in editor and regenerate ID to prevent duplicate keys
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                GenerateId();
                return;
            }

            // In play mode, do not run validation checks to avoid performance issues
            if (Application.isPlaying) return;

            // Check if this is a scene object (not a prefab asset in the project folder)
            if (gameObject.scene.name == null) return;

            // Check for duplicates in the active scene
            UniqueId[] allIds = FindObjectsByType<UniqueId>(FindObjectsSortMode.None);
            foreach (var other in allIds)
            {
                if (other != this && other.id == this.id)
                {
                    // Conflict found: regenerate GUID for this instance
                    GenerateId();
                    Debug.Log($"[UniqueId] Duplicate ID detected on '{gameObject.name}' and '{other.gameObject.name}'. Regenerated new ID: {id}", this);
                    break;
                }
            }
        }
#endif
    }
}
