using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace SaveSystem
{
    [Serializable]
    public struct SceneMapping
    {
        public string sceneName;
        public string displayName;
    }

    /// <summary>
    /// Central manager for saving and loading game states.
    /// Manages active playtime, save slots, and encryption.
    /// </summary>
    public class SaveManager : SingletonMonoBehaviour<SaveManager>
    {
        [Header("Scene Configuration")]
        [SerializeField] private List<SceneMapping> sceneDisplayNameMappings = new List<SceneMapping>();

        [Header("Encryption Settings")]
        [SerializeField] private bool useObfuscation = true;
        private static readonly byte[] obfuscationKey = Encoding.UTF8.GetBytes("AntigravitySaveObfuscationKey2026");

        // The currently active save data during gameplay
        public SaveData CurrentSaveData { get; private set; }

        private bool isGamePlaying = false;
        
        // Events for extensible state saving/loading
        public static event Action<SaveData> OnBeforeSave;
        public static event Action<SaveData> OnAfterLoad;

        protected override void Awake()
        {
            base.Awake();
            CurrentSaveData = new SaveData();
        }

        private void Update()
        {
            if (isGamePlaying && CurrentSaveData != null)
            {
                CurrentSaveData.playTime += Time.deltaTime;
            }
        }

        /// <summary>
        /// Starts a new game from scratch.
        /// </summary>
        public void StartNewGame(string startSceneName, string startSpawnId)
        {
            CurrentSaveData = new SaveData
            {
                slotIndex = -1,
                sceneName = startSceneName,
                spawnPointId = startSpawnId,
                playTime = 0f,
                saveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            isGamePlaying = true;

            if (PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.ResetStatus();
                PlayerStatus.Instance.PopulateSaveData(CurrentSaveData);
            }

            if (MapSystem.SceneTransitionManager.Instance != null)
            {
                MapSystem.SceneTransitionManager.Instance.LoadSceneAsync(startSceneName, startSpawnId).Forget();
            }
        }

        /// <summary>
        /// Saves the current game state to the specified slot.
        /// </summary>
        public void SaveGame(int slotIndex)
        {
            if (CurrentSaveData == null)
            {
                Debug.LogWarning("[SaveManager] No active save data to save.");
                return;
            }

            CurrentSaveData.slotIndex = slotIndex;
            CurrentSaveData.saveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 1. Get Scene Location Info
            CurrentSaveData.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            CurrentSaveData.mapDisplayName = GetMapDisplayName(CurrentSaveData.sceneName);

            // 2. Get Player Position & Rotation
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                CurrentSaveData.playerPosition = new float[] { player.transform.position.x, player.transform.position.y, player.transform.position.z };
                CurrentSaveData.playerRotation = new float[] { player.transform.rotation.x, player.transform.rotation.y, player.transform.rotation.z, player.transform.rotation.w };
            }

            // 3. Get Player Status Info
            if (PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.PopulateSaveData(CurrentSaveData);
            }

            // 4. Trigger external observers to write custom states
            OnBeforeSave?.Invoke(CurrentSaveData);

            // 5. Serialize and Write to Disk
            try
            {
                string json = JsonUtility.ToJson(CurrentSaveData, true);
                string dataToWrite = useObfuscation ? Obfuscate(json) : json;

                string filePath = GetSaveFilePath(slotIndex);
                File.WriteAllText(filePath, dataToWrite, Encoding.UTF8);
                Debug.Log($"[SaveManager] Saved successfully to: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save game to slot {slotIndex}: {e.Message}");
            }
        }

        /// <summary>
        /// Loads the game state from the specified slot.
        /// </summary>
        public void LoadGame(int slotIndex)
        {
            LoadGameAsync(slotIndex).Forget();
        }

        private async UniTaskVoid LoadGameAsync(int slotIndex)
        {
            SaveData loadedData = LoadRawData(slotIndex);
            if (loadedData == null)
            {
                Debug.LogWarning($"[SaveManager] No save data found or failed to load slot {slotIndex}");
                return;
            }

            CurrentSaveData = loadedData;
            isGamePlaying = true;

            // 1. Restore Player Status Info
            if (PlayerStatus.Instance != null)
            {
                PlayerStatus.Instance.LoadFromSaveData(CurrentSaveData);
            }

            // 2. Extract coordinates
            Vector3? playerPos = null;
            Quaternion? playerRot = null;
            if (CurrentSaveData.playerPosition != null && CurrentSaveData.playerPosition.Length == 3)
            {
                playerPos = new Vector3(CurrentSaveData.playerPosition[0], CurrentSaveData.playerPosition[1], CurrentSaveData.playerPosition[2]);
            }
            if (CurrentSaveData.playerRotation != null && CurrentSaveData.playerRotation.Length == 4)
            {
                playerRot = new Quaternion(CurrentSaveData.playerRotation[0], CurrentSaveData.playerRotation[1], CurrentSaveData.playerRotation[2], CurrentSaveData.playerRotation[3]);
            }

            // 3. Load Map & Transition Scene
            if (MapSystem.SceneTransitionManager.Instance != null)
            {
                await MapSystem.SceneTransitionManager.Instance.LoadSceneAsync(
                    CurrentSaveData.sceneName, 
                    CurrentSaveData.spawnPointId, 
                    playerPos, 
                    playerRot
                );
            }

            // 4. Trigger external observers to restore their custom states
            OnAfterLoad?.Invoke(CurrentSaveData);
            
            Debug.Log($"[SaveManager] Loaded successfully from slot {slotIndex}");
        }

        /// <summary>
        /// Loads and decrypts save data for UI preview without applying it to the game.
        /// </summary>
        public SaveData GetSaveDataPreview(int slotIndex)
        {
            return LoadRawData(slotIndex);
        }

        /// <summary>
        /// Checks if a save slot file exists.
        /// </summary>
        public bool HasSaveData(int slotIndex)
        {
            return File.Exists(GetSaveFilePath(slotIndex));
        }

        /// <summary>
        /// Deletes the save file in the specified slot.
        /// </summary>
        public void DeleteSaveData(int slotIndex)
        {
            string filePath = GetSaveFilePath(slotIndex);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"[SaveManager] Deleted save file at: {filePath}");
            }
        }

        private SaveData LoadRawData(int slotIndex)
        {
            string filePath = GetSaveFilePath(slotIndex);
            if (!File.Exists(filePath)) return null;

            try
            {
                string rawContent = File.ReadAllText(filePath, Encoding.UTF8);
                string json = useObfuscation ? Deobfuscate(rawContent) : rawContent;
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Error reading file at slot {slotIndex}: {e.Message}");
                return null;
            }
        }

        private string GetSaveFilePath(int slotIndex)
        {
            return Path.Combine(Application.persistentDataPath, $"save_slot_{slotIndex}.sav");
        }

        private string GetMapDisplayName(string sceneName)
        {
            var mapping = sceneDisplayNameMappings.Find(m => m.sceneName == sceneName);
            return mapping.sceneName != null ? mapping.displayName : sceneName;
        }

        private string Obfuscate(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] resultBytes = new byte[inputBytes.Length];
            for (int i = 0; i < inputBytes.Length; i++)
            {
                resultBytes[i] = (byte)(inputBytes[i] ^ obfuscationKey[i % obfuscationKey.Length]);
            }
            return Convert.ToBase64String(resultBytes);
        }

        private string Deobfuscate(string base64Input)
        {
            byte[] inputBytes = Convert.FromBase64String(base64Input);
            byte[] resultBytes = new byte[inputBytes.Length];
            for (int i = 0; i < inputBytes.Length; i++)
            {
                resultBytes[i] = (byte)(inputBytes[i] ^ obfuscationKey[i % obfuscationKey.Length]);
            }
            return Encoding.UTF8.GetString(resultBytes);
        }
    }
}
