using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VehicleUIManager : MonoBehaviour
{
    [Header("Containers Input Fields")]
    [SerializeField] private TMP_InputField lengthField;
    [SerializeField] private TMP_InputField widthField;
    [SerializeField] private TMP_InputField heightField;
    [SerializeField] private TMP_InputField capacityField;
    [SerializeField] private TMP_InputField volumeField;

    [Header("Truck Object")]
    [SerializeField] private GameObject truck; // Reference to the truck GameObject

    private void Start()
    {
        InitializeUI();
        HideTruck(); // Hide the truck initially
    }

    private void InitializeUI()
    {
        // Add listeners to input fields
        lengthField.onEndEdit.AddListener(OnLengthChanged);
        widthField.onEndEdit.AddListener(OnWidthChanged);
        heightField.onEndEdit.AddListener(OnHeightChanged);
        capacityField.onEndEdit.AddListener(OnCapacityChanged);
        volumeField.onEndEdit.AddListener(OnVolumeChanged);

        // Initialize fields with default values
        ClearFields();
    }

    private void ClearFields()
    {
        lengthField.text = "0";
        widthField.text = "0";
        heightField.text = "0";
        capacityField.text = "0";
        volumeField.text = "0";
        HideTruck(); // Reset truck visibility when fields are cleared
    }

    #region Input Field Handlers

    private void OnLengthChanged(string value)
    {
        if (float.TryParse(value, out float length))
        {
            Debug.Log($"Length changed to: {length}");
            UpdateVolumeCalculation();
        }
    }

    private void OnWidthChanged(string value)
    {
        if (float.TryParse(value, out float width))
        {
            Debug.Log($"Width changed to: {width}");
            UpdateVolumeCalculation();
        }
    }

    private void OnHeightChanged(string value)
    {
        if (float.TryParse(value, out float height))
        {
            Debug.Log($"Height changed to: {height}");
            UpdateVolumeCalculation();
        }
    }

    private void OnCapacityChanged(string value)
    {
        if (float.TryParse(value, out float capacity))
        {
            Debug.Log($"Capacity changed to: {capacity}");
        }
    }

    private void OnVolumeChanged(string value)
    {
        if (float.TryParse(value, out float volume))
        {
            Debug.Log($"Volume changed to: {volume}");
        }
    }

    private void UpdateVolumeCalculation()
    {
        if (float.TryParse(lengthField.text, out float length) &&
            float.TryParse(widthField.text, out float width) &&
            float.TryParse(heightField.text, out float height))
        {
            // Calculate volume in cubic units (L x B x H)
            float volume = length * width * height;
            volumeField.text = volume.ToString("F2");
            Debug.Log($"Volume calculated: {volume} cubic units");

            // Show truck if all inputs are valid
            ShowTruck();
        }
        else
        {
            // Hide the truck if any input is invalid
            HideTruck();
        }
    }
    #endregion

    #region Truck Visibility Methods
    private void ShowTruck()
    {
        if (truck != null)
        {
            truck.SetActive(true);
            Debug.Log("Truck is now visible.");
        }
    }

    private void HideTruck()
    {
        if (truck != null)
        {
            truck.SetActive(false);
            Debug.Log("Truck is now hidden.");
        }
    }
    #endregion
}
