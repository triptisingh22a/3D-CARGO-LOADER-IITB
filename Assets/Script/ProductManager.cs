using UnityEngine;
using TMPro;
using System.IO;

public class ProductManager : MonoBehaviour
{
    public TMP_InputField nameInput;
    public TMP_InputField lengthInput;
    public TMP_InputField breadthInput;
    public TMP_InputField heightInput;
    public TMP_InputField weightInput;
    public TMP_InputField volumeInput;
    public TextMeshProUGUI quantityText;
    public TMP_InputField VnameInput;
    public TMP_InputField VlengthInput2;
    public TMP_InputField VbreadthInput;
    public TMP_InputField VheightInput;
    public TMP_InputField VweightInput;
    public TMP_InputField VvolumeInput;
    public TextMeshProUGUI VquantityText;
    


    private string filePath;

    private void Start()
    {
        string dataFolder = Path.Combine(Application.dataPath, "Data");

        if (!Directory.Exists(dataFolder))
        {
            Directory.CreateDirectory(dataFolder);
            Debug.Log("Data folder created.");
        }
        else
        {
            Debug.Log("Data folder already exists.");
        }

        filePath = Path.Combine(dataFolder, "ProductData.csv");

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "Product Name,Length,Breadth,Height,Weight,Volume,Quantity\n");
            Debug.Log("CSV file created.");
        }
        else
        {
            Debug.Log("CSV file already exists.");
        }
    }

    public void AddProductToList()
    {
        if (AreAllFieldsFilled())
        {
            SaveProductToCSV();
            ClearInputs();
        }
    }

    private bool AreAllFieldsFilled()
    {
        return !string.IsNullOrEmpty(nameInput.text) &&
               !string.IsNullOrEmpty(lengthInput.text) &&
               !string.IsNullOrEmpty(breadthInput.text) &&
               !string.IsNullOrEmpty(heightInput.text) &&
               !string.IsNullOrEmpty(weightInput.text) &&
               !string.IsNullOrEmpty(volumeInput.text) &&
               !string.IsNullOrEmpty(quantityText.text);
    }

    private void SaveProductToCSV()
    {
        string data = $"{nameInput.text},{lengthInput.text},{breadthInput.text},{heightInput.text}," +
                      $"{weightInput.text},{volumeInput.text},{quantityText.text}\n";

        File.AppendAllText(filePath, data);
        Debug.Log("Data saved to CSV.");
    }

    public void ClearInputs()
    {
        nameInput.text = "";
        lengthInput.text = "";
        breadthInput.text = "";
        heightInput.text = "";
        weightInput.text = "";
        volumeInput.text = "";
        quantityText.text = "0";
    }
}



