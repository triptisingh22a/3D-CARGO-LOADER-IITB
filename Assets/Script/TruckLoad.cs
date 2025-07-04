using System.Collections;
using UnityEngine;
using System.IO;
using System.Text;

public class TruckLoad : MonoBehaviour
{
    public GameObject cargoPrefab;              
    public Transform truckContainer;            
    public Transform cargoAnchorPoint;          
    public float spacing = 1.2f;                
    public float loadDelay = 0.1f;              
    public float groundOffset = 0.05f;          
    public GameObject[] truckPartsToDisable;    
    public string csvFilePath = "Assets/Data/ProductData.csv";

    private int rows, columns, layers;

    private void Start()
    {
        if (cargoPrefab == null || truckContainer == null || cargoAnchorPoint == null)
        {
            Debug.LogError("CargoPrefab, TruckContainer, or CargoAnchorPoint is not assigned.");
        }
    }

    public void OnLoadButtonClick()
    {
        if (cargoAnchorPoint == null || cargoPrefab == null)
        {
            Debug.LogError("CargoPrefab or CargoAnchorPoint is not assigned.");
            return;
        }

        LoadDimensionsFromCSV();
        truckContainer.gameObject.SetActive(true);
        truckContainer.localScale = Vector3.one;
        DisableTruckParts();
        StartCoroutine(LoadCargo());
    }

    private void DisableTruckParts()
    {
        foreach (GameObject part in truckPartsToDisable)
        {
            if (part != null && part != truckContainer.gameObject)
            {
                part.SetActive(false);
            }
        }
    }

    private void LoadDimensionsFromCSV()
    {
        if (!File.Exists(csvFilePath))
        {
            Debug.LogError($"CSV file not found at: {csvFilePath}");
            return;
        }

        string[] lines = File.ReadAllLines(csvFilePath, Encoding.UTF8);

        if (lines.Length < 2)
        {
            Debug.LogError("CSV file is empty or missing data.");
            return;
        }

        string[] values = lines[1].Split(',');

        if (values.Length < 5)
        {
            Debug.LogError("CSV format is incorrect.");
            return;
        }

        if (!int.TryParse(values[1], out columns) ||
            !int.TryParse(values[2], out rows) ||
            !int.TryParse(values[3], out layers))
        {
            Debug.LogError("Failed to parse dimensions from CSV.");
            return;
        }

        Debug.Log($"CSV Loaded: Rows={rows}, Columns={columns}, Layers={layers}");
    }

    private IEnumerator LoadCargo()
    {
        ClearPreviousCargo();
        float boxHeight = cargoPrefab.GetComponent<Renderer>().bounds.size.y;
        float truckFloorHeight = GetTruckFloorHeight();

        for (int y = 0; y < layers; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                for (int z = 0; z < rows; z++)
                {
                    Vector3 localPosition = new Vector3(
                        x * spacing,
                        y * spacing + boxHeight / 2f,
                        z * spacing
                    );

                    GameObject box = Instantiate(cargoPrefab, cargoAnchorPoint);
                    box.transform.localPosition = localPosition; 
                    yield return new WaitForSeconds(loadDelay);
                }
            }
        }

        Debug.Log($"Cargo loaded: {rows}x{columns}x{layers} boxes.");
    }

    private float GetTruckFloorHeight()
    {
        RaycastHit hit;
        Vector3 rayStart = truckContainer.position + Vector3.up * 2f;  
        float rayDistance = 5f;

        // Layer mask for the truck floor
        int floorLayerMask = 1 << LayerMask.NameToLayer("TruckFloor");

        if (Physics.Raycast(rayStart, Vector3.down, out hit, rayDistance, floorLayerMask))
        {
            Debug.Log($"Detected truck surface at: {hit.point.y}");
            Debug.DrawRay(rayStart, Vector3.down * rayDistance, Color.green, 5f);
            return hit.point.y + groundOffset; 
        }

        Debug.LogWarning("Failed to detect truck surface, using default Y.");
        return truckContainer.position.y;
    }

    private void ClearPreviousCargo()
    {
        foreach (Transform child in cargoAnchorPoint)
        {
            Destroy(child.gameObject);
        }
    }
}
