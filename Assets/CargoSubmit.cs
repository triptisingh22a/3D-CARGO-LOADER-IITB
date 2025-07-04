using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;
using System;

[System.Serializable]
public class BoxData
{
    public string boxType;
    public string length; 
    public string width;   
    public string height;  
    public string weight;  
    public string volume; 
    public string quantity;
}

[System.Serializable]
public class BoxPayload
{
    public String CONTAINER_LENGTH_IN = "96";
    public String CONTAINER_WIDTH_IN = "60";
    public String CONTAINER_HEIGHT_IN = "72";
    public String CONTAINER_CAPACITY_G = "2500000";

    public List<BoxData> BOX_TYPES = new List<BoxData>();
}

[System.Serializable]
public class BoxFormInput
{
    public string boxType;
    public string length;
    public string width;
    public string height;
    public string weight;
    public string volume;
    public string quantity; 

    public void PrintToConsole()
    {
        Debug.Log(" Box Form Input (Raw Strings):");
        Debug.Log($"Type: {boxType}, Length: {length}, Width: {width}, Height: {height}, Weight: {weight}, Volume: {volume}, Quantity: {quantity}");
    }
}

public class BoxPropertyReader : MonoBehaviour
{
    public TextMeshProUGUI boxTypeText;
    public TextMeshProUGUI lengthText;
    public TextMeshProUGUI widthText;
    public TextMeshProUGUI heightText;
    public TextMeshProUGUI weightText;
    public TextMeshProUGUI volumeText;
    public TextMeshProUGUI  quantitText;
    
    public void OnSubmit()
    {
        BoxFormInput input = new BoxFormInput
        {
            boxType = boxTypeText.text.Trim(),
            length = lengthText.text.Trim(),
            width = widthText.text.Trim(),
            height = heightText.text.Trim(),
            weight = weightText.text.Trim(),
            volume = volumeText.text.Trim(),
            quantity = quantitText.text.Trim()
        };
        string json = JsonUtility.ToJson(input, true);
        string folder = Application.streamingAssetsPath;
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        string filePath = Path.Combine(folder, "box_data.json");
        File.WriteAllText(filePath, json);
        Debug.Log($"Box data saved to: {filePath}");
    }
}

public class CargoSubmit : MonoBehaviour
{
    [Header("Box Panels")]
    [SerializeField] private List<BoxPanelConfiguration> boxPanelConfigs = new List<BoxPanelConfiguration>();

    [Header("Submit Button")]
    [SerializeField] private GameObject submitButtonObject;

    [System.Serializable]
    public class BoxPanelConfiguration
    {
        [Header("Direct Text References")]
        public TextMeshProUGUI lengthText;
        public TextMeshProUGUI widthText;
        public TextMeshProUGUI heightText;
        public TextMeshProUGUI weightText;
        public TextMeshProUGUI volumeText;
        public TextMeshProUGUI typeText;
        public TextMeshProUGUI quantityText;
    }

    private void Start()
    {
        if (submitButtonObject != null)
        {
            Button submitButton = submitButtonObject.GetComponent<Button>();
            if (submitButton != null)
            {
                submitButton.onClick.AddListener(SaveToJSON);
                Debug.Log(" Submit button listener added successfully");
            }
            else
            {
                Debug.LogError(" Button component not found on the assigned GameObject!");
            }
        }
        else
        {
            Debug.LogError(" Submit Button GameObject not assigned in the inspector!");
        }
    }

    public void SaveToJSON()
    {
        BoxPayload payload = new BoxPayload();
        Debug.Log("🚀 Starting to collect data from " + boxPanelConfigs.Count + " panels");

        for (int i = 0; i < boxPanelConfigs.Count; i++)
        {
            BoxPanelConfiguration config = boxPanelConfigs[i];
            BoxFormInput formInput = ExtractFormInput(config);
            formInput.PrintToConsole();

            // Add more detailed debug information
            Debug.Log($"Panel {i+1} - Type: '{formInput.boxType}', Length: '{formInput.length}', " +
                      $"Width: '{formInput.width}', Height: '{formInput.height}', " +
                      $"Weight: '{formInput.weight}', Volume: '{formInput.volume}, Quantity: '{formInput.quantity}'");

            // Directly use the input strings without parsing
            BoxData box = new BoxData
            {
                boxType = string.IsNullOrEmpty(formInput.boxType) ? "Box" : formInput.boxType,
                length = formInput.length,
                width = formInput.width,
                height = formInput.height,
                weight = formInput.weight,
                volume = formInput.volume,
                quantity= formInput.quantity
            };
            
            payload.BOX_TYPES.Add(box);
            Debug.Log($" Added BoxData: {box.boxType} ({box.length} x {box.width} x {box.height})");
        }

        Debug.Log(" Collected data from " + boxPanelConfigs.Count + " panels");

        if (payload.BOX_TYPES.Count > 0)
    {
        string json = JsonUtility.ToJson(payload, true);
        Debug.Log(" Final Payload:\n" + json);

        string filePath = Path.Combine(Application.persistentDataPath, "cargo_payload.json");
        File.WriteAllText(filePath, json);
        Debug.Log(" Saved JSON to: " + filePath);
        
        // Call the API sender
        FindObjectOfType<JsonApiSender>()?.SendJsonToApi();
    }
    else
    {
        Debug.LogError(" No valid box data found to save!");
    }
}    private BoxFormInput ExtractFormInput(BoxPanelConfiguration config)
    {
        return new BoxFormInput
        {
            boxType = config.typeText?.text.Trim() ?? "",
            length = config.lengthText?.text.Trim() ?? "",
            width = config.widthText?.text.Trim() ?? "",
            height = config.heightText?.text.Trim() ?? "",
            weight = config.weightText?.text.Trim() ?? "",
            volume = config.volumeText?.text.Trim() ?? "",
            quantity = config.quantityText?.text.Trim() ?? ""
        };
    }

   

    public void LoadFromJSON()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "cargo_payload.json");
        Debug.Log("JSON file path: " + filePath); 
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            BoxPayload payload = JsonUtility.FromJson<BoxPayload>(json);

            for (int i = 0; i < payload.BOX_TYPES.Count && i < boxPanelConfigs.Count; i++)
            {
                BoxData box = payload.BOX_TYPES[i];
                BoxPanelConfiguration config = boxPanelConfigs[i];

                config.lengthText.text = box.length;
                config.widthText.text = box.width;
                config.heightText.text = box.height;
                config.weightText.text = box.weight;
                config.volumeText.text = box.volume;
                config.typeText.text = box.boxType;
                config.quantityText.text = box.quantity;
                
            }

            Debug.Log(" Loaded and populated box data from JSON");
        }
        else
        {
            Debug.LogError(" No JSON file found to load");
        }
    }

    public void OnPreviewBoxData()
    {
        if (boxPanelConfigs.Count > 0)
        {
            PrintCurrentBoxData(boxPanelConfigs[0]);
        }
        else
        {
            Debug.LogWarning(" No box panels configured!");
        }
    }

    public void PrintCurrentBoxData(BoxPanelConfiguration config)
    {
        BoxFormInput formInput = ExtractFormInput(config);
        
        BoxData box = new BoxData
        {
            boxType = string.IsNullOrEmpty(formInput.boxType) ? "Box" : formInput.boxType,
            length = formInput.length,
            width = formInput.width,
            height = formInput.height,
            weight = formInput.weight,
            volume = formInput.volume,
            quantity = formInput.quantity
        };
        
        Debug.Log(" Preview Box Data:");
        Debug.Log("Type: " + box.boxType);
        Debug.Log("Length: " + box.length);
        Debug.Log("Width: " + box.width);
        Debug.Log("Height: " + box.height);
        Debug.Log("Weight: " + box.weight);
        Debug.Log("Volume: " + box.volume);
        Debug.Log("Quantity: " + box.quantity);
    }
}