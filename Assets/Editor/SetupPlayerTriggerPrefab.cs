using UnityEngine;
using UnityEditor;
using Player;

public class SetupPlayerTriggerPrefab
{
    public static void Execute()
    {
        // 1. Find Player prefabs
        string[] guids = AssetDatabase.FindAssets("Player t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
            
            if (prefabRoot != null && prefabRoot.GetComponent<PlayerController>() != null)
            {
                Debug.Log($"[SetupPlayerTriggerPrefab] Modifying prefab at {path}");
                ConfigurePlayerObject(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        // 2. Modify active scene Player objects if present
        PlayerController[] inScenePlayers = Object.FindObjectsOfType<PlayerController>();
        foreach (var player in inScenePlayers)
        {
            Debug.Log($"[SetupPlayerTriggerPrefab] Modifying scene player object: {player.name}");
            ConfigurePlayerObject(player.gameObject);
            EditorUtility.SetDirty(player.gameObject);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[SetupPlayerTriggerPrefab] Completed successfully!");
    }

    private static void ConfigurePlayerObject(GameObject root)
    {
        Transform triggerTrans = root.transform.Find("Trigger");
        if (triggerTrans == null)
        {
            GameObject triggerGo = new GameObject("Trigger");
            triggerGo.transform.SetParent(root.transform);
            triggerGo.transform.localPosition = new Vector3(0, -0.45f, 0);
            triggerGo.transform.localRotation = Quaternion.identity;
            triggerGo.transform.localScale = Vector3.one;
            triggerTrans = triggerGo.transform;
        }

        // Tag / Layer check
        triggerTrans.gameObject.layer = root.layer;

        // Ensure BoxCollider2D (Trigger)
        BoxCollider2D boxCol = triggerTrans.GetComponent<BoxCollider2D>();
        if (boxCol == null)
        {
            boxCol = triggerTrans.gameObject.AddComponent<BoxCollider2D>();
        }
        boxCol.isTrigger = true;
        boxCol.size = new Vector2(0.75f, 0.75f);
        boxCol.offset = Vector2.zero;

        // Ensure Kinematic Rigidbody2D on child Trigger for instant physics trigger updating
        Rigidbody2D triggerRb = triggerTrans.GetComponent<Rigidbody2D>();
        if (triggerRb == null)
        {
            triggerRb = triggerTrans.gameObject.AddComponent<Rigidbody2D>();
        }
        triggerRb.bodyType = RigidbodyType2D.Kinematic;
        triggerRb.simulated = true;
        triggerRb.useFullKinematicContacts = true;

        // Ensure PlayerTriggerZone
        PlayerTriggerZone triggerZone = triggerTrans.GetComponent<PlayerTriggerZone>();
        if (triggerZone == null)
        {
            triggerZone = triggerTrans.gameObject.AddComponent<PlayerTriggerZone>();
        }

        // Ensure PlayerInteraction references
        PlayerInteraction interaction = root.GetComponent<PlayerInteraction>();
        if (interaction != null)
        {
            SerializedObject so = new SerializedObject(interaction);
            SerializedProperty propTrans = so.FindProperty("triggerTransform");
            SerializedProperty propZone = so.FindProperty("triggerZone");
            SerializedProperty propDownOffset = so.FindProperty("downOffset");

            if (propTrans != null) propTrans.objectReferenceValue = triggerTrans;
            if (propZone != null) propZone.objectReferenceValue = triggerZone;
            if (propDownOffset != null) propDownOffset.vector2Value = new Vector2(0f, -0.95f);
            so.ApplyModifiedProperties();
        }
    }
}
