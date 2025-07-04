using UnityEngine;
using UnityEngine.UI;

public class ToggleUpwayVisibility : MonoBehaviour
{
    public Toggle visibilityToggle;
    public GameObject prefabInstance; 
    private GameObject upwayPart;

    void Start()
    {
        if (prefabInstance == null)
        {
            Debug.LogError("❌ Prefab instance is NULL! Assign the correct clone.");
            return;
        }

        // Find Upway INSIDE the cloned prefab
        upwayPart = prefabInstance.transform.Find("Upway")?.gameObject;

        if (upwayPart == null)
        {
            Debug.LogError("❌ Upway NOT FOUND inside cloned prefab! Check the name.");
            return;
        }
        else
        {
            Debug.Log("✅ Upway FOUND inside cloned prefab!");
            upwayPart.SetActive(false); // Hide at start
        }

        // Add Toggle listener
        visibilityToggle.onValueChanged.RemoveAllListeners();
        visibilityToggle.onValueChanged.AddListener(SetVisibility);
    }

   void SetVisibility(bool isVisible)
{
    Debug.Log("✅ Toggle clicked: " + isVisible);

    if (upwayPart == null)
    {
        Debug.LogError("❌ Upway is NULL! Cannot change visibility.");
        return;
    }

    upwayPart.SetActive(isVisible);
    Canvas.ForceUpdateCanvases(); // Force UI update
    Debug.Log("🔄 Upway visibility changed to: " + isVisible + " | Active: " + upwayPart.activeSelf);
}

}
