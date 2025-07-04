using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CargoManager : MonoBehaviour
{
    [Header("UI Input Fields")]
    [SerializeField] private TMP_InputField itemNoField;
    [SerializeField] private TMP_InputField lengthField;
    [SerializeField] private TMP_InputField widthField;
    [SerializeField] private TMP_InputField heightField;
    [SerializeField] private TMP_InputField weightField;
    [SerializeField] private TMP_InputField volumeField;
    [SerializeField] private TMP_InputField totalboxField;

    [Header("UI Elements")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Transform summaryContainer;
    [SerializeField] private GameObject summaryItemPrefab;

   [SerializeField]  private List<CargoDetails> cargoList = new List<CargoDetails>(); // Stores cargo data

    

    public void SaveCargoDetails()
    {
        // Store input data into a structured CargoDetails object
        CargoDetails newCargo = new CargoDetails(
            itemNoField.text,
            lengthField.text,
            widthField.text,
            heightField.text,
            weightField.text,
            volumeField.text,
            totalboxField.text
        );

        cargoList.Add(newCargo); // Save data to the list
        UpdateSummaryPanel(); // Refresh summary UI

        // Clear input fields for next entry
        itemNoField.text = "";
        lengthField.text = "";
        widthField.text = "";
        heightField.text = "";
        weightField.text = "";
        volumeField.text = "";
        totalboxField.text = "";
    }

    private void UpdateSummaryPanel()
    {
       
        // Display updated cargo list
        foreach (CargoDetails cargo in cargoList)
        {
            GameObject summaryItem = Instantiate(summaryItemPrefab, summaryContainer);
            TMP_Text summaryText = summaryItem.GetComponentInChildren<TMP_Text>();
            summaryText.text = $"Item No: {cargo.itemNo} | Size: {cargo.length}x{cargo.width}x{cargo.height} | Weight: {cargo.weight} | Volume: {cargo.volume} | Total Boxes: {cargo.totalBox}";
        }
    }
}
