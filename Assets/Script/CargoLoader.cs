using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class CargoData
{
    public List<BoxPosition> final_arrangement;
}

[Serializable]
public class BoxPosition
{
    public string box_id;
    public float pos_x;
    public float pos_y;
    public float pos_z;
    public float length;
    public float width;
    public float height;
    public float x_norm;
    public float y_norm;
    public float z_norm;
    public bool placed;
    public float group;
    public bool is_fragile;
    public float weight = 1.0f;
}

[Serializable]
public class TruckData
{
    public float length;
    public float height;
    public float width;
    public float wt_capacity;
}


[Serializable]
public class CargoResponse
{
    public TruckData truck_config;
    public List<BoxPosition> final_arrangement;
}

public class CargoBoxProperties : MonoBehaviour
{
    [Header("Box Properties")]
    public string boxId;
    public float weight = 1.0f;
    public bool isFragile = false;
    public BoxCollider boxCollider;

    // [Header("Constraint Status")]
    // public bool hasOverhangIssue = false;
    // public bool hasOverlapIssue = false;
    // public bool hasInAirIssue = false;
    // public bool isFragileCompromised = false;

    // [Header("Visualization")]
    // public bool showWarnings = true;
    // public Material defaultMaterial;
    // public Material warningMaterial;

    private Renderer boxRenderer;
    public Transform objectToScaleExternally;

    // private void Awake()
    // {
    //     if (boxRenderer != null)
    //         defaultMaterial = boxRenderer.sharedMaterial;
    // }

    // public void SetWarningState(bool hasWarning)
    // {
    //     if (boxRenderer != null && showWarnings && warningMaterial != null)
    //     {
    //         boxRenderer.sharedMaterial = hasWarning ? warningMaterial : defaultMaterial;
    //     }
    // }

    // public void UpdateWarningVisual()
    // {
    //     bool hasAnyIssue = hasOverhangIssue || hasOverlapIssue || hasInAirIssue || isFragileCompromised;
    //     SetWarningState(hasAnyIssue);
    // }

    // public Bounds GetWorldBounds()
    // {
    //     return boxCollider.bounds;
    // }
}

public class CargoLoader : MonoBehaviour
{
    [Header("Cargo References")]
    public GameObject defaultCargoPrefab;
    public GameObject[] cargoPrefabsByGroup;
    public Transform cargoAnchorPoint;

    [Header("Truck References")]
    public Transform truckContainer;
    public Transform cargoBoxCube;

    [Header("API Settings")]
    public string apiBaseUrl = "http://127.0.0.1:8000";

    [Header("Auto-Scaling Settings")]
    public Vector3 maxViewSize = new Vector3(150f, 76f, 71f);
    public float scalingBuffer = 1.0f;
    private float scaleFactor = 1.0f;



    private void Start()
    {
        if (cargoAnchorPoint == null || defaultCargoPrefab == null || truckContainer == null)
        {
            Debug.LogError("Missing required references in inspector");
            return;
        }
        if (cargoBoxCube == null)
        {
            Debug.LogWarning("CargoBoxCube reference is not assigned in inspector");
        }
    }


    public void LoadCargo()
    {
        StartCoroutine(FetchCargoData());
    }

    private IEnumerator FetchCargoData()
    {

        // // for local host
        using (UnityWebRequest www = UnityWebRequest.Get($"{apiBaseUrl}/config"))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to fetch truck config: {www.error}");
                yield break;
            }

            TruckData truckData = JsonUtility.FromJson<TruckData>(www.downloadHandler.text);
            ResizeTruckContainer(truckData.length, truckData.height, truckData.width);
        }

        using (UnityWebRequest www = UnityWebRequest.Get($"{apiBaseUrl}/final_arrangement"))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to fetch cargo data: {www.error}");
                yield break;
            }

            CargoData cargoData = JsonUtility.FromJson<CargoData>(www.downloadHandler.text);
            if (cargoData != null && cargoData.final_arrangement != null && cargoData.final_arrangement.Count > 0)
            {
                PlaceCargo(cargoData.final_arrangement);
            }
            else
            {
                Debug.LogError("Received empty or invalid cargo data");
            }
        }



        // // for 10.119.11.41


        // using (UnityWebRequest www = UnityWebRequest.Get($"{apiBaseUrl}/eachPartitionedData"))
        // {
        //     yield return www.SendWebRequest();

        //     if (www.result != UnityWebRequest.Result.Success)
        //     {
        //         Debug.LogError($"Failed to fetch cargo data: {www.error}");
        //         yield break;
        //     }

        //     // Deserialize the entire JSON into the wrapper class
        //     CargoResponse cargoResponse = JsonUtility.FromJson<CargoResponse>(www.downloadHandler.text);

        //     // Use truck_config to resize container
        //     ResizeTruckContainer(cargoResponse.truck_config.length, cargoResponse.truck_config.height, cargoResponse.truck_config.width);

        //     // Use final_arrangement to place cargo
        //     if (cargoResponse.final_arrangement != null && cargoResponse.final_arrangement.Count > 0)
        //     {
        //         PlaceCargo(cargoResponse.final_arrangement);
        //     }
        //     else
        //     {
        //         Debug.LogError("Received empty or invalid cargo data");
        //     }
        // }



    }

    private void PlaceCargo(List<BoxPosition> boxes)
    {
        ClearExistingCargo();

        foreach (BoxPosition box in boxes)
        {
            GameObject prefab = defaultCargoPrefab;
            int groupIndex = Mathf.FloorToInt(box.group);

            if (cargoPrefabsByGroup != null && groupIndex > 0 && groupIndex <= cargoPrefabsByGroup.Length)
            {
                prefab = cargoPrefabsByGroup[groupIndex - 1];
            }

            GameObject cargoBox = Instantiate(prefab, cargoAnchorPoint);
            cargoBox.name = $"Cargo_{box.box_id}";

            cargoBox.transform.localPosition = new Vector3(
                box.pos_x * scaleFactor,
                box.pos_y * scaleFactor,
                box.pos_z * scaleFactor
            );

            cargoBox.transform.localScale = new Vector3(
                box.length * scaleFactor,
                box.width * scaleFactor,
                box.height * scaleFactor
            );

            if (box.x_norm != 0 || box.y_norm != 0 || box.z_norm != 0)
            {
                Vector3 normalY = new Vector3(0, box.y_norm, 0).normalized;
                Vector3 normalZ = new Vector3(0, 0, box.z_norm).normalized;
                Quaternion rotation = Quaternion.LookRotation(normalZ, normalY);
                cargoBox.transform.localRotation = rotation;
            }
            else
            {
                cargoBox.transform.localRotation = Quaternion.identity;
            }

            CargoBoxProperties boxProps = cargoBox.GetComponent<CargoBoxProperties>();
            if (boxProps == null)
                boxProps = cargoBox.AddComponent<CargoBoxProperties>();

            boxProps.boxId = box.box_id;
            boxProps.weight = box.weight;
            boxProps.isFragile = box.is_fragile;
        }
    }


    private void ResizeTruckContainer(float length, float height, float width)
    {
        // Define max view size
        Vector3 maxViewSize = new Vector3(150f, 76f, 71f);

        // Minimum values for Y and Z axes (40 each)
        float minY = 40f;
        float minZ = 40f;

        // Default to no scaling
        scaleFactor = 1.0f;

        // Only scale down if dimensions exceed max view size
        bool needsScaling = length > maxViewSize.x || height > maxViewSize.y || width > maxViewSize.z;

        if (needsScaling)
        {
            // Calculate scaling factors for each dimension based on max view size
            float scaleX = maxViewSize.x / length;
            float scaleY = maxViewSize.y / height;
            float scaleZ = maxViewSize.z / width;

            // Use the minimal scaling factor to ensure the entire truck fits within max view size
            scaleFactor = Mathf.Min(scaleX, scaleY, scaleZ);

            // Check if height and width meet the minimum requirements after scaling
            float tempScaledHeight = height * scaleFactor;
            float tempScaledWidth = width * scaleFactor;

            if (tempScaledHeight < minY && height > minY)
            {
                float tempScaleY = minY / height;
                scaleFactor = Mathf.Max(scaleFactor, tempScaleY);
            }

            if (tempScaledWidth < minZ && width > minZ)
            {
                float tempScaleZ = minZ / width;
                scaleFactor = Mathf.Max(scaleFactor, tempScaleZ);
            }

            // Apply buffer to prevent clipping
            scaleFactor *= scalingBuffer;
        }

        // Calculate final dimensions with scaling factor (which may be 1.0 if no scaling needed)
        float scaledLength = length * scaleFactor;
        float scaledHeight = height * scaleFactor;
        float scaledWidth = width * scaleFactor;

        // Determine frame height for proper positioning
        float frameHeight = 1.0f * scaleFactor;

        if (cargoBoxCube != null)
        {
            cargoBoxCube.localScale = new Vector3(scaledLength, scaledHeight, scaledWidth);
        }

    }
    private void ClearExistingCargo()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}