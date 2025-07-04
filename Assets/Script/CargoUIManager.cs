using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text;
using System.Collections;

public class CargoUIManager : MonoBehaviour
{
    [Header("Cargo Input Fields")]
    [SerializeField] private TMP_InputField itemNoField;
    [SerializeField] private TMP_InputField lengthField;
    [SerializeField] private TMP_InputField widthField;
    [SerializeField] private TMP_InputField heightField;
    [SerializeField] private TMP_InputField weightField;
    [SerializeField] private TMP_InputField volumeField;
    [SerializeField] private TMP_InputField totalboxField;

    [Header("Cube in Hierarchy")]
    [SerializeField] private GameObject cube;
    [SerializeField] private TMP_Text[] cubeTexts;
    [SerializeField] private GameObject wayUpIcon;
    [SerializeField] private Toggle wayUpToggle;

    [Header("UI Panel Management")]
    [SerializeField] private Transform panelContainer;
    [SerializeField] private GameObject panelPrefab;
    [SerializeField] private Button addItemButton;
    [SerializeField] private Button submitButton; // Added submit button

    // API Settings
    [Header("API Settings")]
    [SerializeField] private string apiUrl = "http://127.0.0.1:8000/start";

    private bool isCubeActive = false;

    private void Start()
    {
        InitializeUI();
        HideCube();

        if (wayUpIcon != null)
            wayUpIcon.gameObject.SetActive(false);

        if (wayUpToggle != null)
            wayUpToggle.onValueChanged.AddListener(ToggleWayUpIcon);

        // Set up submit button to post to API instead of add item button
        if (submitButton != null)
            submitButton.onClick.AddListener(PostDataToAPI);
    }

    private void InitializeUI()
    {
        lengthField.contentType = TMP_InputField.ContentType.IntegerNumber;
        widthField.contentType = TMP_InputField.ContentType.IntegerNumber;
        heightField.contentType = TMP_InputField.ContentType.IntegerNumber;
        weightField.contentType = TMP_InputField.ContentType.DecimalNumber;

        lengthField.onValueChanged.AddListener(OnDimensionInputChanged);
        widthField.onValueChanged.AddListener(OnDimensionInputChanged);
        heightField.onValueChanged.AddListener(OnDimensionInputChanged);
        itemNoField.onEndEdit.AddListener(OnItemNoChanged);

        ClearFields();
    }

    private void ClearFields()
    {
        itemNoField.text = "";
        lengthField.text = "0";
        widthField.text = "0";
        heightField.text = "0";
        weightField.text = "0";
        volumeField.text = "0";
        totalboxField.text = "0";

        HideCube();
        UpdateCubeText("");

        if (wayUpToggle != null)
            wayUpToggle.isOn = false;
    }

    private void OnItemNoChanged(string value)
    {
        Debug.Log($"Item No. changed to: {value}");
        UpdateCubeText(value);
    }

    private void OnDimensionInputChanged(string value)
    {
        if (!isCubeActive &&
            (float.TryParse(lengthField.text, out float length) && length > 0 ||
             float.TryParse(widthField.text, out float width) && width > 0 ||
             float.TryParse(heightField.text, out float height) && height > 0))
        {
            ShowCube();
        }
        UpdateCubeDimensions();
    }

    private void ShowCube()
    {
        if (cube != null)
        {
            cube.SetActive(true);
            isCubeActive = true;
            Debug.Log("Cube is now visible.");
        }
    }

    private void HideCube()
    {
        if (cube != null)
        {
            cube.SetActive(false);
            isCubeActive = false;
            Debug.Log("Cube is now hidden.");
        }
        if (wayUpIcon != null)
            wayUpIcon.gameObject.SetActive(false);
    }

    private void UpdateCubeDimensions()
    {
        if (cube != null &&
            float.TryParse(lengthField.text, out float length) &&
            float.TryParse(widthField.text, out float width) &&
            float.TryParse(heightField.text, out float height))
        {
            cube.transform.localScale = new Vector3(length, height, width);
            Debug.Log($"Cube dimensions updated: Length={length}, Height={height}, Width={width}");

            float volume = length * width * height;
            volumeField.text = volume.ToString("F2");
            Debug.Log($"Volume updated: {volume}");
        }
    }

    private void UpdateCubeText(string text)
    {
        if (cubeTexts != null)
        {
            foreach (var cubeText in cubeTexts)
            {
                if (cubeText != null)
                {
                    cubeText.text = text;
                }
            }
        }
    }

    private void ToggleWayUpIcon(bool isOn)
    {
        if (wayUpIcon != null)
        {
            wayUpIcon.gameObject.SetActive(isOn);
            Debug.Log(isOn ? "This Way Up icon enabled." : "This Way Up icon disabled.");
        }
    }

    public void AddNewPanel()
    {
        if (panelPrefab != null && panelContainer != null)
        {
            Instantiate(panelPrefab, panelContainer);
            Debug.Log("New cargo panel added.");
        }
        else
        {
            Debug.LogWarning("Panel Prefab or Container is missing!");
        }
    }

    // This method posts the current input field data to the API
    public void PostDataToAPI()
    {
        // Validate input fields
        if (string.IsNullOrEmpty(itemNoField.text))
        {
            Debug.LogWarning("Item No is required!");
            return;
        }

        // Parse current input values
        if (!float.TryParse(lengthField.text, out float length) ||
            !float.TryParse(widthField.text, out float width) ||
            !float.TryParse(heightField.text, out float height) ||
            !float.TryParse(weightField.text, out float weight) ||
            !float.TryParse(totalboxField.text, out float quantity))
        {
            Debug.LogWarning("Please enter valid numeric values for all dimensions and quantity!");
            return;
        }

        // Create direct payload with captured values
        StartCoroutine(SendApiRequest(itemNoField.text, length, width, height, weight, quantity));
    }

    private IEnumerator SendApiRequest(string itemNo, float length, float width, float height, float weight, float quantity)
    {
        // Create JSON payload
        string jsonPayload = $@"{{
            ""CONTAINER_LENGTH_IN"": 96,
            ""CONTAINER_WIDTH_IN"": 60,
            ""CONTAINER_HEIGHT_IN"": 72,
            ""CONTAINER_CAPACITY_G"": 2500000,
            ""BOX_TYPES"": {{
                ""XS"": [{length}, {width}, {height}, {weight}, {quantity}]
            }}
        }}";

        // Log the full POST request details
        Debug.Log("=== POST REQUEST ===");
        Debug.Log($"URL: {apiUrl}");
        Debug.Log("Headers:\nContent-Type: application/json");
        Debug.Log($"Body:\n{jsonPayload}");

        using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || 
                www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"API Error: {www.error}");
            }
            else
            {
                Debug.Log("=== RESPONSE ===");
                Debug.Log($"Status Code: {www.responseCode}");
                Debug.Log($"Response Text: {www.downloadHandler.text}");

                // Clear fields after successful submission
                ClearFields();
            }
        }
    }
}