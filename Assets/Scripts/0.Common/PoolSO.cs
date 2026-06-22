using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PoolSO", menuName = "Scriptable Objects/PoolSO")]
public class PoolSO : ScriptableObject
{
    public PoolPreset[] presets;
}

[System.Serializable]
public class PoolPreset
{
    public string name;
    public GameObject prefab;
    public int initialCount = 5;
}