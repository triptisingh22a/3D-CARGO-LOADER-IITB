using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VehicleTabManager : MonoBehaviour
{
    public GameObject tabPrefab; // Assign your Vehicle tab button prefab
    public Transform tabParent; // Assign the Horizontal Layout Group here
    private int vehicleCount = 1;

    public void AddNewVehicle()
    {
        vehicleCount++;
        GameObject newTab = Instantiate(tabPrefab, tabParent);
        newTab.GetComponentInChildren<TMP_Text>().text = "Vehicle-" + vehicleCount;
        newTab.GetComponent<Button>().onClick.AddListener(() => OnTabClicked(vehicleCount));
    }

    void OnTabClicked(int vehicleNumber)
    {
        Debug.Log("Vehicle " + vehicleNumber + " clicked!");
        // Implement logic to switch views or load specific vehicle data
    }
}
