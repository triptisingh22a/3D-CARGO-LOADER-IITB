using UnityEngine;

public class VehicleSwitcher : MonoBehaviour
{
    public GameObject[] vehicles; // Assign vehicle GameObjects in the Inspector

    private void Start()
    {
        // Disable all vehicles initially
        foreach (GameObject vehicle in vehicles)
        {
            vehicle.SetActive(false);
        }

        // Enable the first vehicle by default
        if (vehicles.Length > 0)
        {
            vehicles[0].SetActive(true);
        }
    }

    public void ActivateVehicle(int index)
    {
        if (index < 0 || index >= vehicles.Length) return;

        // Disable all vehicles
        foreach (GameObject vehicle in vehicles)
        {
            vehicle.SetActive(false);
        }

        // Enable the selected vehicle
        vehicles[index].SetActive(true);
    }
}
