using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

public class SavePrefabPositions : MonoBehaviour
{
    [SerializeField] private Button saveButton;
    [SerializeField] private Transform anchorPoint; // Reference to the parent GameObject containing all prefabs
    
    private string savePath => Path.Combine(Application.persistentDataPath, "prefab_positions.json");

    [System.Serializable]
    public class PrefabData
    {
        public string name;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
    }

    [System.Serializable]
    public class PrefabDataList
    {
        public List<PrefabData> items = new List<PrefabData>();
    }

    void Start()
    {
        saveButton.onClick.AddListener(SavePositionsToJson);
        
        // If anchorPoint is not assigned, try to find it automatically
        if (anchorPoint == null)
        {
            GameObject anchorGO = GameObject.Find("AnchorPoint");
            if (anchorGO != null)
            {
                anchorPoint = anchorGO.transform;
                Debug.Log("Automatically found AnchorPoint: " + anchorPoint.name);
            }
            else
            {
                Debug.LogWarning("AnchorPoint not found! Please assign it manually in the inspector.");
            }
        }
    }

    void SavePositionsToJson()
    {
        if (anchorPoint == null)
        {
            Debug.LogError("AnchorPoint is not assigned! Cannot save prefab positions.");
            return;
        }

        PrefabDataList dataList = new PrefabDataList();

        // Get all child objects from the anchor point
        Transform[] allChildren = anchorPoint.GetComponentsInChildren<Transform>();

        foreach (Transform child in allChildren)
        {
            // Skip the anchor point itself
            if (child == anchorPoint)
                continue;

            // Only save direct children of anchor point (not nested children)
            if (child.parent != anchorPoint)
                continue;

            PrefabData data = new PrefabData
            {
                name = child.name,
                position = child.position,
                rotation = child.eulerAngles,
                scale = child.localScale
            };
            dataList.items.Add(data);
        }

        if (dataList.items.Count == 0)
        {
            Debug.LogWarning("No prefab instances found under AnchorPoint!");
            return;
        }

        string json = JsonUtility.ToJson(dataList, true);
        File.WriteAllText(savePath, json);

        Debug.Log($"Saved {dataList.items.Count} prefab positions to:\n{savePath}");
        
        // Optional: Print saved prefab names for verification
        Debug.Log("Saved prefabs: " + string.Join(", ", dataList.items.ConvertAll(x => x.name)));
    }

    // Optional: Method to load positions back (you can call this from another button)
    public void LoadPositionsFromJson()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("Save file not found at: " + savePath);
            return;
        }

        if (anchorPoint == null)
        {
            Debug.LogError("AnchorPoint is not assigned! Cannot load prefab positions.");
            return;
        }

        string json = File.ReadAllText(savePath);
        PrefabDataList dataList = JsonUtility.FromJson<PrefabDataList>(json);

        foreach (PrefabData data in dataList.items)
        {
            // Find the prefab by name under anchor point
            Transform prefabTransform = anchorPoint.Find(data.name);
            
            if (prefabTransform != null)
            {
                prefabTransform.position = data.position;
                prefabTransform.eulerAngles = data.rotation;
                prefabTransform.localScale = data.scale;
            }
            else
            {
                Debug.LogWarning($"Prefab '{data.name}' not found under AnchorPoint when loading!");
            }
        }

        Debug.Log($"Loaded {dataList.items.Count} prefab positions from save file.");
    }
}